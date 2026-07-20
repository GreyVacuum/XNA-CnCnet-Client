using ClientCore;
using ClientCore.I18N;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClientGUI
{
    public class INItializableWindow : XNAPanel
    {
        public INItializableWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        protected CCIniFile ConfigIni { get; private set; }

        private bool hasCloseButton = false;
        private bool _initialized = false;

        /// <summary>
        /// If not null, the client will read an INI file with this name
        /// instead of the window's name.
        /// </summary>
        protected string IniNameOverride { get; set; }

        private static bool AnyChildMatches(IEnumerable<XNAControl> list, Func<XNAControl, bool> isTargetControl)
        {
            foreach (XNAControl child in list)
            {
                bool matched = isTargetControl(child);

                if (matched)
                    return true;

                matched = AnyChildMatches(child.Children, isTargetControl);

                if (matched)
                    return true;
            }

            return false;
        }

        public T FindChild<T>(string childName, bool optional = false) where T : XNAControl
        {
            XNAControl result = null;

            AnyChildMatches(new List<XNAControl>() { this }, control =>
            {
                if (control.Name != childName)
                    return false;

                result = control;
                return true;
            });

            if (result == null && !optional)
                throw new KeyNotFoundException("Could not find required child control: " + childName);

            return (T)result;
        }

        public List<T> FindChildrenStartWith<T>(string prefix) where T : XNAControl
        {
            List<T> result = new List<T>();

            AnyChildMatches(new List<XNAControl>() { this }, control =>
            {
                if (string.IsNullOrEmpty(prefix) ||
                    !string.IsNullOrEmpty(control.Name) && control.Name.StartsWith(prefix))
                    result.Add((T)control);

                return false;
            });

            return result;
        }

        /// <summary>
        /// Attempts to locate the ini config file for the current control.
        /// Only return a config path if it exists.
        /// </summary>
        /// <returns>The ini config file path</returns>
        protected string GetConfigPath()
        {
            string iniFileName = string.IsNullOrWhiteSpace(IniNameOverride) ? Name : IniNameOverride;

            // get theme specific path
            FileInfo configIniPath = SafePath.GetFile(ProgramConstants.GetResourcePath(), FormattableString.Invariant($"{iniFileName}.ini"));
            if (configIniPath.Exists)
                return configIniPath.FullName;

            // get base path
            configIniPath = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), FormattableString.Invariant($"{iniFileName}.ini"));
            if (configIniPath.Exists)
                return configIniPath.FullName;

            if (iniFileName == Name)
                return null; // IniNameOverride must be null, no need to continue

            iniFileName = Name;

            // get theme specific path
            configIniPath = SafePath.GetFile(ProgramConstants.GetResourcePath(), FormattableString.Invariant($"{iniFileName}.ini"));
            if (configIniPath.Exists)
                return configIniPath.FullName;

            // get base path
            configIniPath = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), FormattableString.Invariant($"{iniFileName}.ini"));
            return configIniPath.Exists ? configIniPath.FullName : null;
        }

        public override void Initialize()
        {
            if (_initialized)
                throw new InvalidOperationException("INItializableWindow cannot be initialized twice.");

            string configIniPath = GetConfigPath();

            if (string.IsNullOrEmpty(configIniPath))
            {
                base.Initialize();
                return;
            }

            ConfigIni = new CCIniFile(configIniPath);

            if (Parser.Instance == null)
                _ = new Parser(WindowManager); // Note: Parser.Instance will be set by calling new Parser()

            Parser.Instance.SetPrimaryControl(this);
            ReadINIForControl(this);
            ReadLateAttributesForControl(this);

            ParseExtraControls();

            base.Initialize();

            _initialized = true;
        }

        private void ParseExtraControls()
        {
            var section = ConfigIni.GetSection("$ExtraControls");

            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (!kvp.Key.StartsWith("$CC"))
                    continue;

                string[] parts = kvp.Value.Split(':');
                if (parts.Length != 2)
                    throw new ClientConfigurationException("Invalid $ExtraControl specified in " + Name + ": " + kvp.Value);

                if (!Children.Any(child => child.Name == parts[0]))
                {
                    var control = CreateChildControl(this, kvp.Value);
                    control.Name = parts[0];
                    control.DrawOrder = -Children.Count;
                    ReadINIForControl(control);
                }
            }
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "HasCloseButton")
                hasCloseButton = iniFile.GetBooleanValue(Name, key, hasCloseButton);

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        /// <summary>
        /// 处理控件段的 $Include 指令，合并子ini文件中对应段的内容。
        /// 格式: $Include任意字数串=子ini路径
        /// 例如: $Include00=SpawnGameOptions.ini
        /// </summary>
        private void ProcessControlInclude(XNAControl control, IniSection section)
        {
            // 找出所有 $Include 开头的key
            var includeKeys = section.Keys
                .Where(kvp => kvp.Key.StartsWith("$Include"))
                .ToList();

            foreach (var includeKvp in includeKeys)
            {
                string includePath = includeKvp.Value;
                if (string.IsNullOrWhiteSpace(includePath))
                    continue;

                // 解析路径（支持 $THEME_DIR$ 变量）
                string resolvedPath = ResolveControlIncludePath(includePath);
                FileInfo includeFile = SafePath.GetFile(resolvedPath);

                if (!includeFile.Exists)
                {
                    Logger.Log($"{Name}: $Include file not found for control {control.Name}: {includeFile.FullName}");
                    continue;
                }

                // 加载子ini文件
                CCIniFile includeIni = new CCIniFile(includeFile.FullName);

                // 查找子ini中与控件同名的段
                var includeSection = includeIni.GetSection(control.Name);
                if (includeSection == null)
                {
                    Logger.Log($"{Name}: $Include file {includePath} does not contain section [{control.Name}]");
                    continue;
                }

                // 合并子ini段内容到当前段（子ini的内容覆盖当前段）
                foreach (var kvp in includeSection.Keys)
                {
                    // 移除已存在的同名key，添加新的
                    int existingIndex = section.Keys.FindIndex(k => k.Key == kvp.Key);
                    if (existingIndex >= 0)
                        section.Keys[existingIndex] = kvp;
                    else
                        section.Keys.Add(kvp);
                }
            }

            // 移除已处理的 $Include key，避免后续解析时出错
            section.Keys.RemoveAll(kvp => kvp.Key.StartsWith("$Include"));
        }

        /// <summary>
        /// 解析控件 $Include 路径，支持 $THEME_DIR$ 变量和相对路径。
        /// </summary>
        private string ResolveControlIncludePath(string includePath)
        {
            if (includePath.Contains("$THEME_DIR$"))
                return SafePath.GetFile(includePath.Replace("$THEME_DIR$", ProgramConstants.GetResourcePath())).FullName;

            // 相对于当前ini文件所在目录
            string currentDir = SafePath.GetFileDirectoryName(ConfigIni.FileName);
            return SafePath.CombineFilePath(currentDir, includePath);
        }

        protected void ReadINIForControl(XNAControl control)
        {
            var section = ConfigIni.GetSection(control.Name);
            if (section == null)
                return;

            Parser.Instance.SetPrimaryControl(this);

            // 处理控件段的 $Include 指令，合并子ini内容
            ProcessControlInclude(control, section);

            // shorthand for localization function
            static string Localize(XNAControl control, string attributeName, string defaultValue, bool notify = true)
                => Translation.Instance.LookUp(control, attributeName, defaultValue, notify);

            foreach (var kvp in section.Keys)
            {
                if (kvp.Key.StartsWith("$CC"))
                {
                    var child = CreateChildControl(control, kvp.Value);
                    ReadINIForControl(child);
                    child.Initialize();

                    if (child is ICompositeControl composite)
                    {
                        foreach (var sc in composite.SubControls)
                        {
                            ReadINIForControl(sc);
                            sc.Initialize();
                        }
                    }
                }
                else if (kvp.Key == "$X")
                {
                    control.X = Parser.Instance.GetExprValue(
                        Localize(control, kvp.Key, kvp.Value, notify: false), control);
                }
                else if (kvp.Key == "$Y")
                {
                    control.Y = Parser.Instance.GetExprValue(
                        Localize(control, kvp.Key, kvp.Value, notify: false), control);
                }
                else if (kvp.Key == "$Width")
                {
                    control.Width = Parser.Instance.GetExprValue(
                        Localize(control, kvp.Key, kvp.Value, notify: false), control);
                }
                else if (kvp.Key == "$Height")
                {
                    control.Height = Parser.Instance.GetExprValue(
                        Localize(control, kvp.Key, kvp.Value, notify: false), control);
                }
                else if (kvp.Key == "$TextAnchor" && control is XNALabel)
                {
                    // TODO refactor these to be more object-oriented
                    ((XNALabel)control).TextAnchor = (LabelTextAnchorInfo)Enum.Parse(typeof(LabelTextAnchorInfo), kvp.Value);
                }
                else if (kvp.Key == "$AnchorPoint" && control is XNALabel)
                {
                    string[] parts = kvp.Value.Split(',');
                    if (parts.Length != 2)
                        throw new FormatException("Invalid format for AnchorPoint: " + kvp.Value);
                    ((XNALabel)control).AnchorPoint = new Vector2(Parser.Instance.GetExprValue(parts[0], control), Parser.Instance.GetExprValue(parts[1], control));
                }
                else if (kvp.Key == "$LeftClickAction")
                {
                    if (kvp.Value == "Disable")
                        control.LeftClick += (s, e) => Disable();
                }
                else
                {
                    control.ParseINIAttribute(ConfigIni, kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Reads a second set of attributes for a control's child controls.
        /// Enables linking controls to controls that are defined after them.
        /// </summary>
        private void ReadLateAttributesForControl(XNAControl control)
        {
            var section = ConfigIni.GetSection(control.Name);
            if (section == null)
                return;

            var children = Children.ToList();
            foreach (var child in children)
            {
                // This logic should also be enabled for other types in the future,
                // but it requires changes in XNAUI
                if (!(child is XNATextBox))
                    continue;

                var childSection = ConfigIni.GetSection(child.Name);
                if (childSection == null)
                    continue;

                string nextControl = childSection.GetStringValue("NextControl", null);
                if (!string.IsNullOrWhiteSpace(nextControl))
                {
                    var otherChild = children.Find(c => c.Name == nextControl);
                    if (otherChild != null)
                        ((XNATextBox)child).NextControl = otherChild;
                }

                string previousControl = childSection.GetStringValue("PreviousControl", null);
                if (!string.IsNullOrWhiteSpace(previousControl))
                {
                    var otherChild = children.Find(c => c.Name == previousControl);
                    if (otherChild != null)
                        ((XNATextBox)child).PreviousControl = otherChild;
                }
            }
        }

        private XNAControl CreateChildControl(XNAControl parent, string keyValue)
        {
            string[] parts = keyValue.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                throw new INIConfigException("Invalid child control definition " + keyValue);

            string childName = parts[0];
            if (string.IsNullOrEmpty(childName))
                throw new INIConfigException("Empty name in child control definition for " + parent.Name);

            XNAControl childControl = ClientGUICreator.GetXnaControl(parts[1]);

            if (Array.Exists(childName.ToCharArray(), c => !char.IsLetterOrDigit(c) && c != '_'))
                throw new INIConfigException("Names of INItializableWindow child controls must consist of letters, digits and underscores only. Offending name: " + parts[0]);

            childControl.Name = childName;
            parent.AddChildWithoutInitialize(childControl);
            return childControl;
        }
    }
}
