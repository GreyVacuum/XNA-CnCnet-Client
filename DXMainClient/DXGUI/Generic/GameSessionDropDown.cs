using System;
using System.Collections.Generic;
using System.Linq;

using ClientCore.Extensions;
using ClientCore.I18N;

using ClientGUI;

using DTAClient.Domain.Multiplayer;
using DTAClient.DXGUI.Multiplayer.GameLobby;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic;

public enum InputBoxDataMode
{
    INTEGER,
    STRING
}

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

    public bool EnableRightInputBox { get; private set; } = false;

    public InputBoxDataMode InputBoxDataMode { get; private set; } = InputBoxDataMode.INTEGER;

    public bool InputBoxIntegerScroll { get; private set; } = true;

    public bool InputBoxIntegerScrollMouse { get; private set; } = true;

    public bool InputBoxIntegerScrollKeyBoard { get; private set; } = true;

    public int InputBoxIntegerScrollStep { get; private set; } = 1;

    public int InputBoxIntegerScrollMouseStep { get; private set; } = 1;

    public int InputBoxIntegerScrollKeyBoardStep { get; private set; } = 1;

    public bool InputBoxIntegerAllowNegative { get; private set; } = false;

    public bool InputBoxIntegerAllowPositive { get; private set; } = true;

    /// <summary>
    /// 是否在输入过程中强制检查 MinInputBoxInteger / MaxInputBoxInteger。
    /// 默认 false，允许中间值；设为 true 则输入过程中超出范围即被拒绝。
    /// </summary>
    public bool InputBoxIntegerStrict { get; private set; } = false;

    /// <summary>
    /// 是否在显示正整数时添加 + 前缀（如 *+10）。默认 false，显示为 *10。
    /// </summary>
    public bool InputBoxIntegerShowPositive { get; private set; } = false;

    /// <summary>
    /// 是否在显示负整数时添加 - 前缀（如 *-10）。默认 true。
    /// </summary>
    public bool InputBoxIntegerShowNegative { get; private set; } = true;

    public int MinInputBoxInteger { get; private set; } = int.MinValue;

    public int MaxInputBoxInteger { get; private set; } = int.MaxValue;

    public int InputBoxCustomItems { get; private set; } = 1;

    public string[] InputBoxCustomItemsLabels { get; private set; } = null;

    public string[] InputBoxCustomDefaultItems { get; private set; } = null;

    private bool isInputBoxMode = false;

    private XNATextBox inputTextBox;

    private bool wasInputBoxActive = false;

    private string[] customValues;

    private int customItemStartIndex = -1;

    private int editingCustomSlot = -1;

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
            case "EnableRightInputBox":
                EnableRightInputBox = Conversions.BooleanFromString(value, false);
                return;
            case "InputBoxDataMode":
                if (value.ToUpper() == "INTEGER")
                    InputBoxDataMode = InputBoxDataMode.INTEGER;
                else
                    InputBoxDataMode = InputBoxDataMode.STRING;
                return;
            case "InputBoxIntegerScroll":
                InputBoxIntegerScroll = Conversions.BooleanFromString(value, true);
                InputBoxIntegerScrollMouse = InputBoxIntegerScroll;
                InputBoxIntegerScrollKeyBoard = InputBoxIntegerScroll;
                return;
            case "InputBoxIntegerScroll.Mouse":
                InputBoxIntegerScrollMouse = Conversions.BooleanFromString(value, InputBoxIntegerScroll);
                return;
            case "InputBoxIntegerScroll.KeyBoard":
                InputBoxIntegerScrollKeyBoard = Conversions.BooleanFromString(value, InputBoxIntegerScroll);
                return;
            case "InputBoxIntegerScroll.Integer":
                InputBoxIntegerScrollStep = Conversions.IntFromString(value, 1);
                InputBoxIntegerScrollMouseStep = InputBoxIntegerScrollStep;
                InputBoxIntegerScrollKeyBoardStep = InputBoxIntegerScrollStep;
                return;
            case "InputBoxIntegerScroll.MouseInteger":
                InputBoxIntegerScrollMouseStep = Conversions.IntFromString(value, InputBoxIntegerScrollStep);
                return;
            case "InputBoxIntegerScroll.KeyBoardInteger":
                InputBoxIntegerScrollKeyBoardStep = Conversions.IntFromString(value, InputBoxIntegerScrollStep);
                return;
            case "InputBoxIntegerStrict":
                InputBoxIntegerStrict = Conversions.BooleanFromString(value, false);
                return;
            case "InputBoxIntegerRange":
                {
                    string trimmed = value.Trim();
                    bool hasNeg = trimmed.Contains('-');
                    bool hasPos = trimmed.Contains('+');
                    if (hasNeg && hasPos)
                    {
                        InputBoxIntegerAllowNegative = true;
                        InputBoxIntegerAllowPositive = true;
                    }
                    else if (hasNeg)
                    {
                        InputBoxIntegerAllowNegative = true;
                        InputBoxIntegerAllowPositive = false;
                    }
                    else if (hasPos)
                    {
                        InputBoxIntegerAllowNegative = false;
                        InputBoxIntegerAllowPositive = true;
                    }
                    else
                    {
                        // 未识别的值，保持默认：不允许负数，允许正数
                        InputBoxIntegerAllowNegative = false;
                        InputBoxIntegerAllowPositive = true;
                    }
                }
                return;
            case "InputBoxIntegerRangeShow.Positive":
                InputBoxIntegerShowPositive = Conversions.BooleanFromString(value, false);
                return;
            case "InputBoxIntegerRangeShow.Negative":
                InputBoxIntegerShowNegative = Conversions.BooleanFromString(value, true);
                return;
            case "MinInputBoxInteger":
                MinInputBoxInteger = Conversions.IntFromString(value, int.MinValue);
                return;
            case "MaxInputBoxInteger":
                MaxInputBoxInteger = Conversions.IntFromString(value, int.MaxValue);
                return;
            case "InputBoxCustomItems":
                InputBoxCustomItems = Conversions.IntFromString(value, 1);
                return;
            case "InputBoxCustomItemsLabels":
                InputBoxCustomItemsLabels = value.SplitWithCleanup();
                return;
            case "InputBoxCustomDefaultItems":
                InputBoxCustomDefaultItems = value.SplitWithCleanup();
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
        }

        // Legacy behavior: single spawnIniOption
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
                spawnIni.SetBooleanValue(spawnIniProject, spawnIniOption, SelectedIndex > 0);
                break;
            case DropDownDataWriteMode.INDEX:
                spawnIni.SetIntValue(spawnIniProject, spawnIniOption, SelectedIndex);
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

        // 若 SpawnWriteCustom 开启，将 SpawnIniEntries 写入 spawnmap.ini（自定义项也需要处理）
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

    public string CustomValue
    {
        get
        {
            if (customValues == null || customValues.Length == 0)
                return string.Empty;
            return string.Join("|", customValues);
        }
        set
        {
            if (customValues == null || customValues.Length == 0)
                return;

            string[] parts = (value ?? string.Empty).Split('|');
            for (int i = 0; i < customValues.Length; i++)
            {
                customValues[i] = i < parts.Length ? parts[i] : string.Empty;
                RefreshCustomItemText(i);
            }
        }
    }

    public bool UseCustomValue => IsCustomItemIndex(SelectedIndex);

    /// <summary>
    /// Returns the item index for a given custom slot.
    /// </summary>
    public int GetCustomItemIndex(int slot) => customItemStartIndex + slot;

    /// <summary>
    /// Whether the given item index is a custom item.
    /// </summary>
    public bool IsCustomItem(int index) => IsCustomItemIndex(index);

    /// <summary>
    /// The number of custom value slots.
    /// </summary>
    public int CustomSlotCount => customValues?.Length ?? 0;

    public event EventHandler CustomValueChanged;

    public override void Initialize()
    {
        base.Initialize();

        if (EnableRightInputBox)
        {
            inputTextBox = new XNATextBox(WindowManager)
            {
                Name = Name + "_InputBox",
                Visible = false,
                InputEnabled = false,
                FontIndex = FontIndex,
                MaximumTextLength = 32,
            };
            inputTextBox.Initialize();
            inputTextBox.EnterPressed += InputTextBox_EnterPressed;
            inputTextBox.TextChanged += InputTextBox_TextChanged;
            AddChild(inputTextBox);
            SyncInputBoxSize();

            int slotCount = Math.Max(1, InputBoxCustomItems);
            customValues = new string[slotCount];
            customItemStartIndex = Items.Count;
            for (int i = 0; i < slotCount; i++)
            {
                if (InputBoxCustomDefaultItems != null && i < InputBoxCustomDefaultItems.Length)
                    customValues[i] = InputBoxCustomDefaultItems[i];
                else
                    customValues[i] = string.Empty;
                XNADropDownItem customItem = new()
                {
                    Text = FormatCustomItemLabel(i, customValues[i]),
                    Tag = "__custom_" + i,
                };
                AddItem(customItem);
            }
        }
    }

    private string FormatCustomItemLabel(int slot, string value)
    {
        string label;
        if (InputBoxCustomItemsLabels != null && slot < InputBoxCustomItemsLabels.Length)
            label = InputBoxCustomItemsLabels[slot];
        else
            label = "* {0}";

        if (string.IsNullOrEmpty(value))
            return label.Replace("{0}", "").Trim();

        // INTEGER 模式下，根据 InputBoxIntegerShowPositive/Negative 决定是否显示符号前缀
        if (InputBoxDataMode == InputBoxDataMode.INTEGER && int.TryParse(value, out int intVal))
        {
            if (intVal > 0 && InputBoxIntegerShowPositive)
            {
                // 正数且需要显示 + 前缀
                return label.Replace("{0}", "+" + intVal.ToString());
            }
            else if (intVal < 0)
            {
                if (InputBoxIntegerShowNegative)
                {
                    // 负数且需要显示 - 前缀（保留原有的负号）
                    return label.Replace("{0}", intVal.ToString());
                }
                else
                {
                    // 负数但不显示 - 前缀（显示为绝对值）
                    return label.Replace("{0}", Math.Abs(intVal).ToString());
                }
            }
        }

        return label.Replace("{0}", value);
    }

    private bool IsCustomItemIndex(int index)
    {
        return customItemStartIndex >= 0 && index >= customItemStartIndex && index < Items.Count;
    }

    private int CustomSlotFromIndex(int index)
    {
        return index - customItemStartIndex;
    }

    private void RefreshCustomItemText(int slot)
    {
        if (slot < 0 || slot >= customValues.Length || customItemStartIndex < 0)
            return;
        int itemIndex = customItemStartIndex + slot;
        if (itemIndex < Items.Count)
            Items[itemIndex].Text = FormatCustomItemLabel(slot, customValues[slot]);
    }

    private void SyncInputBoxSize()
    {
        if (inputTextBox == null)
            return;

        inputTextBox.X = 0;
        inputTextBox.Y = 0;
        inputTextBox.Width = Width;
        inputTextBox.Height = Height;
    }

    protected override void OnClientRectangleUpdated()
    {
        base.OnClientRectangleUpdated();
        SyncInputBoxSize();
    }

    private void InputTextBox_TextChanged(object sender, EventArgs e)
        {
            if (InputBoxDataMode == InputBoxDataMode.INTEGER)
            {
                string text = inputTextBox.Text;
                bool valid = true;

                if (text.Length > 0)
                {
                    int startIndex = 0;
                    if (text[0] == '-')
                    {
                        if (!InputBoxIntegerAllowNegative)
                            valid = false;
                        else
                            startIndex = 1;
                    }
                    else if (text[0] == '+')
                    {
                        // 允许前导+号，跳过它
                        startIndex = 1;
                    }

                    if (valid)
                    {
                        for (int i = startIndex; i < text.Length; i++)
                        {
                            if (!char.IsDigit(text[i]))
                            {
                                valid = false;
                                break;
                            }
                        }
                    }

                    if (valid)
                    {
                        if (text.Length > startIndex)
                        {
                            if (text[startIndex] == '0' && text.Length > (startIndex + 1))
                                valid = false;
                        }
                        else if (startIndex > 0)
                        {
                            // 只有符号没有数字（如 "-" 或 "+"），允许继续输入
                        }
                    }

                    // MinInputBoxInteger / MaxInputBoxInteger 是否在输入过程中强制验证
                    // 取决于 InputBoxIntegerStrict。
                    if (valid && text.Length > startIndex)
                    {
                        if (int.TryParse(text, out int intValue))
                        {
                            // 检查正/负范围限制（硬性约束）
                            if (intValue > 0 && !InputBoxIntegerAllowPositive)
                                valid = false;

                            // 严格模式下，输入过程中也检查 Min/Max
                            if (InputBoxIntegerStrict && (intValue < MinInputBoxInteger || intValue > MaxInputBoxInteger))
                                valid = false;
                        }
                    }
                }

                if (!valid)
                {
                    inputTextBox.Text = previousValidText;
                    int pos = Math.Min(inputTextBox.Text.Length, inputTextBox.InputPosition);
                    inputTextBox.InputPosition = pos;
                    return;
                }

                previousValidText = inputTextBox.Text;
            }
        }

    private string previousValidText = string.Empty;

    private void InputTextBox_EnterPressed(object sender, EventArgs e)
    {
        ConfirmInputAndExit();
    }

    private void ConfirmInputAndExit()
    {
        string text = inputTextBox.Text.Trim();

        if (string.IsNullOrEmpty(text))
        {
            // 空输入：将最小值作为默认值。
            // 若禁用负数且最小值为负，则使用 0 作为下限。
            if (InputBoxDataMode == InputBoxDataMode.INTEGER && editingCustomSlot >= 0 && editingCustomSlot < customValues.Length)
            {
                int defaultValue = MinInputBoxInteger;
                if (!InputBoxIntegerAllowNegative && defaultValue < 0)
                    defaultValue = 0;
                if (defaultValue > MaxInputBoxInteger) defaultValue = MaxInputBoxInteger;
                text = defaultValue.ToString();
                inputTextBox.Text = text;
            }
            else if (editingCustomSlot >= 0 && editingCustomSlot < customValues.Length)
            {
                customValues[editingCustomSlot] = string.Empty;
                RefreshCustomItemText(editingCustomSlot);
            }
        }
        else
        {
            if (InputBoxDataMode == InputBoxDataMode.INTEGER)
            {
                if (!int.TryParse(text, out int intVal))
                {
                    // 解析失败：使用最小值作为默认值
                    int fallback = MinInputBoxInteger;
                    if (!InputBoxIntegerAllowNegative && fallback < 0)
                        fallback = 0;
                    if (fallback > MaxInputBoxInteger) fallback = MaxInputBoxInteger;
                    text = fallback.ToString();
                    inputTextBox.Text = text;
                }
                else
                {
                    // 确认时强制检查正/负限制
                    if (intVal > 0 && !InputBoxIntegerAllowPositive)
                    {
                        intVal = 0;
                        text = "0";
                        inputTextBox.Text = text;
                    }

                    // 最终值夹紧到 [MinInputBoxInteger, MaxInputBoxInteger]
                    if (intVal < MinInputBoxInteger)
                    {
                        intVal = MinInputBoxInteger;
                        text = intVal.ToString();
                        inputTextBox.Text = text;
                    }
                    else if (intVal > MaxInputBoxInteger)
                    {
                        intVal = MaxInputBoxInteger;
                        text = intVal.ToString();
                        inputTextBox.Text = text;
                    }
                }
            }

            if (editingCustomSlot >= 0 && editingCustomSlot < customValues.Length)
            {
                customValues[editingCustomSlot] = text;
                RefreshCustomItemText(editingCustomSlot);
                // Select the custom item that was just set
                SelectedIndex = customItemStartIndex + editingCustomSlot;
            }
        }

        editingCustomSlot = -1;
        SwitchToDropDownMode();
        CustomValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SwitchToInputBoxMode()
    {
        if (!EnableRightInputBox || isInputBoxMode)
            return;

        // Determine which custom slot to edit
        if (IsCustomItemIndex(SelectedIndex))
            editingCustomSlot = CustomSlotFromIndex(SelectedIndex);
        else
            editingCustomSlot = 0; // Default to first custom slot

        isInputBoxMode = true;
        AllowDropDown = false;

        // Pre-fill with current custom value or selected item's tag
        if (editingCustomSlot >= 0 && editingCustomSlot < customValues.Length && !string.IsNullOrEmpty(customValues[editingCustomSlot]))
            inputTextBox.Text = customValues[editingCustomSlot];
        else if (SelectedIndex >= 0 && SelectedIndex < Items.Count && !IsCustomItemIndex(SelectedIndex))
            inputTextBox.Text = Items[SelectedIndex].Tag?.ToString() ?? string.Empty;
        else
            inputTextBox.Text = string.Empty;

        previousValidText = inputTextBox.Text;
        inputTextBox.Visible = true;
        inputTextBox.InputEnabled = true;
        WindowManager.SelectedControl = inputTextBox;
        inputTextBox.InputPosition = inputTextBox.Text.Length;
    }

    private void SwitchToDropDownMode()
    {
        if (!isInputBoxMode)
            return;

        isInputBoxMode = false;
        AllowDropDown = true;
        inputTextBox.Visible = false;
        inputTextBox.InputEnabled = false;
    }

    public override void OnRightClick(InputEventArgs inputEventArgs)
    {
        inputEventArgs.Handled = true;

        if (!EnableRightInputBox)
        {
            base.OnRightClick(inputEventArgs);
            return;
        }

        if (!isInputBoxMode)
            SwitchToInputBoxMode();
        else
            ConfirmInputAndExit();
    }

    public override void OnMouseScrolled(InputEventArgs inputEventArgs)
    {
        if (isInputBoxMode && InputBoxDataMode == InputBoxDataMode.INTEGER && InputBoxIntegerScroll && InputBoxIntegerScrollMouse)
        {
            inputEventArgs.Handled = true;
            AdjustIntegerValue(Cursor.ScrollWheelValue > 0 ? 1 : -1, InputBoxIntegerScrollMouseStep);
            return;
        }

        base.OnMouseScrolled(inputEventArgs);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (isInputBoxMode && inputTextBox != null)
        {
            bool isInputBoxActive = WindowManager.SelectedControl == inputTextBox;

            if (wasInputBoxActive && !isInputBoxActive)
            {
                ConfirmInputAndExit();
            }

            wasInputBoxActive = isInputBoxActive;
        }

        if (isInputBoxMode && InputBoxDataMode == InputBoxDataMode.INTEGER && InputBoxIntegerScroll && InputBoxIntegerScrollKeyBoard)
        {
            if (WindowManager.SelectedControl == inputTextBox)
            {
                if (Keyboard.IsKeyHeldDown(Keys.Up))
                {
                    HandleScrollKeyDown(gameTime, () => AdjustIntegerValue(1, InputBoxIntegerScrollKeyBoardStep));
                }
                else if (Keyboard.IsKeyHeldDown(Keys.Down))
                {
                    HandleScrollKeyDown(gameTime, () => AdjustIntegerValue(-1, InputBoxIntegerScrollKeyBoardStep));
                }
                else
                {
                    isScrollingQuickly = false;
                    timeSinceLastScroll = TimeSpan.Zero;
                    scrollKeyTime = TimeSpan.Zero;
                }
            }
        }
    }

    private bool isScrollingQuickly = false;
    private TimeSpan timeSinceLastScroll = TimeSpan.Zero;
    private TimeSpan scrollKeyTime = TimeSpan.Zero;

    private void HandleScrollKeyDown(GameTime gameTime, Action action)
    {
        if (scrollKeyTime.Equals(TimeSpan.Zero))
            action();

        scrollKeyTime += gameTime.ElapsedGameTime;

        if (isScrollingQuickly)
        {
            timeSinceLastScroll += gameTime.ElapsedGameTime;

            if (timeSinceLastScroll > TimeSpan.FromSeconds(0.05))
            {
                timeSinceLastScroll = TimeSpan.Zero;
                action();
            }
        }

        if (scrollKeyTime > TimeSpan.FromSeconds(0.5) && !isScrollingQuickly)
        {
            isScrollingQuickly = true;
            timeSinceLastScroll = TimeSpan.Zero;
        }
    }

    private void AdjustIntegerValue(int direction, int step)
    {
        int currentValue = 0;
        if (!string.IsNullOrEmpty(inputTextBox.Text))
        {
            if (!int.TryParse(inputTextBox.Text, out currentValue))
                currentValue = 0;
        }

        int newValue = currentValue + direction * step;
        newValue = Math.Clamp(newValue, MinInputBoxInteger, MaxInputBoxInteger);
        inputTextBox.Text = newValue.ToString();
        inputTextBox.InputPosition = inputTextBox.Text.Length;
    }

    public override void OnLeftClick(InputEventArgs inputEventArgs)
    {
        // FIXME there's a discrepancy with how base XNAUI handles this
        // it doesn't set handled if changing the setting is not allowed
        inputEventArgs.Handled = true;

        if (isInputBoxMode)
            return;

        if (!AllowDropDown)
            return;

        base.OnLeftClick(inputEventArgs);
    }
}