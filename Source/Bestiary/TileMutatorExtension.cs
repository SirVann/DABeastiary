using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Bestiary
{
    public class TileMutatorExtension : DefModExtension
    {
        public List<NegativeFishingOutcomeDef> negativeFishingOutcomeDef;
        public List<ThingDef> fishThings;

    }

    public class NegativeFishingExtension : DefModExtension
    {
        public PawnKindDef pawnKindDef;
        public float pawnAge = 1f;
        public bool manHunter = false;

    }
}
