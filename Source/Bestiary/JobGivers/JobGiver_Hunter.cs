using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Bestiary
{
    public class JobGiver_HuntAndReturn : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {

            Pawn prey = ExtendedFoodUtils.BestPawnToHuntForPredator(pawn, (predator, prey) => ExtendedFoodUtils.GetPreyScoreCustom(predator, prey),
                forceScanWholeMap:true, ignoreDownedOrDead:true);
            if (prey != null && pawn.CanReserveAndReach(prey, PathEndMode.Touch, Danger.Deadly))
            {
                IntVec3 returnLocation = pawn.Position;
                return JobMaker.MakeJob(DADefOfs.DA_HuntAndReturn, prey, returnLocation);
            }
            
            return null;
        }
    }

    public class JobGiver_BreakOut : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.CanReachMapEdge())
            {
                return null;
            }
            var canExit = RCellFinder.TryFindRandomExitSpot(pawn, out IntVec3 exitPos, TraverseMode.PassAllDestroyableThings);
            using PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, exitPos, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings));
            if (!canExit || path == null || !path.Found || path.NodesLeftCount < 2)
            {
                Log.WarningOnce($"[Bestiary] {pawn} cannot find a way out of the map. Path found: {path?.ToStringSafe()}", 123234892);
                return null;
            }
            else
            {
                Thing thing = path.FirstBlockingBuilding(out IntVec3 cellBefore, pawn);
                if (thing != null)
                {
                    Job job = DigUtility.PassBlockerJob(pawn, thing, cellBefore, canMineMineables: true, canMineNonMineables: true);
                    if (job != null)
                    {
                        return job;
                    }
                }
            }
            return null;
        }
    }

    public class JobGiver_HunterLordGroup : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            Need_Food food = pawn.needs.food;
            if (food.CurLevelPercentage < 0.1f)
            {
                return 1f;
            }
            return 100f;
        }


        protected override Job TryGiveJob(Pawn pawn)
        {
            var lord = pawn.GetLord();
            var primaryHunter = pawn;
            var haulTarget = pawn.Position;
            if (lord != null)
            {
                primaryHunter = lord.GetCapableLeader() ?? pawn;
                haulTarget = lord.CurLordToil.FlagLoc;
            }

            Pawn prey = ExtendedFoodUtils.BestPawnToHuntForPredator(pawn, (predator, prey) =>
                    ExtendedFoodUtils.GetPreyScoreCustom(predator, prey, ignoreBig:false, ignoreDangerous:false, ignoreFence:true),
                maxRegionsToScan: 200, ignoreDownedOrDead: true);

            // Follow the pack lead until they reach the hunting grounds.
            if ((primaryHunter != pawn) && lord.LordJob is LordJob_TrollBurrow tb && !tb.huntingGroundReached)
            {
                return new Job(JobDefOf.Follow, primaryHunter, expiryInterval: 200, checkOverrideOnExpiry: true);
            }

            if (prey != null && pawn.CanReserveAndReach(prey, PathEndMode.Touch, Danger.Deadly))
            {
                var job = JobMaker.MakeJob(DADefOfs.DA_HuntAndReturn);
                job.targetA = prey;
                job.targetB = haulTarget;
                job.checkOverrideOnExpire = true;
                return job;
            }
            return null;
        }
    }

    
}
