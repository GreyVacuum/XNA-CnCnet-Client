using System;
using System.Collections.Generic;
using ClientCore;

namespace DTAClient.Domain.Multiplayer
{
    public class PlayerHouseInfo
    {
        public int SideIndex { get; set; }

        /// <summary>
        /// A side (or, more correctly, house or country depending on the game)
        /// index that is used in rules file of the game.
        /// </summary>
        public int InternalSideIndex
        {
            get
            {
                if (IsSpectator && !string.IsNullOrEmpty(ClientConfiguration.Instance.SpectatorInternalSideIndex))
                    return int.Parse(ClientConfiguration.Instance.SpectatorInternalSideIndex);
                
                if (!string.IsNullOrEmpty(ClientConfiguration.Instance.InternalSideIndices))
                    return Array.ConvertAll(ClientConfiguration.Instance.InternalSideIndices.Split(','), int.Parse)[SideIndex];

                return SideIndex;
            }
        }
        public int ColorIndex { get; set; }
        public int StartingWaypoint { get; set; }

        public int RealStartingWaypoint { get; set; }

        public bool IsSpectator { get; set; }

        /// <summary>
        /// Applies the player's side into the information
        /// and randomizes it if necessary.
        /// </summary>
        /// <param name="pInfo">The PlayerInfo of the player.</param>
        /// <param name="sideCount">The number of sides in the game.</param>
        /// <param name="random">Random number generator.</param>
        /// <param name="disallowedSideArray">A bool array that determines which side indexes are disallowed by game options.</param>
        public void RandomizeSide(PlayerInfo pInfo, int sideCount, Random random,
            bool[] disallowedSideArray, List<int[]> randomSelectors, int randomCount)
        {
            int PickRandomAllowedSide()
            {
                int allowedCount = 0;
                for (int s = 0; s < sideCount; s++)
                {
                    if (!disallowedSideArray[s])
                        allowedCount++;
                }

                if (allowedCount == 0)
                    return 0;

                int sideId;
                do sideId = random.Next(0, sideCount);
                while (disallowedSideArray[sideId]);

                return sideId;
            }

            if (pInfo.SideId == 0 || pInfo.SideId == sideCount + randomCount)
            {
                // The player has selected Random or Spectator
                SideIndex = PickRandomAllowedSide();
            }

            if (pInfo.SideId == 0 || pInfo.SideId == sideCount + randomCount)
            {
                // The player has selected Random or Spectator
                SideIndex = PickRandomAllowedSide();
            }
            else
            {
                // Use custom random selector.
                if (pInfo.SideId < randomCount)
                {
                    int[] randomsides = randomSelectors[pInfo.SideId - 1];
                    int count = randomsides.Length;

                    int allowedCount = 0;
                    foreach (int side in randomsides)
                    {
                        if (!disallowedSideArray[side])
                            allowedCount++;
                    }

                    if (allowedCount == 0)
                    {
                        SideIndex = randomsides[0];
                        return;
                    }

                    int sideId;
                    do sideId = randomsides[random.Next(0, count)];
                    while (disallowedSideArray[sideId]);

                    SideIndex = sideId;
                }
                else
                {
                    // The player has selected a side
                    int selectedSideIndex = pInfo.SideId - randomCount;

                    if (selectedSideIndex >= 0 && selectedSideIndex < sideCount && !disallowedSideArray[selectedSideIndex])
                    {
                        SideIndex = selectedSideIndex;
                    }
                    else
                    {
                        // Selected side is disallowed; fall back to random among allowed sides
                        SideIndex = PickRandomAllowedSide();
                    }
                }
            }
        }

