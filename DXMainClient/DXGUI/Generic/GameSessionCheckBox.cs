using System;
using System.Collections.Generic;
using System.Linq;

using ClientCore.Extensions;
using ClientCore.I18N;

using ClientGUI;

using DTAClient.Domain.Multiplayer;
using DTAClient.DXGUI.Multiplayer.GameLobby;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient.DXGUI.Generic;

public enum CheckBoxMapScoringMode
{
    /// <summary>
    /// The value of the check box makes no difference for scoring maps.
    /// </summary>
    Irrelevant = 0,

    /// <summary>
    /// The check box prevents map scoring when it's checked.
    /// </summary>
    DenyWhenChecked = 1,

    /// <summary>
    /// The check box prevents map scoring when it's unchecked.
    /// </summary>
    DenyWhenUnchecked = 2
}

/// <summary>
/// A game option check box for the game lobby or campaign.
/// </summary>
// TODO split the logic between descendants better and clean up
public class GameSessionCheckBox : XNAClientCheckBox, IGameSessionSetting
{
    private const int DEFAULT_SORT_ORDER = 0;

    public GameSessionCheckBox(WindowManager windowManager) : base (windowManager) { }

    public string OptionName { get; private set; }

    public bool AllowChanges { get; set; } = true;

    // AffectsSpawnIni now true if any spawn ini option (default or indexed) exists
    public bool AffectsSpawnIni => !string.IsNullOrWhiteSpace(spawnIniOption) || spawnIniEntries.Count > 0;
    public bool AffectsMapCode => !string.IsNullOrWhiteSpace(customIniPath) || customIniPaths.Count > 0;

    public bool AllowScoring
        => !((mapScoringMode == CheckBoxMapScoringMode.DenyWhenChecked && Checked)
             || (mapScoringMode == CheckBoxMapScoringMode.DenyWhenUnchecked && !Checked));

    private CheckBoxMapScoringMode mapScoringMode = CheckBoxMapScoringMode.Irrelevant;

    private string spawnIniOption;

    private string spawnIniProject = "Settings";

    private string customIniPath;

    // 支持按索引配置的 CustomIniPath，如 CustomIniPath0、CustomIniPath1 ...
    // CustomIniPath0 会覆盖无后缀的 CustomIniPath（兼容）
    private readonly Dictionary<int, string> customIniPaths = new();

    protected bool reversed;

    private string enabledSpawnIniValue = "True";
    private string disabledSpawnIniValue = "False";

    private bool DefaultChecked { get; set; }

    /// <summary>
    /// Per-index spawn INI entries. Index 0 mirrors the base (no-suffix) values when present.
    /// </summary>
    private readonly Dictionary<int, SpawnIniEntry> spawnIniEntries = new();

    private class SpawnIniEntry
    {
        public string Option;
        public string Project;
        public string EnabledValue;
        public string DisabledValue;
        public bool HasOption => !string.IsNullOrWhiteSpace(Option);
    }

    /// <summary>
    /// Whether this checkbox should be included in the GAME broadcast.
    /// </summary>
    public bool BroadcastToLobby { get; private set; }

    /// <summary>
    /// Whether the icon/text should be shown in the game list.
    /// </summary>
    public bool ShowInGameList { get; private set; }

    /// <summary>
    /// Whether the icon should be shown on the right side of the game list.
    /// Only applies if ShowInGameList is true.
    /// </summary>
    public bool ShowInGameListOnRight { get; private set; }

    /// <summary>
    /// Whether the icon/text should be shown in the game information panel.
    /// </summary>
    public bool ShowInGameInformationPanel { get; private set; }

    /// <summary>
    /// Whether to show only the icon (without text) in the game information panel.
    /// Only applies if ShowInGameInformationPanel is true.
    /// </summary>
    public bool ShowInGameInformationPanelAsIconOnly { get; private set; }

    /// <summary>
    /// Whether the icon should be shown in the game lobby control itself.
    /// </summary>
    public bool ShowIconInGameLobby { get; private set; }

    /// <summary>
    /// Whether this setting should be filterable and shown in the filters panel.
    /// </summary>
    public bool ShowInFilters { get; private set; }

    /// <summary>
    /// The texture name for the icon when setting is enabled.
    /// </summary>
    public string EnabledIcon { get; private set; }

    /// <summary>
    /// The texture name for the icon when setting is disabled.
    /// </summary>
    public string DisabledIcon { get; private set; }

