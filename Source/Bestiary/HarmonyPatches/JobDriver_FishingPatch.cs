using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace Bestiary
{
    [HarmonyPatch(typeof(JobDriver_Fish), "CompleteFishingToil")]
    public static class JobDriver_Fish_CompleteFishingToil_Helper
    {

        public static MethodInfo AnonymousMethod;

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);

            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Ldftn && codes[i + 1].opcode == OpCodes.Newobj)
                {
                    AnonymousMethod = (MethodInfo)codes[i].operand;
                    break;
                }
            }

            return codes;
        }
    }

    [HarmonyPatch]
    public static class JobDriver_Fish_CompleteFishingToil_Transpile
    {
        static MethodBase TargetMethod()
        {
            if (JobDriver_Fish_CompleteFishingToil_Helper.AnonymousMethod != null)
            {
                return JobDriver_Fish_CompleteFishingToil_Helper.AnonymousMethod;
            }
            throw new Exception("Bestiary ::  Could not find the anonymous function for JobDriver_Fish_CompleteFishingToil_Transpile");
        }


        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            MethodInfo injectionSite = AccessTools.Method(typeof(LetterStack), nameof(LetterStack.ReceiveLetter),
            [
                typeof(TaggedString),
                typeof(TaggedString),
                typeof(LetterDef),
                typeof(LookTargets),
                typeof(Faction),
                typeof(Quest),
                typeof(List<ThingDef>),
                typeof(string),
                typeof(int),
                typeof(bool)
            ]);
            FieldInfo jobDriver = AccessTools.Field(typeof(JobDriver), nameof(JobDriver.pawn));
            MethodInfo helperMethod = AccessTools.Method(typeof(JobDriver_Fish_CompleteFishingToil_Transpile), "SpawnCreature");
            bool foundInjection = false;
            foreach (CodeInstruction instruction in codes)
            {
                yield return instruction;
                if (instruction.Calls(injectionSite) && !foundInjection)
                {
                    // NegativeFishingOutcomeDef
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 5);
                    // Returns Pawn
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, jobDriver);
                    // Calls the helper method to ease with the patch
                    yield return new CodeInstruction(OpCodes.Call, helperMethod);
                    foundInjection = true;
                }
            }
            if (!foundInjection)
            {
                Log.Error($"Bestiary :: Failed to find injection point when applying JobDriver_Fish_CompleteFishingToil_Transpile");
            }
        }

        static void SpawnCreature(NegativeFishingOutcomeDef negativeOutcome, Pawn fisher)
        {
            NegativeFishingExtension modExtension = negativeOutcome.GetModExtension<NegativeFishingExtension>();
            if (modExtension?.pawnKindDef == null || fisher == null) return;
            IntVec3 intVec3 = CellFinder.RandomClosewalkCellNear(fisher.Position, fisher.Map, 3);
            Pawn spawned = PawnGenerator.GeneratePawn(modExtension.pawnKindDef, null);
            spawned.ageTracker.AgeBiologicalTicks = (long)(3600000 * modExtension.pawnAge);
            GenSpawn.Spawn(spawned, intVec3, fisher.Map, Rot4.Random, WipeMode.Vanish, false);
            if (modExtension.manHunter)
            {
                spawned.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, true, false, false, null, false, false, false);
            }
        }

    }
}
