using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Bestiary
{
    internal class IncidentWorker_TrollBurrowsErupts : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            var acceptableForTrolls = map.mapTemperature.SeasonAndOutdoorTemperatureAcceptableFor(DADefOfs.DA_BeardedTroll.race) && base.CanFireNowSub(parms);
            if (acceptableForTrolls && base.CanFireNowSub(parms))
            {
                return TrollOutbreakUtilities.TryGetBurrowCell(out _, map);
            }
            return false;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 cell = IntVec3.Invalid;
            int numTospawn = TrollSettings.Settings.burrowsPerTrigger.RandomInRange;
            var modExt = def.GetModExtension<IncidentExtension>();

            for (int i = 0; i < numTospawn; i++)
            {
                if (!TrollOutbreakUtilities.TryGetBurrowCell(out cell, map))
                {
                    break;
                }
                Thing thing = ThingMaker.MakeThing(modExt.weightedThingDefs.RandomElementByWeight(x => x.weight).thingDef);
                GenSpawn.Spawn(thing, cell, map);
            }

            if (cell != IntVec3.Invalid)
            {
                Find.LetterStack.ReceiveLetter(def.letterLabel, def.letterText, def.letterDef, new TargetInfo(cell, map));
                return true;
            }
            return false;
        }
    }

    
}
