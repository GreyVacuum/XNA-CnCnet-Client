using DTAClient.Domain.Multiplayer.CnCNet;
using ClientCore;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using ClientCore;
using System.IO;
using System.Reflection;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A list box for listing CnCNet tunnel servers.
    /// </summary>
    class TunnelListBox : XNAMultiColumnListBox
    {
        private static readonly Dictionary<string, Point> CountryCodeFlagPositions = ParseCountryCodeFlagPositions();
        private const int FLAG_WIDTH = 16;
        private const int FLAG_HEIGHT = 16;

        public TunnelListBox(WindowManager windowManager, TunnelHandler tunnelHandler) : base(windowManager)
        {
            this.tunnelHandler = tunnelHandler;

            tunnelHandler.TunnelsRefreshed += TunnelHandler_TunnelsRefreshed;
            tunnelHandler.TunnelPinged += TunnelHandler_TunnelPinged;

            SelectedIndexChanged += TunnelListBox_SelectedIndexChanged;

            int headerHeight = (int)Renderer.GetTextDimensions("Name", HeaderFontIndex).Y;

            Width = 466;
            Height = LineHeight * 12 + headerHeight + 3;
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);

            using Stream flagsStream = Assembly.GetAssembly(typeof(GameCollection)).GetManifestResourceStream("DTAClient.Icons.flags16.png");
            using var flagsPNG = SixLabors.ImageSharp.Image.Load(flagsStream);
            flagsSpriteSheet = AssetLoader.TextureFromImage(flagsPNG);

            var flagListBox = new FlagListBox(windowManager, tunnelHandler, flagsSpriteSheet);
            flagListBox.FontIndex = FontIndex;
            flagListBox.LineHeight = LineHeight;

            var flagHeader = new XNAPanel(windowManager);
            flagHeader.Width = 20;
            flagHeader.Height = headerHeight + 3;

            AddColumn(flagHeader, flagListBox);

            AddColumn("Name".L10N("Client:Main:NameHeader"), 210);
            AddColumn("Official".L10N("Client:Main:OfficialHeader"), 70);
            AddColumn("Ping".L10N("Client:Main:PingHeader"), 76);
            AddColumn("Players".L10N("Client:Main:PlayersHeader"), 90);
            AllowRightClickUnselect = false;
            AllowKeyboardInput = true;
        }

        private int? _targetVersion;
        public int? TargetVersion
        {
            get => _targetVersion;
            set
            {
                if (_targetVersion != value)
                {
                    _targetVersion = value;
                    isManuallySelectedTunnel = false;
                    manuallySelectedTunnelKey = null;
                    if (ItemCount > 0)
                        TunnelHandler_TunnelsRefreshed(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler ListRefreshed;

        private readonly TunnelHandler tunnelHandler;
        private Texture2D flagsSpriteSheet;

        private int bestTunnelIndex = 0;
        private int lowestTunnelRating = int.MaxValue;

        private bool isManuallySelectedTunnel;
        private string manuallySelectedTunnelKey;

        private List<CnCNetTunnel> GetFilteredTunnels()
        {
            int targetVersion = TargetVersion ?? ((TunnelMode)UserINISettings.Instance.TunnelMode.Value == TunnelMode.V2Legacy ? 2 : 3);
            return tunnelHandler.Tunnels.Where(tunnel => tunnel.Version == targetVersion).ToList();
        }

        private static string GetTunnelKey(CnCNetTunnel tunnel) => GetTunnelKey(tunnel.Address, tunnel.Port);

        private static string GetTunnelKey(string address, int port) => $"{address}:{port}";

        private bool isPreferredSelectedTunnel;

        private bool ignoreNextSelectionChange;

        /// <summary>
        /// The saved preferred (default) tunnel key, read directly from user settings.
        /// Reading from the settings instance keeps all TunnelListBox instances in sync with
        /// the latest saved default, regardless of which window last changed it.
        /// </summary>
        /// <remarks>
        /// Stored as "address:port", because under V3 a tunnel is only identified by both parts
        /// and the same address can host several ports. Preferences saved as a bare address by
        /// older builds are still honoured through an address-only fallback.
        /// </remarks>
        private string PreferredTunnelKey => UserINISettings.Instance.PreferredCnCNetTunnel.Value;

        /// <summary>
        /// Locates the saved preferred (default) tunnel within the currently filtered list.
        /// </summary>
        /// <returns>The index into the filtered list, or -1 when no preference is set or visible.</returns>
        private int FindPreferredTunnelIndex()
        {
            string preferred = PreferredTunnelKey;
            if (string.IsNullOrEmpty(preferred))
                return -1;

            var filteredTunnels = GetFilteredTunnels();

            int index = filteredTunnels.FindIndex(t => GetTunnelKey(t) == preferred);
            if (index > -1)
                return index;

            // Legacy preference: a bare address saved before ports became part of the key.
            return filteredTunnels.FindIndex(t => t.Address == preferred);
        }

        /// <summary>
        /// Selects a tunnel from the list with the given address and port.
        /// </summary>
        /// <param name="address">The address of the tunnel server to select.</param>
        /// <param name="port">The port of the tunnel server to select.</param>
        public void SelectTunnel(string address, int port)
        {
            int index = GetFilteredTunnels().FindIndex(t => t.Address == address && t.Port == port);
            if (index > -1)
            {
                ignoreNextSelectionChange = true;
                SelectedIndex = index;
                isManuallySelectedTunnel = true;
                manuallySelectedTunnelKey = GetTunnelKey(address, port);
                isPreferredSelectedTunnel = false;
            }
        }

        /// <summary>
        /// Selects the saved preferred (default) tunnel, if it is present in the current
        /// tunnel list. Used when a window opens so the player's remembered choice is
        /// honored even if the list was refreshed before this list box existed.
        /// </summary>
        public void SelectPreferredTunnel()
        {
            int index = FindPreferredTunnelIndex();
            if (index > -1)
            {
                ignoreNextSelectionChange = true;
                SelectedIndex = index;
                isPreferredSelectedTunnel = true;
                isManuallySelectedTunnel = false;
            }
        }

        /// <summary>
        /// Applies the saved preferred (default) tunnel when the list becomes visible,
        /// unless the player has already made a manual selection earlier in this session.
        /// Defends against the tunnel list being refreshed before this list box subscribed
        /// to the refresh event, which would otherwise leave the preference unapplied.
        /// </summary>
        public void ApplyPreferredTunnelOnShow()
        {
            if (isManuallySelectedTunnel)
                return;

            SelectPreferredTunnel();
        }

        /// <summary>
        /// Gets whether or not a tunnel from the list with the given address and port is selected.
        /// </summary>
        /// <param name="address">The address of the tunnel server</param>
        /// <param name="port">The port of the tunnel server</param>
        /// <returns>True if tunnel with given address is selected, otherwise false.</returns>
        public bool IsTunnelSelected(string address, int port)
        {
            return GetFilteredTunnels().FindIndex(t => t.Address == address && t.Port == port) == SelectedIndex;
        }

        /// <summary>
        /// Gets whether the currently selected tunnel is already the saved preferred (default) tunnel.
        /// </summary>
        public bool IsCurrentSelectionDefault()
        {
            if (!IsValidIndexSelected())
                return false;

            string preferred = PreferredTunnelKey;
            if (string.IsNullOrEmpty(preferred))
                return false;

            var filteredTunnels = GetFilteredTunnels();
            if (SelectedIndex < 0 || SelectedIndex >= filteredTunnels.Count)
                return false;

            CnCNetTunnel selected = filteredTunnels[SelectedIndex];

            // Exact key match, or a legacy preference saved as a bare address.
            return GetTunnelKey(selected) == preferred || selected.Address == preferred;
        }

        /// <summary>
        /// Gets the label for the "Save as Default" button. When the current selection
        /// already matches the saved default, it reads "Saved as Default" to reflect that no
        /// further action is needed.
        /// </summary>
        public string GetSaveDefaultButtonText() =>
            IsCurrentSelectionDefault()
                ? "Saved as Default".L10N("Client:Main:TunnelAlreadyDefault")
                : "Save as Default".L10N("Client:Main:SaveTunnelAsDefault");

        private void TunnelHandler_TunnelsRefreshed(object sender, EventArgs e)
        {
            ClearItems();

            var filteredTunnels = GetFilteredTunnels();
            int tunnelIndex = 0;
            bestTunnelIndex = 0;
            lowestTunnelRating = int.MaxValue;

            foreach (CnCNetTunnel tunnel in filteredTunnels)
            {
                List<string> info = new List<string>();

                info.Add(""); // Flag column
                info.Add(tunnel.Name);
                info.Add(Conversions.BooleanToString(tunnel.Official, BooleanStringStyle.YESNO));
                info.Add(tunnel.Ping.ToString());
                info.Add(tunnel.Clients + " / " + tunnel.MaxClients);

                AddItem(info, true);

                XNAListBoxItem flagItem = GetItem(0, tunnelIndex);
                if (flagItem != null)
                    flagItem.Tag = GetFlagRectangle(tunnel.CountryCode);

                if ((tunnel.Official || tunnel.Recommended) && tunnel.Ping.IsValid())
                {
                    int rating = GetTunnelRating(tunnel);
                    if (rating < lowestTunnelRating)
                    {
                        bestTunnelIndex = tunnelIndex;
                        lowestTunnelRating = rating;
                    }
                }

                tunnelIndex++;
            }

            if (filteredTunnels.Count > 0)
            {
                if (isManuallySelectedTunnel)
                {
                    int manuallySelectedIndex = filteredTunnels.FindIndex(t => GetTunnelKey(t) == manuallySelectedTunnelKey);

                    if (manuallySelectedIndex == -1)
                    {
                        ignoreNextSelectionChange = true;
                        SelectedIndex = bestTunnelIndex;
                        isManuallySelectedTunnel = false;
                        manuallySelectedTunnelKey = null;
                    }
                    else
                    {
                        ignoreNextSelectionChange = true;
                        SelectedIndex = manuallySelectedIndex;
                    }
                }
                else if (!string.IsNullOrEmpty(PreferredTunnelKey))
                {
                    int preferredIndex = FindPreferredTunnelIndex();
                    if (preferredIndex > -1)
                    {
                        ignoreNextSelectionChange = true;
                        SelectedIndex = preferredIndex;
                        isPreferredSelectedTunnel = true;
                    }
                    else
                    {
                        ignoreNextSelectionChange = true;
                        SelectedIndex = bestTunnelIndex;
                        isPreferredSelectedTunnel = false;
                    }
                }
                else
                {
                    ignoreNextSelectionChange = true;
                    SelectedIndex = bestTunnelIndex;
                }
            }

            ListRefreshed?.Invoke(this, EventArgs.Empty);
        }

        private void TunnelHandler_TunnelPinged(string address, int port)
        {
            var filteredTunnels = GetFilteredTunnels();

            CnCNetTunnel tunnel = tunnelHandler.Tunnels.FirstOrDefault(t => t.Address == address && t.Port == port);
            if (tunnel == null)
                return;

            int filteredIndex = filteredTunnels.FindIndex(t => t.Address == address && t.Port == port);
            if (filteredIndex == -1)
                return;

            XNAListBoxItem lbItem = GetItem(3, filteredIndex);
            lbItem.Text = tunnel.Ping.ToString();

            // If the saved-default (preferred) tunnel became unreachable, fall back to the best one.
            // Checked BEFORE the validity guard below: an unreachable tunnel has an invalid Ping,
            // so testing this inside the guard would never fire.
            if (isPreferredSelectedTunnel && filteredIndex == FindPreferredTunnelIndex() && !tunnel.Ping.IsValid())
            {
                isPreferredSelectedTunnel = false;
                ignoreNextSelectionChange = true;
                SelectedIndex = bestTunnelIndex;
                return;
            }

            if (tunnel.Ping.IsValid())
            {
                int rating = GetTunnelRating(tunnel);

                // Keep track of the best official/recommended tunnel in the background. This is
                // used as the automatic fallback when there is no manual or saved-default selection,
                // and must be updated independently of the current selection.
                bool isNewBest = (tunnel.Recommended || tunnel.Official) && rating < lowestTunnelRating;

                if (isNewBest)
                {
                    bestTunnelIndex = filteredIndex;
                    lowestTunnelRating = rating;
                }

                // A manually chosen or saved-default (preferred) tunnel must NOT be overridden by
                // periodic ping updates, even if another official/recommended tunnel reports a
                // marginally better score. This is what previously discarded the player's saved default.
                if (isManuallySelectedTunnel || isPreferredSelectedTunnel)
                    return;

                // Pure automatic mode: select the tunnel if it is now the best official/recommended one.
                if (isNewBest)
                {
                    ignoreNextSelectionChange = true;
                    SelectedIndex = filteredIndex;
                }
            }
        }

        private int GetTunnelRating(CnCNetTunnel tunnel)
        {
            if (!tunnel.Ping.IsValid())
                return int.MaxValue;

            if (tunnel.Clients >= tunnel.MaxClients)
                return int.MaxValue;

            double usageRatio = (double)tunnel.Clients / tunnel.MaxClients;

            if (usageRatio == 0)
                usageRatio = 0.1;

            usageRatio *= 100.0;

            double rating = Math.Pow(tunnel.Ping.Milliseconds, 2.0) * usageRatio;

            // Slightly penalize unofficial, non-recommended tunnels because their stability is less guaranteed.
            if (!tunnel.Official && !tunnel.Recommended)
                rating *= 1.25;

            return Convert.ToInt32(rating);
        }

        public CnCNetTunnel GetSelectedTunnel()
        {
            if (!IsValidIndexSelected())
                return null;

            return GetFilteredTunnels()[SelectedIndex];
        }

        private void TunnelListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsValidIndexSelected())
                return;

            if (ignoreNextSelectionChange)
            {
                ignoreNextSelectionChange = false;
                return;
            }

            var filteredTunnels = GetFilteredTunnels();
            if (SelectedIndex >= 0 && SelectedIndex < filteredTunnels.Count)
            {
                isManuallySelectedTunnel = true;
                isPreferredSelectedTunnel = false;
                manuallySelectedTunnelKey = GetTunnelKey(filteredTunnels[SelectedIndex]);
            }
        }

        /// <summary>
        /// Automatically selects the currently most stable tunnel server for this session only.
        /// It does NOT persist the choice to the local settings; use "Save as Default" to remember it.
        /// </summary>
        public void AutoSelectBestTunnel()
        {
            var filteredTunnels = GetFilteredTunnels();
            if (filteredTunnels.Count == 0)
                return;

            int bestIndex = 0;
            int lowestRating = int.MaxValue;

            for (int i = 0; i < filteredTunnels.Count; i++)
            {
                int rating = GetTunnelRating(filteredTunnels[i]);
                if (rating < lowestRating)
                {
                    lowestRating = rating;
                    bestIndex = i;
                }
            }

            CnCNetTunnel bestTunnel = filteredTunnels[bestIndex];
            bestTunnelIndex = bestIndex;
            lowestTunnelRating = lowestRating;

            // Treat the auto-selected server as a temporary, in-session manual choice:
            // it sticks until the user changes it, but is not written to PreferredCnCNetTunnel.
            manuallySelectedTunnelKey = GetTunnelKey(bestTunnel);
            isManuallySelectedTunnel = true;
            isPreferredSelectedTunnel = false;

            ignoreNextSelectionChange = true;
            SelectedIndex = bestIndex;
        }

        /// <summary>
        /// Saves the currently selected tunnel server as the preferred (default) tunnel,
        /// so it is automatically selected on subsequent launches.
        /// </summary>
        public void SaveCurrentAsDefault()
        {
            if (!IsValidIndexSelected())
                return;

            var filteredTunnels = GetFilteredTunnels();
            if (SelectedIndex < 0 || SelectedIndex >= filteredTunnels.Count)
                return;

            CnCNetTunnel tunnel = filteredTunnels[SelectedIndex];

            isPreferredSelectedTunnel = true;
            isManuallySelectedTunnel = false;
            bestTunnelIndex = SelectedIndex;
            ignoreNextSelectionChange = true;
            SelectedIndex = SelectedIndex;

            SavePreferredTunnel(tunnel);
        }

        private void SavePreferredTunnel(CnCNetTunnel tunnel)
        {
            if (tunnel == null)
                return;

            UserINISettings.Instance.PreferredCnCNetTunnel.Value = GetTunnelKey(tunnel);
            UserINISettings.Instance.SaveSettings();
        }

        private static Dictionary<string, Point> ParseCountryCodeFlagPositions()
        {
            // 16px version from
            // https://github.com/lafeber/world-flags-sprite
            // The original vertical strip (offsets in its css files) is repacked
            // into a 16x16 grid, as XNA cannot load textures larger than 2048px.
            return new Dictionary<string, Point>
            {
                ["ad"] = new(96, 16),    ["ae"] = new(112, 16),   ["af"] = new(128, 16),   ["ag"] = new(144, 16),
                ["ai"] = new(160, 16),   ["al"] = new(176, 16),   ["am"] = new(192, 16),   ["ao"] = new(208, 16),
                ["aq"] = new(224, 16),   ["ar"] = new(240, 16),   ["as"] = new(0, 32),     ["at"] = new(16, 32),
                ["au"] = new(32, 32),    ["aw"] = new(48, 32),    ["ax"] = new(64, 32),    ["az"] = new(80, 32),
                ["ba"] = new(96, 32),    ["bb"] = new(112, 32),   ["bd"] = new(128, 32),   ["be"] = new(144, 32),
                ["bf"] = new(160, 32),   ["bg"] = new(176, 32),   ["bh"] = new(192, 32),   ["bi"] = new(208, 32),
                ["bj"] = new(224, 32),   ["bl"] = new(144, 80),   ["bm"] = new(240, 32),   ["bn"] = new(0, 48),
                ["bo"] = new(16, 48),    ["bq"] = new(192, 160),  ["br"] = new(32, 48),    ["bs"] = new(48, 48),
                ["bt"] = new(64, 48),    ["bv"] = new(208, 160),  ["bw"] = new(80, 48),    ["by"] = new(96, 48),
                ["bz"] = new(112, 48),   ["ca"] = new(128, 48),   ["cd"] = new(144, 48),   ["cf"] = new(160, 48),
                ["cg"] = new(176, 48),   ["ch"] = new(192, 48),   ["ci"] = new(208, 48),   ["ck"] = new(224, 48),
                ["cl"] = new(240, 48),   ["cm"] = new(0, 64),     ["cn"] = new(16, 64),    ["co"] = new(32, 64),
                ["cp"] = new(144, 80),   ["cr"] = new(48, 64),    ["cu"] = new(64, 64),    ["cv"] = new(80, 64),
                ["cw"] = new(80, 240),   ["cy"] = new(96, 64),    ["cz"] = new(112, 64),   ["de"] = new(128, 64),
                ["dj"] = new(144, 64),   ["dk"] = new(160, 64),   ["dm"] = new(176, 64),   ["do"] = new(192, 64),
                ["dz"] = new(208, 64),   ["ec"] = new(224, 64),   ["ee"] = new(240, 64),   ["eg"] = new(0, 80),
                ["eh"] = new(16, 80),    ["er"] = new(32, 80),    ["es"] = new(48, 80),    ["et"] = new(64, 80),
                ["fi"] = new(80, 80),    ["fj"] = new(96, 80),    ["fm"] = new(112, 80),   ["fo"] = new(128, 80),
                ["fr"] = new(144, 80),   ["ga"] = new(160, 80),   ["gb"] = new(176, 80),   ["gd"] = new(192, 80),
                ["ge"] = new(208, 80),   ["gg"] = new(224, 80),   ["gh"] = new(240, 80),   ["gi"] = new(0, 96),
                ["gl"] = new(16, 96),    ["gm"] = new(32, 96),    ["gn"] = new(48, 96),    ["gp"] = new(64, 96),
                ["gq"] = new(80, 96),    ["gr"] = new(96, 96),    ["gt"] = new(112, 96),   ["gu"] = new(128, 96),
                ["gw"] = new(144, 96),   ["gy"] = new(160, 96),   ["hk"] = new(176, 96),   ["hn"] = new(192, 96),
                ["hr"] = new(208, 96),   ["ht"] = new(224, 96),   ["hu"] = new(240, 96),   ["id"] = new(0, 112),
                ["ie"] = new(16, 112),   ["il"] = new(32, 112),   ["im"] = new(48, 112),   ["in"] = new(64, 112),
                ["iq"] = new(80, 112),   ["ir"] = new(96, 112),   ["is"] = new(112, 112),  ["it"] = new(128, 112),
                ["je"] = new(144, 112),  ["jm"] = new(160, 112),  ["jo"] = new(176, 112),  ["jp"] = new(192, 112),
                ["ke"] = new(208, 112),  ["kg"] = new(224, 112),  ["kh"] = new(240, 112),  ["ki"] = new(0, 128),
                ["km"] = new(16, 128),   ["kn"] = new(32, 128),   ["kp"] = new(48, 128),   ["kr"] = new(64, 128),
                ["kw"] = new(80, 128),   ["ky"] = new(96, 128),   ["kz"] = new(112, 128),  ["la"] = new(128, 128),
                ["lb"] = new(144, 128),  ["lc"] = new(160, 128),  ["li"] = new(176, 128),  ["lk"] = new(192, 128),
                ["lr"] = new(208, 128),  ["ls"] = new(224, 128),  ["lt"] = new(240, 128),  ["lu"] = new(0, 144),
                ["lv"] = new(16, 144),   ["ly"] = new(32, 144),   ["ma"] = new(48, 144),   ["mc"] = new(0, 112),
                ["md"] = new(64, 144),   ["me"] = new(80, 144),   ["mf"] = new(144, 80),   ["mg"] = new(96, 144),
                ["mh"] = new(112, 144),  ["mk"] = new(128, 144),  ["ml"] = new(144, 144),  ["mm"] = new(160, 144),
                ["mn"] = new(176, 144),  ["mo"] = new(192, 144),  ["mq"] = new(208, 144),  ["mr"] = new(224, 144),
                ["ms"] = new(240, 144),  ["mt"] = new(0, 160),    ["mu"] = new(16, 160),   ["mv"] = new(32, 160),
                ["mw"] = new(48, 160),   ["mx"] = new(64, 160),   ["my"] = new(80, 160),   ["mz"] = new(96, 160),
                ["na"] = new(112, 160),  ["nc"] = new(128, 160),  ["ne"] = new(144, 160),  ["ng"] = new(160, 160),
                ["ni"] = new(176, 160),  ["nl"] = new(192, 160),  ["no"] = new(208, 160),  ["np"] = new(224, 160),
                ["nq"] = new(208, 160),  ["nr"] = new(240, 160),  ["nu"] = new(112, 240),  ["nz"] = new(0, 176),
                ["om"] = new(16, 176),   ["pa"] = new(32, 176),   ["pe"] = new(48, 176),   ["pf"] = new(64, 176),
                ["pg"] = new(80, 176),   ["ph"] = new(96, 176),   ["pk"] = new(112, 176),  ["pl"] = new(128, 176),
                ["pr"] = new(144, 176),  ["ps"] = new(160, 176),  ["pt"] = new(176, 176),  ["pw"] = new(192, 176),
                ["py"] = new(208, 176),  ["qa"] = new(224, 176),  ["re"] = new(240, 176),  ["ro"] = new(0, 192),
                ["rs"] = new(16, 192),   ["ru"] = new(32, 192),   ["rw"] = new(48, 192),   ["sa"] = new(64, 192),
                ["sb"] = new(80, 192),   ["sc"] = new(96, 192),   ["sd"] = new(112, 192),  ["se"] = new(128, 192),
                ["sg"] = new(144, 192),  ["sh"] = new(176, 80),   ["si"] = new(160, 192),  ["sj"] = new(208, 160),
                ["sk"] = new(176, 192),  ["sl"] = new(192, 192),  ["sm"] = new(208, 192),  ["sn"] = new(224, 192),
                ["so"] = new(240, 192),  ["sr"] = new(0, 208),    ["ss"] = new(96, 240),   ["st"] = new(16, 208),
                ["sv"] = new(32, 208),   ["sx"] = new(64, 240),   ["sy"] = new(48, 208),   ["sz"] = new(64, 208),
                ["tc"] = new(80, 208),   ["td"] = new(96, 208),   ["tg"] = new(112, 208),  ["th"] = new(128, 208),
                ["tj"] = new(144, 208),  ["tl"] = new(160, 208),  ["tm"] = new(176, 208),  ["tn"] = new(192, 208),
                ["to"] = new(208, 208),  ["tr"] = new(224, 208),  ["tt"] = new(240, 208),  ["tv"] = new(0, 224),
                ["tw"] = new(16, 224),   ["tz"] = new(32, 224),   ["ua"] = new(48, 224),   ["ug"] = new(64, 224),
                ["us"] = new(80, 224),   ["uy"] = new(96, 224),   ["uz"] = new(112, 224),  ["va"] = new(128, 224),
                ["vc"] = new(144, 224),  ["ve"] = new(160, 224),  ["vg"] = new(176, 224),  ["vi"] = new(192, 224),
                ["vn"] = new(208, 224),  ["vu"] = new(224, 224),  ["ws"] = new(240, 224),  ["ye"] = new(0, 240),
                ["yt"] = new(144, 80),   ["za"] = new(16, 240),   ["zm"] = new(32, 240),   ["zw"] = new(48, 240)
            };
        }

        private static Rectangle? GetFlagRectangle(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode))
                return null;

            string code = countryCode.ToLowerInvariant();
            if (CountryCodeFlagPositions.TryGetValue(code, out Point position))
            {
                return new Rectangle(position.X, position.Y, FLAG_WIDTH, FLAG_HEIGHT);
            }

            return null;
        }

        /// <summary>
        /// Custom listbox that draws country flags.
        /// </summary>
        private class FlagListBox : XNAListBox
        {
            private readonly TunnelHandler tunnelHandler;
            private readonly Texture2D flagsSpriteSheet;

            public FlagListBox(WindowManager windowManager, TunnelHandler tunnelHandler, Texture2D flagsSpriteSheet)
                : base(windowManager)
            {
                this.tunnelHandler = tunnelHandler;
                this.flagsSpriteSheet = flagsSpriteSheet;
            }

            public override void Draw(GameTime gameTime)
            {
                DrawPanel();

                int height = 2 - (ViewTop % LineHeight);

                for (int i = TopIndex; i < Items.Count; i++)
                {
                    if (height > Height)
                        break;

                    Rectangle? flagRect = Items[i].Tag as Rectangle?;

                    if (flagRect.HasValue && flagsSpriteSheet != null)
                    {
                        int x = (Width - FLAG_WIDTH) / 2;
                        DrawTexture(flagsSpriteSheet,
                            flagRect.Value,
                            new Rectangle(x, height, FLAG_WIDTH, FLAG_HEIGHT),
                            Color.White);
                    }

                    height += LineHeight;
                }

                if (DrawBorders)
                    DrawPanelBorders();

                DrawChildren(gameTime);
            }
        }
    }
}
