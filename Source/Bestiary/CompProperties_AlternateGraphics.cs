using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Bestiary
{
    public class CompProperties_AlternateGraphics : CompProperties
    {
        public List<List<AlternateGraphic>> alternateGraphics;

        public CompProperties_AlternateGraphics()
        {
            compClass = typeof(Comp_AlternateGraphics);
        }
    }
}
