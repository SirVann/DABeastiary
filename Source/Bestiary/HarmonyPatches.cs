using System;
using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Bestiary
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);

        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("Rimworld.Alite.Bestiary");

            harmony.Patch(AccessTools.Method(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts)),
                postfix: new HarmonyMethod(patchType, nameof(MakeRecipeProductsPostfix)));
        }

        public static IEnumerable<Thing> MakeRecipeProductsPostfix(IEnumerable<Thing> __result, RecipeDef recipeDef, List<Thing> ingredients)
        {
            List<ThingDef> productsToFactor = new List<ThingDef>();
            float sizeFactor = -1;

            float corpseCount = 0;
            float totalBodySize = 0;

            if (!ingredients.NullOrEmpty())
                foreach (Thing thing in ingredients)
                    if (thing is Corpse corpse && corpse.InnerPawn?.kindDef?.HasModExtension<ProductByBodySize>() == true)
                    {
                        productsToFactor.AddRange(corpse.InnerPawn.kindDef.GetModExtension<ProductByBodySize>().products);
                        corpseCount++;
                        totalBodySize += corpse.InnerPawn.BodySize;
                    }

            if (corpseCount > 0)
                sizeFactor = totalBodySize / corpseCount; // Gets an average just in case multiple corpses are being used.

            for (int i = 0; i < __result.EnumerableCount(); i++)
            {
                Thing thing = __result.ElementAt(i);
                if (sizeFactor > -1 && productsToFactor.Contains(thing.def))
                    thing.stackCount = (int)Math.Ceiling((float)thing.stackCount * sizeFactor);
                yield return thing;
            }
        }
    }
}
