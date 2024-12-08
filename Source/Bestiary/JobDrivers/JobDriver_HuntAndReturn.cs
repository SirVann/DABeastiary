using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using RimWorld;
using Verse.AI.Group;
using System.Security.Cryptography;
using RimWorld.Planet;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Scripting.GarbageCollector;
using UnityEngine;

namespace Bestiary
{
    public class JobDriver_HuntAndReturn : JobDriver
    {
        private bool notifiedPlayerAttacked;
        private bool notifiedPlayerAttacking;
        private bool firstHit = true;

        private const TargetIndex VictimInd = TargetIndex.A;

        private const TargetIndex StoreCellInd = TargetIndex.B;

        private int jobStartTick = -1;
        private int attempts = 10;

        // How long to hunt before giving up/restarting.
        private const int MaxHuntTicks = 5000;

        // How far away from the target to stand when shooting relative to max range.
        // For colonists this would be 95%, but I figure animals should be a more aggressive.
        public const float MaxRangeFactor = 0.75f;

        // This somewhat weird targeting Logic is joinked from Ludeon's JobDriver_Hunt
        // Better to use the same to avoid unecessary suprises.
        public Pawn Prey
        {
            get
            {
                Corpse corpse = Corpse;
                if (corpse != null)
                {
                    return corpse.InnerPawn;
                }
                return (Pawn)job?.GetTarget(TargetIndex.A).Thing;
            }
        }
        private Corpse Corpse => job?.GetTarget(VictimInd).Thing as Corpse;

