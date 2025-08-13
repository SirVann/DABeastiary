using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Bestiary
{
    [HarmonyPatch(typeof(FishingUtility), nameof(FishingUtility.GetNegativeFishingOutcomes))]
    public static class FishingUtility_GetNegativeFishingOutcomes
    {
        public static void Postfix(ref List<NegativeFishingOutcomeDef> __result, Pawn pawn, IntVec3 cell)
        {
            if (__result.Count > 0)
            {
                Map map = pawn.Map;
                foreach (TileMutatorDef mutatorDef in map.TileInfo.Mutators)
                {
                    List<NegativeFishingOutcomeDef> outcomeList = mutatorDef.GetModExtension<TileMutatorExtension>()?.negativeFishingOutcomeDef;
                    if (outcomeList?.Count > 0)
                    {
                        foreach (NegativeFishingOutcomeDef negativeOutcomeDef in outcomeList)
                        {
                            __result.AddDistinct(negativeOutcomeDef);
                        }
                        break;
                    }
                }
            }
        }
    }

}
