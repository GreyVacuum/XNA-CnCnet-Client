using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using ClientCore;

namespace ClientGUI
{
    /// <summary>
    /// XNAInteractionButton - 用于以 INI 配置指定的打开/关闭/可用性检查的按钮。
    /// 解析的 INI 字段：
    ///   OpenFiles
    ///   ExitProcess
    ///   TargetSuffixs
    ///   OpenDisableButtonTime
    ///   ExitDisableButtonTime
    ///   ProcessExists.AllowedButtonChecks
    ///   ProcessExists.DisableButtonChecks
    ///   TextColorDisabled
    /// </summary>
    public class XNAInteractionButton : XNAButton
    {
        private string[] _openFiles = Array.Empty<string>();
        private string[] _exitProcesses = Array.Empty<string>();
        private HashSet<string> _targetSuffixes = new(StringComparer.OrdinalIgnoreCase) { ".exe", ".bat", ".txt", ".ini", ".log" };
        private int _openDisableSeconds = 0;
        private int _exitDisableSeconds = 0;
        private string[] _processExistsAllowed = Array.Empty<string>();
        private string[] _processExistsDisable = Array.Empty<string>();
        private Color? _textColorDisabledOverride;

        private DateTime? _clickDisabledUntil;

        public XNAInteractionButton(WindowManager windowManager) : base(windowManager)
        {
        }

        protected override void ParseControlINIAttribute(Rampastring.Tools.IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "OpenFiles":
                    _openFiles = SplitAndTrim(value);
                    return;
                case "ExitProcess":
                    _exitProcesses = SplitAndTrim(value);
                    return;
                case "TargetSuffixs":
                case "TargetSuffixes":
                    {
                        var arr = SplitAndTrim(value);
                        if (arr.Length > 0)
                        {
                            _targetSuffixes = new HashSet<string>(arr.Select(s => s.StartsWith(".") ? s : "." + s),
                                StringComparer.OrdinalIgnoreCase);
                        }
                        return;
                    }
                case "OpenDisableButtonTime":
                    _openDisableSeconds = Rampastring.Tools.Conversions.IntFromString(value, 0);
                    return;
                case "ExitDisableButtonTime":
                    _exitDisableSeconds = Rampastring.Tools.Conversions.IntFromString(value, 0);
                    return;
                case "ProcessExists.AllowedButtonChecks":
                    _processExistsAllowed = SplitAndTrim(value);
                    return;
                case "ProcessExists.DisableButtonChecks":
                    _processExistsDisable = SplitAndTrim(value);
                    return;
                case "TextColorDisabled":
                    _textColorDisabledOverride = AssetLoader.GetColorFromString(value);
                    TextColorDisabled = _textColorDisabledOverride.Value;
                    return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void Initialize()
        {
            // Ensure disabled text color from override is applied if set
            if (_textColorDisabledOverride.HasValue)
                TextColorDisabled = _textColorDisabledOverride.Value;

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Re-enable after Open/Exit timed disable
            if (_clickDisabledUntil.HasValue && DateTime.UtcNow >= _clickDisabledUntil.Value)
            {
                AllowClick = true;
                _clickDisabledUntil = null;
            }

            // Evaluate process-existence rules to set Enabled/AllowClick
            bool allowedByAllowedChecks = true;
            if (_processExistsAllowed.Length > 0)
            {
                // If any of the listed process names exists -> allowed, else not allowed
                allowedByAllowedChecks = _processExistsAllowed.Any(pn => ProcessExistsByName(pn));
            }

            bool disabledByDisableChecks = false;
            if (_processExistsDisable.Length > 0)
            {
                // If any of the listed process names exists -> disable the button
                disabledByDisableChecks = _processExistsDisable.Any(pn => ProcessExistsByName(pn));
            }

            // Compute final enabled state (do not override general Enabled property if user code set it directly).
            // We only modify AllowClick which governs interactivity for XNAButton.
            if (!allowedByAllowedChecks || disabledByDisableChecks)
            {
                AllowClick = false;
            }
            else
            {
                if (!_clickDisabledUntil.HasValue) // don't enable while timed disable active
                    AllowClick = true;
            }
        }

        public override void OnLeftClick(Rampastring.XNAUI.InputEventArgs inputEventArgs)
        {
            if (!AllowClick)
                return;

            // If ExitProcess configured, attempt to close/kill listed processes
            if (_exitProcesses.Length > 0)
            {
                TryExitProcesses(_exitProcesses);

                if (_exitDisableSeconds > 0)
                    TemporarilyDisableButton(_exitDisableSeconds);
            }

            // If OpenFiles configured, attempt to start them
            if (_openFiles.Length > 0)
            {
                TryOpenFiles(_openFiles);

                if (_openDisableSeconds > 0)
                    TemporarilyDisableButton(_openDisableSeconds);
            }

            base.OnLeftClick(inputEventArgs);
            inputEventArgs.Handled = true;
        }

        private void TemporarilyDisableButton(int seconds)
        {
            if (seconds <= 0)
                return;

            AllowClick = false;
            _clickDisabledUntil = DateTime.UtcNow.AddSeconds(seconds);
        }

        private void TryOpenFiles(string[] files)
        {
            foreach (var f in files)
            {
                if (string.IsNullOrWhiteSpace(f))
                    continue;

                try
                {
                    string path = f.Trim('"').Trim();

                    // If path has no extension or extension not allowed, skip
                    string ext = Path.GetExtension(path);
                    if (string.IsNullOrEmpty(ext) || !_targetSuffixes.Contains(ext))
                    {
                        // attempt to treat values without extension as commands; still try to start
                        if (!string.IsNullOrEmpty(ext))
                            continue;
                    }

                    // If absolute or relative file exists, start it. Otherwise attempt resource/config search places.
                    if (!File.Exists(path))
                    {
                        var candidates = new[]
                        {
                            path,
                            Path.Combine(ProgramConstants.GetResourcePath(), path),
                            Path.Combine(ProgramConstants.GetBaseResourcePath(), path),
                            Path.Combine(ProgramConstants.GamePath, path),
                            Path.GetFileName(path)
                        };

                        string found = candidates.FirstOrDefault(File.Exists);
                        if (found != null)
                            path = found;
                    }

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    };

                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    // Log and continue with next
                    Rampastring.Tools.Logger.Log($"XNAInteractionButton: failed to open '{f}': {ex.Message}");
                }
            }
        }

