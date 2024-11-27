using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace Bestiary
{
    public class IncidentDefChances
    {
        public IncidentDef incidentDef;
        public float weight = 0.5f;
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "incidentDef", xmlRoot.Name);
            weight = ParseHelper.FromString<float>(xmlRoot.InnerText);
        }
    }
    public class GameConditionExtension : DefModExtension
    {
        public float chancePerDay = 0.5f;

        public List<IncidentDefChances> weightedIncidents = [];
    }
}
