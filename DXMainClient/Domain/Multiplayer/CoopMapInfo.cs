#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer
{
    public class CoopMapInfo
    {
        [JsonInclude]
        public List<CoopHouseInfo> EnemyHouses = new List<CoopHouseInfo>();

        [JsonInclude]
        public List<CoopHouseInfo> AllyHouses = new List<CoopHouseInfo>();

        [JsonInclude]
        public List<int> DisallowedPlayerSides = new List<int>();

        [JsonInclude]
        public List<int> DisallowedPlayerColors = new List<int>();

        public const int START_LOCATION_COUNT = 8;

        [JsonInclude]
        public List<int>[] DisallowedPlayerSidesByStart = new List<int>[START_LOCATION_COUNT];

        [JsonInclude]
        public List<int>[] DisallowedPlayerColorsByStart = new List<int>[START_LOCATION_COUNT];

        public CoopMapInfo() { }

        public void Initialize(IniSection section)
        {
            DisallowedPlayerSides = section.GetListValue("DisallowedPlayerSides", ',', int.Parse);
            DisallowedPlayerColors = section.GetListValue("DisallowedPlayerColors", ',', int.Parse);

            for (int i = 0; i < START_LOCATION_COUNT; i++)
            {
                var startSpecificSides = section.GetListValue($"DisallowedPlayerSides.Start{i}", ',', int.Parse);
                DisallowedPlayerSidesByStart[i] = DisallowedPlayerSides.Union(startSpecificSides).Distinct().ToList();

                var startSpecificColors = section.GetListValue($"DisallowedPlayerColors.Start{i}", ',', int.Parse);
                DisallowedPlayerColorsByStart[i] = DisallowedPlayerColors.Union(startSpecificColors).Distinct().ToList();
            }

            EnemyHouses = CoopHouseInfo.GetGenericHouseInfoList(section, "EnemyHouse");
            AllyHouses = CoopHouseInfo.GetGenericHouseInfoList(section, "AllyHouse");
        }

    }
}