        protected virtual bool CanSlaughter => false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref jobStartTick, "jobStartTick", 0);
            Scribe_Values.Look(ref firstHit, "firstHit", defaultValue: false);
            Scribe_Values.Look(ref notifiedPlayerAttacking, "notifiedPlayerAttacking", defaultValue: false);
        }

        public override string GetReport()
        {
            if (Prey != null)
            {
                return JobUtility.GetResolvedJobReport(job.def.reportString, Prey);
            }
            return base.GetReport();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Prey, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil startToil = ToilMaker.MakeToil("MakeNewToils");
            startToil.initAction = delegate
            {
                jobStartTick = Find.TickManager.TicksGame;
            };


            AddFinishAction(delegate
            {
                Map.attackTargetsCache.UpdateTarget(pawn);
            });
            Toil trySelectCorpse = ToilMaker.MakeToil("MakeNewToils");
            trySelectCorpse.initAction = delegate
            {
                Pawn actor = trySelectCorpse?.actor;
                Corpse corpse = Corpse;
                if (actor == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                if (corpse == null)
                {
                    Pawn prey = Prey;
                    if (prey == null)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                }
                // We have a live prey, drag that back instead.
                if (Prey == null && corpse != null)
                {
                    if (actor.Faction == Faction.OfPlayer)
                    {
                        corpse.SetForbidden(value: false, warnOnFail: false);
                    }
                    else
                    {
                        corpse.SetForbidden(value: true, warnOnFail: false);
                    }
                    actor.CurJob.SetTarget(VictimInd, corpse);
                }
            };
            yield return startToil;

            var getAttackVerb = Toils_Combat.TrySetJobToUseAttackVerb(VictimInd);

            Toil startCollectCorpseLabel = Toils_General.Label();
            Toil gotoCastPos = Toils_Combat
                .GotoCastPosition(VictimInd, TargetIndex.None, closeIfDowned: false, MaxRangeFactor)
                .JumpIfDespawnedOrNull(VictimInd, startCollectCorpseLabel).FailOn(() => Find.TickManager.TicksGame > jobStartTick + MaxHuntTicks);

            gotoCastPos.AddPreTickAction(CheckWarnPlayer);

            ////////////////////////////////////////////
            /// Combat
            yield return getAttackVerb;
            yield return gotoCastPos;
            yield return Toils_Jump.JumpIfTargetNotHittable(VictimInd, getAttackVerb);
            var castVerb = Toils_Combat.CastVerb(VictimInd, canHitNonTargetPawns: false).JumpIfTargetDespawnedOrNullOrDowned(VictimInd, startCollectCorpseLabel).FailOn(() => Find.TickManager.TicksGame > jobStartTick + MaxHuntTicks);
            castVerb.AddFinishAction(delegate
            {
                if (firstHit && !notifiedPlayerAttacked && PawnUtility.ShouldSendNotificationAbout(Prey))
                {
                    notifiedPlayerAttacked = true;
                    Messages.Message("MessageAttackedByPredator".Translate(Prey.LabelShort, pawn.LabelIndefinite(), Prey.Named("PREY"), pawn.Named("PREDATOR")).CapitalizeFirst(), Prey, MessageTypeDefOf.ThreatSmall);
                }
            });
            castVerb.AddPreTickAction(delegate
            {
                if (pawn?.GetLord()?.LordJob is LordJob_TrollBurrow lordJob)
                {
                    lordJob.Notify_HuntingGroundReached();
                }
            });
            yield return castVerb;
            yield return CustomToils.JumpIfTargetDespawnedOrNullOrDowned(VictimInd, startCollectCorpseLabel);

            yield return Toils_Jump.Jump(gotoCastPos);  // Go back into  combat is target is still up.

            ////////////////////////////////////////////
            /// Collect Corpse
            yield return Toils_Jump.Jump(startCollectCorpseLabel);
            yield return startCollectCorpseLabel;
            yield return trySelectCorpse;
            yield return StartCollectCorpseToil();
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(VictimInd);
            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(StoreCellInd);
            yield return carryToCell;
            yield return Toils_Haul.PlaceHauledThingInCell(StoreCellInd, carryToCell, storageMode: pawn.Faction == Faction.OfPlayer);
        }

        private Toil StartCollectCorpseToil()
        {
            Toil toil = ToilMaker.MakeToil("StartCollectCorpseToil");
            toil.initAction = delegate
            {
                if (Prey == null)
                {
                    toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
                else
                {
                    TaleRecorder.RecordTale(TaleDefOf.Hunted, pawn, Prey);
                    bool pawnIsNotDowned = Prey.Spawned && !Prey.DeadOrDowned;
                    ThingWithComps haulTarget = Prey?.Spawned == true ? Prey : Prey?.Corpse;
                    bool isCorpse = haulTarget is Corpse;

                    if (haulTarget == null || pawnIsNotDowned || !pawn.CanReserveAndReach(haulTarget, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    }
                    else
                    {
                        if (pawn?.Faction == Faction.OfPlayer && StoreUtility.TryFindBestBetterStoreCellFor(haulTarget, pawn, map: Map, StoragePriority.Unstored, pawn.Faction, out var foundCell))
                        {
                            if (isCorpse) haulTarget.SetForbidden(value: false);
                            pawn.Reserve(haulTarget, job);
                            pawn.Reserve(foundCell, job);
                            job.SetTarget(StoreCellInd, foundCell);
                            job.SetTarget(VictimInd, haulTarget);
                            job.count = 1;
                            job.haulMode = HaulMode.ToCellStorage;
                        }
                        else
                        {
                            if (isCorpse) haulTarget.SetForbidden(value: true);
                            int radius = 7;
                            int minRadius = radius / 2;

                            IntVec3 randomOffset = GetValidPointInDoughnut(radius, minRadius).RandomElement();

                            // Add the randomOffset to the flagLoc and find a nearby walkable cell
                            IntVec3 basePos = job.GetTarget(StoreCellInd).Cell;
                            IntVec3 targetPos = basePos + randomOffset;

                            if (!targetPos.InBounds(pawn.Map)) { targetPos = basePos; }
                            IntVec3 intVec = CellFinder.RandomClosewalkCellNear(targetPos, Map, 5);

                            pawn.Reserve(haulTarget, job);
                            pawn.Reserve(intVec, job);
                            job.SetTarget(StoreCellInd, targetPos);
                            job.SetTarget(VictimInd, haulTarget);
                            job.count = 1;
                        }
                    }
                }
            };
            return toil;
        }
        List<IntVec3> GetValidPointInDoughnut(int radius, int minRadius)
        {
            int diameter = radius * 2 + 1;
            return Enumerable.Range(-radius, diameter)
                .SelectMany(dx => Enumerable.Range(-radius, diameter), (dx, dz) => new IntVec3(dx, 0, dz))
                .Where(position =>
                {
                    int lengthSquared = position.LengthHorizontalSquared;
                    bool isWithinMinRadius = lengthSquared >= minRadius * minRadius;
                    bool isWithinMaxRadius = lengthSquared <= radius * radius;
                    return isWithinMinRadius && isWithinMaxRadius;
                }).ToList();
        }

        private void CheckWarnPlayer()
        {
            if (notifiedPlayerAttacking)
            {
                return;
            }
            Pawn prey = Prey;
            if (prey.Spawned && prey.Faction == Faction.OfPlayer && Find.TickManager.TicksGame > pawn.mindState.lastPredatorHuntingPlayerNotificationTick + 2500 && prey.Position.InHorDistOf(pawn.Position, 60f))
            {
                if (prey.RaceProps.Humanlike)
                {
                    Find.LetterStack.ReceiveLetter("LetterLabelPredatorHuntingColonist".Translate(pawn.LabelShort, prey.LabelDefinite(), pawn.Named("PREDATOR"), prey.Named("PREY")).CapitalizeFirst(), "LetterPredatorHuntingColonist".Translate(pawn.LabelIndefinite(), prey.LabelDefinite(), pawn.Named("PREDATOR"), prey.Named("PREY")).CapitalizeFirst(), LetterDefOf.ThreatBig, pawn);
                }
                else
                {
                    Messages.Message((prey.Name.Numerical ? "LetterPredatorHuntingColonist" : "MessagePredatorHuntingPlayerAnimal").Translate(pawn.Named("PREDATOR"), prey.Named("PREY")), pawn, MessageTypeDefOf.ThreatBig);
                }
                pawn.mindState.Notify_PredatorHuntingPlayerNotification();
                notifiedPlayerAttacking = true;
            }
        }
    }
    public static class CustomToils
    {
        public static Toil LogPrint(string message)
        {
            Log.ResetMessageCount();
            Log.Message($"Added Debug Toil: {message}");
            return Toils_General.Do(delegate { Log.Message(message); });
        }

        public static Toil JumpIfTargetDespawnedOrNullOrDowned(this Toil toil, TargetIndex ind, Toil jumpToil)
        {
            return toil.JumpIf(delegate
            {
                Thing thing = toil.actor.jobs.curJob.GetTarget(ind).Thing;
                try
                {
                    return thing == null || !thing.Spawned || (thing is Pawn p && p.health?.State != PawnHealthState.Mobile);
                }
                catch (Exception e)
                {
                    Log.Error($"[Bestiary] Error in JumpIfTargetDespawnedOrNullOrDowned: {e}");
                    return false;
                }
            }, jumpToil);
        }

        public static Toil JumpIfTargetDespawnedOrNullOrDowned(TargetIndex ind, Toil jumpToil)
        {
            Toil toil = ToilMaker.MakeToil("JumpIfTargetDespawnedOrNullOrDowned");
            toil.initAction = delegate
            {
                Thing thing = toil.actor.jobs.curJob.GetTarget(ind).Thing;
                try
                {
                    if (thing == null || !thing.Spawned || (thing is Pawn p && p.health?.State != PawnHealthState.Mobile))
                    {
                        toil.actor.jobs.curDriver.JumpToToil(jumpToil);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"[Bestiary] Error in JumpIfTargetDespawnedOrNullOrDowned: {e}");
                }
            };
            return toil;
        }

    }
}
