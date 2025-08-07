using RimWorld;
using UnityEngine;
using Verse;

namespace BestiaryGoldilox
{
    public class IncidentWorker_GoldiloxPasses : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map.gameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout))
            {
                return false;
            }
            if (ModsConfig.BiotechActive && map.gameConditionManager.ConditionIsActive(GameConditionDefOf.NoxiousHaze))
            {
                return false;
            }
            if (!map.mapTemperature.SeasonAndOutdoorTemperatureAcceptableFor(ThingDefOf.DA_Goldilox))
            {
                return false;
            }
            IntVec3 cell;
            return TryFindEntryCell(map, out cell);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!TryFindEntryCell(map, out var cell))
            {
                return false;
            }
            PawnKindDef DA_Goldilox = PawnKindDefOf.DA_Goldilox;
            int value = GenMath.RoundRandom(StorytellerUtility.DefaultThreatPointsNow(map) / DA_Goldilox.combatPower);
            int max = Rand.RangeInclusive(3, 6);
            value = Mathf.Clamp(value, 2, max);
            int num = Rand.RangeInclusive(90000, 150000);
            IntVec3 result = IntVec3.Invalid;
            if (!RCellFinder.TryFindRandomCellOutsideColonyNearTheCenterOfTheMap(cell, map, 10f, out result))
            {
                result = IntVec3.Invalid;
            }
            Pawn pawn = null;
            for (int i = 0; i < value; i++)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(cell, map, 10);
                pawn = PawnGenerator.GeneratePawn(DA_Goldilox);
                GenSpawn.Spawn(pawn, loc, map, Rot4.Random);
                pawn.mindState.exitMapAfterTick = Find.TickManager.TicksGame + num;
                if (result.IsValid)
                {
                    pawn.mindState.forcedGotoPosition = CellFinder.RandomClosewalkCellNear(result, map, 10);
                }
            }
            SendStandardLetter("LetterLabelGoldiloxPasses".Translate(DA_Goldilox.label).CapitalizeFirst(), "LetterGoldiloxPasses".Translate(DA_Goldilox.label), LetterDefOf.PositiveEvent, parms, pawn);
            return true;
        }

        private bool TryFindEntryCell(Map map, out IntVec3 cell)
        {
            return RCellFinder.TryFindRandomPawnEntryCell(out cell, map, CellFinder.EdgeRoadChance_Animal + 0.2f);
        }
    }
}
