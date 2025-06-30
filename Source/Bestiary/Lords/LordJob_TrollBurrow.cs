using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Noise;

namespace Bestiary
{
    public static class LordUtils
    {
        public static Pawn GetCapableLeader(this Lord l) => l.ownedPawns.FirstOrDefault(p => !p.DeadOrDowned);
    }

    public class LordJob_TrollBurrow : LordJob
    {
        // Based on LordJob_DefendPoint
        private int tickLastActive = 0;

        public ThingWithComps burrow;

        private float? wanderRadius;

        private float? defendRadius;

        private bool isCaravanSendable;

        private bool addFleeToil;

        protected int maxTimeSpentWorking = 10000;

        public int timeSpentActiveToday = 0;

        public override bool IsCaravanSendable => isCaravanSendable;

        public override bool AddFleeToil => addFleeToil;

        public bool huntingGroundReached = false;

        public int bodiesNearDen = 0;

        public LordJob_TrollBurrow()
        {
            tickLastActive = Find.TickManager.TicksGame;
        }

        public LordJob_TrollBurrow(ThingWithComps burrow, float? wanderRadius = null, float? defendRadius = null, bool isCaravanSendable = false, bool addFleeToil = true, int maxActivePerDay=10000)
        {
            this.burrow = burrow;
            this.wanderRadius = wanderRadius;
            this.defendRadius = defendRadius;
            this.isCaravanSendable = isCaravanSendable;
            this.addFleeToil = addFleeToil;
            this.maxTimeSpentWorking = maxActivePerDay;
            tickLastActive = Find.TickManager.TicksGame;
        }

        protected bool CanWorkMore() => timeSpentActiveToday < maxTimeSpentWorking;

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new();

            var burrowToil = new LordToil_DenAnimalToil(burrow.Position, wanderRadius: wanderRadius, defendRadius: defendRadius);
            stateGraph.AddToil(burrowToil);

            var hunterToil = new LordToil_DenAnimalHunt(burrow.Position, wanderRadius: wanderRadius);
            stateGraph.AddToil(hunterToil);

            //var hunterToil = new LordToil_DenAnimalHunt(burrow.Position, wanderRadius: wanderRadius);
            //stateGraph.AddToil(hunterToil);

            var startHunting = new Transition(burrowToil, hunterToil);

            startHunting.AddPreAction(
                new TransitionAction_Custom(() =>
                {
                    huntingGroundReached = false;
                })
            );
            // If someone starts hunting, everyone else follows until they endgage.
            startHunting.AddTrigger(new Trigger_TickCondition(() =>
            {
                if (bodiesNearDen > 0 || !CanWorkMore())
                {
                    return false;
                }
                bool urgentlyHungry = lord.ownedPawns.Any(p => (int)p.needs.food.CurCategory >= (int)HungerCategory.UrgentlyHungry);
                int averageHungry = (int)lord.ownedPawns.Average(p => (int)p.needs.food.CurCategory);

                if (urgentlyHungry || averageHungry >= (int)HungerCategory.Hungry)
                {
                    return true;
                }
                return false;
            }, checkEveryTicks: 100));
            stateGraph.AddTransition(startHunting);

            var doneHunting = new Transition(hunterToil, burrowToil);
            doneHunting.AddPreAction(
                new TransitionAction_Custom(() =>
                {
                    timeSpentActiveToday = 9999999;
                })
            );
            doneHunting.AddTrigger(new Trigger_TickCondition(() =>
            {
                if (!CanWorkMore())
                {
                    return true;
                }
                // Check if there is at least one fresh corpse near the burrow.
                int bodiesNearBurrow = GetSetBodiesNearBurrow();
                if (bodiesNearBurrow > 1)
                {
                    // Can't hurt to hunt a little bit more, right? The less bodies the more to not transition to reguarl toil.
                    return !Rand.Chance(0.90f / bodiesNearBurrow);
                }
                return false;
            }, checkEveryTicks:1000));
            stateGraph.AddTransition(doneHunting);

            return stateGraph;
        }

        public virtual void Notify_HuntingGroundReached()
        {
            huntingGroundReached = true;
        }

        
        public override void LordJobTick()
        {
            const int tickFq = 1000;
            if (lord.ticksInToil % tickFq == 0 && lord.ownedPawns.Count > 0)
            {
                lord.ownedPawns.RemoveAll(p => p.Faction == Faction.OfPlayer);

                // Check what toil we are in.
                if (lord.CurLordToil is LordToil_DenAnimalHunt toil)
                {
                    timeSpentActiveToday += tickFq;
                }
                if (lord.ticksInToil % tickFq * 10 == 0)
                {
                    GetSetBodiesNearBurrow();
                }

            }
            if (lord.ticksInToil % 60000 == 0)
            {
                timeSpentActiveToday = 0;
            }
        }

        private int GetSetBodiesNearBurrow()
        {
            // Get the size of a grid tile
            float tileWidth = Find.WorldGrid.AverageTileSize;

            var map = lord.Map;
            var corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).Where(c => c is Corpse corpse && corpse.InnerPawn?.RaceProps?.IsFlesh == true && !corpse.IsDessicated() && corpse.GetRotStage() == RotStage.Fresh);
            var downedPawns = map.mapPawns.AllPawns.Where(p => p.Downed && p.RaceProps?.IsFlesh == true);
            // Check if they are within 12 tiles of the burrow.
            int countNearBurrows = corpses.Where(c => c.Position.InHorDistOf(burrow.Position, 24))?.Count() ?? 0 + downedPawns.Where(p => p.Position.InHorDistOf(burrow.Position, 24))?.Count() ?? 0;
            bodiesNearDen = countNearBurrows;
            return countNearBurrows;
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref burrow, "burrow");
            Scribe_Values.Look(ref wanderRadius, "wanderRadius");
            Scribe_Values.Look(ref defendRadius, "defendRadius");
            Scribe_Values.Look(ref isCaravanSendable, "isCaravanSendable", defaultValue: false);
            Scribe_Values.Look(ref addFleeToil, "addFleeToil", defaultValue: false);
            Scribe_Values.Look(ref tickLastActive, "tickLastActive");
            Scribe_Values.Look(ref timeSpentActiveToday, "timeSpentActiveToday");
            Scribe_Values.Look(ref maxTimeSpentWorking, "maxActivePerDay", 10000);
            Scribe_Values.Look(ref huntingGroundReached, "huntingGroundsReached", defaultValue: false);
            Scribe_Values.Look(ref bodiesNearDen, "bodiesNearDen", defaultValue: 0);
        }
    }
}
