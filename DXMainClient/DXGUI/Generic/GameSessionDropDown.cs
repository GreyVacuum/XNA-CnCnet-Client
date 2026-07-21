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

namespace DTAClient.DXGUI.Generic;

/// <summary>
/// A game option drop-down for the game lobby or campaign.
/// </summary>
// TODO split the logic between descendants better and clean up
public class GameSessionDropDown : XNAClientDropDown, IGameSessionSetting
{

    private const int DEFAULT_SORT_ORDER = 0;

    public GameSessionDropDown(WindowManager windowManager) : base(windowManager) { }

    public string OptionName { get; private set; }
    public bool AffectsSpawnIni => HasAnySpawnIniEntryWrittenToSpawnIni();
    public bool AffectsMapCode => dataWriteMode == DropDownDataWriteMode.MAPCODE || HasAnySpawnIniEntryWrittenToMapCode();
    public bool AllowScoring => true;  // TODO

    private bool HasAnySpawnIniEntry()
        => !string.IsNullOrWhiteSpace(spawnIniOption) || spawnIniEntries.Count > 0;

    private bool HasSpawnIniEntry(int index)
    {
        if (index == 0)
            return !string.IsNullOrWhiteSpace(spawnIniOption) ||
                   (spawnIniEntries.TryGetValue(0, out var e) && e.HasOption);
        return spawnIniEntries.TryGetValue(index, out var entry) && entry.HasOption;
    }

    private bool HasAnySpawnIniEntryWrittenToSpawnIni()
    {
        if (!HasAnySpawnIniEntry())
            return false;
        if (HasSpawnIniEntry(0))
        {
            bool writeCustom = spawnWriteCustoms.TryGetValue(0, out var v) ? v : spawnWriteCustom;
            if (!writeCustom)
                return true;
        }
        foreach (var idx in spawnIniEntries.Keys.Where(k => k != 0))
        {
            bool writeCustom = spawnWriteCustoms.TryGetValue(idx, out var v) ? v : spawnWriteCustom;
            if (!writeCustom)
                return true;
        }
        return false;
    }

    private bool HasAnySpawnIniEntryWrittenToMapCode()
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

    private DropDownDataWriteMode dataWriteMode = DropDownDataWriteMode.BOOLEAN;

    private string spawnIniOption = string.Empty;

    private string spawnIniProject = "Settings";

    // SpawnWriteCustom 控制是否将 SpawnIni 条目写入 spawnmap.ini 而非 spawn.ini
    private bool spawnWriteCustom = false;
    private readonly Dictionary<int, bool> spawnWriteCustoms = new();

    // SpawnIniValueCheck 控制写入 SpawnIni 前是否检查值非空，为空则不写入
    private bool spawnIniValueCheck = false;
    private readonly Dictionary<int, bool> spawnIniValueChecks = new();

    private int defaultIndex;

    /// <summary>
    /// Whether this dropdown should be included in the GAME broadcast.
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
    /// Sort order for displaying icons in the GameInformationPanel and GameListBox.
    /// Lower values appear first.
    /// </summary>
    public int SortOrder { get; private set; } = DEFAULT_SORT_ORDER;

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        // shorthand for localization function
        static string Localize(XNAControl control, string attributeName, string defaultValue, bool notify = true)
            => Translation.Instance.LookUp(control, attributeName, defaultValue, notify);

