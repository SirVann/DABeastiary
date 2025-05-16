using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Bestiary
{
    public class Comp_AlternateGraphics : ThingComp
    {
        public CompProperties_AlternateGraphics Props => (CompProperties_AlternateGraphics)props;

        private int index = -1;

        public bool TryGetAlternate(Pawn pawn, out AlternateGraphic alternateGraphic, out int i)
        {
            alternateGraphic = null;

            if (Props.alternateGraphics.Count > pawn.ageTracker.CurLifeStageIndex)
            {
                var alternateGraphics = Props.alternateGraphics[pawn.ageTracker.CurLifeStageIndex];
                if (index != -1 && index < Props.alternateGraphics.Count)
                    alternateGraphic = alternateGraphics[index];
                else if (!alternateGraphics.NullOrEmpty() && alternateGraphics.TryRandomElementByWeight(arg => arg.Weight, out alternateGraphic))
                    index = alternateGraphics.IndexOf(alternateGraphic);
                else
                    index = -1;
            }
            
            i = index;
            return alternateGraphic != null;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref index, "index", -1);
        }
    }
}
