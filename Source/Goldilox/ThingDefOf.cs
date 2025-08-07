using RimWorld;
using Verse;

namespace BestiaryGoldilox
{
    [DefOf]
    public static class ThingDefOf
    {
        public static ThingDef DA_Goldilox;

        static ThingDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
    }
}
