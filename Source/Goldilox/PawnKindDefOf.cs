using RimWorld;
using Verse;

namespace BestiaryGoldilox
{
    [DefOf]
    public static class PawnKindDefOf
    {
        public static PawnKindDef DA_Goldilox;

        static PawnKindDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(PawnKindDefOf));
    }
}
