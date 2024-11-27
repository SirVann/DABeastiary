using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Bestiary
{
    public class TrollOutbreak_GameCondition : GameCondition
    {
        private GameConditionExtension extensionOnThis = null;
        protected GameConditionExtension ExtensionOnThis => extensionOnThis ??= def.modExtensions.OfType<GameConditionExtension>().FirstOrDefault();
        public override int TransitionTicks => 300;

        int nextBurrowTick = int.MaxValue;
        public override void Init()
        {
            Messages.Message(def.startMessage, MessageTypeDefOf.ThreatBig);
        }

        public override float TemperatureOffset()
        {
            return GameConditionUtility.LerpInOutValue(this, TransitionTicks, -5f);
        }

        public override void GameConditionTick()
        {
            if (nextBurrowTick == int.MaxValue || Find.TickManager.TicksGame == nextBurrowTick)
            {
                if (nextBurrowTick == int.MaxValue)
                {
                    // Make sure we actually get _something_ during the event.
                    nextBurrowTick = Find.TickManager.TicksGame + Rand.Range(0, 60000);
                    return;
                }

                var targetIncident = ExtensionOnThis.weightedIncidents.RandomElementByWeight(i => i.weight).incidentDef;
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(targetIncident.category, SingleMap);
                targetIncident.Worker.TryExecute(parms);
                SetNextEvent();
            }
        }

        protected void SetNextEvent()
        {
            double chancePerDay = ExtensionOnThis.chancePerDay;
            const int ticksPerDay = 60000;

            double chancePerTick = chancePerDay / ticksPerDay;
            double randomValue = Rand.Value;

            int ticksToNextEvent = (int)(Math.Log(1 - randomValue) / Math.Log(1 - chancePerTick));

            if (ticksToNextEvent < 1)
            {
                Log.Warning("Ticks to next Troll Outbreak sub-event is less than 1, setting to 1");
                ticksToNextEvent = 1;
            }

            nextBurrowTick = Find.TickManager.TicksGame + ticksToNextEvent;
            //Log.Message($"Next outbreak event in {ticksToNextEvent} ticks");
        }
    }
}
