using Verse;
using System.Collections.Generic;

namespace Bestiary
{
    public class Comp_AlternateGraphics : ThingComp
    {
        public CompProperties_AlternateGraphics Props => (CompProperties_AlternateGraphics)props;

        private int index = -1;

        public bool TryGetAlternate(Pawn pawn, out AlternateGraphic alternateGraphic, out int i)
        {
            alternateGraphic = null;
            List<List<AlternateGraphic>> alternateGraphicList = pawn.gender == Gender.Female && !Props.alternateFemaleGraphics.NullOrEmpty() ?
                Props.alternateFemaleGraphics : Props.alternateGraphics;
            if (alternateGraphicList.Count > pawn.ageTracker.CurLifeStageIndex)
            {
                var alternateGraphics = alternateGraphicList[pawn.ageTracker.CurLifeStageIndex];
                if (index != -1 && index < alternateGraphicList.Count)
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
