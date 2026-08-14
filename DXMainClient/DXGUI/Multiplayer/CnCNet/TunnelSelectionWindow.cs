using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;
using ClientCore.Extensions;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A window for selecting a CnCNet tunnel server.
    /// </summary>
    class TunnelSelectionWindow : XNAWindow
    {
        public TunnelSelectionWindow(WindowManager windowManager, TunnelHandler tunnelHandler) : base(windowManager)
        {
            this.tunnelHandler = tunnelHandler;
        }

        public event EventHandler<TunnelEventArgs> TunnelSelected;

        private readonly TunnelHandler tunnelHandler;
        private TunnelListBox lbTunnelList;
        private XNALabel lblDescription;
        private XNAClientButton btnApply;
        private XNAClientButton btnAutoSelectTunnel;
        private XNAClientButton btnSaveDefaultTunnel;

        private string originalTunnelAddress;

        public override void Initialize()
        {
            if (Initialized)
                return;

            Name = "TunnelSelectionWindow";

            BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = nameof(lblDescription);
            lblDescription.Text = "Line 1" + Environment.NewLine + "Line 2";
            lblDescription.X = UIDesignConstants.EMPTY_SPACE_SIDES + UIDesignConstants.CONTROL_HORIZONTAL_MARGIN;
            lblDescription.Y = UIDesignConstants.EMPTY_SPACE_TOP + UIDesignConstants.CONTROL_VERTICAL_MARGIN;
            AddChild(lblDescription);

            lbTunnelList = new TunnelListBox(WindowManager, tunnelHandler);
            lbTunnelList.Name = nameof(lbTunnelList);
            lbTunnelList.Y = lblDescription.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN;
            lbTunnelList.X = UIDesignConstants.EMPTY_SPACE_SIDES + UIDesignConstants.CONTROL_HORIZONTAL_MARGIN;
            AddChild(lbTunnelList);
            lbTunnelList.SelectedIndexChanged += LbTunnelList_SelectedIndexChanged;

            // Set the window width first so that button centering calculations use the correct value.
            Width = lbTunnelList.Right + UIDesignConstants.CONTROL_HORIZONTAL_MARGIN + UIDesignConstants.EMPTY_SPACE_SIDES;

            const int buttonHorizontalGap = UIDesignConstants.CONTROL_HORIZONTAL_MARGIN * 2;
            const int buttonVerticalGap = UIDesignConstants.CONTROL_VERTICAL_MARGIN * 2;

            // Row 1: quick tunnel actions (auto + save), centered.
            int row1ButtonWidth = UIDesignConstants.BUTTON_WIDTH_121;
            int row1TotalWidth = row1ButtonWidth * 2 + buttonHorizontalGap;
            int row1X = (Width - row1TotalWidth) / 2;
            int row1Y = lbTunnelList.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN * 2;

            btnAutoSelectTunnel = new XNAClientButton(WindowManager);
            btnAutoSelectTunnel.Name = nameof(btnAutoSelectTunnel);
            btnAutoSelectTunnel.Width = row1ButtonWidth;
            btnAutoSelectTunnel.Height = UIDesignConstants.BUTTON_HEIGHT;
            btnAutoSelectTunnel.Text = "Auto Select".L10N("Client:Main:AutoSelectTunnel");
            btnAutoSelectTunnel.X = row1X;
            btnAutoSelectTunnel.Y = row1Y;
            btnAutoSelectTunnel.LeftClick += BtnAutoSelectTunnel_LeftClick;
            AddChild(btnAutoSelectTunnel);

            btnSaveDefaultTunnel = new XNAClientButton(WindowManager);
            btnSaveDefaultTunnel.Name = nameof(btnSaveDefaultTunnel);
            btnSaveDefaultTunnel.Width = row1ButtonWidth;
            btnSaveDefaultTunnel.Height = UIDesignConstants.BUTTON_HEIGHT;
            btnSaveDefaultTunnel.Text = "Save as Default".L10N("Client:Main:SaveTunnelAsDefault");
            btnSaveDefaultTunnel.X = row1X + row1ButtonWidth + buttonHorizontalGap;
            btnSaveDefaultTunnel.Y = row1Y;
            btnSaveDefaultTunnel.LeftClick += BtnSaveDefaultTunnel_LeftClick;
            AddChild(btnSaveDefaultTunnel);

            // Row 2: dialog actions (apply + cancel), centered.
            int row2ButtonWidth = UIDesignConstants.BUTTON_WIDTH_92;
            int row2TotalWidth = row2ButtonWidth * 2 + buttonHorizontalGap;
            int row2X = (Width - row2TotalWidth) / 2;
            int row2Y = row1Y + UIDesignConstants.BUTTON_HEIGHT + buttonVerticalGap;

            btnApply = new XNAClientButton(WindowManager);
            btnApply.Name = nameof(btnApply);
            btnApply.Width = row2ButtonWidth;
            btnApply.Height = UIDesignConstants.BUTTON_HEIGHT;
            btnApply.Text = "Apply".L10N("Client:Main:ButtonApply");
            btnApply.X = row2X;
            btnApply.Y = row2Y;
            btnApply.LeftClick += BtnApply_LeftClick;
            AddChild(btnApply);

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.Width = row2ButtonWidth;
            btnCancel.Height = UIDesignConstants.BUTTON_HEIGHT;
            btnCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
            btnCancel.X = row2X + row2ButtonWidth + buttonHorizontalGap;
            btnCancel.Y = row2Y;
            btnCancel.LeftClick += BtnCancel_LeftClick;
            AddChild(btnCancel);

            Height = btnApply.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN + UIDesignConstants.EMPTY_SPACE_BOTTOM;

            base.Initialize();
        }

        private void BtnApply_LeftClick(object sender, EventArgs e)
        {
            Disable();

            if (!lbTunnelList.IsValidIndexSelected())
                return;

            CnCNetTunnel tunnel = tunnelHandler.Tunnels[lbTunnelList.SelectedIndex];
            TunnelSelected?.Invoke(this, new TunnelEventArgs(tunnel));
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e) => Disable();

        private void BtnAutoSelectTunnel_LeftClick(object sender, EventArgs e)
        {
            lbTunnelList.AutoSelectBestTunnel();
            UpdateSaveDefaultButton();
        }

        private void BtnSaveDefaultTunnel_LeftClick(object sender, EventArgs e)
        {
            lbTunnelList.SaveCurrentAsDefault();
            UpdateSaveDefaultButton();
        }

        private void LbTunnelList_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnApply.AllowClick = !lbTunnelList.IsTunnelSelected(originalTunnelAddress) && lbTunnelList.IsValidIndexSelected();
            UpdateSaveDefaultButton();
        }

        /// <summary>
        /// Refreshes the "Save as Default" button: it dims (and reads "Saved as Default") when the
        /// current selection already matches the saved default, otherwise it is enabled and reads "Save as Default".
        /// </summary>
        private void UpdateSaveDefaultButton()
        {
            bool isCurrentDefault = lbTunnelList.IsCurrentSelectionDefault();
            btnSaveDefaultTunnel.AllowClick = !isCurrentDefault && lbTunnelList.IsValidIndexSelected();
            btnSaveDefaultTunnel.Text = lbTunnelList.GetSaveDefaultButtonText();
        }

        /// <summary>
        /// Sets the window's description and selects the tunnel server
        /// with the given address.
        /// </summary>
        /// <param name="description">The window description.</param>
        /// <param name="tunnelAddress">The address of the tunnel server to select.</param>
        public void Open(string description, string tunnelAddress = null)
        {
            lblDescription.Text = description;
            originalTunnelAddress = tunnelAddress;

            if (!string.IsNullOrWhiteSpace(tunnelAddress))
                lbTunnelList.SelectTunnel(tunnelAddress);
            else
                // No specific tunnel requested: honor the player's saved default (memory)
                // instead of clearing the selection, so the remembered server stays selected.
                lbTunnelList.SelectPreferredTunnel();

            if (lbTunnelList.SelectedIndex > -1)
            {
                lbTunnelList.SetTopIndex(0);

                while (lbTunnelList.SelectedIndex > lbTunnelList.LastIndex)
                    lbTunnelList.TopIndex++;
            }

            btnApply.AllowClick = false;
            UpdateSaveDefaultButton();
            Enable();
        }
    }

    class TunnelEventArgs : EventArgs
    {
        public TunnelEventArgs(CnCNetTunnel tunnel)
        {
            Tunnel = tunnel;
        }

        public CnCNetTunnel Tunnel { get; }
    }
}
