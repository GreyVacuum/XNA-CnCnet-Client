#nullable enable
using System;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    public class HostedCnCNetGame : GenericHostedGame
    {
        public HostedCnCNetGame() { }

        public HostedCnCNetGame(string channelName, string revision, string gamever, int maxPlayers,
            string roomName, bool passworded,
            bool tunneled,
            string[] players, string adminName, string mapName, string gameMode, string mapHash)
        {
            ChannelName = channelName;
            Revision = revision;
            GameVersion = gamever;
            MaxPlayers = maxPlayers;
            RoomName = roomName;
            Passworded = passworded;
            Tunneled = tunneled;
            Players = players;
            HostName = adminName;
            Map = mapName;
            GameMode = gameMode;
            MapHash = mapHash;
        }

        public string? ChannelName { get; set; }
        public string? Revision { get; set; }
        public bool Tunneled { get; set; }
        public bool IsLadder { get; set; }
        public string? MatchID { get; set; }
        public CnCNetTunnel? TunnelServer { get; set; }
        public int[]? BroadcastedGameOptionValues { get; set; }

        /// <summary>
        /// Display text of each broadcast (BroadcastToLobby) drop-down's current
        /// selection, ordered like the drop-down part of
        /// <see cref="BroadcastedGameOptionValues"/>. Entries are null/empty for
        /// drop-downs whose host selection is a regular item (observers fall back
        /// to their local item text in that case). Carried in the trailing fields
        /// of the game-options CSV so observers can show the host's custom input
        /// instead of their local defaults.
        /// </summary>
        public string[]? BroadcastedDropdownCustomTexts { get; set; }

        public override PingValue Ping => TunnelServer?.Ping ?? PingValue.Unknown;

        public override bool Equals(GenericHostedGame other)
            => other is HostedCnCNetGame hostedCnCNetGame
                ? string.Equals(hostedCnCNetGame.ChannelName, ChannelName, StringComparison.InvariantCultureIgnoreCase)
                : base.Equals(other);
    }
}
