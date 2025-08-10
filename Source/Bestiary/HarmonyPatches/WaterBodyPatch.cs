using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Bestiary
{
    [HarmonyPatch(typeof(WaterBody), nameof(WaterBody.CommonFishIncludingExtras), MethodType.Getter)]
    public static class WaterBody_CommonFishIncludingExtras
    {
        private static IEnumerable<ThingDef> Postfix(IEnumerable<ThingDef> __result, WaterBody __instance)
        {
            foreach (ThingDef thingDef in __result)
            {
                yield return thingDef;
            }
            IList<TileMutatorDef> mutator = __instance.map.Tile.Tile.Mutators;
            foreach (TileMutatorDef mutatorDef in mutator)
            {
                List<ThingDef> outcomeList = mutatorDef.GetModExtension<TileMutatorExtension>()?.fishThings;
                if (outcomeList?.Count > 0)
                {
                    foreach (ThingDef thingDef in outcomeList)
                    {
                        yield return thingDef;
                    }
                }
            }
        }
    }
}
