using System;
using System.Collections.Generic;

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
    public bool AffectsSpawnIni => dataWriteMode != DropDownDataWriteMode.MAPCODE && (!string.IsNullOrWhiteSpace(spawnIniOption) || spawnIniEntries.Count > 0);
    public bool AffectsMapCode => dataWriteMode == DropDownDataWriteMode.MAPCODE;
    public bool AllowScoring => true;  // TODO

    private DropDownDataWriteMode dataWriteMode = DropDownDataWriteMode.BOOLEAN;

    private string spawnIniOption = string.Empty;

    private string spawnIniProject = "Settings";

    private int defaultIndex;

    /// <summary>
    /// Per-index spawn INI entries for the dropdown control.
    /// Index 0 mirrors the base (no-suffix) values for compatibility.
    /// </summary>
    private readonly Dictionary<int, SpawnIniEntry> spawnIniEntries = new();

    private class SpawnIniEntry
    {
        public string Option;
        public string Project;
        public bool HasOption => !string.IsNullOrWhiteSpace(Option);
    }

    /// <summary>
    /// Per-index item tags/labels. Index entries override the global Items / ItemLabels when present.
    /// </summary>
    private readonly Dictionary<int, (string Tag, string Label)> indexedItems = new();

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
            case "Items":
                string[] items = value.SplitWithCleanup();
                string[] itemLabels = iniFile.GetStringListValue(Name, "ItemLabels", "");
                string[] iconNames = iniFile.GetStringListValue(Name, "Icons", "");
                // Build items, applying per-index overrides when present
                for (int i = 0; i < items.Length; i++)
                {
                    bool hasLabel = itemLabels.Length > i && !string.IsNullOrEmpty(itemLabels[i]);
                    string defaultTag = items[i];
                    string tag = indexedItems.ContainsKey(i) && !string.IsNullOrEmpty(indexedItems[i].Tag) ? indexedItems[i].Tag : defaultTag;
                    string defaultLabel = hasLabel ? itemLabels[i] : defaultTag;
                    string label = indexedItems.ContainsKey(i) && !string.IsNullOrEmpty(indexedItems[i].Label) ? indexedItems[i].Label : defaultLabel;
                    string iconName = iconNames.Length > i ? iconNames[i] : null;
                    XNADropDownItem item = new()
                    {
                        Text = Localize(this, $"Item{i}", label),
                        Tag = tag,
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

        // handle indexed items: keys like Item0, Item1 ...
        int idx = ParseSuffix(key, "Item");
        if (idx >= 0)
        {
            // store tag; label may be set by ItemLabelN
            var label = indexedItems.ContainsKey(idx) ? indexedItems[idx].Label : null;
            indexedItems[idx] = (Tag: value, Label: label);
            // if the Items list already contains an item at this index, update it
            if (Items.Count > idx)
            {
                string text = !string.IsNullOrEmpty(label) ? label : value;
                Items[idx].Tag = value;
                Items[idx].Text = Localize(this, $"Item{idx}", text);
            }
            return;
        }

        idx = ParseSuffix(key, "ItemLabel");
        if (idx >= 0)
        {
            var tag = indexedItems.ContainsKey(idx) ? indexedItems[idx].Tag : null;
            indexedItems[idx] = (Tag: tag, Label: value);
            if (Items.Count > idx)
            {
                string text = !string.IsNullOrEmpty(value) ? value : (Items[idx].Tag?.ToString() ?? "");
                Items[idx].Text = Localize(this, $"Item{idx}", text);
            }
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

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public int Value
    {
        get => SelectedIndex;
        set => SelectedIndex = value;
    }

    public void ApplySpawnIniCode(IniFile spawnIni)
    {
        if (!AffectsSpawnIni || SelectedIndex < 0 || SelectedIndex >= Items.Count)
            return;

        // if we have indexed spawn ini entries, write the ones configured
        if (spawnIniEntries.Count > 0)
        {
            foreach (var kvp in spawnIniEntries)
            {
                var entry = kvp.Value;
                if (!entry.HasOption)
                    continue;

                string project = string.IsNullOrEmpty(entry.Project) ? spawnIniProject : entry.Project;

                // For dropdown, we want to write the selected item's tag as string
                // Only write if the selected index corresponds to an item that should trigger this entry.
                // User requirement: "SpawnIniOption1 > 判断是否设置，保证下面需要运行"
                // Interpretation: if SpawnIniOptionN is present, always write it using the selected index tag.
                string tag = Items[SelectedIndex].Tag?.ToString() ?? "";
                spawnIni.SetStringValue(project, entry.Option, tag);
            }

            return;
        }

        // Legacy behavior: single spawnIniOption
        if (String.IsNullOrEmpty(spawnIniOption))
        {
            Logger.Log("GameLobbyDropDown.WriteSpawnIniCode: " + Name + " has no associated spawn INI option!");
            return;
        }

        switch (dataWriteMode)
        {
            case DropDownDataWriteMode.BOOLEAN:
                spawnIni.SetBooleanValue(spawnIniProject, spawnIniOption, SelectedIndex > 0);
                break;
            case DropDownDataWriteMode.INDEX:
                spawnIni.SetIntValue(spawnIniProject, spawnIniOption, SelectedIndex);
                break;
            default:
            case DropDownDataWriteMode.STRING:
                spawnIni.SetStringValue(spawnIniProject, spawnIniOption, Items[SelectedIndex].Tag.ToString());
                break;
        }
    }

    public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
    {
        if (!AffectsMapCode || SelectedIndex < 0 || SelectedIndex >= Items.Count) return;

        string customIniPath;
        customIniPath = Items[SelectedIndex].Tag.ToString();

        MapCodeHelper.ApplyMapCode(mapIni, customIniPath, gameMode);
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