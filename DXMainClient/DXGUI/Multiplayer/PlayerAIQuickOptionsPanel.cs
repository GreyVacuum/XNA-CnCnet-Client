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
        private const int tabButtonWidth = 92;
        private const int formatPainterBtnWidth = 75;
        private const int maxAIPlayers = 8;

        private bool _isHost;
        private bool _suppressEvents;
        private int _sideSelectorCount;
        private bool[] _fpDefaultChecked = new bool[maxAIPlayers];

        public bool SideRandomSelectionEnabled { get; private set; } = true;
        public bool SideSelectorsRandomSelectionEnabled { get; private set; } = true;
        public bool SideRandomItemEnabled { get; private set; } = true;
        public bool ColorRandomSelectionEnabled { get; private set; } = true;
        public bool ColorRandomItemEnabled { get; private set; } = true;

        public EventHandler AddAIRequested;
        public EventHandler RemoveAIRequested;
        public EventHandler FillAllAIRequested;
        public EventHandler RemoveAllAIRequested;
        public EventHandler OptionsChanged;
        public EventHandler<List<int>> FormatPainterApplyRequested;
        public EventHandler ResetRequested;

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
        private XNAClientButton btnResetQuickOptions;

        private XNAClientTabControl tabControl;
        private XNAPanel pnlQuickActions;
        private XNAPanel pnlFormatPainter;

        private XNAClientButton btnFPConfirm;
        private XNAClientButton bpFPSelect;
        private XNAClientButton btnFPCancel;
        private List<XNAClientCheckBox> chkFPPlayers;

        private List<MultiplayerColor> MPColors;

        public PlayerAIQuickOptionsPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public int AIDifficultyLevel => cmbAIQuickDifficultyLevel == null ? 2 : (cmbAIQuickDifficultyLevel.SelectedIndex == 0 ? -1 : cmbAIQuickDifficultyLevel.SelectedIndex - 1);
        public int AISideIndex => cmbAIQuickSide == null ? -1 : (cmbAIQuickSide.SelectedIndex == 0 ? -1 : cmbAIQuickSide.SelectedIndex - 1);
        public int AIColorIndex => cmbAIQuickColor == null ? 0 : (cmbAIQuickColor.SelectedIndex == 0 ? -1 : cmbAIQuickColor.SelectedIndex - 1);
        public int AITeamId => cmbAIQuickTeam == null ? 0 : (cmbAIQuickTeam.SelectedIndex == 0 ? -1 : cmbAIQuickTeam.SelectedIndex - 1);
        public bool AutoAssignAIStarts => chkAutoAssignAIStarts?.Checked ?? false;
        public bool RandomAIDifficulty => chkRandomAIDifficulty?.Checked ?? false;
        public bool RandomAISide => chkRandomAISide?.Checked ?? false;
        public bool RandomAIColor => chkRandomAIColor?.Checked ?? false;
        public bool RandomAITeam => chkRandomAITeam?.Checked ?? false;

        public int DifficultyItemCount => cmbAIQuickDifficultyLevel?.Items.Count ?? 0;
        public int SideItemCount => cmbAIQuickSide?.Items.Count ?? 0;
        public int ColorItemCount => cmbAIQuickColor?.Items.Count ?? 0;
        public int TeamItemCount => cmbAIQuickTeam?.Items.Count ?? 0;

        public override void Initialize()
        {
            Name = nameof(PlayerAIQuickOptionsPanel);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            Visible = false;

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

            var lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = "lblHeader";
            lblHeader.Text = "AI Quick Options".L10N("Client:Main:AIQuickOptions");
            lblHeader.ClientRectangle = new Rectangle(defaultX, topMargin, 0, 18);
            AddChild(lblHeader);

            var lblRandomHeader = new XNALabel(WindowManager);
            lblRandomHeader.Name = "lblRandomHeader";
            lblRandomHeader.Text = "Random".L10N("Client:Main:Random");
            lblRandomHeader.ClientRectangle = new Rectangle(chkX - 12, lblHeader.Y, 0, 0);
            AddChild(lblRandomHeader);

            int rowY = lblHeader.Bottom + titleToFirstRow;

            cmbAIQuickDifficultyLevel = AddDropdownRow(ref rowY, "lblDifficulty",
                "Difficulty:".L10N("Client:Main:AIDifficultyLabel"), nameof(cmbAIQuickDifficultyLevel),
                new[] {
                    "Don't Set".L10N("Client:Main:AIDifficultyDontSet"),
                    "Easy".L10N("Client:Main:AIDifficultyEasy"),
                    "Medium".L10N("Client:Main:AIDifficultyMedium"),
                    "Hard".L10N("Client:Main:AIDifficultyHard")
                }, 3, out chkRandomAIDifficulty);

            cmbAIQuickSide = AddDropdownRow(ref rowY, "lblSide",
                "Side:".L10N("Client:Main:AISideLabel"), nameof(cmbAIQuickSide),
                BuildSideItems(), 1, out chkRandomAISide);

            cmbAIQuickColor = AddDropdownRow(ref rowY, "lblColor",
                "Color:".L10N("Client:Main:AIColorLabel"), nameof(cmbAIQuickColor),
                BuildColorItems(), 1, out chkRandomAIColor);

            cmbAIQuickTeam = AddDropdownRow(ref rowY, "lblTeam",
                "Team:".L10N("Client:Main:AITeamLabel"), nameof(cmbAIQuickTeam),
                BuildTeamItems(), 1, out chkRandomAITeam);

            rowY += rowSpacing;
            chkAutoAssignAIStarts = new XNAClientCheckBox(WindowManager);
            chkAutoAssignAIStarts.Name = nameof(chkAutoAssignAIStarts);
            chkAutoAssignAIStarts.Text = "Auto Assign Starts".L10N("Client:Main:AutoAssignAIStarts");
            chkAutoAssignAIStarts.ClientRectangle = new Rectangle(defaultX, rowY, 0, 0);
            chkAutoAssignAIStarts.Checked = false;
            AddChild(chkAutoAssignAIStarts);

            int resetBtnWidth = 60;
            int resetBtnRightMargin = 14;
            int resetBtnX = panelWidth - resetBtnRightMargin - resetBtnWidth;
            btnResetQuickOptions = new XNAClientButton(WindowManager);
            btnResetQuickOptions.Name = "btnResetQuickOptions";
            btnResetQuickOptions.Text = "Reset".L10N("Client:Main:Reset");
            btnResetQuickOptions.ClientRectangle = new Rectangle(resetBtnX, rowY - 2, resetBtnWidth, ddHeight);
            if (AssetLoader.AssetExists("75pxbtn.png"))
                btnResetQuickOptions.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            if (AssetLoader.AssetExists("75pxbtn_c.png"))
                btnResetQuickOptions.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            btnResetQuickOptions.LeftClick += BtnResetQuickOptions_LeftClick;
            AddChild(btnResetQuickOptions);

            rowY += chkAutoAssignAIStarts.Height + autoAssignAboveBtn + rowSpacing;

            int tabTotalWidth = tabButtonWidth * 2 + 4;
            int tabX = (panelWidth - tabTotalWidth) / 2;

            tabControl = new XNAClientTabControl(WindowManager);
            tabControl.Name = nameof(tabControl);
            tabControl.ClientRectangle = new Rectangle(tabX, rowY, 0, 23);
            tabControl.AddTab("Quick".L10N("Client:Main:AIQuickActionsTab"), tabButtonWidth);
            tabControl.AddTab("Format".L10N("Client:Main:AIFormatPainterTab"), tabButtonWidth);
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            AddChild(tabControl);

            rowY += tabControl.Height + 6;

            int contentY = rowY;
            int bottomMargin = 12;

            int estimatedContentHeight = Math.Max(
                ddHeight * 2 + rowSpacing + 20,
                ddHeight + 12 + (ddHeight + 6) * 2 + 16);
            int panelHeight = contentY + estimatedContentHeight + bottomMargin;
            ClientRectangle = new Rectangle(X, Y, panelWidth, panelHeight);

            int contentAreaHeight = panelHeight - contentY - bottomMargin;

            pnlQuickActions = new XNAPanel(WindowManager);
            pnlQuickActions.Name = nameof(pnlQuickActions);
            pnlQuickActions.ClientRectangle = new Rectangle(0, contentY, panelWidth, contentAreaHeight);
            pnlQuickActions.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 0), 1, 1);
            AddChild(pnlQuickActions);

            pnlFormatPainter = new XNAPanel(WindowManager);
            pnlFormatPainter.Name = nameof(pnlFormatPainter);
            pnlFormatPainter.ClientRectangle = new Rectangle(0, contentY, panelWidth, contentAreaHeight);
            pnlFormatPainter.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 0), 1, 1);
            pnlFormatPainter.Visible = false;
            AddChild(pnlFormatPainter);

            chkFPPlayers = new List<XNAClientCheckBox>();

            BuildQuickActionsPanel();
            BuildFormatPainterPanel();

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
        }

        private void BuildQuickActionsPanel()
        {
            const int btnWidth = 90;
            const int btnSpacing = 12;
            int pnlWidth = pnlQuickActions.Width;
            int pnlHeight = pnlQuickActions.Height;
            int totalWidth = btnWidth * 2 + btnSpacing;
            int totalHeight = ddHeight * 2 + rowSpacing;
            int startX = (pnlWidth - totalWidth) / 2;
            int startY = (pnlHeight - totalHeight) / 2;

            btnAddAIQuick = CreatePanelButton(pnlQuickActions, nameof(btnAddAIQuick),
                "Add AI".L10N("Client:Main:AddAIQuick"), startX, startY, btnWidth);
            btnAddAIQuick.LeftClick += (s, a) => AddAIRequested?.Invoke(this, EventArgs.Empty);

            btnAIQuickFillAll = CreatePanelButton(pnlQuickActions, nameof(btnAIQuickFillAll),
                "Fill All".L10N("Client:Main:FillAllAIQuick"), startX + btnWidth + btnSpacing, startY, btnWidth);
            btnAIQuickFillAll.LeftClick += (s, a) => FillAllAIRequested?.Invoke(this, EventArgs.Empty);

            int row2Y = startY + ddHeight + rowSpacing;

            btnRemoveAIQuick = CreatePanelButton(pnlQuickActions, nameof(btnRemoveAIQuick),
                "Remove AI".L10N("Client:Main:RemoveAIQuick"), startX, row2Y, btnWidth);
            btnRemoveAIQuick.LeftClick += (s, a) => RemoveAIRequested?.Invoke(this, EventArgs.Empty);

            btnAIQuickRemoveAll = CreatePanelButton(pnlQuickActions, nameof(btnAIQuickRemoveAll),
                "Remove All".L10N("Client:Main:RemoveAllAIQuick"), startX + btnWidth + btnSpacing, row2Y, btnWidth);
            btnAIQuickRemoveAll.LeftClick += (s, a) => RemoveAllAIRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BuildFormatPainterPanel()
        {
            int pnlWidth = pnlFormatPainter.Width;
            int pnlHeight = pnlFormatPainter.Height;
            int btnSpacing = 6;
            int btnTotalWidth = formatPainterBtnWidth * 3 + btnSpacing * 2;
            int btnStartX = (pnlWidth - btnTotalWidth) / 2;

            int contentStartY = 12;

            int btnY = contentStartY;

            btnFPConfirm = CreatePanelButton(pnlFormatPainter, nameof(btnFPConfirm),
                "Confirm".L10N("Client:Main:FormatPainterConfirm"), btnStartX, btnY, formatPainterBtnWidth, "75pxbtn");
            btnFPConfirm.LeftClick += BtnFPConfirm_LeftClick;

            bpFPSelect = CreatePanelButton(pnlFormatPainter, nameof(bpFPSelect),
                "Select All".L10N("Client:Main:FormatPainterSelectAll"), btnStartX + formatPainterBtnWidth + btnSpacing, btnY, formatPainterBtnWidth, "75pxbtn");
            bpFPSelect.LeftClick += BtnFPSelectAll_LeftClick;

            btnFPCancel = CreatePanelButton(pnlFormatPainter, nameof(btnFPCancel),
                "Deselect".L10N("Client:Main:FormatPainterDeselect"), btnStartX + (formatPainterBtnWidth + btnSpacing) * 2, btnY, formatPainterBtnWidth, "75pxbtn");
            btnFPCancel.LeftClick += BtnFPDeselectAll_LeftClick;
        }

        private void RebuildFormatPainterCheckboxes(int aiCount)
        {
            foreach (var chk in chkFPPlayers)
            {
                pnlFormatPainter.RemoveChild(chk);
                chk.Dispose();
            }
            chkFPPlayers.Clear();

            if (aiCount <= 0 || pnlFormatPainter == null)
                return;

            int pnlWidth = pnlFormatPainter.Width;
            int pnlHeight = pnlFormatPainter.Height;
            int chkPerRow = 4;
            int chkSpacing = 10;
            int chkColWidth = chkBoxWidth + 16 + chkSpacing;
            int visibleCols = Math.Min(aiCount, chkPerRow);
            int chkRowWidth = chkColWidth * visibleCols - chkSpacing;
            int chkStartX = (pnlWidth - chkRowWidth) / 2;

            int chkRows = (aiCount + chkPerRow - 1) / chkPerRow;
            int chkBlockHeight = chkRows * (ddHeight + 4) - 4;
            int chkStartY = ddHeight + 16; // below the button row

            for (int i = 0; i < aiCount; i++)
            {
                int row = i / chkPerRow;
                int col = i % chkPerRow;
                int chkXPos = chkStartX + col * chkColWidth;
                int chkYPos = chkStartY + row * (ddHeight + 4);

                var chk = new XNAClientCheckBox(WindowManager);
                chk.Name = "chkFPPlayer" + (i + 1);
                chk.Text = (i + 1).ToString();
                chk.ClientRectangle = new Rectangle(chkXPos, chkYPos, 0, 0);
                chk.Checked = i < _fpDefaultChecked.Length && _fpDefaultChecked[i];
                chk.InputEnabled = _isHost;
                pnlFormatPainter.AddChild(chk);
                chkFPPlayers.Add(chk);
            }
        }

        public void UpdateFormatPainterPlayerCount(int aiCount)
        {
            if (pnlFormatPainter == null)
                return;
            RebuildFormatPainterCheckboxes(aiCount);
        }

        private void BtnFPConfirm_LeftClick(object sender, EventArgs e)
        {
            if (!_isHost)
                return;

            var selectedIndices = new List<int>();
            for (int i = 0; i < chkFPPlayers.Count; i++)
            {
                if (chkFPPlayers[i].Checked)
                    selectedIndices.Add(i);
            }

            if (selectedIndices.Count > 0)
                FormatPainterApplyRequested?.Invoke(this, selectedIndices);
        }

        private void BtnFPSelectAll_LeftClick(object sender, EventArgs e)
        {
            foreach (var chk in chkFPPlayers)
                chk.Checked = true;
        }

        private void BtnFPDeselectAll_LeftClick(object sender, EventArgs e)
        {
            foreach (var chk in chkFPPlayers)
                chk.Checked = false;
        }

        private void BtnResetQuickOptions_LeftClick(object sender, EventArgs e)
        {
            ResetRequested?.Invoke(this, EventArgs.Empty);
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            pnlQuickActions.Visible = tabControl.SelectedTab == 0;
            pnlFormatPainter.Visible = tabControl.SelectedTab == 1;
        }

        private XNAClientButton CreatePanelButton(XNAPanel parent, string name, string text, int x, int y, int width, string btnTexBase = "92pxbtn")
        {
            var btn = new XNAClientButton(WindowManager);
            btn.Name = name;
            btn.Text = text;
            btn.ClientRectangle = new Rectangle(x, y, width, ddHeight);
            if (AssetLoader.AssetExists(btnTexBase + ".png"))
                btn.IdleTexture = AssetLoader.LoadTexture(btnTexBase + ".png");
            if (AssetLoader.AssetExists(btnTexBase + "_c.png"))
                btn.HoverTexture = AssetLoader.LoadTexture(btnTexBase + "_c.png");
            parent.AddChild(btn);
            return btn;
        }

        private XNAClientDropDown AddDropdownRow(
            ref int rowY, string lblName, string lblText, string ddName,
            string[] items, int selectedIndex,
            out XNAClientCheckBox chk)
        {
            var lbl = new XNALabel(WindowManager);
            lbl.Name = lblName;
            lbl.Text = lblText;
            lbl.ClientRectangle = new Rectangle(labelX, rowY, 0, 0);
            AddChild(lbl);

            var dd = new XNAClientDropDown(WindowManager);
            dd.Name = ddName;
            dd.ClientRectangle = new Rectangle(ddX, rowY - 2, ddWidth, ddHeight);
            foreach (string item in items)
                dd.AddItem(item);
            dd.SelectedIndex = selectedIndex;
            AddChild(dd);

            chk = new XNAClientCheckBox(WindowManager);
            chk.Checked = false;
            chk.ClientRectangle = new Rectangle(chkX, rowY - 1, 0, 0);
            AddChild(chk);

            XNAClientCheckBox chkRef = chk;
            XNAClientDropDown ddRef = dd;
            int originalSelectedIndex = dd.SelectedIndex;
            chkRef.CheckedChanged += (sender, args) =>
            {
                bool randomEnabled = chkRef.Checked;
                if (randomEnabled)
                {
                    originalSelectedIndex = ddRef.SelectedIndex;
                    ddRef.OverrideText = "Random Assignment".L10N("Client:Main:RandomAssignment");
                    ddRef.InputEnabled = false;
                    ddRef.AllowDropDown = false;
                }
                else
                {
                    ddRef.OverrideText = null;
                    ddRef.SelectedIndex = originalSelectedIndex;
                    ddRef.InputEnabled = true;
                    ddRef.AllowDropDown = true;
                }
            };

            int rowHeight = Math.Max(ddHeight, chkRef.Height);
            rowY += rowHeight + rowSpacing;
            return dd;
        }

        private string[] BuildSideItems()
        {
            var list = new List<string>
            {
                "Don't Set".L10N("Client:Main:AISideDontSet"),
                "Random".L10N("Client:Sides:RandomSide")
            };
            return list.ToArray();
        }

        public void SetSideItems(string[] sides, List<string> randomSelectorNames)
        {
            if (cmbAIQuickSide == null)
                return;

            int previousIndex = cmbAIQuickSide.SelectedIndex;

            while (cmbAIQuickSide.Items.Count > 2)
                cmbAIQuickSide.Items.RemoveAt(2);

            if (randomSelectorNames != null)
            {
                _sideSelectorCount = randomSelectorNames.Count;
                foreach (string selectorName in randomSelectorNames)
                    cmbAIQuickSide.AddItem(selectorName);
            }
            else
            {
                _sideSelectorCount = 0;
            }

            if (sides != null)
            {
                foreach (string side in sides)
                    cmbAIQuickSide.AddItem(side.L10N($"INI:Sides:{side}"));
            }

            if (previousIndex >= 0 && previousIndex < cmbAIQuickSide.Items.Count)
                cmbAIQuickSide.SelectedIndex = previousIndex;
            else
                cmbAIQuickSide.SelectedIndex = 1;
        }

        private string[] BuildColorItems()
        {
            var list = new List<string>
            {
                "Don't Set".L10N("Client:Main:AIColorDontSet"),
                "Random".L10N("Client:Main:RandomColor")
            };
            if (MPColors != null)
            {
                foreach (MultiplayerColor mpColor in MPColors)
                    list.Add(mpColor.Name);
            }
            return list.ToArray();
        }

        private string[] BuildTeamItems()
        {
            var list = new List<string>
            {
                "Don't Set".L10N("Client:Main:AITeamDontSet"),
                "-"
            };
            ProgramConstants.TEAMS.ForEach(list.Add);
            return list.ToArray();
        }

        public void SetMPColors(List<MultiplayerColor> colors)
        {
            MPColors = colors;

            if (cmbAIQuickColor != null && MPColors != null)
            {
                while (cmbAIQuickColor.Items.Count > 2)
                    cmbAIQuickColor.Items.RemoveAt(2);

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
            if (tabControl != null) tabControl.InputEnabled = true;
            if (btnResetQuickOptions != null) btnResetQuickOptions.InputEnabled = enable;
            if (btnFPConfirm != null) btnFPConfirm.InputEnabled = enable;
            if (bpFPSelect != null) bpFPSelect.InputEnabled = enable;
            if (btnFPCancel != null) btnFPCancel.InputEnabled = enable;
            if (chkFPPlayers != null)
            {
                foreach (var chk in chkFPPlayers)
                    if (chk != null) chk.InputEnabled = enable;
            }
        }

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
            int difficultySelectedIndex = difficultyIdx + 1;
            if (difficultySelectedIndex >= 0 && difficultySelectedIndex < cmbAIQuickDifficultyLevel.Items.Count)
                cmbAIQuickDifficultyLevel.SelectedIndex = difficultySelectedIndex;

            int sideIdx = ini.GetIntValue(section, nameof(cmbAIQuickSide), 0);
            int sideSelectedIndex = sideIdx + 1;
            if (sideSelectedIndex >= 0 && sideSelectedIndex < cmbAIQuickSide.Items.Count)
                cmbAIQuickSide.SelectedIndex = sideSelectedIndex;

            int colorIdx = ini.GetIntValue(section, nameof(cmbAIQuickColor), 0);
            int colorSelectedIndex = colorIdx + 1;
            if (colorSelectedIndex >= 0 && colorSelectedIndex < cmbAIQuickColor.Items.Count)
                cmbAIQuickColor.SelectedIndex = colorSelectedIndex;

            int teamIdx = ini.GetIntValue(section, nameof(cmbAIQuickTeam), 0);
            int teamSelectedIndex = teamIdx + 1;
            if (teamSelectedIndex >= 0 && teamSelectedIndex < cmbAIQuickTeam.Items.Count)
                cmbAIQuickTeam.SelectedIndex = teamSelectedIndex;

            SideRandomSelectionEnabled = ini.GetBooleanValue(section, "Side.RandomAISelection", true);
            SideSelectorsRandomSelectionEnabled = ini.GetBooleanValue(section, "SideSelectors.RandomAISelection", true);
            SideRandomItemEnabled = ini.GetBooleanValue(section, "SideRandom.RandomAISelection", true);
            ColorRandomSelectionEnabled = ini.GetBooleanValue(section, "Color.RandomAISelection", true);
            ColorRandomItemEnabled = ini.GetBooleanValue(section, "ColorRandom.RandomAISelection", true);

            for (int i = 0; i < maxAIPlayers; i++)
                _fpDefaultChecked[i] = ini.GetBooleanValue(section, "chkAIPlayer" + i, false);

            _suppressEvents = false;

            UpdateUIFromRandomSelectionConfig();

            EnableControls(_isHost);
        }

        private void UpdateUIFromRandomSelectionConfig()
        {
        }

        public List<int> GetSideRandomIndices()
        {
            var indices = new List<int>();
            int totalItems = cmbAIQuickSide?.Items.Count ?? 0;

            if (SideRandomItemEnabled && totalItems > 1)
                indices.Add(1);

            int selectorStart = 2;
            int selectorEnd = 2 + _sideSelectorCount;
            if (SideSelectorsRandomSelectionEnabled && _sideSelectorCount > 0)
            {
                for (int i = selectorStart; i < selectorEnd && i < totalItems; i++)
                    indices.Add(i);
            }

            int sideStart = selectorEnd;
            if (SideRandomSelectionEnabled && sideStart < totalItems)
            {
                for (int i = sideStart; i < totalItems; i++)
                    indices.Add(i);
            }

            return indices;
        }

        public List<int> GetColorRandomIndices()
        {
            var indices = new List<int>();
            int totalItems = cmbAIQuickColor?.Items.Count ?? 0;

            if (ColorRandomItemEnabled && totalItems > 1)
                indices.Add(1);

            if (ColorRandomSelectionEnabled && totalItems > 2)
            {
                for (int i = 2; i < totalItems; i++)
                    indices.Add(i);
            }

            return indices;
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

            int difficultySelectedIndex = options.DifficultyLevel + 1;
            if (difficultySelectedIndex >= 0 && difficultySelectedIndex < cmbAIQuickDifficultyLevel.Items.Count)
                cmbAIQuickDifficultyLevel.SelectedIndex = difficultySelectedIndex;

            int sideSelectedIndex = options.SideIndex + 1;
            if (sideSelectedIndex >= 0 && sideSelectedIndex < cmbAIQuickSide.Items.Count)
                cmbAIQuickSide.SelectedIndex = sideSelectedIndex;

            int colorSelectedIndex = options.ColorIndex + 1;
            if (colorSelectedIndex >= 0 && colorSelectedIndex < cmbAIQuickColor.Items.Count)
                cmbAIQuickColor.SelectedIndex = colorSelectedIndex;

            int teamSelectedIndex = options.TeamId + 1;
            if (teamSelectedIndex >= 0 && teamSelectedIndex < cmbAIQuickTeam.Items.Count)
                cmbAIQuickTeam.SelectedIndex = teamSelectedIndex;

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
