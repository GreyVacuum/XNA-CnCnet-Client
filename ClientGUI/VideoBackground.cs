#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Xna.Framework.Graphics;

namespace ClientGUI
{
    /// <summary>
    /// WPF MediaPlayer-based video background for UI panels.
    /// Uses Windows built-in Media Foundation for lightweight video decoding
    /// without requiring external dependencies like VLC.
    ///
    /// <para>Threading model — multi-threaded pipeline:</para>
    /// <list type="bullet">
    /// <item>A dedicated background STA thread with its own <see cref="Dispatcher"/>
    /// (the C# counterpart of a Web Worker) owns the <see cref="MediaPlayer"/>,
    /// <see cref="VideoDrawing"/>, <see cref="DrawingVisual"/> and
    /// <see cref="RenderTargetBitmap"/>. All expensive work — media decoding,
    /// visual rendering, pixel extraction and the BGRA→RGBA conversion — happens
    /// there, off the client's main thread. The conversion is additionally chunked
    /// across CPU cores so a single frame never monopolizes one core for long.</item>
    /// <item>The game's main thread performs only lightweight render scheduling:
    /// when a freshly captured frame is announced (a frame-ready flag plus a
    /// triple-buffered pixel pool), it uploads the pixels into the XNA texture via
    /// <c>SetData</c>. The worker is never waited on from the main thread.</item>
    /// <item>Frame capture is throttled by a configurable frame interval (default
    /// 33ms ≈ 30fps, matching the INI key <c>BackgroundVideoFrameInterval</c>), and
    /// gated on <see cref="MediaPlayer.State"/> being <c>Playing</c> — while paused,
    /// stopped or ended the scheduler stays idle instead of re-capturing identical
    /// frames. Gating on the player state (rather than comparing
    /// <see cref="MediaPlayer.Position"/>) keeps the effective frame rate equal to the
    /// configured interval: WPF's Position can lag the render cadence and skipping
    /// renders on equal positions previously caused visible stutter.</item>
    /// <item>Graceful degradation: if the background worker cannot be established,
    /// dies, or requests fallback before producing its first frame, the component
    /// transparently falls back to the original synchronous capture path on the
    /// calling thread. After the first frame, a worker failure freezes the last good
    /// frame instead of disrupting the UI with a mid-game reconstruction.</item>
    /// </list>
    /// </summary>
    public class VideoBackground : IDisposable
    {
        private const int FrameIntervalMs = 33; // ~30fps default - smoother than the old 66ms/15fps
        private const int SchedulerTickMs = 8; // worker scheduler granularity (enough for 60fps configs)
        private const int WorkerBootTimeoutMs = 3000;
        private const int WorkerShutdownTimeoutMs = 2000;

        // ---- Main-thread owned resources ----
        private Texture2D? _texture;
        private readonly int _width;
        private readonly int _height;
        private readonly int _stride;
        private readonly string _videoPath;
        private readonly bool _looping;
        private readonly int _frameIntervalMs;

        // ---- Triple-buffered pixel pool (never allocated per frame) ----
        // The worker writes into _workerWriteBuffer, publishes the finished buffer via
        // _readyIndex/_frameReady, and the main thread uploads it with SetData and only
        // then releases it via _releasedIndex. A buffer is therefore never written by
        // the worker while the main thread is uploading it.
        private readonly byte[] _pixelBuffer0;
        private readonly byte[] _pixelBuffer1;
        private readonly byte[] _pixelBuffer2;
        private byte[] _workerWriteBuffer;
        private int _readyIndex = -1;
        private int _frameReady; // 1 = a frame awaits upload; only accessed via Volatile.Read/Write
        private int _releasedIndex = -1;

        // ---- Background worker ("Web Worker" equivalent) ----
        private Thread? _workerThread;
        private volatile Dispatcher? _workerDispatcher;
        private MediaPlayer? _player;                      // worker-thread owned
        private DrawingVisual? _workerVisual;              // worker-thread owned
        private VideoDrawing? _workerVideoDrawing;         // worker-thread owned
        private RenderTargetBitmap? _workerRenderTarget;   // worker-thread owned
        private readonly Stopwatch _workerFrameStopwatch = new();
        private readonly Stopwatch _workerBootStopwatch = new();
        private volatile bool _workerReady;
        private volatile bool _mediaOpened;
        private volatile bool _hasProducedFrame;
        private volatile bool _fallbackRequested;
        private bool _fallbackInProgress;