        private void TryExitProcesses(string[] processNames)
        {
            foreach (var pn in processNames)
            {
                if (string.IsNullOrWhiteSpace(pn))
                    continue;

                string procName = Path.GetFileNameWithoutExtension(pn).Trim();

                try
                {
                    var procs = Process.GetProcessesByName(procName);
                    foreach (var p in procs)
                    {
                        try
                        {
                            // Try graceful close
                            if (!p.HasExited)
                            {
                                try
                                {
                                    p.CloseMainWindow();
                                }
                                catch { /* ignore */ }

                                if (!p.WaitForExit(2000))
                                {
                                    try
                                    {
                                        // Attempt to call Kill(bool) when available (kills process tree).
                                        var killWithTree = typeof(Process).GetMethod("Kill", new[] { typeof(bool) });
                                        if (killWithTree != null)
                                        {
                                            killWithTree.Invoke(p, new object[] { true });
                                        }
                                        else
                                        {
                                            // Fallback for runtimes that don't support Kill(bool).
                                            // On Windows use taskkill to ensure the process tree is terminated.
                                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                            {
                                                try
                                                {
                                                    var tpsi = new ProcessStartInfo
                                                    {
                                                        FileName = "taskkill",
                                                        Arguments = $"/PID {p.Id} /T /F",
                                                        CreateNoWindow = true,
                                                        UseShellExecute = false,
                                                        RedirectStandardOutput = true,
                                                        RedirectStandardError = true
                                                    };

                                                    using (var tp = Process.Start(tpsi))
                                                    {
                                                        tp?.WaitForExit(5000);
                                                    }
                                                }
                                                catch { /* ignore */ }
                                            }
                                            else
                                            {
                                                // Non-windows fallback
                                                p.Kill();
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        try { p.Kill(); } catch { }
                                    }
                                }
                            }
                        }
                        catch (Exception exInner)
                        {
                            Rampastring.Tools.Logger.Log($"XNAInteractionButton: failed to exit process '{p.ProcessName}': {exInner.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Rampastring.Tools.Logger.Log($"XNAInteractionButton: error while enumerating '{pn}': {ex.Message}");
                }
            }
        }

        private static string[] SplitAndTrim(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            var results = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (c == '"')
                {
                    // toggle quote state, do not include quote char in output
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && (c == ',' || c == ';'))
                {
                    var token = sb.ToString().Trim();
                    if (token.Length > 0)
                        results.Add(token);
                    sb.Clear();
                    continue;
                }

                sb.Append(c);
            }

            var last = sb.ToString().Trim();
            if (last.Length > 0)
                results.Add(last);

            return results.ToArray();
        }

        private static bool ProcessExistsByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            string pn = Path.GetFileNameWithoutExtension(name).Trim();
            try
            {
                return Process.GetProcessesByName(pn).Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}