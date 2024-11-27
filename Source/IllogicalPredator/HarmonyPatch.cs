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

                if (conditions.ignoreBodySize)
                    // If body size should be ignored, check if combat power should be ignored or if the prey combat power is below the threshold
                    __result = prey.BodySize > predator.RaceProps.maxPreyBodySize || prey.kindDef.combatPower > 2f * predator.kindDef.combatPower;
                else
                    // If body size shouldn't be ignored, double check the body size condition isn't what caused the false result, then check to make sure power is actually what caused the false result 
                    __result |= prey.BodySize < predator.RaceProps.maxPreyBodySize && prey.kindDef.combatPower > 2f * predator.kindDef.combatPower;

                // Check the conditions that normally appear after the body size checks in the vanilla method. The above situations are checking that the body size or combat power are what made the result false, but these need to be checked to ensure none of the future conditions would have also made it false
                if (__result)
                    if ((predator.Faction != null && prey.Faction != null && !predator.HostileTo(prey)) ||
                        (predator.Faction != null && prey.HostFaction != null && !predator.HostileTo(prey)) ||
                        (predator.Faction == Faction.OfPlayer && prey.Faction == Faction.OfPlayer) ||
                        (predator.RaceProps.herdAnimal && predator.def == prey.def) ||
                        (prey.IsHiddenFromPlayer() || prey.IsPsychologicallyInvisible()) ||
                        (ModsConfig.AnomalyActive && prey.IsMutant && !prey.mutant.Def.canBleed))
                        __result = false;
            }
        }
    }
}
