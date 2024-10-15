using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Bestiary;
public class GenStep_Scatterer_Biome : GenStep_ScatterThings
{
    public List<BiomeDef> allowedBiomes;
    public override void Generate(Map map, GenStepParams parms)
    {
        if (allowedBiomes != null && allowedBiomes.Contains(map.Biome))
        {
            base.Generate(map, parms);
        }
    }

}