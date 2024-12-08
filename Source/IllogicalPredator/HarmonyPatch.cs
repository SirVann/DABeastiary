using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IllogicalPredator
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);

        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("Rimworld.Alite.IllogicalPredators");

            harmony.Patch(AccessTools.Method(typeof(FoodUtility), nameof(FoodUtility.IsAcceptablePreyFor)),
                postfix: new HarmonyMethod(patchType, nameof(IsAcceptablePreyForPostfix)));
        }

        public static void IsAcceptablePreyForPostfix(ref bool __result, Pawn predator, Pawn prey)
        {
            if (!__result && predator.kindDef.HasModExtension<PredatorConditions>())
            {
                PredatorConditions conditions = predator.kindDef.GetModExtension<PredatorConditions>();

                // Checks to see if combat power was a reason that the vanilla check failed
                __result |= prey.kindDef.combatPower > 2f * predator.kindDef.combatPower;

                // If a basic cp check doesn't find anything, this checks if the weird combat power check was the reason for the failure
                __result |= prey.kindDef.combatPower * prey.health.summaryHealth.SummaryHealthPercent * prey.BodySize >=
                    predator.kindDef.combatPower * predator.health.summaryHealth.SummaryHealthPercent * predator.BodySize;

                // If the pawn shouldn't be ignoring body size, then this checks to make sure body size wasn't the reason for the failure
                if (!conditions.ignoreBodySize)
                    __result &= prey.BodySize > predator.RaceProps.maxPreyBodySize;

                // Check the conditions that normally appear after the body size checks in the vanilla method. The above situations are checking that the body size or combat power are what made the result false, but these need to be checked to ensure none of the future conditions would have also made it false
                if (__result)
                    __result &= ((predator.Faction == null || prey.Faction == null || predator.HostileTo(prey)) &&
                        (predator.Faction == null || prey.HostFaction == null || predator.HostileTo(prey)) &&
                        (predator.Faction != Faction.OfPlayer || prey.Faction != Faction.OfPlayer) &&
                        (!predator.RaceProps.herdAnimal || predator.def != prey.def) &&
                        (!prey.IsHiddenFromPlayer() && !prey.IsPsychologicallyInvisible()) &&
                        (!ModsConfig.AnomalyActive || !prey.IsMutant || prey.mutant.Def.canBleed));
            }
        }
    }
}