        /// <summary>
        /// Applies the player's color into the information and randomizes
        /// it if necessary. If the color is randomized, it's removed
        /// from the list of available colors.
        /// </summary>
        /// <param name="pInfo">The PlayerInfo of the player.</param>
        /// <param name="freeColors">The list of available (un-used) colors.</param>
        /// <param name="mpColors">The list of all multiplayer colors.</param>
        /// <param name="random">Random number generator.</param>
        public void RandomizeColor(PlayerInfo pInfo, List<int> freeColors,
            List<MultiplayerColor> mpColors, Random random)
        {
            int selectedColorId = pInfo.ColorId - 1;
            bool selectedColorValid = selectedColorId >= 0 && selectedColorId < mpColors.Count;

            if (pInfo.ColorId == 0 || !selectedColorValid)
            {
                // The player has selected Random for their color, or their selected color index is invalid.
                // Pick a random color from the pool of still-available colors.
                if (freeColors.Count > 0)
                {
                    int randomizedColorIndex = random.Next(0, freeColors.Count);
                    int actualColorId = freeColors[randomizedColorIndex];

                    ColorIndex = mpColors[actualColorId].GameColorIndex;
                    freeColors.RemoveAt(randomizedColorIndex);
                }
                else if (selectedColorValid)
                {
                    // No free colors left, but the player's explicit selection is valid; use it.
                    ColorIndex = mpColors[selectedColorId].GameColorIndex;
                }
                else
                {
                    // No free colors and no valid explicit selection; fall back to a random color from the full list.
                    ColorIndex = mpColors[random.Next(0, mpColors.Count)].GameColorIndex;
                }
            }
            else
            {
                // The player has selected a specific color. Always honor that choice
                // (GameLobbyBase.Randomize has already removed it from the global pool,
                // so it may not appear in the local freeColors list passed here).
                ColorIndex = mpColors[selectedColorId].GameColorIndex;
                freeColors.Remove(selectedColorId);
            }
        }

        /// <summary>
        /// Applies the player's starting location into the information and
        /// randomizes it if necessary. If the starting location is randomized,
        /// the starting location is removed from the list of available starting locations.
        /// </summary>
        /// <param name="pInfo">The PlayerInfo of the player.</param>
        /// <param name="freeStartingLocations">List of free starting locations.</param>
        /// <param name="random">Random number generator.</param>
        /// <param name="takenStartingLocations">A list of starting locations that are already occupied.</param>
        /// <param name="overrideGameRandomLocations"></param>
        /// <returns>True if the player's starting location index exceeds the map's number of starting waypoints,
        /// otherwise false.</returns>
        public void RandomizeStart(
            PlayerInfo pInfo, 
            Random random,
            List<int> freeStartingLocations, 
            List<int> takenStartingLocations,
            bool overrideGameRandomLocations
        )
        {
            overrideGameRandomLocations |= ClientConfiguration.Instance.UseClientRandomStartLocations;
            if (IsSpectator)
            {
                StartingWaypoint = 90;
                return;
            }

            if (pInfo.StartingLocation == 0)
            {
                // Randomize starting location

                if (!overrideGameRandomLocations)
                {
                    // The game uses its own randomization logic that places
                    // randomized players on the opposite side of the map
                    // Players seem to prefer this behaviour, so use -1 to
                    // leave randomizing the starting location to the game itself
                    RealStartingWaypoint = -1;
                    StartingWaypoint = -1;
                    return;
                }

                // Let the client pick starting positions.
                if (freeStartingLocations.Count == 0) // No free starting locs available
                {
                    RealStartingWaypoint = -1;
                    StartingWaypoint = -1;
                    return;
                }

                int waypointIndex = random.Next(0, freeStartingLocations.Count);
                RealStartingWaypoint = freeStartingLocations[waypointIndex];
                StartingWaypoint = RealStartingWaypoint;
                freeStartingLocations.Remove(StartingWaypoint);
                return;
            }

            // Use the player's selected starting location
            RealStartingWaypoint = pInfo.StartingLocation - 1;

            if (takenStartingLocations.Contains(RealStartingWaypoint))
            {
                StartingWaypoint = -1; // Unknown starting location, stacked with another player
                return;
            }

            takenStartingLocations.Add(RealStartingWaypoint);

            StartingWaypoint = RealStartingWaypoint;
        }
    }
}