        switch (key)
        {
            case "Items":
                string[] items = value.SplitWithCleanup();
                string[] itemLabels = iniFile.GetStringListValue(Name, "ItemLabels", "");
                string[] iconNames = iniFile.GetStringListValue(Name, "Icons", "");
                for (int i = 0; i < items.Length; i++)
                {
                    bool hasLabel = itemLabels.Length > i && !string.IsNullOrEmpty(itemLabels[i]);
                    string iconName = iconNames.Length > i ? iconNames[i] : null;
                    XNADropDownItem item = new()
                    {
                        Text = Localize(this, $"Item{i}",
                            hasLabel ? itemLabels[i] : items[i]),
                        Tag = items[i],
                        Texture = !string.IsNullOrEmpty(iconName) ? AssetLoader.LoadTexture(iconName) : null,
                    };
                    AddItem(item);
                }
                return;
            case "DataWriteMode":
                if (value.ToUpper() == "INDEX")
                    dataWriteMode = DropDownDataWriteMode.INDEX;
                else if (value.ToUpper() == "BOOLEAN")
                    dataWriteMode = DropDownDataWriteMode.BOOLEAN;
                else if (value.ToUpper() == "MAPCODE")
                    dataWriteMode = DropDownDataWriteMode.MAPCODE;
                else
                    dataWriteMode = DropDownDataWriteMode.STRING;
                return;
            case "SpawnIniOption":
                spawnIniOption = value;
                return;
            case "SpawnIniProject":
                spawnIniProject = value;
                return;
            case "SpawnWriteCustom":
                spawnWriteCustom = Conversions.BooleanFromString(value, false);
                spawnWriteCustoms[0] = spawnWriteCustom;
                return;
            case "SpawnIniValueCheck":
                spawnIniValueCheck = Conversions.BooleanFromString(value, false);
                spawnIniValueChecks[0] = spawnIniValueCheck;
                return;
            case "DefaultIndex":
                SelectedIndex = int.Parse(value);
                defaultIndex = SelectedIndex;
                return;
            case "OptionName":
                OptionName = Localize(this, "OptionName", value);
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
            case "SortOrder":
                SortOrder = int.Parse(value);
                return;
        }

        // spawn INI indexed attributes
        idx = ParseSuffix(key, "SpawnIniOption");
        if (idx >= 0)
        {
            if (!spawnIniEntries.TryGetValue(idx, out var entry))
            {
                entry = new SpawnIniEntry();
                spawnIniEntries[idx] = entry;
            }
            entry.Option = value;
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

        idx = ParseSuffix(key, "SpawnWriteCustom");
        if (idx >= 0)
        {
            spawnWriteCustoms[idx] = Conversions.BooleanFromString(value, false);
            if (idx == 0)
                spawnWriteCustom = spawnWriteCustoms[idx];
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
        get => SelectedIndex;
        set => SelectedIndex = value;
    }

    public void ApplySpawnIniCode(IniFile spawnIni)
    {
        if (!AffectsSpawnIni)
            return;

        if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
            return;

        // If a custom item is selected, write its custom value
        if (IsCustomItemIndex(SelectedIndex))
        {
            int slot = CustomSlotFromIndex(SelectedIndex);
            string cv = customValues[slot];

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

                    if (ShouldSkipEmptySpawnIniValue(kvp.Key, cv))
                        continue;

                    string project = string.IsNullOrEmpty(entry.Project) ? spawnIniProject : entry.Project;
                    spawnIni.SetStringValue(project, entry.Option, cv);
                }

                return;
            }

            if (String.IsNullOrEmpty(spawnIniOption))
            {
                Logger.Log("GameLobbyDropDown.WriteSpawnIniCode: " + Name + " has no associated spawn INI option!");
                return;
            }

            bool writeCustom0 = spawnWriteCustoms.TryGetValue(0, out var v0) ? v0 : spawnWriteCustom;
            if (writeCustom0)
                return;

            if (ShouldSkipEmptySpawnIniValue(0, cv))
                return;

            if (InputBoxDataMode == InputBoxDataMode.INTEGER)
            {
                if (int.TryParse(cv, out int intValue))
                    spawnIni.SetIntValue(spawnIniProject, spawnIniOption, intValue);
                else
                    spawnIni.SetStringValue(spawnIniProject, spawnIniOption, cv);
            }
            else
            {
                spawnIni.SetStringValue(spawnIniProject, spawnIniOption, cv);
            }

            return;
        }

        // if we have indexed spawn ini entries, write the ones configured
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

                string tag = Items[SelectedIndex].Tag?.ToString() ?? "";
                if (ShouldSkipEmptySpawnIniValue(kvp.Key, tag))
                    continue;

