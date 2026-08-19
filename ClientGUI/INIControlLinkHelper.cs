using Rampastring.Tools;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClientGUI
{
    /// <summary>
    /// Shared implementation of "late" INI child-control attributes that link controls
    /// to each other after the full control tree has been built.
    /// Supports <c>$Toggles</c> / <c>$Opens</c> / <c>$Exits</c> on buttons and check
    /// boxes, <c>$ToggleN</c> / <c>$OpenN</c> / <c>$ExitN</c> (and their simple
    /// sequential forms) on tab controls and drop-downs, and NextControl /
    /// PreviousControl on text boxes.
    /// Used by both <see cref="INItializableWindow"/> and <see cref="XNAWindowBase"/>.
    /// </summary>
    internal static class INIControlLinkHelper
    {
        /// <summary>
        /// Reads a second set of attributes for a control's child controls.
        /// Enables linking controls to controls that are defined after them.
        /// </summary>
        /// <param name="iniFile">The INI file that is being read from.</param>
        /// <param name="root">The control whose child controls are processed.</param>
        public static void ReadLateAttributes(IniFile iniFile, XNAControl root)
        {
            var section = iniFile.GetSection(root.Name);
            if (section == null)
                return;

            // 修复：使用传入的 root 的子控件集合，而不是顶层 this.Children
            var children = root.Children.ToList();
            foreach (var child in children)
            {
                var childSection = iniFile.GetSection(child.Name);

                if (childSection == null)
                {
                    // 仍然需要递归遍历子节点以处理更深层级的控件
                    ReadLateAttributes(iniFile, child);
                    continue;
                }

                // compute root window for lookups (the enclosing window/panel)
                var parentWindow = UIHelpers.FindParentWindow(child) ?? root;

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
                    ReadLateAttributes(iniFile, child);
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
                ReadLateAttributes(iniFile, child);
            }
        }
    }
}