    /// <summary>
    /// Sort order for displaying icons in the GameInformationPanel and GameListBox.
    /// Lower values appear first.
    /// </summary>
    public int SortOrder { get; private set; } = DEFAULT_SORT_ORDER;

    // --- 新增：支持父复选框（兼容 SettingCheckBoxBase），并支持 N 写法（索引） ---
    // 单一兼容属性（保持与 SettingCheckBoxBase 同名）
    private string _parentCheckBoxName;
    public string ParentCheckBoxName
    {
        get => _parentCheckBoxName;
        set
        {
            _parentCheckBoxName = value;
            // 更新单一 parent（兼容旧用法）
            UpdateSingleParent(FindParentCheckBoxByName(_parentCheckBoxName));
        }
    }

    private XNAClientCheckBox _parentCheckBoxSingle;
    public XNAClientCheckBox ParentCheckBox
    {
        get => _parentCheckBoxSingle;
        set
        {
            // 取消旧单一 parent 事件
            if (_parentCheckBoxSingle != null)
                _parentCheckBoxSingle.CheckedChanged -= ParentCheckBox_CheckedChanged;

            _parentCheckBoxSingle = value;
            RefreshParentCheckBoxVisualState();

            if (_parentCheckBoxSingle != null)
                _parentCheckBoxSingle.CheckedChanged += ParentCheckBox_CheckedChanged;
        }
    }

    /// <summary>
    /// Value required from parent check-box control if set (单一兼容用).
    /// </summary>
    public bool ParentCheckBoxRequiredValue { get; set; } = true;

    /// <summary>
    /// 可选：定义父复选框显示使用的图像（默认已拆为选中/未选两个图像）
    /// 可指定为 "checkedTex,uncheckedTex" 或 单一纹理（用于兼容旧写法）.
    /// </summary>
    public string ParentCheckBoxTexture { get; set; } = null;

    // 新增：当父控件导致禁用时，子控件应显示为选中/未选的状态（默认 false 未选）
    public bool ParentChecked { get; set; } = false;

    // 拆分两个默认纹理：选中时与未选时使用的禁用纹理
    private string ParentCheckBoxTextureChecked { get; set; } = "checkBoxCheckedD.png";
    private string ParentCheckBoxTextureUnchecked { get; set; } = "checkBoxClearD.png";

    // 支持索引形式的父复选框配置（N 写法）
    private readonly Dictionary<int, string> parentCheckBoxNames = new();
    private readonly Dictionary<int, XNAClientCheckBox> parentCheckBoxes = new();
    private readonly Dictionary<int, bool> parentCheckBoxRequiredValues = new();
    // 索引对应的纹理（选中/未选分别保存）
    private readonly Dictionary<int, string> parentCheckBoxTextureCheckedStrings = new();
    private readonly Dictionary<int, string> parentCheckBoxTextureUncheckedStrings = new();
    // 索引对应的 ParentChecked 值（用于决定禁用时的显示 Checked 状态）
    private readonly Dictionary<int, bool> parentCheckBoxCheckedValues = new();

    // 用于还原默认禁用纹理
    private Texture2D defaultDisabledCheckedTexture;
    private Texture2D defaultDisabledClearTexture;

    // 标记：是否已完成父控件绑定解析（避免每帧重复尝试）
    private bool parentBindingsResolved = false;

    // ------------------------------------------------------------------

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        static string Localize(XNAControl control, string attributeName, string defaultValue, bool notify = true)
            => Translation.Instance.LookUp(control, attributeName, defaultValue, notify);

        // helper to parse numeric suffix, returns -1 if no suffix matched
        static int ParseSuffix(string key, string prefix)
        {
            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return -1;
            string suffix = key.Substring(prefix.Length);
            if (suffix.Length == 0) return -1;
            if (int.TryParse(suffix, out int idx)) return idx;
            return -1;
        }

