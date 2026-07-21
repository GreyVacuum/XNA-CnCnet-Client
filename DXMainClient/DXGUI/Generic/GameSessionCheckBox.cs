using System;

using ClientGUI;

using DTAClient.Domain.Multiplayer;
using DTAClient.DXGUI.Multiplayer.GameLobby;

using Rampastring.Tools;
using Rampastring.XNAUI;

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

    public bool AllowChanges { get; set; } = true;

    // AffectsSpawnIni now true if any spawn ini option (default or indexed) exists
    public bool AffectsSpawnIni => HasAnySpawnIniEntry() || ShouldWriteCustomToSpawnIni();
    public bool AffectsMapCode => HasAnyCustomIniPath() || ShouldWriteSpawnToMapCode();

    private bool HasAnySpawnIniEntry()
        => !string.IsNullOrWhiteSpace(spawnIniOption) || spawnIniEntries.Count > 0;

    private bool HasAnyCustomIniPath()
        => !string.IsNullOrWhiteSpace(customIniPath) || customIniPaths.Count > 0;

    private bool HasSpawnIniEntry(int index)
    {
        if (index == 0)
            return !string.IsNullOrWhiteSpace(spawnIniOption) ||
                   (spawnIniEntries.TryGetValue(0, out var e) && e.HasOption);
        return spawnIniEntries.TryGetValue(index, out var entry) && entry.HasOption;
    }

    private bool HasCustomIniPath(int index)
    {
        if (index == 0)
            return !string.IsNullOrWhiteSpace(customIniPath) || customIniPaths.ContainsKey(0);
        return customIniPaths.ContainsKey(index) && !string.IsNullOrWhiteSpace(customIniPaths[index]);
    }

    private bool ShouldWriteSpawnToMapCode()
    {
        if (!HasAnySpawnIniEntry())
            return false;
        if (HasSpawnIniEntry(0))
        {
            bool writeCustom = spawnWriteCustoms.TryGetValue(0, out var v) ? v : spawnWriteCustom;
            if (writeCustom)
                return true;
        }
        foreach (var idx in spawnIniEntries.Keys.Where(k => k != 0))
        {
            bool writeCustom = spawnWriteCustoms.TryGetValue(idx, out var v) ? v : spawnWriteCustom;
            if (writeCustom)
                return true;
        }
        return false;
    }

    private bool ShouldWriteCustomToSpawnIni()
    {
        if (!HasAnyCustomIniPath())
            return false;
        if (HasCustomIniPath(0))
        {
            bool writeSpawn = customWriteSpawns.TryGetValue(0, out var v) ? v : customWriteSpawn;
            if (writeSpawn)
                return true;
        }
        foreach (var idx in customIniPaths.Keys.Where(k => k != 0))
        {
            bool writeSpawn = customWriteSpawns.TryGetValue(idx, out var v) ? v : customWriteSpawn;
            if (writeSpawn)
                return true;
        }
        return false;
    }

    public bool AllowScoring
        => !((mapScoringMode == CheckBoxMapScoringMode.DenyWhenChecked && Checked)
             || (mapScoringMode == CheckBoxMapScoringMode.DenyWhenUnchecked && !Checked));

    private CheckBoxMapScoringMode mapScoringMode = CheckBoxMapScoringMode.Irrelevant;

    private string spawnIniOption;

    private string customIniPath;

    // 支持按索引配置的 CustomIniPath，如 CustomIniPath0、CustomIniPath1 ...
    // CustomIniPath0 会覆盖无后缀的 CustomIniPath（兼容）
    private readonly Dictionary<int, string> customIniPaths = new();

    // SpawnWriteCustom 控制是否将 SpawnIni 条目写入 spawnmap.ini 而非 spawn.ini
    private bool spawnWriteCustom = false;
    private readonly Dictionary<int, bool> spawnWriteCustoms = new();

    // CustomWriteSpawn 控制是否将 CustomIniPath 应用到 spawn.ini 而非 spawnmap.ini（仅 CheckBox）
    private bool customWriteSpawn = false;
    private readonly Dictionary<int, bool> customWriteSpawns = new();

    // SpawnIniValueCheck 控制写入 SpawnIni 前是否检查值非空，为空则不写入
    private bool spawnIniValueCheck = false;
    private readonly Dictionary<int, bool> spawnIniValueChecks = new();

    protected bool reversed;

    private string enabledSpawnIniValue = "True";
    private string disabledSpawnIniValue = "False";

    private bool DefaultChecked { get; set; }

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

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "SpawnIniOption":
                spawnIniOption = value;
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
            case "SpawnWriteCustom":
                spawnWriteCustom = Conversions.BooleanFromString(value, false);
                spawnWriteCustoms[0] = spawnWriteCustom;
                return;
            case "CustomWriteSpawn":
                customWriteSpawn = Conversions.BooleanFromString(value, false);
                customWriteSpawns[0] = customWriteSpawn;
                return;
            case "SpawnIniValueCheck":
                spawnIniValueCheck = Conversions.BooleanFromString(value, false);
                spawnIniValueChecks[0] = spawnIniValueCheck;
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
        }

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

        idx = ParseSuffix(key, "SpawnWriteCustom");
        if (idx >= 0)
        {
            spawnWriteCustoms[idx] = Conversions.BooleanFromString(value, false);
            if (idx == 0)
                spawnWriteCustom = spawnWriteCustoms[idx];
            return;
        }

        idx = ParseSuffix(key, "CustomWriteSpawn");
        if (idx >= 0)
        {
            customWriteSpawns[idx] = Conversions.BooleanFromString(value, false);
            if (idx == 0)
                customWriteSpawn = customWriteSpawns[idx];
            return;
        }

        idx = ParseSuffix(key, "SpawnIniValueCheck");
        if (idx >= 0)
        {
            spawnIniValueChecks[idx] = Conversions.BooleanFromString(value, false);
            if (idx == 0)
                spawnIniValueCheck = spawnIniValueChecks[idx];
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

        // 写入未开启 SpawnWriteCustom 的 SpawnIniEntries 到 spawn.ini
        if (spawnIniEntries.Count > 0)
        {
            foreach (var kvp in spawnIniEntries)
            {
                var entry = kvp.Value;
                if (!entry.HasOption)
                    continue;

                bool writeCustom = spawnWriteCustoms.TryGetValue(kvp.Key, out var v) ? v : spawnWriteCustom;
                if (writeCustom)
                    continue;

                string project = string.IsNullOrEmpty(entry.Project) ? spawnIniProject : entry.Project;
                string value = GetSpawnIniValue(entry);
                if (ShouldSkipEmptySpawnIniValue(kvp.Key, value))
                    continue;

                spawnIni.SetStringValue(project, entry.Option, value);
            }
        }
        else
        {
            // Fallback: legacy single-option behavior
            if (String.IsNullOrEmpty(spawnIniOption))
                return;

            bool writeCustom = spawnWriteCustoms.TryGetValue(0, out var v0) ? v0 : spawnWriteCustom;
            if (writeCustom)
                return;

            string outVal = GetSpawnIniValue(enabledSpawnIniValue, disabledSpawnIniValue);
            if (ShouldSkipEmptySpawnIniValue(0, outVal))
                return;

            spawnIni.SetStringValue(spawnIniProject, spawnIniOption, outVal);
        }

        // 若 CustomWriteSpawn 开启，将 CustomIniPath 应用到 spawn.ini 而非 spawnmap.ini
        ApplyCustomIniPathsByConfig(spawnIni, null, writeToSpawn: true);
    }

    public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
    {
        if (!AffectsMapCode || Checked == reversed)
            return;

        // 若 SpawnWriteCustom 开启，将 SpawnIniEntries 写入 spawnmap.ini
        if (spawnIniEntries.Count > 0)
        {
            foreach (var kvp in spawnIniEntries)
            {
                var entry = kvp.Value;
                if (!entry.HasOption)
                    continue;

                bool writeCustom = spawnWriteCustoms.TryGetValue(kvp.Key, out var v) ? v : spawnWriteCustom;
                if (!writeCustom)
                    continue;

                string project = string.IsNullOrEmpty(entry.Project) ? spawnIniProject : entry.Project;
                string value = GetSpawnIniValue(entry);
                if (ShouldSkipEmptySpawnIniValue(kvp.Key, value))
                    continue;

                mapIni.SetStringValue(project, entry.Option, value);
            }
        }
        else if (!string.IsNullOrEmpty(spawnIniOption))
        {
            bool writeCustom = spawnWriteCustoms.TryGetValue(0, out var v0) ? v0 : spawnWriteCustom;
            if (writeCustom)
            {
                string outVal = GetSpawnIniValue(enabledSpawnIniValue, disabledSpawnIniValue);
                if (ShouldSkipEmptySpawnIniValue(0, outVal))
                    return;

                mapIni.SetStringValue(spawnIniProject, spawnIniOption, outVal);
            }
        }

        // 默认将 CustomIniPath 应用到 spawnmap.ini，除非 CustomWriteSpawn 开启
        ApplyCustomIniPathsByConfig(mapIni, gameMode, writeToSpawn: false);
    }

    private string GetSpawnIniValue(SpawnIniEntry entry)
    {
        string enabledVal = string.IsNullOrEmpty(entry.EnabledValue) ? enabledSpawnIniValue : entry.EnabledValue;
        string disabledVal = string.IsNullOrEmpty(entry.DisabledValue) ? disabledSpawnIniValue : entry.DisabledValue;
        return (Checked != reversed) ? enabledVal : disabledVal;
    }

    private string GetSpawnIniValue(string enabledValue, string disabledValue)
    {
        return (Checked != reversed) ? enabledValue : disabledValue;
    }

    private bool ShouldSkipEmptySpawnIniValue(int index, string value)
    {
        bool check = spawnIniValueChecks.TryGetValue(index, out var v) ? v : spawnIniValueCheck;
        return check && string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// 根据 CustomWriteSpawn 配置将 CustomIniPath 应用到指定目标文件。
    /// </summary>
    /// <param name="targetIni">目标 INI 文件。</param>
    /// <param name="gameMode">当前游戏模式。</param>
    /// <param name="writeToSpawn">true 表示目标为 spawn.ini，false 表示目标为 spawnmap.ini。</param>
    private void ApplyCustomIniPathsByConfig(IniFile targetIni, GameMode gameMode, bool writeToSpawn)
    {
        if (targetIni == null)
            return;

        if (customIniPaths.Count > 0)
        {
            var keys = new List<int>(customIniPaths.Keys);
            keys.Sort();
            foreach (var k in keys)
            {
                var path = customIniPaths[k];
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                bool writeSpawn = customWriteSpawns.TryGetValue(k, out var v) ? v : customWriteSpawn;
                if (writeSpawn == writeToSpawn)
                    MapCodeHelper.ApplyMapCode(targetIni, path, gameMode);
            }
        }
        else if (!string.IsNullOrWhiteSpace(customIniPath))
        {
            if (customWriteSpawn == writeToSpawn)
                MapCodeHelper.ApplyMapCode(targetIni, customIniPath, gameMode);
        }
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
}