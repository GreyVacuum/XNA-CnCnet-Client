using System;
using System.Collections.Generic;
using ClientCore;
using ClientGUI;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    internal class PlayerSlotsScrollPanel : XNAScrollPanel
    {
        public PlayerSlotsScrollPanel(WindowManager windowManager) : base(windowManager) { }
        public XNAPanel GetContentPanel() => ContentPanel;
    }

    public class PlayerNameOptionsPanel : XNAPanel
    {
        private const int ddHeight = 21;
        private const int sideMargin = 22;
        private const int topMargin = 20;
        private const int labelX = sideMargin;
        private const int inputX = 105;
        private const int inputWidth = 130;
        private const int rowSpacing = 14;
        private const int titleToFirstRow = 18;
        private const int panelWidth = 250;
        private const int defaultX = sideMargin;
        private const int maxOtherPlayers = 7;
        private const int slotHeight = 20;
        private const int slotPadding = 6;
        // Room reserved for the vertical scrollbar inside the player slots scroll panel.
        private const int scrollBarRoom = 20;

        private bool _isHost;
        private bool _hasReceivedHostState = false;

        public EventHandler OptionsChanged;

        private XNAClientCheckBox chkAllowCustomNames;
        private XNAClientCheckBox chkEnableCustomName;
        private XNATextBox tbCustomName;
        private XNALabel lblLobbyName;
        private XNALabel lblLobbyNameValue;
        private List<XNAPanel> otherPlayerSlots;
        private List<XNATextBox> otherPlayerTextBoxes;
        private PlayerSlotsScrollPanel playerSlotsScrollPanel;

        /// <summary>
        /// Stores custom names received from other players, keyed by their lobby name.
        /// </summary>
        private Dictionary<string, string> otherCustomNames = new Dictionary<string, string>();
        private Dictionary<string, bool> otherCustomNameEnabled = new Dictionary<string, bool>();

        public PlayerNameOptionsPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public bool AllowCustomNames => chkAllowCustomNames?.Checked ?? false;
        public bool IsCustomNameEnabled => chkEnableCustomName?.Checked ?? false;
        public string CustomName => tbCustomName?.Text ?? string.Empty;

        /// <summary>
        /// Whether the host's AllowCustomNames state has been received at least once.
        /// </summary>
        public bool HasReceivedHostState => _hasReceivedHostState;

        /// <summary>
        /// Returns the effective in-game name for the local player.
        /// Uses custom name if enabled and allowed, otherwise the lobby name.
        /// </summary>
        public string GetEffectiveLocalName()
        {
            if (AllowCustomNames && IsCustomNameEnabled && !string.IsNullOrEmpty(CustomName))
                return CustomName;
            return ProgramConstants.PLAYERNAME;
        }

        /// <summary>
        /// Returns the effective in-game name for another player.
        /// Uses their custom name if they have one enabled, otherwise their lobby name.
        /// </summary>
        public string GetEffectivePlayerName(string lobbyName)
        {
            if (AllowCustomNames &&
                otherCustomNameEnabled.TryGetValue(lobbyName, out bool enabled) && enabled &&
                otherCustomNames.TryGetValue(lobbyName, out string customName) &&
                !string.IsNullOrEmpty(customName))
                return customName;
            return lobbyName;
        }

        public override void Initialize()
        {
            Name = nameof(PlayerNameOptionsPanel);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            Visible = false;

            // Close button
            var btnClose = new XNAClientButton(WindowManager);
            btnClose.Name = "btnClose";
            btnClose.ClientRectangle = new Rectangle(0, 0, 0, 0);
            if (AssetLoader.AssetExists("optionsButtonClose.png"))
            {
                btnClose.IdleTexture = AssetLoader.LoadTexture("optionsButtonClose.png");
                btnClose.HoverTexture = AssetLoader.AssetExists("optionsButtonClose_c.png")
                    ? AssetLoader.LoadTexture("optionsButtonClose_c.png")
                    : null;
            }
            btnClose.LeftClick += (sender, args) => Disable();
            AddChild(btnClose);

            // Header label
            var lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = "lblHeader";
            lblHeader.Text = "Player Name Options".L10N("Client:Main:PlayerNameOptions");
            lblHeader.ClientRectangle = new Rectangle(defaultX, topMargin, 0, 18);
            AddChild(lblHeader);

            int rowY = lblHeader.Bottom + titleToFirstRow;

            // === Master switch (host only) ===
            chkAllowCustomNames = new XNAClientCheckBox(WindowManager);
            chkAllowCustomNames.Name = nameof(chkAllowCustomNames);
            chkAllowCustomNames.Text = "Allow Custom Names".L10N("Client:Main:AllowCustomNames");
            chkAllowCustomNames.ClientRectangle = new Rectangle(defaultX, rowY, 0, 0);
            chkAllowCustomNames.Checked = false;
            chkAllowCustomNames.CheckedChanged += (s, a) =>
            {
                UpdateEnableControls();
                OnOptionsChanged();
            };
            AddChild(chkAllowCustomNames);

            // === Local player section ===
            rowY += chkAllowCustomNames.Height + rowSpacing;

            lblLobbyName = new XNALabel(WindowManager);
            lblLobbyName.Name = nameof(lblLobbyName);
            lblLobbyName.Text = "Lobby Name:".L10N("Client:Main:LobbyNameLabel");
            lblLobbyName.ClientRectangle = new Rectangle(labelX, rowY, 0, 0);
            AddChild(lblLobbyName);

            lblLobbyNameValue = new XNALabel(WindowManager);
            lblLobbyNameValue.Name = nameof(lblLobbyNameValue);
            lblLobbyNameValue.Text = ProgramConstants.PLAYERNAME;
            lblLobbyNameValue.ClientRectangle = new Rectangle(inputX, rowY, 0, 0);
            AddChild(lblLobbyNameValue);

            // === Enable checkbox + input box ===
            rowY += ddHeight + rowSpacing;

            chkEnableCustomName = new XNAClientCheckBox(WindowManager);
            chkEnableCustomName.Name = nameof(chkEnableCustomName);
            chkEnableCustomName.Text = "Enable".L10N("Client:Main:EnableCustomName");
            chkEnableCustomName.ClientRectangle = new Rectangle(labelX, rowY, 0, 0);
            chkEnableCustomName.Checked = false;
            chkEnableCustomName.CheckedChanged += (s, a) =>
            {
                UpdateEnableControls();
                OnOptionsChanged();
            };
            AddChild(chkEnableCustomName);

            var lblNewName = new XNALabel(WindowManager);
            lblNewName.Name = "lblNewName";
            lblNewName.Text = "New Name:".L10N("Client:Main:NewNameLabel");
            lblNewName.ClientRectangle = new Rectangle(inputX, rowY, 0, 0);
            AddChild(lblNewName);

            rowY += ddHeight + rowSpacing;

            tbCustomName = new XNATextBox(WindowManager);
            tbCustomName.Name = nameof(tbCustomName);
            tbCustomName.MaximumTextLength = ClientConfiguration.Instance.MaxNameLength;
            tbCustomName.ClientRectangle = new Rectangle(inputX, rowY - 2, inputWidth, ddHeight);
            tbCustomName.Text = UserINISettings.Instance.SettingsIni.GetStringValue(
                UserINISettings.MULTIPLAYER, "CustomPlayerName", string.Empty);
            tbCustomName.InputEnabled = true;
            tbCustomName.TextChanged += (s, a) =>
            {
                UserINISettings.Instance.SettingsIni.SetStringValue(
                    UserINISettings.MULTIPLAYER, "CustomPlayerName", tbCustomName.Text);
                UserINISettings.Instance.SaveSettings();
                OnOptionsChanged();
            };
            AddChild(tbCustomName);

            // === Other players section ===
            rowY += ddHeight + rowSpacing + 6;

            var lblOtherHeader = new XNALabel(WindowManager);
            lblOtherHeader.Name = "lblOtherHeader";
            lblOtherHeader.Text = "Other Players:".L10N("Client:Main:OtherPlayersLabel");
            lblOtherHeader.ClientRectangle = new Rectangle(labelX, rowY, 0, 0);
            AddChild(lblOtherHeader);

            rowY += ddHeight + 4;

            // Create scroll panel for player slots
            const int scrollPanelHeight = 100; // Show ~4 slots at a time
            playerSlotsScrollPanel = new PlayerSlotsScrollPanel(WindowManager);
            playerSlotsScrollPanel.Name = "playerSlotsScrollPanel";
            playerSlotsScrollPanel.ClientRectangle = new Rectangle(
                labelX, rowY,
                panelWidth - sideMargin - labelX, scrollPanelHeight);
            playerSlotsScrollPanel.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 200), 1, 1);
            playerSlotsScrollPanel.DrawBorders = true;
            playerSlotsScrollPanel.BorderColor = new Color(80, 80, 100);
            // Enable both horizontal and vertical scrolling so long player names
            // can be revealed by scrolling horizontally instead of being clipped.
            playerSlotsScrollPanel.AllowScroll = (true, true);
            AddChild(playerSlotsScrollPanel);

            var contentPanel = playerSlotsScrollPanel.GetContentPanel();

            otherPlayerSlots = new List<XNAPanel>();
            otherPlayerTextBoxes = new List<XNATextBox>();
            int slotY = 0;
            int minSlotWidth = playerSlotsScrollPanel.Width - scrollBarRoom;
            for (int i = 0; i < maxOtherPlayers; i++)
            {
                var slot = new XNAPanel(WindowManager);
                slot.Name = "pnlOtherSlot" + (i + 1);
                slot.BackgroundTexture = AssetLoader.CreateTexture(new Color(30, 30, 40, 200), 1, 1);
                slot.DrawBorders = true;
                slot.BorderColor = new Color(80, 80, 100);
                slot.InputEnabled = false;
                slot.ClientRectangle = new Rectangle(
                    0, slotY,
                    minSlotWidth, slotHeight);
                slot.Visible = false;
                contentPanel.AddChild(slot);
                otherPlayerSlots.Add(slot);

                // Use XNATextBox instead of XNALabel for displaying long names
                var tb = new XNATextBox(WindowManager);
                tb.Name = "tbOther" + (i + 1);
                tb.Text = string.Empty;
                tb.ClientRectangle = new Rectangle(slotPadding, (slotHeight - ddHeight) / 2, slot.Width - slotPadding * 2, ddHeight);
                tb.InputEnabled = false; // Read-only
                tb.Visible = false;
                slot.AddChild(tb);
                otherPlayerTextBoxes.Add(tb);

                slotY += slotHeight + 4;
            }

            base.Initialize();

            // Set panel size so GetWindowRectangle() returns a non-zero rectangle,
            // otherwise the parent's GetChildOnCursor() never finds this panel
            // and none of its children receive input.
            Width = panelWidth;
            Height = rowY + scrollPanelHeight + sideMargin;

            btnClose.ClientRectangle = new Rectangle(
                0,
                0,
                btnClose.Width,
                btnClose.Height);

            UpdateEnableControls();
        }

        private void OnOptionsChanged()
        {
            OptionsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Updates the lobby name display for the local player.
        /// </summary>
        public void UpdateLobbyName()
        {
            if (lblLobbyNameValue != null)
                lblLobbyNameValue.Text = ProgramConstants.PLAYERNAME;
        }

        /// <summary>
        /// Updates the other players list display.
        /// </summary>
        public void UpdateOtherPlayers(List<string> playerNames)
        {
            if (otherPlayerTextBoxes == null || otherPlayerSlots == null)
                return;

            int idx = 0;
            foreach (string name in playerNames)
            {
                if (name == ProgramConstants.PLAYERNAME)
                    continue;
                if (idx >= maxOtherPlayers)
                    break;

                string displayText = name;
                if (otherCustomNameEnabled.TryGetValue(name, out bool enabled) && enabled &&
                    otherCustomNames.TryGetValue(name, out string customName) &&
                    !string.IsNullOrEmpty(customName))
                {
                    displayText = $"{name} -> {customName}";
                }

                otherPlayerTextBoxes[idx].Text = $"{idx + 1}. {displayText}";
                otherPlayerTextBoxes[idx].Visible = true;
                otherPlayerSlots[idx].Visible = true;
                idx++;
            }

            for (int i = idx; i < maxOtherPlayers; i++)
            {
                otherPlayerTextBoxes[i].Visible = false;
                otherPlayerTextBoxes[i].Text = string.Empty;
                otherPlayerSlots[i].Visible = false;
            }

            UpdateSlotWidths();
        }

        /// <summary>
        /// Resizes each visible player slot (and its inner text box) so that the
        /// full text is displayed without being clipped. When the resulting width
        /// exceeds the scroll panel's viewport, the horizontal scrollbar takes
        /// over so the content never visually overflows the panel.
        /// </summary>
        private void UpdateSlotWidths()
        {
            if (playerSlotsScrollPanel == null)
                return;

            // Minimum width keeps the slot filling the visible area when text is short.
            int minSlotWidth = playerSlotsScrollPanel.Width - scrollBarRoom;
            int textMargin = slotPadding * 2 + 4; // padding both sides + small extra buffer

            foreach (var slot in otherPlayerSlots)
            {
                int slotIndex = otherPlayerSlots.IndexOf(slot);
                var tb = otherPlayerTextBoxes[slotIndex];

                int desiredWidth = minSlotWidth;
                if (slot.Visible && !string.IsNullOrEmpty(tb.Text))
                {
                    int textWidth = (int)Renderer.GetTextDimensions(tb.Text, tb.FontIndex).X;
                    desiredWidth = Math.Max(minSlotWidth, textWidth + textMargin);
                }

                if (slot.Width != desiredWidth)
                {
                    slot.ClientRectangle = new Rectangle(
                        slot.X, slot.Y, desiredWidth, slot.Height);
                    tb.ClientRectangle = new Rectangle(
                        slotPadding, (slot.Height - ddHeight) / 2,
                        slot.Width - slotPadding * 2, ddHeight);
                }
            }
        }

        /// <summary>
        /// Stores a received custom name from another player.
        /// </summary>
        public void SetOtherCustomName(string lobbyName, bool enabled, string customName)
        {
            otherCustomNameEnabled[lobbyName] = enabled;
            otherCustomNames[lobbyName] = customName;
        }

        /// <summary>
        /// Clears all received custom names (used when the player list changes).
        /// </summary>
        public void ClearOtherCustomNames()
        {
            otherCustomNames.Clear();
            otherCustomNameEnabled.Clear();
        }

        /// <summary>
<<<<<<< HEAD
=======
        /// Resets the panel state when leaving a game lobby.
        /// </summary>
        public void Reset()
        {
            ClearOtherCustomNames();
            _hasReceivedHostState = false;
        }

        /// <summary>
>>>>>>> develop
        /// Returns all stored other-player custom name states.
        /// Used by the host to relay existing members' states to a newly joined member.
        /// </summary>
        public List<(string Name, bool Enabled, string CustomName)> GetOtherPlayerStates()
        {
            var result = new List<(string, bool, string)>();
            foreach (var kvp in otherCustomNames)
            {
                string name = kvp.Key;
                string customName = kvp.Value;
                bool enabled = otherCustomNameEnabled.TryGetValue(name, out bool e) && e;
                result.Add((name, enabled, customName));
            }
            return result;
        }

        /// <summary>
        /// Serializes the local player's options to a message string for broadcasting.
        /// </summary>
        public string ToMessage()
        {
            // Format: <allowCustomNames>;<isEnabled>;<customName>
            // allowCustomNames is only meaningful from host
            return $"{(AllowCustomNames ? "1" : "0")};{(IsCustomNameEnabled ? "1" : "0")};{CustomName}";
        }

        /// <summary>
        /// Applies a received message to update state.
        /// If sender is host, updates the master switch.
        /// Always updates the sender's custom name info.
        /// </summary>
        public void ApplyMessage(string sender, bool isHost, string message)
        {
            var parts = message.Split(';');
            if (parts.Length < 3)
                return;

            bool allowCustom = parts[0] == "1";
            bool enabled = parts[1] == "1";
            string customName = parts[2];

            if (isHost)
            {
                _hasReceivedHostState = true;
                if (chkAllowCustomNames != null && chkAllowCustomNames.Checked != allowCustom)
                {
                    chkAllowCustomNames.Checked = allowCustom;
                    UpdateEnableControls();
                }
            }

            if (sender != ProgramConstants.PLAYERNAME)
            {
                SetOtherCustomName(sender, enabled, customName);
            }
        }

        public void SetIsHost(bool isHost)
        {
            _isHost = isHost;
            UpdateEnableControls();
        }

        /// <summary>
        /// Updates AllowChecking/InputEnabled for chkAllowCustomNames (host only),
        /// chkEnableCustomName (when AllowCustomNames is on),
        /// and tbCustomName (when both AllowCustomNames and IsCustomNameEnabled are on).
        /// </summary>
        public void UpdateEnableControls()
        {
            if (chkAllowCustomNames != null)
            {
                chkAllowCustomNames.AllowChecking = _isHost;
                chkAllowCustomNames.InputEnabled = _isHost;
            }

            bool allowCustom = AllowCustomNames;
            if (chkEnableCustomName != null)
            {
                chkEnableCustomName.AllowChecking = allowCustom;
                chkEnableCustomName.InputEnabled = allowCustom;
            }

            if (tbCustomName != null)
                tbCustomName.InputEnabled = true;
        }

        /// <summary>
        /// Loads default values from the [PlayerNameOptions] section of GameOptions.ini.
        /// Checkbox values: Yes/No.
        /// </summary>
        public void LoadDefaults(IniFile ini)
        {
            const string section = "PlayerNameOptions";

            chkAllowCustomNames.Checked = ini.GetBooleanValue(section, nameof(chkAllowCustomNames), false);
            chkEnableCustomName.Checked = ini.GetBooleanValue(section, nameof(chkEnableCustomName), false);

            UpdateEnableControls();
        }
    }
}
