using System;
using System.Collections.Generic;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using ClientCore;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    public class PlayerAIQuickOptionsPanel : XNAPanel
    {
        private const int ddHeight = 21;
        private const int ddWidth = 110;
        private const int chkBoxWidth = 18;
        private const int panelWidth = 250;
        private const int sideMargin = 27;
        private const int topMargin = 20;
        private const int labelX = sideMargin;
        private const int ddX = 85;
        private const int chkX = ddX + ddWidth + 16;
        private const int rowSpacing = 14;
        private const int titleToFirstRow = 18;
        private const int autoAssignAboveBtn = 6;
        private const int defaultX = sideMargin;

        private bool _isHost;
        private bool _suppressEvents;

        public EventHandler AddAIRequested;
        public EventHandler RemoveAIRequested;
        public EventHandler FillAllAIRequested;
        public EventHandler RemoveAllAIRequested;
        public EventHandler OptionsChanged;

        private XNAClientButton btnAddAIQuick;
        private XNAClientButton btnRemoveAIQuick;
        private XNAClientButton btnAIQuickFillAll;
        private XNAClientButton btnAIQuickRemoveAll;
        private XNAClientDropDown cmbAIQuickDifficultyLevel;
        private XNAClientDropDown cmbAIQuickSide;
        private XNAClientDropDown cmbAIQuickColor;
        private XNAClientDropDown cmbAIQuickTeam;
        private XNAClientCheckBox chkAutoAssignAIStarts;
        private XNAClientCheckBox chkRandomAIDifficulty;
        private XNAClientCheckBox chkRandomAISide;
        private XNAClientCheckBox chkRandomAIColor;
        private XNAClientCheckBox chkRandomAITeam;

        private List<MultiplayerColor> MPColors;

        public PlayerAIQuickOptionsPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public int AIDifficultyLevel => cmbAIQuickDifficultyLevel?.SelectedIndex ?? 2;
        public int AISideIndex => cmbAIQuickSide?.SelectedIndex ?? 0;
        public int AIColorIndex => cmbAIQuickColor?.SelectedIndex ?? 0;
        public int AITeamId => cmbAIQuickTeam?.SelectedIndex ?? 0;
        public bool AutoAssignAIStarts => chkAutoAssignAIStarts?.Checked ?? false;
        public bool RandomAIDifficulty => chkRandomAIDifficulty?.Checked ?? false;
        public bool RandomAISide => chkRandomAISide?.Checked ?? false;
        public bool RandomAIColor => chkRandomAIColor?.Checked ?? false;
        public bool RandomAITeam => chkRandomAITeam?.Checked ?? false;

        public int SideItemCount => cmbAIQuickSide?.Items.Count ?? 0;
        public int ColorItemCount => cmbAIQuickColor?.Items.Count ?? 0;
        public int TeamItemCount => cmbAIQuickTeam?.Items.Count ?? 0;

        public override void Initialize()
        {
            Name = nameof(PlayerAIQuickOptionsPanel);
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
            lblHeader.Text = "AI Quick Options".L10N("Client:Main:AIQuickOptions");
            lblHeader.ClientRectangle = new Rectangle(defaultX, topMargin, 0, 18);
            AddChild(lblHeader);

            // Random column header
            var lblRandomHeader = new XNALabel(WindowManager);
            lblRandomHeader.Name = "lblRandomHeader";
            lblRandomHeader.Text = "Random".L10N("Client:Main:Random");
            lblRandomHeader.ClientRectangle = new Rectangle(chkX - 12, lblHeader.Y, 0, 0);
            AddChild(lblRandomHeader);

            int rowY = lblHeader.Bottom + titleToFirstRow;

            // === Row 1: Difficulty ===
            cmbAIQuickDifficultyLevel = AddDropdownRow(ref rowY, "lblDifficulty",
                "Difficulty:".L10N("Client:Main:AIDifficultyLabel"), nameof(cmbAIQuickDifficultyLevel),
                new[] {
                    "Easy".L10N("Client:Main:AIDifficultyEasy"),
                    "Medium".L10N("Client:Main:AIDifficultyMedium"),
                    "Hard".L10N("Client:Main:AIDifficultyHard")
                }, 2, out chkRandomAIDifficulty);

            // === Row 2: Side ===
            string[] sides = ClientConfiguration.Instance.Sides.Split(',');
            cmbAIQuickSide = AddDropdownRow(ref rowY, "lblSide",
                "Side:".L10N("Client:Main:AISideLabel"), nameof(cmbAIQuickSide),
                BuildSideItems(sides), 0, out chkRandomAISide);

            // === Row 3: Color ===
            cmbAIQuickColor = AddDropdownRow(ref rowY, "lblColor",
                "Color:".L10N("Client:Main:AIColorLabel"), nameof(cmbAIQuickColor),
                BuildColorItems(), 0, out chkRandomAIColor);

            // === Row 4: Team ===
            cmbAIQuickTeam = AddDropdownRow(ref rowY, "lblTeam",
                "Team:".L10N("Client:Main:AITeamLabel"), nameof(cmbAIQuickTeam),
                BuildTeamItems(), 0, out chkRandomAITeam);

            // === Row 5: Auto-assign starts checkbox ===
            rowY += rowSpacing;
            chkAutoAssignAIStarts = new XNAClientCheckBox(WindowManager);
            chkAutoAssignAIStarts.Name = nameof(chkAutoAssignAIStarts);
            chkAutoAssignAIStarts.Text = "Auto Assign Starts".L10N("Client:Main:AutoAssignAIStarts");
            chkAutoAssignAIStarts.ClientRectangle = new Rectangle(defaultX, rowY, 0, 0);
            chkAutoAssignAIStarts.Checked = false;
            AddChild(chkAutoAssignAIStarts);

            // === Row 6: Add AI / Remove AI ===
            rowY += chkAutoAssignAIStarts.Height + autoAssignAboveBtn + rowSpacing;
            const int btnWidth = 90;
            const int btnSpacing = 12;

            btnAddAIQuick = CreateButton(nameof(btnAddAIQuick),
                "Add AI".L10N("Client:Main:AddAIQuick"), defaultX, rowY, btnWidth);
            btnAddAIQuick.LeftClick += (s, a) => AddAIRequested?.Invoke(this, EventArgs.Empty);

            btnRemoveAIQuick = CreateButton(nameof(btnRemoveAIQuick),
                "Remove AI".L10N("Client:Main:RemoveAIQuick"),
                btnAddAIQuick.Right + btnSpacing, rowY, btnWidth);
            btnRemoveAIQuick.LeftClick += (s, a) => RemoveAIRequested?.Invoke(this, EventArgs.Empty);

            // === Row 7: Fill All / Remove All ===
            rowY += ddHeight + rowSpacing;
            btnAIQuickFillAll = CreateButton(nameof(btnAIQuickFillAll),
                "Fill All".L10N("Client:Main:FillAllAIQuick"), defaultX, rowY, btnWidth);
            btnAIQuickFillAll.LeftClick += (s, a) => FillAllAIRequested?.Invoke(this, EventArgs.Empty);

            btnAIQuickRemoveAll = CreateButton(nameof(btnAIQuickRemoveAll),
                "Remove All".L10N("Client:Main:RemoveAllAIQuick"),
                btnAIQuickFillAll.Right + btnSpacing, rowY, btnWidth);
            btnAIQuickRemoveAll.LeftClick += (s, a) => RemoveAllAIRequested?.Invoke(this, EventArgs.Empty);

            // Raise OptionsChanged when any control changes
            cmbAIQuickDifficultyLevel.SelectedIndexChanged += (s, e) => { if (!_suppressEvents) OptionsChanged?.Invoke(this, EventArgs.Empty); };
            cmbAIQuickSide.SelectedIndexChanged += (s, e) => { if (!_suppressEvents) OptionsChanged?.Invoke(this, EventArgs.Empty); };
            cmbAIQuickColor.SelectedIndexChanged += (s, e) => { if (!_suppressEvents) OptionsChanged?.Invoke(this, EventArgs.Empty); };
            cmbAIQuickTeam.SelectedIndexChanged += (s, e) => { if (!_suppressEvents) OptionsChanged?.Invoke(this, EventArgs.Empty); };
            chkAutoAssignAIStarts.CheckedChanged += (s, e) => { if (!_suppressEvents) OptionsChanged?.Invoke(this, EventArgs.Empty); };
            chkRandomAIDifficulty.CheckedChanged += (s, e) => { if (!_suppressEvents) OptionsChanged?.Invoke(this, EventArgs.Empty); };
            chkRandomAISide.CheckedChanged += (s, e) => { if (!_suppressEvents) OptionsChanged?.Invoke(this, EventArgs.Empty); };
            chkRandomAIColor.CheckedChanged += (s, e) => { if (!_suppressEvents) OptionsChanged?.Invoke(this, EventArgs.Empty); };
            chkRandomAITeam.CheckedChanged += (s, e) => { if (!_suppressEvents) OptionsChanged?.Invoke(this, EventArgs.Empty); };

            base.Initialize();

            btnClose.ClientRectangle = new Rectangle(
                0,
                0,
                btnClose.Width,
                btnClose.Height);
        }

        /// <summary>
        /// Creates a single dropdown row: label + dropdown + random checkbox.
        /// Returns the dropdown; outputs the checkbox.
        /// </summary>
        private XNAClientDropDown AddDropdownRow(
            ref int rowY, string lblName, string lblText, string ddName,
            string[] items, int selectedIndex,
            out XNAClientCheckBox chk)
        {
            // Label (left-aligned)
            var lbl = new XNALabel(WindowManager);
            lbl.Name = lblName;
            lbl.Text = lblText;
            lbl.ClientRectangle = new Rectangle(labelX, rowY, 0, 0);
            AddChild(lbl);

            // Dropdown
            var dd = new XNAClientDropDown(WindowManager);
            dd.Name = ddName;
            dd.ClientRectangle = new Rectangle(ddX, rowY - 2, ddWidth, ddHeight);
            foreach (string item in items)
                dd.AddItem(item);
            dd.SelectedIndex = selectedIndex;
            AddChild(dd);

            // Random checkbox on the right
            chk = new XNAClientCheckBox(WindowManager);
            chk.Checked = false;
            chk.ClientRectangle = new Rectangle(chkX, rowY - 1, 0, 0);
            AddChild(chk);

            // When random checkbox is toggled, enable/disable the dropdown and hide its arrow
            XNAClientCheckBox chkRef = chk;
            chkRef.CheckedChanged += (sender, args) =>
            {
                bool disabled = chkRef.Checked;
                dd.InputEnabled = !disabled;
                dd.AllowDropDown = !disabled;
            };

            // Use the tallest control height + spacing for next row
            int rowHeight = Math.Max(ddHeight, chkRef.Height);
            rowY += rowHeight + rowSpacing;
            return dd;
        }

        private XNAClientButton CreateButton(string name, string text, int x, int y, int width)
        {
            var btn = new XNAClientButton(WindowManager);
            btn.Name = name;
            btn.Text = text;
            btn.ClientRectangle = new Rectangle(x, y, width, ddHeight);
            if (AssetLoader.AssetExists("92pxbtn.png"))
                btn.IdleTexture = AssetLoader.LoadTexture("92pxbtn.png");
            if (AssetLoader.AssetExists("92pxbtn_c.png"))
                btn.HoverTexture = AssetLoader.LoadTexture("92pxbtn_c.png");
            AddChild(btn);
            return btn;
        }

        private string[] BuildSideItems(string[] sides)
        {
            var list = new List<string> { "Random".L10N("Client:Sides:RandomSide") };
            foreach (string side in sides)
                list.Add(side.L10N($"INI:Sides:{side}"));
            return list.ToArray();
        }

        private string[] BuildColorItems()
        {
            var list = new List<string> { "Random".L10N("Client:Main:RandomColor") };
            if (MPColors != null)
            {
                foreach (MultiplayerColor mpColor in MPColors)
                    list.Add(mpColor.Name);
            }
            return list.ToArray();
        }

        private string[] BuildTeamItems()
        {
            var list = new List<string> { "-" };
            ProgramConstants.TEAMS.ForEach(list.Add);
            return list.ToArray();
        }

        public void SetMPColors(List<MultiplayerColor> colors)
        {
            MPColors = colors;

            if (cmbAIQuickColor != null && MPColors != null)
            {
                while (cmbAIQuickColor.Items.Count > 1)
                    cmbAIQuickColor.Items.RemoveAt(1);

                foreach (MultiplayerColor mpColor in MPColors)
                    cmbAIQuickColor.AddItem(mpColor.Name);

                cmbAIQuickColor.SelectedIndex = 0;
            }
        }

        public void SetIsHost(bool isHost)
        {
            _isHost = isHost;
            EnableControls(_isHost);
        }

        public void EnableControls(bool enable)
        {
            if (btnAddAIQuick != null) btnAddAIQuick.InputEnabled = enable;
            if (btnRemoveAIQuick != null) btnRemoveAIQuick.InputEnabled = enable;
            if (btnAIQuickFillAll != null) btnAIQuickFillAll.InputEnabled = enable;
            if (btnAIQuickRemoveAll != null) btnAIQuickRemoveAll.InputEnabled = enable;
            if (cmbAIQuickDifficultyLevel != null)
            {
                cmbAIQuickDifficultyLevel.InputEnabled = enable && !RandomAIDifficulty;
                cmbAIQuickDifficultyLevel.AllowDropDown = enable && !RandomAIDifficulty;
            }
            if (cmbAIQuickSide != null)
            {
                cmbAIQuickSide.InputEnabled = enable && !RandomAISide;
                cmbAIQuickSide.AllowDropDown = enable && !RandomAISide;
            }
            if (cmbAIQuickColor != null)
            {
                cmbAIQuickColor.InputEnabled = enable && !RandomAIColor;
                cmbAIQuickColor.AllowDropDown = enable && !RandomAIColor;
            }
            if (cmbAIQuickTeam != null)
            {
                cmbAIQuickTeam.InputEnabled = enable && !RandomAITeam;
                cmbAIQuickTeam.AllowDropDown = enable && !RandomAITeam;
            }
            if (chkAutoAssignAIStarts != null) chkAutoAssignAIStarts.InputEnabled = enable;
            if (chkRandomAIDifficulty != null) chkRandomAIDifficulty.InputEnabled = enable;
            if (chkRandomAISide != null) chkRandomAISide.InputEnabled = enable;
            if (chkRandomAIColor != null) chkRandomAIColor.InputEnabled = enable;
            if (chkRandomAITeam != null) chkRandomAITeam.InputEnabled = enable;
        }

        /// <summary>
        /// Loads default values from the [PlayerAIQuickOptions] section of GameOptions.ini.
        /// Dropdown values: 0-based index, -1 is treated as 0 (selects the first item).
        /// Checkbox values: Yes/No.
        /// </summary>
        public void LoadDefaults(IniFile ini)
        {
            const string section = "PlayerAIQuickOptions";

            _suppressEvents = true;

            chkRandomAIDifficulty.Checked = ini.GetBooleanValue(section, nameof(chkRandomAIDifficulty), false);
            chkRandomAISide.Checked = ini.GetBooleanValue(section, nameof(chkRandomAISide), false);
            chkRandomAIColor.Checked = ini.GetBooleanValue(section, nameof(chkRandomAIColor), false);
            chkRandomAITeam.Checked = ini.GetBooleanValue(section, nameof(chkRandomAITeam), false);
            chkAutoAssignAIStarts.Checked = ini.GetBooleanValue(section, nameof(chkAutoAssignAIStarts), false);

            int difficultyIdx = ini.GetIntValue(section, nameof(cmbAIQuickDifficultyLevel), 2);
            if (difficultyIdx == -1) difficultyIdx = 0;
            if (difficultyIdx >= 0 && difficultyIdx < cmbAIQuickDifficultyLevel.Items.Count)
                cmbAIQuickDifficultyLevel.SelectedIndex = difficultyIdx;

            int sideIdx = ini.GetIntValue(section, nameof(cmbAIQuickSide), 0);
            if (sideIdx == -1) sideIdx = 0;
            if (sideIdx >= 0 && sideIdx < cmbAIQuickSide.Items.Count)
                cmbAIQuickSide.SelectedIndex = sideIdx;

            int colorIdx = ini.GetIntValue(section, nameof(cmbAIQuickColor), 0);
            if (colorIdx == -1) colorIdx = 0;
            if (colorIdx >= 0 && colorIdx < cmbAIQuickColor.Items.Count)
                cmbAIQuickColor.SelectedIndex = colorIdx;

            int teamIdx = ini.GetIntValue(section, nameof(cmbAIQuickTeam), 0);
            if (teamIdx == -1) teamIdx = 0;
            if (teamIdx >= 0 && teamIdx < cmbAIQuickTeam.Items.Count)
                cmbAIQuickTeam.SelectedIndex = teamIdx;

            _suppressEvents = false;

            EnableControls(_isHost);
        }

        public PlayerAIQuickOptions GetAIQuickOptions()
        {
            return new PlayerAIQuickOptions
            {
                DifficultyLevel = AIDifficultyLevel,
                SideIndex = AISideIndex,
                ColorIndex = AIColorIndex,
                TeamId = AITeamId,
                RandomDifficulty = RandomAIDifficulty,
                RandomSide = RandomAISide,
                RandomColor = RandomAIColor,
                RandomTeam = RandomAITeam,
                AutoAssignStarts = AutoAssignAIStarts
            };
        }

        public void SetAIQuickOptions(PlayerAIQuickOptions options)
        {
            if (options == null) return;

            _suppressEvents = true;

            if (options.DifficultyLevel >= 0 && options.DifficultyLevel < cmbAIQuickDifficultyLevel.Items.Count)
                cmbAIQuickDifficultyLevel.SelectedIndex = options.DifficultyLevel;
            if (options.SideIndex >= 0 && options.SideIndex < cmbAIQuickSide.Items.Count)
                cmbAIQuickSide.SelectedIndex = options.SideIndex;
            if (options.ColorIndex >= 0 && options.ColorIndex < cmbAIQuickColor.Items.Count)
                cmbAIQuickColor.SelectedIndex = options.ColorIndex;
            if (options.TeamId >= 0 && options.TeamId < cmbAIQuickTeam.Items.Count)
                cmbAIQuickTeam.SelectedIndex = options.TeamId;

            chkRandomAIDifficulty.Checked = options.RandomDifficulty;
            chkRandomAISide.Checked = options.RandomSide;
            chkRandomAIColor.Checked = options.RandomColor;
            chkRandomAITeam.Checked = options.RandomTeam;
            chkAutoAssignAIStarts.Checked = options.AutoAssignStarts;

            _suppressEvents = false;

            EnableControls(_isHost);
        }
    }
}
