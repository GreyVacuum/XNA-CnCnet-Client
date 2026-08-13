#nullable enable
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Xna.Framework.Graphics;

namespace ClientGUI
{
    /// <summary>
    /// WPF MediaPlayer-based video background for UI panels.
    /// Uses Windows built-in Media Foundation for lightweight video decoding
    /// without requiring external dependencies like VLC.
    /// Frame capture is driven by the game's Update() loop for optimal performance.
    /// </summary>
    public class VideoBackground : IDisposable
    {
        private const int FrameIntervalMs = 66; // ~15fps - balanced for performance

        private MediaPlayer? _player;
        private Texture2D? _texture;
        private RenderTargetBitmap? _renderTarget;
        private readonly DrawingVisual _visual = new();
        private readonly VideoDrawing _videoDrawing;
        private readonly Rect _videoRect;
        private bool _disposed;
        private bool _isPlaying;
        private readonly Stopwatch _frameStopwatch = new();
        private readonly byte[] _pixelBuffer;
        private readonly int _width;
        private readonly int _height;

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
        public VideoBackground(
            GraphicsDevice graphicsDevice,
            string videoPath,
            int width,
            int height,
            bool looping = true,
            bool muted = false,
            float volume = 1.0f,
            float scale = 0.5f)
        {
            _width = width;
            _height = height;

            _texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
            _renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            _pixelBuffer = new byte[width * height * 4];

            _videoRect = new Rect(0, 0, width, height);
            _videoDrawing = new VideoDrawing
            {
                Player = null!,
                Rect = _videoRect
            };

            _player = new MediaPlayer();
            _player.Open(new Uri(videoPath, UriKind.Absolute));
            _player.IsMuted = muted;
            _player.Volume = Math.Clamp(volume, 0f, 1f);
            _player.MediaOpened += (s, e) =>
            {
                _isPlaying = true;
                _player.Play();
            };
            _player.MediaEnded += (s, e) =>
            {
                if (looping && _player != null)
                {
                    _player.Position = TimeSpan.Zero;
                    _player.Play();
                }
            };

            _frameStopwatch.Start();
        }

        /// <summary>
        /// Updates the video frame capture. Call this once per game Update() cycle.
        /// Frame capture is throttled internally for performance.
        /// </summary>
        public void Update()
        {
            if (_disposed || !_isPlaying || _player == null || _renderTarget == null)
                return;

            if (_frameStopwatch.ElapsedMilliseconds < FrameIntervalMs)
                return;

            _frameStopwatch.Restart();

            try
            {
                // Update the video drawing with current player
                _videoDrawing.Player = _player;

                // Render video into visual
                using (var ctx = _visual.RenderOpen())
                {
                    ctx.DrawDrawing(_videoDrawing);
                }

                // Capture the visual into render target
                _renderTarget.Render(_visual);

                // Extract BGRA pixel data
                int stride = _width * 4;
                _renderTarget.CopyPixels(_pixelBuffer, stride, 0);

                // Convert BGRA to RGBA using Span for better performance
                ConvertBgraToRgba(_pixelBuffer);

                // Update XNA texture
                _texture?.SetData(_pixelBuffer);
            }
            catch
            {
                // Silently ignore frame capture failures
            }
        }

        /// <summary>
        /// Converts BGRA pixel buffer to RGBA by swapping red and blue channels.
        /// </summary>
        private static void ConvertBgraToRgba(byte[] pixels)
        {
            int length = pixels.Length;

            for (int i = 0; i < length; i += 4)
            {
                byte temp = pixels[i];
                pixels[i] = pixels[i + 2];
                pixels[i + 2] = temp;
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
                if (_player != null)
                {
                    _player.Close();
                    _player = null;
                }

                if (_texture != null)
                {
                    _texture.Dispose();
                    _texture = null;
                }

                _renderTarget = null;
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

        /// <summary>
        /// Gets whether the video is currently playing audio.
        /// </summary>
        public bool IsMuted => _player?.IsMuted ?? true;

        /// <summary>
        /// Mutes or unmutes the video audio.
        /// </summary>
        public void SetMuted(bool muted)
        {
            if (_player != null)
                _player.IsMuted = muted;
        }

        /// <summary>
        /// Sets the video audio volume (0.0-1.0).
        /// </summary>
        public void SetVolume(float volume)
        {
            if (_player != null)
                _player.Volume = Math.Clamp(volume, 0f, 1f);
        }
    }
}
