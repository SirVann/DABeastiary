﻿using System.Collections.Generic;
using Verse;
using RimWorld;

namespace Bestiary
{
    public class CompProperties_BurrowSpawner : CompProperties
    {
        public List<PawnKindDef> spawnablePawnKinds;

        public SoundDef spawnSound;

        public string spawnMessageKey;

        public string noPawnsLeftToSpawnKey;

        public string pawnsLeftToSpawnKey;

        public bool showNextSpawnInInspect;

        public float defendRadius = 21f;

        public int initialPawnsCount;

        public float initialPawnsPoints;

        public float maxSpawnedPawnsPoints = -1f;

        public FloatRange pawnSpawnIntervalDays = new FloatRange(0.85f, 1.15f);

        public int pawnSpawnRadius = 2;

        public IntRange maxPawnsToSpawn = IntRange.zero;

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