                string project = string.IsNullOrEmpty(entry.Project) ? spawnIniProject : entry.Project;
                spawnIni.SetStringValue(project, entry.Option, tag);
            }

            return;

        if (String.IsNullOrEmpty(spawnIniOption))
        {
            Logger.Log("GameLobbyDropDown.WriteSpawnIniCode: " + Name + " has no associated spawn INI option!");
            return;
        }

        bool writeCustomLegacy = spawnWriteCustoms.TryGetValue(0, out var vLegacy) ? vLegacy : spawnWriteCustom;
        if (writeCustomLegacy)
            return;

        switch (dataWriteMode)
        {
            case DropDownDataWriteMode.BOOLEAN:
                spawnIni.SetBooleanValue("Settings", spawnIniOption, SelectedIndex > 0);
                break;
            case DropDownDataWriteMode.INDEX:
                spawnIni.SetIntValue("Settings", spawnIniOption, SelectedIndex);
                break;
            default:
            case DropDownDataWriteMode.STRING:
                string tag = Items[SelectedIndex].Tag.ToString();
                if (ShouldSkipEmptySpawnIniValue(0, tag))
                    return;
                spawnIni.SetStringValue(spawnIniProject, spawnIniOption, tag);
                break;
        }
    }

    public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
    {
        if (!AffectsMapCode || SelectedIndex < 0 || SelectedIndex >= Items.Count) return;

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

                string tag = IsCustomItemIndex(SelectedIndex)
                    ? customValues[CustomSlotFromIndex(SelectedIndex)]
                    : (Items[SelectedIndex].Tag?.ToString() ?? "");
                if (ShouldSkipEmptySpawnIniValue(kvp.Key, tag))
                    continue;

                string project = string.IsNullOrEmpty(entry.Project) ? spawnIniProject : entry.Project;
                mapIni.SetStringValue(project, entry.Option, tag);
            }
        }
        else if (!string.IsNullOrEmpty(spawnIniOption))
        {
            bool writeCustom = spawnWriteCustoms.TryGetValue(0, out var v0) ? v0 : spawnWriteCustom;
            if (writeCustom)
            {
                if (IsCustomItemIndex(SelectedIndex))
                {
                    string cv = customValues[CustomSlotFromIndex(SelectedIndex)];
                    if (ShouldSkipEmptySpawnIniValue(0, cv))
                        return;

                    if (InputBoxDataMode == InputBoxDataMode.INTEGER)
                    {
                        if (int.TryParse(cv, out int intValue))
                            mapIni.SetIntValue(spawnIniProject, spawnIniOption, intValue);
                        else
                            mapIni.SetStringValue(spawnIniProject, spawnIniOption, cv);
                    }
                    else
                    {
                        mapIni.SetStringValue(spawnIniProject, spawnIniOption, cv);
                    }
                }
                else
                {
                    switch (dataWriteMode)
                    {
                        case DropDownDataWriteMode.BOOLEAN:
                            mapIni.SetBooleanValue(spawnIniProject, spawnIniOption, SelectedIndex > 0);
                            break;
                        case DropDownDataWriteMode.INDEX:
                            mapIni.SetIntValue(spawnIniProject, spawnIniOption, SelectedIndex);
                            break;
                        default:
                        case DropDownDataWriteMode.STRING:
                            string tag = Items[SelectedIndex].Tag.ToString();
                            if (ShouldSkipEmptySpawnIniValue(0, tag))
                                return;
                            mapIni.SetStringValue(spawnIniProject, spawnIniOption, tag);
                            break;
                    }
                }

                return;
            }
        }

        // 原有 MAPCODE 逻辑
        if (dataWriteMode == DropDownDataWriteMode.MAPCODE && !IsCustomItemIndex(SelectedIndex))
        {
            string customIniPath = Items[SelectedIndex].Tag.ToString();
            MapCodeHelper.ApplyMapCode(mapIni, customIniPath, gameMode);
        }
    }

    private bool ShouldSkipEmptySpawnIniValue(int index, string value)
    {
        bool check = spawnIniValueChecks.TryGetValue(index, out var v) ? v : spawnIniValueCheck;
        return check && string.IsNullOrWhiteSpace(value);
    }

    public override void OnLeftClick(InputEventArgs inputEventArgs)
    {
        // FIXME there's a discrepancy with how base XNAUI handles this
        // it doesn't set handled if changing the setting is not allowed
        inputEventArgs.Handled = true;
            
        if (!AllowDropDown)
            return;

        base.OnLeftClick(inputEventArgs);
    }
}