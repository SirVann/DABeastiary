using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Bestiary
{

    [HarmonyPatch(typeof(WaterBody), nameof(WaterBody.SetFishTypes))]
    public static class WaterBody_SetFishTypes
    {
        private static void Postfix(WaterBody __instance, ref List<ThingDef> ___commonFish)
        {
            if (__instance.map?.Biome.fishTypes == null)
            {
                return;
            }
            IList<TileMutatorDef> mutator = __instance.map.TileInfo.Mutators;
            foreach (TileMutatorDef mutatorDef in mutator)
            {
                List<ThingDef> outcomeList = mutatorDef.GetModExtension<TileMutatorExtension>()?.fishThings;
                if (outcomeList?.Count > 0)
                {
                    foreach (ThingDef thingDef in outcomeList)
                    {
                        ___commonFish.AddDistinct(thingDef);
                    }
                }
            }

        }
    }
}