        // ---- Synchronous fallback pipeline (main-thread owned) ----
        private MediaPlayer? _syncPlayer;
        private RenderTargetBitmap? _syncRenderTarget;
        private readonly DrawingVisual _syncVisual = new();
        private VideoDrawing? _syncVideoDrawing;
        private readonly Stopwatch _syncStopwatch = new();
        private bool _syncMode;

        // ---- Cross-thread cached control state ----
        // WPF objects are thread-affine, so control values are cached on the main
        // thread and marshaled to the worker asynchronously. The caches also survive
        // a sync fallback (they are re-applied on MediaOpened).
        private volatile bool _isMuted;
        private float _volume;
        private volatile bool _isPaused;

        private volatile bool _disposed;

        /// <summary>
        /// Gets the current video frame as an XNA Texture2D.
        /// </summary>
        public Texture2D Texture => _texture ?? throw new ObjectDisposedException(nameof(VideoBackground));

        /// <summary>
        /// Creates a new WPF video background.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device used to create the output texture.</param>
        /// <param name="videoPath">Full path to the video file.</param>
        /// <param name="width">Desired video width in pixels.</param>
        /// <param name="height">Desired video height in pixels.</param>
        /// <param name="looping">Whether the video should loop (default: true).</param>
        /// <param name="muted">Whether the video should be muted (default: false).</param>
        /// <param name="volume">Video volume 0.0-1.0 (default: 1.0).</param>
        /// <param name="scale">Video scaling factor (unused, kept for API compatibility).</param>
        /// <param name="frameIntervalMs">Minimum interval between captured frames in milliseconds
        /// (default: 33 = ~30fps). Lower values look smoother but cost more CPU on the worker;
        /// e.g. 16 for ~60fps, 66 for ~15fps.</param>
        public VideoBackground(
            GraphicsDevice graphicsDevice,
            string videoPath,
            int width,
            int height,
            bool looping = true,
            bool muted = false,
            float volume = 1.0f,
            float scale = 0.5f,
            int frameIntervalMs = FrameIntervalMs)
        {
            _width = width;
            _height = height;
            _stride = width * 4;
            _videoPath = videoPath;
            _looping = looping;
            _frameIntervalMs = Math.Max(1, frameIntervalMs);
            _isMuted = muted;
            _volume = Math.Clamp(volume, 0f, 1f);

            _texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
            _pixelBuffer0 = new byte[width * height * 4];
            _pixelBuffer1 = new byte[width * height * 4];
            _pixelBuffer2 = new byte[width * height * 4];
            _workerWriteBuffer = _pixelBuffer0;

            // Preferred path: offload all video work to the background worker.
            if (!StartWorkerThread())
            {
                // Degraded path: the environment cannot host the background pipeline;
                // fall back to the original synchronous capture on this (main) thread.
                try
                {
                    SetupSyncPipeline();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("VideoBackground: sync pipeline setup failed: " + ex);
                    _syncMode = true;
                }
            }
        }

        // =====================================================================
        // Background worker ("Web Worker" equivalent)
        // =====================================================================

        private bool StartWorkerThread()
        {
            try
            {
                _workerThread = new Thread(WorkerMain)
                {
                    IsBackground = true,
                    Name = "VideoBackground.Worker"
                };
                _workerThread.SetApartmentState(ApartmentState.STA); // WPF requires an STA thread
                _workerThread.Start();
                _workerBootStopwatch.Start();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("VideoBackground: unable to start worker thread: " + ex);
                _fallbackRequested = true;
                return false;
            }
        }

        private void WorkerMain()
        {
            try
            {
                _workerDispatcher = Dispatcher.CurrentDispatcher;

                // All WPF objects are created on this thread so they are owned by it.
                _player = new MediaPlayer();
                _workerVisual = new DrawingVisual();
                _workerVideoDrawing = new VideoDrawing
                {
                    Player = _player,
                    Rect = new Rect(0, 0, _width, _height)
                };
                _workerRenderTarget = new RenderTargetBitmap(_width, _height, 96, 96, PixelFormats.Pbgra32);

                _player.IsMuted = _isMuted;
                _player.Volume = _volume;
                _player.MediaOpened += WorkerMediaOpened;
                _player.MediaEnded += WorkerMediaEnded;
                _player.MediaFailed += WorkerMediaFailed;
                _player.Open(new Uri(_videoPath, UriKind.Absolute));

                _workerFrameStopwatch.Start();

                // Frame scheduler: ticks at fine granularity; the actual capture is
                // throttled and frame-ready driven inside the tick handler.
                var scheduler = new DispatcherTimer(DispatcherPriority.Background)
                {
                    Interval = TimeSpan.FromMilliseconds(SchedulerTickMs)
                };
                scheduler.Tick += WorkerSchedulerTick;
                scheduler.Start();

                _workerReady = true;
                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("VideoBackground: worker failed: " + ex);
                _fallbackRequested = true;
            }
            finally
            {
                _workerReady = false;
            }
        }

