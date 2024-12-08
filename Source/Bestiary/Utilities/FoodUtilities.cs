using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using static RimWorld.FoodUtility;
using UnityEngine;

namespace Bestiary
{
    public static class ExtendedFoodUtils
    {
        private static List<Pawn> tmpPredatorCandidates = [];
        public static Pawn BestPawnToHuntForPredator(Pawn predator, Func<Pawn, Pawn, float> preyScoreFunc,
            bool forceScanWholeMap=false, int maxRegionsToScan = 30, bool ignoreDownedOrDead = false)
        {
            if (forceScanWholeMap)
            {
                maxRegionsToScan = -1;
            }
            if (predator.meleeVerbs.TryGetMeleeVerb(null) == null)
            {
                return null;
            }

            bool flag = false;
            if (predator.health.summaryHealth.SummaryHealthPercent < 0.25f)
            {
                flag = true;
            }

            tmpPredatorCandidates.Clear();
            if (maxRegionsToScan < 0)
            {
                tmpPredatorCandidates.AddRange(predator.Map.mapPawns.AllPawnsSpawned);
            }
            else
            {
                TraverseParms traverseParms = TraverseParms.For(predator);
                RegionTraverser.BreadthFirstTraverse(predator.Position, predator.Map, (Region from, Region to) => to.Allows(traverseParms, isDestination: true), delegate (Region x)
                {
                    List<Thing> list = x.ListerThings.ThingsInGroup(ThingRequestGroup.Pawn);
                    for (int j = 0; j < list.Count; j++)
                    {
                        tmpPredatorCandidates.Add((Pawn)list[j]);
                    }

                    return false;
                }, maxRegionsToScan);
            }
            if (ignoreDownedOrDead)
            {
                tmpPredatorCandidates = tmpPredatorCandidates.Where(x => !x.DeadOrDowned).ToList();
            }

            Pawn targetPrey = null;
            float num = 0f;
            bool tutorialMode = TutorSystem.TutorialMode;
            for (int i = 0; i < tmpPredatorCandidates.Count; i++)
            {
                Pawn prey2 = tmpPredatorCandidates[i];
                if (predator.GetDistrict() == prey2.GetDistrict() && predator != prey2 && (!flag || prey2.Downed) && IsAcceptablePreyFor(predator, prey2) && predator.CanReach(prey2, PathEndMode.ClosestTouch, Danger.Deadly) && !prey2.IsForbidden(predator) && (!tutorialMode || prey2.Faction != Faction.OfPlayer))
                {
                    float preyScoreFor = preyScoreFunc(predator, prey2);
                    if (preyScoreFor > num || targetPrey == null)
                    {
                        num = preyScoreFor;
                        targetPrey = prey2;
                    }
                }
            }

            tmpPredatorCandidates.Clear();
            return targetPrey;
        }

        public static float GetPreyScoreCustom(Pawn predator, Pawn prey, float lengthFactor = 1,
            bool ignoreHealthy=false, bool preferHealthy=false,
            bool ignoreBig = false, bool preferBig = false,
            bool ignoreDangerous = false, bool preferDangerous = false,
            bool ignoreFence = false)
        {
            const float baseMultiplier = 56f;
            float combatPower = ignoreDangerous && !preferDangerous ? 1 : prey.kindDef.combatPower / predator.kindDef.combatPower;
            float health = ignoreHealthy || preferHealthy  ? 1 : prey.health.summaryHealth.SummaryHealthPercent;
            float bodySizeFactor = ignoreBig || preferBig ? 1 : prey.ageTracker.CurLifeStage.bodySizeFactor;
            float lengthHorizontal = (predator.Position - prey.Position).LengthHorizontal * lengthFactor;

            float bodySizeFactorPreferBig = preferBig ? prey.ageTracker.CurLifeStage.bodySizeFactor : 1;
            float preferHealthyFactor = preferHealthy ? health : 1;


            if (ignoreHealthy) { health = 1; }
            if (prey.Downed)
            {
                health = Mathf.Min(health, 0.2f);
            }

            float num3 = 0f - lengthHorizontal - baseMultiplier * health * health * combatPower * bodySizeFactor + baseMultiplier * bodySizeFactorPreferBig * preferHealthyFactor;
            if (prey.RaceProps.Humanlike)
            {
                num3 -= 15f;
            }
            else if (!ignoreFence && IsPreyProtectedFromPredatorByFence(predator, prey))
            {
                num3 -= 17f;
            }

            return num3;
        }
    }
}
