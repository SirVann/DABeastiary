using System;
using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Bestiary
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);

        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("Rimworld.Alite.Bestiary");

            harmony.Patch(AccessTools.Method(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts)),
                postfix: new HarmonyMethod(patchType, nameof(MakeRecipeProductsPostfix)));

            harmony.PatchAll();
        }

        public static IEnumerable<Thing> MakeRecipeProductsPostfix(IEnumerable<Thing> __result, RecipeDef recipeDef, List<Thing> ingredients)
        {
            List<Thing> originalProducts = new List<Thing>(__result.ToList());
            List<ThingDef> productsToFactor = [];

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

            foreach (Thing thing in originalProducts)
            {
                if (sizeFactor > 0 && productsToFactor.Contains(thing.def))
                    thing.stackCount = (int)Math.Ceiling((float)thing.stackCount * sizeFactor);
                yield return thing;
            }
        }
    }
}
