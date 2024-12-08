using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Bestiary
{
    /// <summary>
    /// Could also be exposed to the mod settings menu and used as defaults.
    /// </summary>
    public class TrollSettings : Def
    {
        public static TrollSettings _cachedSettings = null;
        

        [Unsaved]
        private HashSet<TerrainDef> burrownTerrainCache = null;

        public static TrollSettings Settings => _cachedSettings ??= DefDatabase<TrollSettings>.AllDefsListForReading.First();
        public HashSet<TerrainDef> BurrowTerrains => burrownTerrainCache ??= new HashSet<TerrainDef>(burrowTerrains);

        public FloatRange wanderThreatScale = new(0.25f, 1.0f);
        public IntRange trollWanderStayTime = new(30000, 120000);
        public float trollStickAroundChance = 0.35f;

        public int burrowIdealDistanceToColony = 50;
        public int burrowMinimumDistanceToColony = 20;
        public IntRange burrowsPerTrigger = new(1, 2);
        private List<TerrainDef> burrowTerrains = [];
    }

    public static class Dev
    {
        public readonly static bool debugMode = true;
    }

}
