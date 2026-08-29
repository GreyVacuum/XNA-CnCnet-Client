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
        private static readonly Dictionary<string, int> CountryCodeFlagOffsets = ParseCountryCodeFlagOffsets();
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
            var flagsPNG = SixLabors.ImageSharp.Image.Load(flagsStream);
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

        private static Dictionary<string, int> ParseCountryCodeFlagOffsets()
        {
            // 16px version from
            // https://github.com/lafeber/world-flags-sprite
            // Offsets are located in the css files.
            return new Dictionary<string, int>
            {
                ["ad"] = 352,  ["ae"] = 368,  ["af"] = 384,  ["ag"] = 400,
                ["ai"] = 416,  ["al"] = 432,  ["am"] = 448,  ["ao"] = 464,
                ["aq"] = 480,  ["ar"] = 496,  ["as"] = 512,  ["at"] = 528,
                ["au"] = 544,  ["aw"] = 560,  ["ax"] = 576,  ["az"] = 592,
                ["ba"] = 608,  ["bb"] = 624,  ["bd"] = 640,  ["be"] = 656,
                ["bf"] = 672,  ["bg"] = 688,  ["bh"] = 704,  ["bi"] = 720,
                ["bj"] = 736,  ["bl"] = 1424, ["bm"] = 752,  ["bn"] = 768,
                ["bo"] = 784,  ["bq"] = 2752, ["br"] = 800,  ["bs"] = 816,
                ["bt"] = 832,  ["bv"] = 2768, ["bw"] = 848,  ["by"] = 864,
                ["bz"] = 880,  ["ca"] = 896,  ["cd"] = 912,  ["cf"] = 928,
                ["cg"] = 944,  ["ch"] = 960,  ["ci"] = 976,  ["ck"] = 992,
                ["cl"] = 1008, ["cm"] = 1024, ["cn"] = 1040, ["co"] = 1056,
                ["cp"] = 1424, ["cr"] = 1072, ["cu"] = 1088, ["cv"] = 1104,
                ["cw"] = 3920, ["cy"] = 1120, ["cz"] = 1136, ["de"] = 1152,
                ["dj"] = 1168, ["dk"] = 1184, ["dm"] = 1200, ["do"] = 1216,
                ["dz"] = 1232, ["ec"] = 1248, ["ee"] = 1264, ["eg"] = 1280,
                ["eh"] = 1296, ["er"] = 1312, ["es"] = 1328, ["et"] = 1344,
                ["fi"] = 1360, ["fj"] = 1376, ["fm"] = 1392, ["fo"] = 1408,
                ["fr"] = 1424, ["ga"] = 1440, ["gb"] = 1456, ["gd"] = 1472,
                ["ge"] = 1488, ["gg"] = 1504, ["gh"] = 1520, ["gi"] = 1536,
                ["gl"] = 1552, ["gm"] = 1568, ["gn"] = 1584, ["gp"] = 1600,
                ["gq"] = 1616, ["gr"] = 1632, ["gt"] = 1648, ["gu"] = 1664,
                ["gw"] = 1680, ["gy"] = 1696, ["hk"] = 1712, ["hn"] = 1728,
                ["hr"] = 1744, ["ht"] = 1760, ["hu"] = 1776, ["id"] = 1792,
                ["ie"] = 1808, ["il"] = 1824, ["im"] = 1840, ["in"] = 1856,
                ["iq"] = 1872, ["ir"] = 1888, ["is"] = 1904, ["it"] = 1920,
                ["je"] = 1936, ["jm"] = 1952, ["jo"] = 1968, ["jp"] = 1984,
                ["ke"] = 2000, ["kg"] = 2016, ["kh"] = 2032, ["ki"] = 2048,
                ["km"] = 2064, ["kn"] = 2080, ["kp"] = 2096, ["kr"] = 2112,
                ["kw"] = 2128, ["ky"] = 2144, ["kz"] = 2160, ["la"] = 2176,
                ["lb"] = 2192, ["lc"] = 2208, ["li"] = 2224, ["lk"] = 2240,
                ["lr"] = 2256, ["ls"] = 2272, ["lt"] = 2288, ["lu"] = 2304,
                ["lv"] = 2320, ["ly"] = 2336, ["ma"] = 2352, ["mc"] = 1792,
                ["md"] = 2368, ["me"] = 2384, ["mf"] = 1424, ["mg"] = 2400,
                ["mh"] = 2416, ["mk"] = 2432, ["ml"] = 2448, ["mm"] = 2464,
                ["mn"] = 2480, ["mo"] = 2496, ["mq"] = 2512, ["mr"] = 2528,
                ["ms"] = 2544, ["mt"] = 2560, ["mu"] = 2576, ["mv"] = 2592,
                ["mw"] = 2608, ["mx"] = 2624, ["my"] = 2640, ["mz"] = 2656,
                ["na"] = 2672, ["nc"] = 2688, ["ne"] = 2704, ["ng"] = 2720,
                ["ni"] = 2736, ["nl"] = 2752, ["no"] = 2768, ["np"] = 2784,
                ["nq"] = 2768, ["nr"] = 2800, ["nu"] = 3952, ["nz"] = 2816,
                ["om"] = 2832, ["pa"] = 2848, ["pe"] = 2864, ["pf"] = 2880,
                ["pg"] = 2896, ["ph"] = 2912, ["pk"] = 2928, ["pl"] = 2944,
                ["pr"] = 2960, ["ps"] = 2976, ["pt"] = 2992, ["pw"] = 3008,
                ["py"] = 3024, ["qa"] = 3040, ["re"] = 3056, ["ro"] = 3072,
                ["rs"] = 3088, ["ru"] = 3104, ["rw"] = 3120, ["sa"] = 3136,
                ["sb"] = 3152, ["sc"] = 3168, ["sd"] = 3184, ["se"] = 3200,
                ["sg"] = 3216, ["sh"] = 1456, ["si"] = 3232, ["sj"] = 2768,
                ["sk"] = 3248, ["sl"] = 3264, ["sm"] = 3280, ["sn"] = 3296,
                ["so"] = 3312, ["sr"] = 3328, ["ss"] = 3936, ["st"] = 3344,
                ["sv"] = 3360, ["sx"] = 3904, ["sy"] = 3376, ["sz"] = 3392,
                ["tc"] = 3408, ["td"] = 3424, ["tg"] = 3440, ["th"] = 3456,
                ["tj"] = 3472, ["tl"] = 3488, ["tm"] = 3504, ["tn"] = 3520,
                ["to"] = 3536, ["tr"] = 3552, ["tt"] = 3568, ["tv"] = 3584,
                ["tw"] = 3600, ["tz"] = 3616, ["ua"] = 3632, ["ug"] = 3648,
                ["us"] = 3664, ["uy"] = 3680, ["uz"] = 3696, ["va"] = 3712,
                ["vc"] = 3728, ["ve"] = 3744, ["vg"] = 3760, ["vi"] = 3776,
                ["vn"] = 3792, ["vu"] = 3808, ["ws"] = 3824, ["ye"] = 3840,
                ["yt"] = 1424, ["za"] = 3856, ["zm"] = 3872, ["zw"] = 3888
            };
        }

        private static Rectangle? GetFlagRectangle(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode))
                return null;

            string code = countryCode.ToLowerInvariant();
            if (CountryCodeFlagOffsets.TryGetValue(code, out int yOffset))
            {
                return new Rectangle(0, yOffset, FLAG_WIDTH, FLAG_HEIGHT);
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

                    if (flagRect.HasValue)
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