        private void WorkerMediaOpened(object? sender, EventArgs e)
        {
            if (_player == null)
                return;

            // Re-apply the latest control state: SetMuted/SetVolume may have run
            // before the media finished opening.
            _player.IsMuted = _isMuted;
            _player.Volume = _volume;

            _mediaOpened = true;
            if (!_isPaused)
                _player.Play();
        }

        private void WorkerMediaEnded(object? sender, EventArgs e)
        {
            if (_looping && !_isPaused && _player != null)
            {
                _player.Position = TimeSpan.Zero;
                _player.Play();
            }
        }

        private void WorkerMediaFailed(object? sender, ExceptionEventArgs e)
        {
            Debug.WriteLine("VideoBackground: media failed: " + e.ErrorException);
            if (!_hasProducedFrame)
                _fallbackRequested = true;
            // After the first frame the last good frame is kept (graceful freeze).
        }

        private void WorkerSchedulerTick(object? sender, EventArgs e)
        {
            if (_disposed || !_mediaOpened || _player == null ||
                _workerRenderTarget == null || _workerVisual == null || _workerVideoDrawing == null)
                return;

            // Explicit pause gate: while paused the media clock is not Active (the
            // gate below would already skip rendering), but keeping the intent clear
            // guarantees no frames are captured during a pause.
            if (_isPaused)
                return;

            // Only capture while the media clock is actively running (playing). This
            // gates on the clock state instead of comparing MediaPlayer.Position: WPF's
            // Position can lag the render cadence, and skipping renders on equal
            // positions made the effective frame rate drop below the configured interval
            // (visible stutter). If the clock is not yet available, capture is allowed so
            // the first frames are never withheld.
            var clock = _player.Clock;
            if (clock != null && clock.CurrentState != ClockState.Active)
                return;

            if (_workerFrameStopwatch.ElapsedMilliseconds < _frameIntervalMs)
                return;

            _workerFrameStopwatch.Restart();

            try
            {
                // Update the video drawing with the current player
                _workerVideoDrawing.Player = _player;

                // Render video into visual
                using (var ctx = _workerVisual.RenderOpen())
                {
                    ctx.DrawDrawing(_workerVideoDrawing);
                }

                // Capture the visual into render target
                _workerRenderTarget.Render(_workerVisual);

                // Extract BGRA pixel data
                byte[] target = _workerWriteBuffer;
                _workerRenderTarget.CopyPixels(target, _stride, 0);

                // Convert BGRA to RGBA, chunked across CPU cores
                ConvertBgraToRgbaParallel(target, _height, _stride);

                // Hand the finished frame to the main thread
                PublishFrame(target);
                _hasProducedFrame = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("VideoBackground: frame capture failed: " + ex);
                if (!_hasProducedFrame)
                    _fallbackRequested = true;
                // Transient failures after the first frame just skip the frame.
            }
        }

        private void PublishFrame(byte[] buffer)
        {
            int index = ReferenceEquals(buffer, _pixelBuffer0) ? 0
                : ReferenceEquals(buffer, _pixelBuffer1) ? 1 : 2;

            // Announce the finished frame for the main thread.
            Volatile.Write(ref _readyIndex, index);
            Volatile.Write(ref _frameReady, 1);

            // Pick the next write buffer: the one the main thread released most recently.
            // A released buffer is guaranteed free because the main thread only releases
            // a buffer after its SetData upload has completed. If the last-released buffer
            // is the one just published (not yet re-released), use the next one instead —
            // only the just-published buffer can be about to be uploaded, so this can
            // never race with the main-thread upload.
            int next = Volatile.Read(ref _releasedIndex);
            if (next < 0 || next == index)
                next = (index + 1) % 3;
            _workerWriteBuffer = next == 0 ? _pixelBuffer0 : next == 1 ? _pixelBuffer1 : _pixelBuffer2;
        }

        // =====================================================================
        // Main-thread side: lightweight render scheduling only
        // =====================================================================