        switch (key)
        {
            case "OptionName":
                OptionName = Localize(this, "OptionName", value);
                return;
            case "SpawnIniOption":
                spawnIniOption = value;
                return;
            case "SpawnIniProject":
                spawnIniProject = value;
                return;
            case "EnabledSpawnIniValue":
                enabledSpawnIniValue = value;
                return;
            case "DisabledSpawnIniValue":
                disabledSpawnIniValue = value;
                return;
            case "CustomIniPath":
                customIniPath = value;
                return;
            case "Reversed":
                reversed = Conversions.BooleanFromString(value, false);
                return;
            case "Checked":
                bool checkedValue = Conversions.BooleanFromString(value, false);
                DefaultChecked = Checked = checkedValue;
                return;
            case "MapScoringMode":
                mapScoringMode = (CheckBoxMapScoringMode)Enum.Parse(typeof(CheckBoxMapScoringMode), value);
                return;
            case "BroadcastToLobby":
                BroadcastToLobby = Conversions.BooleanFromString(value, false);
                return;
            case "ShowInGameList":
                ShowInGameList = Conversions.BooleanFromString(value, false);
                return;
            case "ShowInGameListOnRight":
                ShowInGameListOnRight = Conversions.BooleanFromString(value, false);
                return;
            case "ShowInGameInformationPanel":
                ShowInGameInformationPanel = Conversions.BooleanFromString(value, false);
                return;
            case "ShowInGameInformationPanelAsIconOnly":
                ShowInGameInformationPanelAsIconOnly = Conversions.BooleanFromString(value, false);
                return;
            case "ShowIconInGameLobby":
                ShowIconInGameLobby = Conversions.BooleanFromString(value, false);
                return;
            case "ShowInFilters":
                ShowInFilters = Conversions.BooleanFromString(value, false);
                return;
            case "EnabledIcon":
                EnabledIcon = value;
                return;
            case "DisabledIcon":
                DisabledIcon = value;
                return;
            case "SortOrder":
                SortOrder = int.Parse(value);
                return;

            // 新增：兼容单一 ParentCheckBox 配置 或 支持逗号分隔多个父控件名
            case "ParentCheckBoxName":
                // 支持 "name1, name2, name3" 形式——视为索引写法
                if (!string.IsNullOrWhiteSpace(value) && value.Contains(','))
                {
                    parentCheckBoxNames.Clear();
                    var parts = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim())
                                     .Where(s => !string.IsNullOrEmpty(s))
                                     .ToArray();
                    for (int i = 0; i < parts.Length; i++)
                        parentCheckBoxNames[i] = parts[i];

                    // 重新绑定索引父控件（会尝试在父容器中查找）
                    UpdateIndexedParentBindings();
                }
                else
                {
                    // 兼容旧写法：单一父控件名
                    ParentCheckBoxName = value;
                }
                return;

