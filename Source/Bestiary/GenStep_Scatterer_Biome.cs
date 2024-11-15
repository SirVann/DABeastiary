using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Bestiary
{
    public class GenStep_Scatterer_Biome : GenStep_ScatterThings
    {
        public List<BiomeDef> allowedBiomes;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!allowedBiomes.NullOrEmpty() && allowedBiomes.Contains(map.Biome))
            {
                usedSpots.Clear();
                base.Generate(map, parms);
            }
        }
    }
}