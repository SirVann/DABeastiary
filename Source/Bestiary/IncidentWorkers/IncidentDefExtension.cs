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
    public class ThingDefChances
    {
        public ThingDef thingDef;
        public float weight = 0.5f;
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "thingDef", xmlRoot.Name);
            weight = ParseHelper.FromString<float>(xmlRoot.InnerText);
        }
    }
    public class IncidentExtension : DefModExtension
    {
        public List<ThingDefChances> weightedThingDefs = [];
    }
}
