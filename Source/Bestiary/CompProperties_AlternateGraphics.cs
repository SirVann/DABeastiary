using System.Collections.Generic;
using Verse;

namespace Bestiary
{
    public class CompProperties_AlternateGraphics : CompProperties
    {
        public List<List<AlternateGraphic>> alternateGraphics;

        public List<List<AlternateGraphic>> alternateFemaleGraphics;

        public CompProperties_AlternateGraphics()
        {
            compClass = typeof(Comp_AlternateGraphics);
        }
    }
}
