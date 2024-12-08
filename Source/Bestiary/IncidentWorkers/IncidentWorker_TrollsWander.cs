using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Noise;


namespace Bestiary
{

    public class IncidentWorker_TrollsWander : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return map.mapTemperature.SeasonAndOutdoorTemperatureAcceptableFor(DADefOfs.DA_BeardedTroll.race) &&
                base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var trollSettings = TrollSettings.Settings;
            float threatScale = trollSettings.wanderThreatScale.RandomInRange;
            
            bool stickAround = Rand.Chance(trollSettings.trollStickAroundChance);
            int stayTime = trollSettings.trollWanderStayTime.RandomInRange;

            Map map = (Map)parms.target;
            PawnKindDef trollKind = DADefOfs.DA_BeardedTroll;
            IntVec3 result = parms.spawnCenter;
            if (!result.IsValid && !RCellFinder.TryFindRandomPawnEntryCell(out result, map, CellFinder.EdgeRoadChance_Animal))
            {
                return false;
            }
            int animalCount = AggressiveAnimalIncidentUtility.GetAnimalsCount(trollKind, parms.points * threatScale);

            List<Pawn> trollList = Enumerable.Range(0, animalCount)
                                        .Select(_ => PawnGenerator.GeneratePawn(trollKind, parms.faction))
                                        .ToList();

            Rot4 rot = Rot4.FromAngleFlat((map.Center - result).AngleFlat);
            for (int i = 0; i < trollList.Count; i++)
            {
                Pawn pawn = trollList[i];
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(result, map, 10);
                QuestUtility.AddQuestTag(GenSpawn.Spawn(pawn, loc, map, rot), parms.questTag);
                if (!stickAround)
                {
                    pawn.mindState.exitMapAfterTick = Find.TickManager.TicksGame + stayTime;
                }

                var foodNeed = pawn.needs?.food;
                if (foodNeed != null)
                {
                    foodNeed.CurLevel = Rand.Range(0.1f, 0.6f);
                }
            }
            SendStandardLetter(parms, trollList, animalCount);
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            return true;
        }
    }

}
