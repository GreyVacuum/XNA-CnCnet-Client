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
using System.Reflection;

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

            // First create full control tree from INI (including $CC children and extra controls)
            ReadINIForControl(this);
            ParseExtraControls();

            // Then process late attributes that may reference any control in the tree
            ReadLateAttributesForControl(this);

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
                    // Read attributes for the newly created extra control so its children get created too.
                    ReadINIForControl(control);
                    // Initialize the newly created control now that its subtree is built.
                    control.Initialize();
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
                    // recursively build child's subtree
                    ReadINIForControl(child);
                    // initialize child control once subtree is created
                    child.Initialize();

                    if (child is ICompositeControl composite)
                    {
                        foreach (var sc in composite.SubControls)
                        {
                            ReadINIForControl(sc);
                            sc.Initialize();
                        }
                    }

                    // NOTE: Do NOT process late attributes here.
                    // Late attributes (e.g. $Toggles/$Opens/$Exits) are processed
                    // once the full control tree for the window has been created
                    // by calling ReadLateAttributesForControl(this) from Initialize().
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

            // 修复：使用传入的 control 的子控件集合，而不是顶层 this.Children
            var children = control.Children.ToList();
            foreach (var child in children)
            {
                var childSection = ConfigIni.GetSection(child.Name);

                if (childSection == null)
                {
                    // 仍然需要递归遍历子节点以处理更深层级的控件
                    ReadLateAttributesForControl(child);
                    continue;
                }

                // compute root window for lookups (the enclosing window/panel)
                var parentWindow = UIHelpers.FindParentWindow(child) ?? this;

                // Handle buttons & checkboxes being able to toggle other controls.
                if (child is XNAButton || child is XNACheckBox)
                {
                    string toggles = childSection.GetStringValue("$Toggles", null);

                    if (!string.IsNullOrWhiteSpace(toggles))
                    {
                        var controlnames = toggles.Split(',', StringSplitOptions.RemoveEmptyEntries);


                        foreach (var controlName in controlnames)
                        {
                            // lookup relative to the child window/panel, recursively
                            var toggleControl = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, controlName.Trim(), recursive: true);

                            if (toggleControl is not null)
                            {
                                if (child is XNACheckBox checkBox)
                                {
                                    checkBox.CheckedChanged += (sender, args) =>
                                    {
                                        toggleControl.Enabled = !toggleControl.Enabled;
                                        toggleControl.Visible = !toggleControl.Visible;
                                    };
                                }
                                else
                                {
                                    child.LeftClick += (sender, args) =>
                                    {
                                        toggleControl.Enabled = !toggleControl.Enabled;
                                        toggleControl.Visible = !toggleControl.Visible;
                                    };
                                }
                            }
                        }
                    }

                    // 新增: 支持 $Opens / $Exits（打开 / 关闭）用于按钮和复选框
                    string opens = childSection.GetStringValue("$Opens", null);
                    string exits = childSection.GetStringValue("$Exits", null);

                    var openControls = new List<XNAControl>();
                    var exitControls = new List<XNAControl>();

                    if (!string.IsNullOrWhiteSpace(opens))
                    {
                        foreach (var name in opens.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()))
                        {
                            var c = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, name, recursive: true);
                            if (c != null)
                                openControls.Add(c);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(exits))
                    {
                        foreach (var name in exits.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()))
                        {
                            var c = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, name, recursive: true);
                            if (c != null)
                                exitControls.Add(c);
                        }
                    }

                    if (openControls.Count > 0 || exitControls.Count > 0)
                    {
                        if (child is XNACheckBox cb)
                        {
                            // Checked -> 打开 openControls, 关闭 exitControls
                            cb.CheckedChanged += (s, e) =>
                            {
                                bool active = cb.Checked;
                                foreach (var oc in openControls)
                                {
                                    if (oc != null)
                                    {
                                        oc.Visible = active;
                                        oc.Enabled = active;
                                    }
                                }
                                foreach (var xc in exitControls)
                                {
                                    if (xc != null)
                                    {
                                        xc.Visible = !active;
                                        xc.Enabled = !active;
                                    }
                                }
                            };

                            // 初始化状态
                            bool initial = cb.Checked;
                            foreach (var oc in openControls)
                            {
                                if (oc != null)
                                {
                                    oc.Visible = initial;
                                    oc.Enabled = initial;
                                }
                            }
                            foreach (var xc in exitControls)
                            {
                                if (xc != null)
                                {
                                    xc.Visible = !initial;
                                    xc.Enabled = !initial;
                                }
                            }
                        }
                        else
                        {
                            // Button: 点击时执行打开/关闭
                            child.LeftClick += (s, e) =>
                            {
                                foreach (var oc in openControls)
                                {
                                    if (oc != null)
                                    {
                                        oc.Visible = true;
                                        oc.Enabled = true;
                                    }
                                }
                                foreach (var xc in exitControls)
                                {
                                    if (xc != null)
                                    {
                                        xc.Visible = false;
                                        xc.Enabled = false;
                                    }
                                }
                            };
                        }
                    }
                }

                // New: support XNATabControl controlling visibility/enabled state of controls per-tab
                if (child is XNATabControl tabControl)
                {
                    // Map: tab index -> list of control names (comma-separated)
                    var tabToggleMap = new Dictionary<int, List<XNAControl>>();

                    // First, process explicit $ToggleN keys (preferred)
                    foreach (var kvp in childSection.Keys)
                    {
                        if (!kvp.Key.StartsWith("$Toggle", StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        string idxStr = kvp.Key.Substring(7); // after "$Toggle"
                        if (!int.TryParse(idxStr, out int idx))
                            continue;

                        var names = kvp.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
                        foreach (var name in names)
                        {
                            var toggleControl = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, name, recursive: true);
                            if (toggleControl != null)
                            {
                                if (!tabToggleMap.ContainsKey(idx))
                                    tabToggleMap[idx] = new List<XNAControl>();
                                tabToggleMap[idx].Add(toggleControl);
                            }
                        }
                    }

                    // Secondly, support $Toggles as a simple comma-separated list that maps sequentially to tabs.
                    // Example: $Toggles=PanelA,PanelB,PanelC => Toggle0=PanelA, Toggle1=PanelB, ...
                    string simpleToggles = childSection.GetStringValue("$Toggles", null);
                    if (!string.IsNullOrWhiteSpace(simpleToggles))
                    {
                        var names = simpleToggles.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(names[i]))
                                continue;

                            var toggleControl = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, names[i], recursive: true);
                            if (toggleControl == null)
                                continue;

                            if (!tabToggleMap.ContainsKey(i))
                                tabToggleMap[i] = new List<XNAControl>();

                            // Avoid duplicates
                            if (!tabToggleMap[i].Contains(toggleControl))
                                tabToggleMap[i].Add(toggleControl);
                        }
                    }

                    if (tabToggleMap.Count > 0)
                    {
                        void ApplyTabToggles(int selectedTab)
                        {
                            // Build a set of controls that should be visible/enabled for the selected tab
                            var shouldBeVisible = new HashSet<XNAControl>();
                            foreach (var kv in tabToggleMap)
                            {
                                if (kv.Key == selectedTab)
                                {
                                    foreach (var c in kv.Value)
                                        shouldBeVisible.Add(c);
                                }
                            }

                            // For all mapped controls, enable/visible if in shouldBeVisible, otherwise disable/hide
                            var allMappedControls = tabToggleMap.SelectMany(kv => kv.Value).Distinct();
                            foreach (var mapped in allMappedControls)
                            {
                                if (mapped != null)
                                {
                                    bool active = shouldBeVisible.Contains(mapped);
                                    mapped.Visible = active;
                                    mapped.Enabled = active;
                                }
                            }
                        }

                        // initialize state based on current SelectedTab
                        ApplyTabToggles(tabControl.SelectedTab);

                        // subscribe to tab changes
                        tabControl.SelectedIndexChanged += (s, e) =>
                        {
                            ApplyTabToggles(tabControl.SelectedTab);
                        };
                    }

                    // 新增: 支持 $OpenN / $ExitN 以及简单 $Opens / $Exits（按序映射到标签）
                    var tabOpenMap = new Dictionary<int, List<XNAControl>>();
                    var tabExitMap = new Dictionary<int, List<XNAControl>>();

                    foreach (var kvp in childSection.Keys)
                    {
                        if (kvp.Key.StartsWith("$Open", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string idxStr = kvp.Key.Substring(5); // after "$Open"
                            if (!int.TryParse(idxStr, out int idx)) continue;
                            var names = kvp.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
                            foreach (var name in names)
                            {
                                var c = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, name, recursive: true);
                                if (c != null)
                                {
                                    if (!tabOpenMap.ContainsKey(idx)) tabOpenMap[idx] = new List<XNAControl>();
                                    tabOpenMap[idx].Add(c);
                                }
                            }
                        }
                        else if (kvp.Key.StartsWith("$Exit", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string idxStr = kvp.Key.Substring(5); // after "$Exit"
                            if (!int.TryParse(idxStr, out int idx)) continue;
                            var names = kvp.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
                            foreach (var name in names)
                            {
                                var c = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, name, recursive: true);
                                if (c != null)
                                {
                                    if (!tabExitMap.ContainsKey(idx)) tabExitMap[idx] = new List<XNAControl>();
                                    tabExitMap[idx].Add(c);
                                }
                            }
                        }
                    }

                    // 支持简单格式 $Opens 和 $Exits 映射到索引序列
                    string simpleOpens = childSection.GetStringValue("$Opens", null);
                    if (!string.IsNullOrWhiteSpace(simpleOpens))
                    {
                        var names = simpleOpens.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(names[i])) continue;
                            var c = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, names[i], recursive: true);
                            if (c == null) continue;
                            if (!tabOpenMap.ContainsKey(i)) tabOpenMap[i] = new List<XNAControl>();
                            if (!tabOpenMap[i].Contains(c)) tabOpenMap[i].Add(c);
                        }
                    }

                    string simpleExits = childSection.GetStringValue("$Exits", null);
                    if (!string.IsNullOrWhiteSpace(simpleExits))
                    {
                        var names = simpleExits.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(names[i])) continue;
                            var c = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, names[i], recursive: true);
                            if (c == null) continue;
                            if (!tabExitMap.ContainsKey(i)) tabExitMap[i] = new List<XNAControl>();
                            if (!tabExitMap[i].Contains(c)) tabExitMap[i].Add(c);
                        }
                    }

                    if (tabOpenMap.Count > 0 || tabExitMap.Count > 0)
                    {
                        void ApplyTabOpenExit(int selectedTab)
                        {
                            var shouldOpen = new HashSet<XNAControl>();
                            if (tabOpenMap.TryGetValue(selectedTab, out var opensList))
                            {
                                foreach (var c in opensList) shouldOpen.Add(c);
                            }

                            var allMapped = new List<XNAControl>();
                            allMapped.AddRange(tabOpenMap.SelectMany(k => k.Value));
                            allMapped.AddRange(tabExitMap.SelectMany(k => k.Value));

                            foreach (var mapped in allMapped.Distinct())
                            {
                                if (mapped == null) continue;
                                bool active = shouldOpen.Contains(mapped);
                                mapped.Visible = active;
                                mapped.Enabled = active;
                            }

                            // exit mapping: any mapped in tabExitMap[selectedTab] should be closed (override opens)
                            if (tabExitMap.TryGetValue(selectedTab, out var exitsList))
                            {
                                foreach (var ex in exitsList)
                                {
                                    if (ex != null)
                                    {
                                        ex.Visible = false;
                                        ex.Enabled = false;
                                    }
                                }
                            }
                        }

                        ApplyTabOpenExit(tabControl.SelectedTab);
                        tabControl.SelectedIndexChanged += (s, e) => ApplyTabOpenExit(tabControl.SelectedTab);
                    }
                }

                // New: support XNADropDown controlling visibility/enabled state of controls per-selected-index
                if (child is XNADropDown dropDown)
                {
                    var ddToggleMap = new Dictionary<int, List<XNAControl>>();

                    // explicit $ToggleN keys
                    foreach (var kvp in childSection.Keys)
                    {
                        if (!kvp.Key.StartsWith("$Toggle", StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        string idxStr = kvp.Key.Substring(7);
                        if (!int.TryParse(idxStr, out int idx))
                            continue;

                        var names = kvp.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
                        foreach (var name in names)
                        {
                            var toggleControl = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, name, recursive: true);
                            if (toggleControl != null)
                            {
                                if (!ddToggleMap.ContainsKey(idx))
                                    ddToggleMap[idx] = new List<XNAControl>();
                                ddToggleMap[idx].Add(toggleControl);
                            }
                        }
                    }

                    // support simple $Toggles as sequential mapping
                    string simpleDdToggles = childSection.GetStringValue("$Toggles", null);
                    if (!string.IsNullOrWhiteSpace(simpleDdToggles))
                    {
                        var names = simpleDdToggles.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(names[i])) continue;
                            var toggleControl = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, names[i], recursive: true);
                            if (toggleControl == null) continue;
                            if (!ddToggleMap.ContainsKey(i))
                                ddToggleMap[i] = new List<XNAControl>();
                            if (!ddToggleMap[i].Contains(toggleControl))
                                ddToggleMap[i].Add(toggleControl);
                        }
                    }

                    if (ddToggleMap.Count > 0)
                    {
                        void ApplyDropDownToggles(int selectedIndex)
                        {
                            var shouldBeVisible = new HashSet<XNAControl>();
                            foreach (var kv in ddToggleMap)
                            {
                                if (kv.Key == selectedIndex)
                                {
                                    foreach (var c in kv.Value)
                                        shouldBeVisible.Add(c);
                                }
                            }

                            var allMappedControls = ddToggleMap.SelectMany(kv => kv.Value).Distinct();
                            foreach (var mapped in allMappedControls)
                            {
                                if (mapped != null)
                                {
                                    bool active = shouldBeVisible.Contains(mapped);
                                    mapped.Visible = active;
                                    mapped.Enabled = active;
                                }
                            }
                        }

                        // initialize
                        ApplyDropDownToggles(dropDown.SelectedIndex);

                        // subscribe to index changes
                        dropDown.SelectedIndexChanged += (s, e) =>
                        {
                            ApplyDropDownToggles(dropDown.SelectedIndex);
                        };
                    }

                    // 新增: 支持 $OpenN / $ExitN 以及简单 $Opens / $Exits（按序映射到下拉项）
                    var ddOpenMap = new Dictionary<int, List<XNAControl>>();
                    var ddExitMap = new Dictionary<int, List<XNAControl>>();

                    foreach (var kvp in childSection.Keys)
                    {
                        if (kvp.Key.StartsWith("$Open", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string idxStr = kvp.Key.Substring(5);
                            if (!int.TryParse(idxStr, out int idx)) continue;
                            foreach (var name in kvp.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()))
                            {
                                var c = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, name, recursive: true);
                                if (c != null)
                                {
                                    if (!ddOpenMap.ContainsKey(idx)) ddOpenMap[idx] = new List<XNAControl>();
                                    ddOpenMap[idx].Add(c);
                                }
                            }
                        }
                        else if (kvp.Key.StartsWith("$Exit", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string idxStr = kvp.Key.Substring(5);
                            if (!int.TryParse(idxStr, out int idx)) continue;
                            foreach (var name in kvp.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()))
                            {
                                var c = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, name, recursive: true);
                                if (c != null)
                                {
                                    if (!ddExitMap.ContainsKey(idx)) ddExitMap[idx] = new List<XNAControl>();
                                    ddExitMap[idx].Add(c);
                                }
                            }
                        }
                    }

                    string simpleDdOpens = childSection.GetStringValue("$Opens", null);
                    if (!string.IsNullOrWhiteSpace(simpleDdOpens))
                    {
                        var names = simpleDdOpens.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(names[i])) continue;
                            var c = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, names[i], recursive: true);
                            if (c == null) continue;
                            if (!ddOpenMap.ContainsKey(i)) ddOpenMap[i] = new List<XNAControl>();
                            if (!ddOpenMap[i].Contains(c)) ddOpenMap[i].Add(c);
                        }
                    }

                    string simpleDdExits = childSection.GetStringValue("$Exits", null);
                    if (!string.IsNullOrWhiteSpace(simpleDdExits))
                    {
                        var names = simpleDdExits.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(names[i])) continue;
                            var c = UIHelpers.FindMatchingChild<XNAControl>(parentWindow, names[i], recursive: true);
                            if (c == null) continue;
                            if (!ddExitMap.ContainsKey(i)) ddExitMap[i] = new List<XNAControl>();
                            if (!ddExitMap[i].Contains(c)) ddExitMap[i].Add(c);
                        }
                    }

                    if (ddOpenMap.Count > 0 || ddExitMap.Count > 0)
                    {
                        void ApplyDropDownOpenExit(int selectedIndex)
                        {
                            var shouldOpen = new HashSet<XNAControl>();
                            if (ddOpenMap.TryGetValue(selectedIndex, out var opensList))
                            {
                                foreach (var c in opensList) shouldOpen.Add(c);
                            }

                            var allMapped = new List<XNAControl>();
                            allMapped.AddRange(ddOpenMap.SelectMany(k => k.Value));
                            allMapped.AddRange(ddExitMap.SelectMany(k => k.Value));

                            foreach (var mapped in allMapped.Distinct())
                            {
                                if (mapped == null) continue;
                                bool active = shouldOpen.Contains(mapped);
                                mapped.Visible = active;
                                mapped.Enabled = active;
                            }

                            if (ddExitMap.TryGetValue(selectedIndex, out var exitsList))
                            {
                                foreach (var ex in exitsList)
                                {
                                    if (ex != null)
                                    {
                                        ex.Visible = false;
                                        ex.Enabled = false;
                                    }
                                }
                            }
                        }

                        ApplyDropDownOpenExit(dropDown.SelectedIndex);
                        dropDown.SelectedIndexChanged += (s, e) => ApplyDropDownOpenExit(dropDown.SelectedIndex);
                    }
                }

                // This logic should also be enabled for other types in the future,
                // but it requires changes in XNAUI
                if (!(child is XNATextBox))
                {
                    // 在处理完当前 child 的延迟属性之后，递归处理 child 的子控件
                    ReadLateAttributesForControl(child);
                    continue;
                }

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

                // 递归处理子控件
                ReadLateAttributesForControl(child);
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

            // XNAScrollPanel hosts scrollbars and a content panel as its direct
            // children, so $CC child controls must be mounted onto its ContentPanel
            // rather than the scroll panel itself. ContentPanel is a protected field,
            // so it is accessed via reflection here.
            if (parent is XNAScrollPanel scrollPanel)
            {
                FieldInfo contentPanelField = typeof(XNAScrollPanel).GetField("ContentPanel",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (contentPanelField?.GetValue(scrollPanel) is XNAPanel contentPanel)
                {
                    contentPanel.AddChildWithoutInitialize(childControl);
                    return childControl;
                }
            }

            parent.AddChildWithoutInitialize(childControl);
            return childControl;
        }
    }
}
