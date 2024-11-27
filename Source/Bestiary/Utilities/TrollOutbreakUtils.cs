using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using System.Diagnostics;
using UnityEngine;


namespace Bestiary
{
    public static class TrollOutbreakUtilities
    {
        [Unsaved]
        private static readonly Dictionary<Map, List<LocationCandidate>> burrowCandidates = [];
        //public static int TotalSpawnedBurrowsCount(Map map)
        //{
        //    List<Thing> list = map.listerThings.ThingsOfDef(BDefOfs.DA_TrollBurrow);
        //    return list.Count;
        //}

        public static bool TryGetBurrowCell(out IntVec3 pickedCell, Map map)
        {
            if (burrowCandidates.TryGetValue(map, out List<LocationCandidate> candidateList))
            {
                if (!candidateList.TryRandomElementByWeight((LocationCandidate x) => x.score, out var dictResult))
                {
                    pickedCell = IntVec3.Invalid;
                    return false;
                }
                else
                {
                    pickedCell = dictResult.cell;
                    return true;
                }
            }

            CellFinderUtility.CalculateDistanceToColonyBuildingGrid(map);

            burrowCandidates[map] = [];
            pickedCell = IntVec3.Invalid;

            List<(float score, IntVec3 pos)> cellScore = [];
            TrollSettings trollSettings = TrollSettings.Settings;

            foreach (var cell in map.AllCells)
            {
                float score = GetCellScore(map, cell, avoidColonyBuildings: true, trollSettings);
                if (score > 0)
                {
                    cellScore.Add((score, cell));
                }
            }

            if (cellScore.Count == 0)  // Ignore colony building avoidance if no cells are found
            {
                foreach (var cell in map.AllCells)
                {
                    float score = GetCellScore(map, cell, avoidColonyBuildings: false, trollSettings);
                    if (score > 0)
                    {
                        cellScore.Add((score, cell));
                    }
                }
            }
            if (cellScore.Count == 0)
            {
                Log.Warning($"[Bestiary] Could not find any potential cells for borrows on map {map}");
                return false;
            }

            burrowCandidates[map] = cellScore.Select(x => new LocationCandidate(x.pos, x.score)).ToList();
            burrowCandidates[map].TryRandomElementByWeight((LocationCandidate x) => x.score, out var result);
            pickedCell = result.cell;

            return true;
        }

        /// <summary>
        /// Get suitability for Troll Burrow. Currently any valid pos returns 1.0, but this could easily be expanded to include more complex logic.
        /// </summary>
        /// <returns></returns>
        private static float GetCellScore(Map map, IntVec3 cell, bool avoidColonyBuildings, TrollSettings settings)
        {
            float score = 0;
            if (avoidColonyBuildings)
            {
                const float colonyDistScore = 2f;
                Byte distance = CellFinderUtility.DistToColonyBuilding[cell];
                if (distance <  settings.burrowMinimumDistanceToColony) { return 0; }
                if (distance > settings.burrowIdealDistanceToColony)
                {
                    score += colonyDistScore;
                }
                else
                {
                    float iDist = settings.burrowIdealDistanceToColony;
                    score += colonyDistScore * (Mathf.Clamp(iDist - distance, 0.01f, iDist) / iDist);
                }
            }
            else
            {
                score += 1;
            }

            if (cell.GetRegion(map) == null) { return 0; }
            if (!cell.Walkable(map)) { return 0; }
            if (!TrollSettings.Settings.BurrowTerrains.Contains(cell.GetTerrain(map))) { return 0; }
            if (cell.Fogged(map)) { return 0; }
            if (CellHasBlockingThings(cell, map)) { return 0; }
            if (cell.Roofed(map) && !cell.GetRoof(map).isThickRoof) { return 0; }
            return score;
        }

        private static bool CellHasBlockingThings(IntVec3 cell, Map map)
        {
            List<Thing> thingList = cell.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i] is Pawn || thingList[i] is Building) { return true; }

                if (thingList[i].def.category == ThingCategory.Building && thingList[i].def.passability == Traversability.Impassable)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
