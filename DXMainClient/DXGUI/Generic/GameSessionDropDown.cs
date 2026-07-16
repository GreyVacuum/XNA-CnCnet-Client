using System;
using System.Collections.Generic;

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
    public bool AffectsSpawnIni => dataWriteMode != DropDownDataWriteMode.MAPCODE && (!string.IsNullOrWhiteSpace(spawnIniOption) || spawnIniEntries.Count > 0);
    public bool AffectsMapCode => dataWriteMode == DropDownDataWriteMode.MAPCODE;
    public bool AllowScoring => true;  // TODO

    private DropDownDataWriteMode dataWriteMode = DropDownDataWriteMode.BOOLEAN;

    private string spawnIniOption = string.Empty;

    private string spawnIniProject = "Settings";

    private int defaultIndex;

    public bool EnableRightInputBox { get; private set; } = false;

    public InputBoxDataMode InputBoxDataMode { get; private set; } = InputBoxDataMode.INTEGER;

    public bool InputBoxIntegerScroll { get; private set; } = true;

    public bool InputBoxIntegerScrollMouse { get; private set; } = true;

    public bool InputBoxIntegerScrollKeyBoard { get; private set; } = true;

    public int InputBoxIntegerScrollStep { get; private set; } = 1;

    public bool InputBoxIntegerAllowNegative { get; private set; } = false;

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
                return;
            case "InputBoxIntegerRange":
                InputBoxIntegerAllowNegative = value.Trim() == "-";
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

                string project = string.IsNullOrEmpty(entry.Project) ? spawnIniProject : entry.Project;

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
        if (IsCustomItemIndex(SelectedIndex)) return;

        string customIniPath;
        customIniPath = Items[SelectedIndex].Tag.ToString();

        MapCodeHelper.ApplyMapCode(mapIni, customIniPath, gameMode);
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
                    }

                    if (valid)
                    {
                        if (int.TryParse(text, out int intValue))
                        {
                            if (intValue < MinInputBoxInteger || intValue > MaxInputBoxInteger)
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
            // Clear the custom slot being edited
            if (editingCustomSlot >= 0 && editingCustomSlot < customValues.Length)
            {
                customValues[editingCustomSlot] = string.Empty;
                RefreshCustomItemText(editingCustomSlot);
            }
        }
        else
        {
            if (InputBoxDataMode == InputBoxDataMode.INTEGER)
            {
                if (!int.TryParse(text, out _))
                    text = "0";
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
            AdjustIntegerValue(Cursor.ScrollWheelValue > 0 ? 1 : -1);
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
                    HandleScrollKeyDown(gameTime, () => AdjustIntegerValue(1));
                }
                else if (Keyboard.IsKeyHeldDown(Keys.Down))
                {
                    HandleScrollKeyDown(gameTime, () => AdjustIntegerValue(-1));
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

    private void AdjustIntegerValue(int direction)
    {
        int currentValue = 0;
        if (!string.IsNullOrEmpty(inputTextBox.Text))
        {
            if (!int.TryParse(inputTextBox.Text, out currentValue))
                currentValue = 0;
        }

        int newValue = currentValue + direction * InputBoxIntegerScrollStep;
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