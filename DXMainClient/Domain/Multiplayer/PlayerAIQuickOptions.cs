using System.Text;

namespace DTAClient.Domain.Multiplayer
{
    public class PlayerAIQuickOptions
    {
        public const string CNCNET_MESSAGE_KEY = "AIQ";
        public const string LAN_MESSAGE_KEY = "AIQOPTS";
        private const char MESSAGE_SEPARATOR = ';';

        public int DifficultyLevel { get; set; } = 2;
        public int SideIndex { get; set; } = 0;
        public int ColorIndex { get; set; } = 0;
        public int TeamId { get; set; } = 0;
        public bool RandomDifficulty { get; set; } = false;
        public bool RandomSide { get; set; } = false;
        public bool RandomColor { get; set; } = false;
        public bool RandomTeam { get; set; } = false;
        public bool AutoAssignStarts { get; set; } = false;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(DifficultyLevel);
            sb.Append(MESSAGE_SEPARATOR);
            sb.Append(SideIndex);
            sb.Append(MESSAGE_SEPARATOR);
            sb.Append(ColorIndex);
            sb.Append(MESSAGE_SEPARATOR);
            sb.Append(TeamId);
            sb.Append(MESSAGE_SEPARATOR);
            sb.Append(RandomDifficulty ? "1" : "0");
            sb.Append(RandomSide ? "1" : "0");
            sb.Append(RandomColor ? "1" : "0");
            sb.Append(RandomTeam ? "1" : "0");
            sb.Append(MESSAGE_SEPARATOR);
            sb.Append(AutoAssignStarts ? "1" : "0");
            return sb.ToString();
        }

        public string ToCncnetMessage() => $"{CNCNET_MESSAGE_KEY} {ToString()}";
        public string ToLanMessage() => $"{LAN_MESSAGE_KEY} {ToString()}";

        public static PlayerAIQuickOptions FromMessage(string message)
        {
            var parts = message.Split(MESSAGE_SEPARATOR);
            var result = new PlayerAIQuickOptions();
            if (parts.Length >= 1 && int.TryParse(parts[0], out int diff)) result.DifficultyLevel = diff;
            if (parts.Length >= 2 && int.TryParse(parts[1], out int side)) result.SideIndex = side;
            if (parts.Length >= 3 && int.TryParse(parts[2], out int color)) result.ColorIndex = color;
            if (parts.Length >= 4 && int.TryParse(parts[3], out int team)) result.TeamId = team;
            if (parts.Length >= 5)
            {
                var bools = parts[4];
                if (bools.Length > 0) result.RandomDifficulty = bools[0] == '1';
                if (bools.Length > 1) result.RandomSide = bools[1] == '1';
                if (bools.Length > 2) result.RandomColor = bools[2] == '1';
                if (bools.Length > 3) result.RandomTeam = bools[3] == '1';
            }
            if (parts.Length >= 6 && parts[5].Length > 0) result.AutoAssignStarts = parts[5][0] == '1';
            return result;
        }
    }
}
