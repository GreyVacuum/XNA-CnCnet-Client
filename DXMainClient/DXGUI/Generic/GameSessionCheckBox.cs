using System;
using System.Collections.Generic;

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

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
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
}