        /// <summary>
        /// Updates the video frame capture. Call this once per game Update() cycle.
        /// In worker mode this is lightweight: it only uploads a freshly captured
        /// frame into the texture. All decoding/rendering happens on the background worker.
        /// </summary>
        public void Update()
        {
            if (_disposed)
                return;

            if (_syncMode)
            {
                SyncCapture();
                return;
            }

            // Boot/runtime watchdog. If the worker never became ready, died, or
            // requested degradation before producing any frame, fall back to the
            // synchronous pipeline. After the first frame, failures freeze the last
            // good frame (graceful) instead of reconstructing the pipeline mid-game.
            if (!_workerReady || (_fallbackRequested && !_hasProducedFrame))
            {
                if (!_fallbackInProgress && !_hasProducedFrame)
                {
                    bool workerAlive = _workerThread != null && _workerThread.IsAlive;
                    bool bootTimedOut = _workerBootStopwatch.ElapsedMilliseconds > WorkerBootTimeoutMs;
                    if (_fallbackRequested || !workerAlive || bootTimedOut)
                    {
                        _fallbackInProgress = true;
                        try { PerformSyncFallback(); }
                        finally { _fallbackInProgress = false; }
                    }
                }
                return;
            }

            // The only heavy main-thread operation: upload the latest ready frame.
            if (Volatile.Read(ref _frameReady) == 1)
                UploadReadyFrame();
        }

        private void UploadReadyFrame()
        {
            // Clear the ready flag first so a frame published while we are uploading
            // is picked up on the next Update instead of being lost.
            Volatile.Write(ref _frameReady, 0);
            int index = Volatile.Read(ref _readyIndex);
            byte[] buffer = index == 0 ? _pixelBuffer0 : index == 1 ? _pixelBuffer1 : _pixelBuffer2;

            try
            {
                _texture?.SetData(buffer);
            }
            catch
            {
                // Silently ignore upload failures (e.g. device lost during shutdown)
            }
            finally
            {
                // Release only after the upload completed; the worker may reuse it now.
                Volatile.Write(ref _releasedIndex, index);
            }
        }

        // =====================================================================
        // Graceful degradation: synchronous capture on the calling thread
        // =====================================================================

        private void PerformSyncFallback()
        {
            Debug.WriteLine("VideoBackground: degrading to the synchronous capture pipeline.");
            ShutdownWorker();
            try
            {
                SetupSyncPipeline();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("VideoBackground: sync fallback failed: " + ex);
                _syncMode = true; // enter degraded state; SyncCapture safely no-ops
            }
        }

