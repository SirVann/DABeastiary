using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Bestiary
{
    [HarmonyPatch]
    public static class NotifyDamageTakenPatch
    {
        [HarmonyPatch(typeof(Pawn_MindState), nameof(Pawn_MindState.Notify_DamageTaken))]
        [HarmonyPostfix]
        public static void Notify_DamageTakenPostfix(Pawn_MindState __instance, DamageInfo dinfo)
        {
            var pawn = __instance.pawn;
            if (!dinfo.Def.ExternalViolenceFor(pawn))
            {
                return;
            }

            if (pawn.Spawned && !pawn.DeadOrDowned && pawn.kindDef == DADefOfs.DA_BeardedTroll)
            {
                Pawn aggressor = dinfo.Instigator as Pawn;
                if (!__instance.mentalStateHandler.InMentalState && dinfo.Instigator != null &&
                    (aggressor != null || dinfo.Instigator is Building_Turret))
                {
                    if (pawn.CurJob is Job curJob && curJob.def == DADefOfs.DA_HuntAndReturn && curJob.AnyTargetIs(aggressor))
                    {
                        return;
                    }

                    bool validManhunterTrigger = (
                            dinfo.Instigator.Faction != null &&
                            (dinfo.Instigator.Faction.def.humanlikeFaction || (aggressor != null && (int)aggressor.def.race.intelligence >= 1)) &&
                            (pawn.Faction == null || !pawn.Faction.def.humanlikeFaction) &&
                            (pawn.IsNonMutantAnimal || pawn.IsWildMan()) &&
                            Rand.Chance(PawnUtility.GetManhunterOnDamageChance(pawn, dinfo.Instigator))
                        );

                    bool isEngagedInMelee = pawn.CurJob?.def == JobDefOf.AttackMelee;
                    float distanceToAggressor = pawn.Position.DistanceTo(dinfo.Instigator.Position);

                    if (!isEngagedInMelee && validManhunterTrigger && Rand.Chance(0.50f))
                    {
                        __instance.StartPackManhunterBecauseOfPawnAction(aggressor, "AnimalManhunterFromDamage", causedByDamage: true);
                    }
                    else if (aggressor != null && !isEngagedInMelee && distanceToAggressor < 20 && pawn.CanReach(dinfo.Instigator, PathEndMode.Touch, Danger.Deadly))
                    {
                        var jobDef = JobDefOf.AttackMelee;
                        var job = new Job(jobDef, dinfo.Instigator)
                        {
                            expiryInterval = Rand.Range(500, 2000),
                            checkOverrideOnExpire = true
                        };
                        pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                    }
                    else if (!isEngagedInMelee && validManhunterTrigger)
                    {
                        __instance.StartManhunterBecauseOfPawnAction(aggressor, "AnimalManhunterFromDamage", causedByDamage: true);
                    }
                }
                else if (dinfo.Instigator != null && dinfo.Def.makesAnimalsFlee && Pawn_MindState.CanStartFleeingBecauseOfPawnAction(pawn))
                {
                    __instance.StartFleeingBecauseOfPawnAction(dinfo.Instigator);
                }
            }
        }

        public static void StartPackManhunterBecauseOfPawnAction(this Pawn_MindState mState, Pawn instigator, string letterTextKey, bool causedByDamage = false)
        {
            Pawn pawn = mState.pawn;
            if (!mState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter))
            {
                return;
            }

            string text = letterTextKey.Translate(pawn.Label, pawn.Named("PAWN")).AdjustedFor(pawn);
            GlobalTargetInfo globalTargetInfo = pawn;

            int num2 = 1;
            foreach (Pawn packmate in mState.GetPackmates(pawn, 24f))
            {
                if (packmate.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, forced: false, forceWake: false, causedByMood: false, null, transitionSilently: false, causedByDamage))
                {
                    num2++;
                }
            }
            globalTargetInfo = new TargetInfo(pawn.Position, pawn.Map);
            text += "\n\n";
            text += "AnimalManhunterOthers".Translate(pawn.kindDef.GetLabelPlural(), pawn);
            string text2 = (pawn.IsNonMutantAnimal ? pawn.Label : pawn.def.label);
            string text3 = "LetterLabelAnimalManhunterRevenge".Translate(text2).CapitalizeFirst();
            Find.LetterStack.ReceiveLetter(text3, text, (num2 == 1) ? LetterDefOf.ThreatSmall : LetterDefOf.ThreatBig, globalTargetInfo);
        }
    }
}
