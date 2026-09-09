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
public class GameSessionCheckBox : XNAClientCheckBox, IGameSessionSetting, IGameSessionParentLockAdapter
{
    private const int DEFAULT_SORT_ORDER = 0;

    public GameSessionCheckBox(WindowManager windowManager) : base (windowManager) { }

    public string OptionName { get; private set; }

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

    private string spawnIniProject = "Settings";

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

    // --- 父控件约束（GameSessionParentControl 引擎，GameSessionParentControl.cs） ---
    // 锁定/解锁的具体动作由本控件的 OnUnlocked / OnLocked 实现。

    /// <summary>父控件约束引擎实例。</summary>
    private readonly GameSessionParentControl parentControl = new();

    /// <summary>默认禁用纹理缓存，用于解锁后还原。</summary>
    private Texture2D defaultDisabledCheckedTexture;
    private Texture2D defaultDisabledClearTexture;

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

        // ParentControl 系列键（普通式 / 逗号式 / N 式）统一由引擎处理
        if (parentControl.TryParseAttribute(key, value))
            return;

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

    public override void Initialize()
    {
        base.Initialize();

        // 缓存默认禁用纹理以便还原
        defaultDisabledCheckedTexture = DisabledCheckedTexture ?? UISettings.ActiveSettings.CheckBoxDisabledCheckedTexture;
        defaultDisabledClearTexture = DisabledClearTexture ?? UISettings.ActiveSettings.CheckBoxDisabledClearTexture;

        // 引擎在锁定状态跳变时回调本控件的 OnUnlocked / OnLocked
        parentControl.Attach(this);
        parentControl.Refresh(this);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // 引擎驱动：重试绑定 + 跳变检测；无父控件配置时为廉价空操作
        parentControl.Refresh(this);
    }

    /// <summary>
    /// 约束满足：恢复可编辑与默认禁用纹理。
    /// （IGameSessionParentLockAdapter，仅跳变时被引擎调用一次）
    /// </summary>
    public void OnUnlocked()
    {
        AllowChanges = true;
        AllowChecking = true;
        DisabledCheckedTexture = defaultDisabledCheckedTexture;
        DisabledClearTexture = defaultDisabledClearTexture;
    }

    /// <summary>
    /// 约束不满足：锁定子项，并按 LockedValue（或索引值）展示状态。
    /// （IGameSessionParentLockAdapter，仅跳变时被引擎调用一次）
    /// </summary>
    public void OnLocked(GameSessionParentControl engine, int firstLockedIndex)
    {
        AllowChanges = false;
        AllowChecking = false;
        Checked = engine.GetLockedCheckedDisplay(firstLockedIndex);
        ApplyDisabledTexture(engine.GetLockedTextureName(firstLockedIndex));
    }

    private void ApplyDisabledTexture(string texName)
    {
        if (!string.IsNullOrEmpty(texName))
        {
            try
            {
                var tex = AssetLoader.LoadTexture(texName);
                if (tex != null)
                {
                    DisabledCheckedTexture = tex;
                    DisabledClearTexture = tex;
                    return;
                }
            }
            catch
            {
            }
        }

        DisabledCheckedTexture = defaultDisabledCheckedTexture;
        DisabledClearTexture = defaultDisabledClearTexture;
    }
}

