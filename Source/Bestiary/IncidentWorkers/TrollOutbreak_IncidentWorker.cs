using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;

namespace Bestiary
{
    internal class TrollOutbreak_IncidentWorker : IncidentWorker_MakeGameCondition
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return map.mapTemperature.SeasonAndOutdoorTemperatureAcceptableFor(BDefOfs.DA_BeardedTroll.race) &&
                base.CanFireNowSub(parms);
        }
    }
}
