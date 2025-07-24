using System.Collections.Generic;
using Verse;
using RimWorld;
using System;

namespace Bestiary
{
    public class CompProperties_BurrowSpawner : CompProperties
    {
        public Type lordJob;
        
        public List<PawnKindDef> spawnablePawnKinds;

        public SoundDef spawnSound;

        public string spawnMessageKey;

        public string noPawnsLeftToSpawnKey;

        public string pawnsLeftToSpawnKey;

        public bool showNextSpawnInInspect;

        public float defendRadius = 21f;

        public float wanderRadius = 7f;

        public int initialPawnsCount;

        public float initialThreatScale = 0.2f;

        public float maxThreatScale = 0.5f;

        public int maxSpawnedAtSameTime = 10;

        public FloatRange pawnSpawnIntervalDays = new FloatRange(0.85f, 1.15f);

        public int pawnSpawnRadius = 2;

        public IntRange maxPawnsToSpawn = IntRange.Zero;

        public int maxTimeSpentHunting = 10000;

        public bool chooseSingleTypeToSpawn;

        public string nextSpawnInspectStringKey;

        public string nextSpawnInspectStringKeyDormant;

        public FactionDef faction;

        public CompProperties_BurrowSpawner()
        {
            compClass = typeof(CompBurrowSpawn);
        }
    }
}