        private void ShutdownWorker()
        {
            Dispatcher? dispatcher = _workerDispatcher;
            if (dispatcher != null && !dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished)
            {
                try
                {
                    // Close the player on the worker thread, then stop its dispatcher.
                    dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        try { _player?.Close(); }
                        catch { /* player may already be closed */ }
                        _player = null;
                        dispatcher.BeginInvokeShutdown(DispatcherPriority.Send);
                    }));
                }
                catch
                {
                    // Dispatcher is already shutting down; nothing to marshal.
                }
            }

            Thread? thread = _workerThread;
            if (thread != null)
            {
                if (!thread.Join(WorkerShutdownTimeoutMs))
                    Debug.WriteLine("VideoBackground: worker thread did not stop within the timeout; abandoning it.");
                _workerThread = null;
            }

            _workerDispatcher = null;
            _player = null;
            _workerVisual = null;
            _workerVideoDrawing = null;
            _workerRenderTarget = null;
        }

        private void SetupSyncPipeline()
        {
            _syncStopwatch.Reset();
            _syncVideoDrawing = new VideoDrawing
            {
                Player = null!,
                Rect = new Rect(0, 0, _width, _height)
            };
            _syncRenderTarget = new RenderTargetBitmap(_width, _height, 96, 96, PixelFormats.Pbgra32);

            _syncPlayer = new MediaPlayer();
            _syncPlayer.Open(new Uri(_videoPath, UriKind.Absolute));
            _syncPlayer.IsMuted = _isMuted;
            _syncPlayer.Volume = _volume;
            _syncPlayer.MediaOpened += (s, e) =>
            {
                _syncPlayer.IsMuted = _isMuted;
                _syncPlayer.Volume = _volume;
                _mediaOpened = true;
                if (!_isPaused)
                    _syncPlayer.Play();
            };
            _syncPlayer.MediaEnded += (s, e) =>
            {
                if (_looping && !_isPaused && _syncPlayer != null)
                {
                    _syncPlayer.Position = TimeSpan.Zero;
                    _syncPlayer.Play();
                }
            };

            _syncMode = true;
            _syncStopwatch.Start();
        }

        private void SyncCapture()
        {
            if (_syncPlayer == null || _syncRenderTarget == null || _syncVideoDrawing == null || !_mediaOpened)
                return;

            if (_syncStopwatch.ElapsedMilliseconds < _frameIntervalMs)
                return;

            _syncStopwatch.Restart();

            try
            {
                // Update the video drawing with current player
                _syncVideoDrawing.Player = _syncPlayer;

                // Render video into visual
                using (var ctx = _syncVisual.RenderOpen())
                {
                    ctx.DrawDrawing(_syncVideoDrawing);
                }

                // Capture the visual into render target
                _syncRenderTarget.Render(_syncVisual);

                // Extract BGRA pixel data
                _syncRenderTarget.CopyPixels(_workerWriteBuffer, _stride, 0);

                // Convert BGRA to RGBA (chunked, byte-identical to the sequential loop)
                ConvertBgraToRgbaParallel(_workerWriteBuffer, _height, _stride);

                // Update XNA texture
                _texture?.SetData(_workerWriteBuffer);
            }
            catch
            {
                // Silently ignore frame capture failures
            }
        }

        // =====================================================================
        // Pixel conversion
        // =====================================================================

        /// <summary>
        /// Converts BGRA pixel buffer to RGBA by swapping red and blue channels.
        /// Each row is an independent chunk processed in parallel across CPU cores,
        /// so a single frame never monopolizes one core for long. The result is
        /// byte-identical to the sequential conversion.
        /// </summary>
        private static void ConvertBgraToRgbaParallel(byte[] pixels, int height, int stride)
        {
            int degree = Math.Max(1, Environment.ProcessorCount);
            Parallel.For(0, height,
                new ParallelOptions { MaxDegreeOfParallelism = degree },
                row =>
                {
                    int rowStart = row * stride;
                    int rowEnd = rowStart + stride;
                    for (int i = rowStart; i < rowEnd; i += 4)
                    {
                        byte temp = pixels[i];
                        pixels[i] = pixels[i + 2];
                        pixels[i + 2] = temp;
                    }
                });
        }

        // =====================================================================
        // Control API (unchanged)
        // =====================================================================

        /// <summary>
        /// Gets whether the video is currently playing audio.
        /// </summary>
        public bool IsMuted => _isMuted;

        /// <summary>
        /// Gets the current video audio volume (0.0-1.0). Returns the cached value,
        /// which is the source of truth for both the worker and the sync pipeline.
        /// </summary>
        public float Volume => _volume;

        /// <summary>
        /// Mutes or unmutes the video audio. Thread-safe: the value is applied
        /// on the worker thread (or the sync player) asynchronously.
        /// </summary>
        public void SetMuted(bool muted)
        {
            _isMuted = muted;
            if (_syncMode)
            {
                if (_syncPlayer != null)
                    _syncPlayer.IsMuted = muted;
            }
            else
            {
                MarshalToWorker(player => player.IsMuted = muted);
            }
        }

        /// <summary>
        /// Sets the video audio volume (0.0-1.0). Thread-safe.
        /// </summary>
        public void SetVolume(float volume)
        {
            _volume = Math.Clamp(volume, 0f, 1f);
            if (_syncMode)
            {
                if (_syncPlayer != null)
                    _syncPlayer.Volume = _volume;
            }
            else
            {
                MarshalToWorker(player => player.Volume = _volume);
            }
        }

        /// <summary>
        /// Gets whether the video playback is currently paused.
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Pauses the video playback, which also stops audio output and media
        /// decoding so the video consumes no resources (e.g. while the game runs).
        /// The last rendered frame remains visible. Thread-safe; use
        /// <see cref="Resume"/> to continue from the paused position.
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
            if (_syncMode)
            {
                if (_syncPlayer != null)
                    _syncPlayer.Pause();
            }
            else
            {
                MarshalToWorker(player => player.Pause());
            }
        }

        /// <summary>
        /// Resumes the video playback from the paused position. Thread-safe.
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
            if (_syncMode)
            {
                if (_syncPlayer != null)
                    _syncPlayer.Play();
            }
            else
            {
                MarshalToWorker(player => player.Play());
            }
        }

        private void MarshalToWorker(Action<MediaPlayer> action)
        {
            Dispatcher? dispatcher = _workerDispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                return; // Worker not ready/stopped: the cached value is applied on MediaOpened

            try
            {
                dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                {
                    if (_player != null)
                        action(_player);
                }));
            }
            catch
            {
                // Dispatcher shutdown race: ignore; the cached value is applied on reopen.
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                ShutdownWorker();

                if (_syncPlayer != null)
                {
                    _syncPlayer.Close();
                    _syncPlayer = null;
                }

                _syncRenderTarget = null;
                _syncVideoDrawing = null;

                if (_texture != null)
                {
                    _texture.Dispose();
                    _texture = null;
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Shuts down shared resources. Kept for API compatibility.
        /// </summary>
        public static void ShutdownLibVLC()
        {
            // WPF MediaPlayer doesn't require global shutdown
        }
    }
}