            case "ParentCheckBoxRequiredValue":
                // 支持逗号分隔的多个值或单一值复制到所有索引父控件
                if (!string.IsNullOrWhiteSpace(value) && value.Contains(','))
                {
                    var parts = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim())
                                     .Where(s => !string.IsNullOrEmpty(s))
                                     .ToArray();
                    if (parentCheckBoxNames.Count == 0)
                    {
                        // 没有索引父控件时，若只有一个值则设置单一属性；若多个值则使用第一个作为兼容行为
                        if (parts.Length == 1)
                        {
                            ParentCheckBoxRequiredValue = Conversions.BooleanFromString(parts[0], true);
                        }
                        else
                        {
                            ParentCheckBoxRequiredValue = Conversions.BooleanFromString(parts[0], true);
                        }
                    }
                    else
                    {
                        // 有索引父控件：如果只有一个值，复制到所有；否则按索引映射，不够时用最后一个值填充
                        bool[] bools = parts.Select(p => Conversions.BooleanFromString(p, true)).ToArray();
                        var keys = parentCheckBoxNames.Keys.OrderBy(k => k).ToArray();
                        for (int i = 0; i < keys.Length; i++)
                        {
                            bool v;
                            if (bools.Length == 1)
                                v = bools[0];
                            else if (i < bools.Length)
                                v = bools[i];
                            else
                                v = bools.Last();
                            parentCheckBoxRequiredValues[keys[i]] = v;
                        }
                    }
                }
                else
                {
                    // 单一布尔值
                    bool single = Conversions.BooleanFromString(value, true);
                    if (parentCheckBoxNames.Count == 0)
                    {
                        ParentCheckBoxRequiredValue = single;
                    }
                    else
                    {
                        // 复制到所有已知索引父控件
                        foreach (var k in parentCheckBoxNames.Keys)
                            parentCheckBoxRequiredValues[k] = single;
                    }
                }

                RefreshParentCheckBoxVisualState();
                return;

            case "ParentCheckBoxTexture":
                // 支持 "checkedTex,uncheckedTex" 或 单一纹理（复制到两者）
                if (!string.IsNullOrEmpty(value) && value.Contains(','))
                {
                    var parts = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim()).ToArray();
                    if (parts.Length >= 2)
                    {
                        ParentCheckBoxTextureChecked = parts[0];
                        ParentCheckBoxTextureUnchecked = parts[1];
                    }
                    else if (parts.Length == 1)
                    {
                        ParentCheckBoxTextureChecked = parts[0];
                        ParentCheckBoxTextureUnchecked = parts[0];
                    }
                }
                else
                {
                    // 单一纹理：兼容旧写法，复制到两者
                    if (!string.IsNullOrEmpty(value))
                    {
                        ParentCheckBoxTextureChecked = value;
                        ParentCheckBoxTextureUnchecked = value;
                    }
                }

                ParentCheckBoxTexture = string.IsNullOrEmpty(value) ? ParentCheckBoxTexture : value;
                return;

            case "ParentChecked":
                ParentChecked = Conversions.BooleanFromString(value, false);
                return;
        }

        // handle indexed spawn ini attributes
        int idx;
        idx = ParseSuffix(key, "SpawnIniOption");
        if (idx >= 0)
        {
            if (!spawnIniEntries.TryGetValue(idx, out var entry))
            {
                entry = new SpawnIniEntry();
                spawnIniEntries[idx] = entry;
            }
            entry.Option = value;
            // index 0 overrides base no-suffix option for compatibility
            if (idx == 0)
                spawnIniOption = value;
            return;
        }

        idx = ParseSuffix(key, "SpawnIniProject");
        if (idx >= 0)
        {
            if (!spawnIniEntries.TryGetValue(idx, out var entry))
            {
                entry = new SpawnIniEntry();
                spawnIniEntries[idx] = entry;
            }
            entry.Project = value;
            if (idx == 0)
                spawnIniProject = value;
            return;
        }

        idx = ParseSuffix(key, "EnabledSpawnIniValue");
        if (idx >= 0)
        {
            if (!spawnIniEntries.TryGetValue(idx, out var entry))
            {
                entry = new SpawnIniEntry();
                spawnIniEntries[idx] = entry;
            }
            entry.EnabledValue = value;
            if (idx == 0)
                enabledSpawnIniValue = value;
            return;
        }

        idx = ParseSuffix(key, "DisabledSpawnIniValue");
        if (idx >= 0)
        {
            if (!spawnIniEntries.TryGetValue(idx, out var entry))
            {
                entry = new SpawnIniEntry();
                spawnIniEntries[idx] = entry;
            }
            entry.DisabledValue = value;
            if (idx == 0)
                disabledSpawnIniValue = value;
            return;
        }

        // handle indexed CustomIniPath attributes like CustomIniPath0, CustomIniPath1 ...
        idx = ParseSuffix(key, "CustomIniPath");
        if (idx >= 0)
        {
            customIniPaths[idx] = value;
            if (idx == 0)
                customIniPath = value;
            return;
        }

        // --- 新增：支持索引形式的 ParentCheckBox 属性，例如 ParentCheckBoxName0, ParentCheckBoxRequiredValue0, ParentCheckBoxTexture0, ParentChecked0 ---
        idx = ParseSuffix(key, "ParentCheckBoxName");
        if (idx >= 0)
        {
            parentCheckBoxNames[idx] = value;
            // 立即解析并绑定对应控件（若已创建）
            UpdateIndexedParentBindings();
            return;
        }

        idx = ParseSuffix(key, "ParentCheckBoxRequiredValue");
        if (idx >= 0)
        {
            parentCheckBoxRequiredValues[idx] = Conversions.BooleanFromString(value, true);
            RefreshParentCheckBoxVisualState();
            return;
        }

        idx = ParseSuffix(key, "ParentCheckBoxTexture");
        if (idx >= 0)
        {
            // 支持 "checkedTex,uncheckedTex" 或 单一纹理
            if (!string.IsNullOrEmpty(value) && value.Contains(','))
            {
                var parts = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => s.Trim()).ToArray();
                if (parts.Length >= 2)
                {
                    parentCheckBoxTextureCheckedStrings[idx] = parts[0];
                    parentCheckBoxTextureUncheckedStrings[idx] = parts[1];
                }
                else if (parts.Length == 1)
                {
                    parentCheckBoxTextureCheckedStrings[idx] = parts[0];
                    parentCheckBoxTextureUncheckedStrings[idx] = parts[0];
                }
            }
            else
            {
                parentCheckBoxTextureCheckedStrings[idx] = string.IsNullOrEmpty(value) ? null : value;
                parentCheckBoxTextureUncheckedStrings[idx] = string.IsNullOrEmpty(value) ? null : value;
            }

            // 索引 0 也更新基础属性以兼容旧写法（如果需要）
            if (idx == 0)
            {
                if (parentCheckBoxTextureCheckedStrings.TryGetValue(0, out var c) && !string.IsNullOrEmpty(c))
                    ParentCheckBoxTextureChecked = c;
                if (parentCheckBoxTextureUncheckedStrings.TryGetValue(0, out var u) && !string.IsNullOrEmpty(u))
                    ParentCheckBoxTextureUnchecked = u;

                // 同时保留原字符串字段
                ParentCheckBoxTexture = string.IsNullOrEmpty(value) ? ParentCheckBoxTexture : value;
            }

            return;
        }

        idx = ParseSuffix(key, "ParentChecked");
        if (idx >= 0)
        {
            parentCheckBoxCheckedValues[idx] = Conversions.BooleanFromString(value, false);
            if (idx == 0)
                ParentChecked = parentCheckBoxCheckedValues[idx];
            RefreshParentCheckBoxVisualState();
            return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public int Value
    {
        get => Checked ? 1 : 0;  // 0 = unchecked/off, 1 = checked/on
        set => Checked = value != 0;  // 0 = unchecked/off, 1 = checked/on
    }

    public void ApplySpawnIniCode(IniFile spawnIni)
    {
        if (!AffectsSpawnIni)
            return;

        // If there are indexed entries, write each configured one.
        if (spawnIniEntries.Count > 0)
        {
            foreach (var kvp in spawnIniEntries)
            {
                var entry = kvp.Value;
                if (!entry.HasOption)
                    continue;

                string project = string.IsNullOrEmpty(entry.Project) ? spawnIniProject : entry.Project;
                string enabledVal = string.IsNullOrEmpty(entry.EnabledValue) ? enabledSpawnIniValue : entry.EnabledValue;
                string disabledVal = string.IsNullOrEmpty(entry.DisabledValue) ? disabledSpawnIniValue : entry.DisabledValue;

                string value = (Checked != reversed) ? enabledVal : disabledVal;
                spawnIni.SetStringValue(project, entry.Option, value);
            }

            return;
        }

        // Fallback: legacy single-option behavior
        if (String.IsNullOrEmpty(spawnIniOption))
            return;

        string outVal = disabledSpawnIniValue;
        if (Checked != reversed)
        {
            outVal = enabledSpawnIniValue;
        }

        spawnIni.SetStringValue(spawnIniProject, spawnIniOption, outVal);
    }
        
    public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
    {
        if (!AffectsMapCode || Checked == reversed)
            return;

        // 如果配置了索引形式的 CustomIniPath，按索引升序逐个应用
        if (customIniPaths.Count > 0)
        {
            var keys = new List<int>(customIniPaths.Keys);
            keys.Sort();
            foreach (var k in keys)
            {
                var path = customIniPaths[k];
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                MapCodeHelper.ApplyMapCode(mapIni, path, gameMode);
            }
            return;
        }

        // 兼容旧逻辑：单一路径
        if (!string.IsNullOrWhiteSpace(customIniPath))
            MapCodeHelper.ApplyMapCode(mapIni, customIniPath, gameMode);
    }

    public override void OnLeftClick(InputEventArgs inputEventArgs)
    {
        // FIXME there's a discrepancy with how base XNAUI handles this
        // it doesn't set handled if changing the setting is not allowed
        inputEventArgs.Handled = true;
            
        if (!AllowChanges)
            return;

        base.OnLeftClick(inputEventArgs);
    }

    public void ResetToDefault()
    {
        if (!AllowChanges)
            throw new InvalidOperationException("Cannot reset to default when changes are not allowed.");

        Checked = DefaultChecked;
    }

    public override void Initialize()
    {
        base.Initialize();

        // 缓存默认禁用纹理以便还原
        defaultDisabledCheckedTexture = DisabledCheckedTexture ?? UISettings.ActiveSettings.CheckBoxDisabledCheckedTexture;
        defaultDisabledClearTexture = DisabledClearTexture ?? UISettings.ActiveSettings.CheckBoxDisabledClearTexture;

        // 父控件名可能在 Initialize 前解析但控件层级尚未准备好，重新解析索引父控件
        UpdateIndexedParentBindings();

        // 如果使用单一 ParentCheckBoxName，尝试在此时解析并绑定（防止解析时 Parent 为空导致未绑定）
        if (!string.IsNullOrEmpty(_parentCheckBoxName) && Parent != null && _parentCheckBoxSingle == null)
        {
            UpdateSingleParent(FindParentCheckBoxByName(_parentCheckBoxName));
        }

        RefreshParentCheckBoxVisualState();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (HasParentCheckBoxConfiguration())
            RefreshParentCheckBoxVisualState();

        // 如果尚未解析到父控件，且已经有父容器，则在 Update 中尝试继续绑定；
        // 这样能在初始化阶段父控件尚未加入时确保稍后能正确绑定并立即应用纹理。
        if (!parentBindingsResolved && Parent != null)
        {
            // 尝试解析索引父控件（内部会绑定事件并调用 UpdateAllowChecking）
            UpdateIndexedParentBindings();

            // 尝试解析单一父控件（兼容旧写法）
            if (!string.IsNullOrEmpty(_parentCheckBoxName) && _parentCheckBoxSingle == null)
            {
                UpdateSingleParent(FindParentCheckBoxByName(_parentCheckBoxName));
            }

            // 检查是否已有任一父控件绑定成功
            bool anyBound = _parentCheckBoxSingle != null;
            if (!anyBound)
            {
                foreach (var kvp in parentCheckBoxes)
                {
                    if (kvp.Value != null)
                    {
                        anyBound = true;
                        break;
                    }
                }
            }

            if (anyBound)
            {
                parentBindingsResolved = true;
                // 一旦绑定成功，立即应用检查（包含禁用纹理）
                RefreshParentCheckBoxVisualState();
            }
        }
    }

    // --- 新增方法：查找父控件以及索引父控件的绑定管理 ---

    private XNAClientCheckBox FindParentCheckBoxByName(string name)
    {
        if (string.IsNullOrEmpty(name) || Parent == null)
            return null;

        foreach (var control in Parent.Children)
        {
            if (control is XNAClientCheckBox cb && control.Name == name)
                return cb;
        }

        return null;
    }

    private void UpdateSingleParent(XNAClientCheckBox parent)
    {
        // 取消旧事件（已在 setter 中处理 ParentCheckBox）
        ParentCheckBox = parent;
    }

    private void UpdateIndexedParentBindings()
    {
        // 解绑所有已绑定的索引父控件事件
        foreach (var kvp in parentCheckBoxes)
        {
            if (kvp.Value != null)
                kvp.Value.CheckedChanged -= ParentCheckBox_CheckedChanged;
        }

        parentCheckBoxes.Clear();

        // 重新解析并绑定所有索引父控件（尝试使用已缓存的名称）
        var keys = new List<int>(parentCheckBoxNames.Keys);
        foreach (var k in keys)
        {
            var name = parentCheckBoxNames[k];
            var found = FindParentCheckBoxByName(name);
            parentCheckBoxes[k] = found;
            if (found != null)
                found.CheckedChanged += ParentCheckBox_CheckedChanged;
        }

        RefreshParentCheckBoxVisualState();
    }

    private void ParentCheckBox_CheckedChanged(object sender, EventArgs e) => RefreshParentCheckBoxVisualState();

    private void UpdateAllowChecking()
    {
        // 优先使用索引形式（如果存在任何索引父控件配置）
        if (parentCheckBoxNames.Count > 0)
        {
            bool allMatch = true;
            int failingIndex = -1;
            foreach (var kvp in parentCheckBoxNames)
            {
                var idx = kvp.Key;
                parentCheckBoxes.TryGetValue(idx, out var parent);
                // 如果父控件未找到，则视为不满足要求
                if (parent == null)
                {
                    allMatch = false;
                    failingIndex = idx;
                    break;
                }
                bool required = parentCheckBoxRequiredValues.ContainsKey(idx) ? parentCheckBoxRequiredValues[idx] : true;
                if (parent.Checked != required)
                {
                    allMatch = false;
                    failingIndex = idx;
                    break;
                }
            }

            if (allMatch)
            {
                // 当所有父项都满足要求时，按照配置禁用当前控件并根据 ParentChecked 展示选中状态
                AllowChanges = false;
                AllowChecking = false;

                // 选择用于决定 Checked/纹理的索引优先级：取第一个已配置索引（升序）
                var firstIdx = parentCheckBoxNames.Keys.OrderBy(k => k).FirstOrDefault();

                bool parentCheckedForDisplay = ParentChecked;
                if (parentCheckBoxCheckedValues.TryGetValue(firstIdx, out var pcv))
                    parentCheckedForDisplay = pcv;

                Checked = parentCheckedForDisplay;

                // 使用通用或索引对应的纹理（优先索引纹理）并根据 parentCheckedForDisplay 选择 checked/unchecked 纹理
                string texName = null;
                if (parentCheckBoxTextureCheckedStrings.TryGetValue(firstIdx, out var idxCheckedTex) &&
                    parentCheckBoxTextureUncheckedStrings.TryGetValue(firstIdx, out var idxUncheckedTex) &&
                    (!string.IsNullOrEmpty(idxCheckedTex) || !string.IsNullOrEmpty(idxUncheckedTex)))
                {
                    texName = parentCheckedForDisplay ? idxCheckedTex : idxUncheckedTex;
                }
                else
                {
                    texName = parentCheckedForDisplay ? ParentCheckBoxTextureChecked : ParentCheckBoxTextureUnchecked;
                }

                if (!string.IsNullOrEmpty(texName))
                {
                    try
                    {
                        var tex = AssetLoader.LoadTexture(texName);
                        if (tex != null)
                        {
                            DisabledCheckedTexture = tex;
                            DisabledClearTexture = tex;
                        }
                        else
                        {
                            DisabledCheckedTexture = defaultDisabledCheckedTexture;
                            DisabledClearTexture = defaultDisabledClearTexture;
                        }
                    }
                    catch
                    {
                        DisabledCheckedTexture = defaultDisabledCheckedTexture;
                        DisabledClearTexture = defaultDisabledClearTexture;
                    }
                }
                else
                {
                    DisabledCheckedTexture = defaultDisabledCheckedTexture;
                    DisabledClearTexture = defaultDisabledClearTexture;
                }
            }
            else
            {
                // 只要不是「全部匹配」，就允许更改
                AllowChanges = true;
                AllowChecking = true;
                DisabledCheckedTexture = defaultDisabledCheckedTexture;
                DisabledClearTexture = defaultDisabledClearTexture;
            }

            return;
        }

        // 否则使用单一兼容父控件（SettingCheckBoxBase 的行为）
        if (ParentCheckBox != null)
        {
            if (ParentCheckBox.Checked == ParentCheckBoxRequiredValue)
            {
                AllowChanges = true;
                AllowChecking = true;
                DisabledCheckedTexture = defaultDisabledCheckedTexture;
                DisabledClearTexture = defaultDisabledClearTexture;
            }
            else
            {
                AllowChanges = false;
                AllowChecking = false;

                // 当父控件导致禁用时，根据 ParentChecked 决定显示为选中还是未选
                Checked = ParentChecked;

                // 选择纹理：根据 ParentChecked 使用对应默认纹理
                string texName = ParentChecked ? ParentCheckBoxTextureChecked : ParentCheckBoxTextureUnchecked;

                if (!string.IsNullOrEmpty(texName))
                {
                    try
                    {
                        var tex = AssetLoader.LoadTexture(texName);
                        if (tex != null)
                        {
                            DisabledCheckedTexture = tex;
                            DisabledClearTexture = tex;
                        }
                        else
                        {
                            DisabledCheckedTexture = defaultDisabledCheckedTexture;
                            DisabledClearTexture = defaultDisabledClearTexture;
                        }
                    }
                    catch
                    {
                        DisabledCheckedTexture = defaultDisabledCheckedTexture;
                        DisabledClearTexture = defaultDisabledClearTexture;
                    }
                }
                else
                {
                    DisabledCheckedTexture = defaultDisabledCheckedTexture;
                    DisabledClearTexture = defaultDisabledClearTexture;
                }
            }
        }
    }

    private bool HasParentCheckBoxConfiguration()
    {
        return parentCheckBoxNames.Count > 0
            || _parentCheckBoxSingle != null
            || !string.IsNullOrEmpty(_parentCheckBoxName);
    }

    private void RefreshParentCheckBoxVisualState()
    {
        // 游戏结束后界面会刷新，重新应用父控件相关的禁用纹理，避免 ParentCheckBoxTexture 失效
        if (!HasParentCheckBoxConfiguration())
            return;

        UpdateAllowChecking();
    }
}