using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using RimWorld;

namespace Bestiary
{
    public class CompBurrowSpawn : ThingComp
    {
        CompProperties_BurrowSpawner Props => (CompProperties_BurrowSpawner)props;

        private float SpawnedPawnsPoints
        {
            get
            {
                FilterOutUnspawnedPawns();
                float num = 0f;
                for (int i = 0; i < spawnedPawns.Count; i++)
                {
                    num += spawnedPawns[i].kindDef.combatPower;
                }
                return num;
            }
        }

        public bool Active
        {
            get
            {
                return pawnsLeftToSpawn != 0 && !Dormant;
            }
        }

        public CompCanBeDormant DormancyComp
        {
            get
            {
                CompCanBeDormant result;
                if ((result = dormancyCompCached) == null)
                {
                    result = (dormancyCompCached = parent.TryGetComp<CompCanBeDormant>());
                }
                return result;
            }
        }

        public bool Dormant
        {
            get
            {
                return DormancyComp != null && !DormancyComp.Awake;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            
        }

        private float ThreatPointsHere() => StorytellerUtility.DefaultThreatPointsNow(parent.Map);
        private float ThreatPointMaxNow(float maxThreadScale) => StorytellerUtility.DefaultThreatPointsNow(parent.Map) * maxThreadScale;

        private void SpawnInitialPawns()
        {
            int num = 0;
            while (num < Props.initialPawnsCount && TrySpawnPawn(out Pawn pawn))
            {
                num++;
            }
            SpawnPawnsUntilPoints(Props.initialThreatScale * ThreatPointsHere());
            CalculateNextPawnSpawnTick();
        }

        public void SpawnPawnsUntilPoints(float points)
        {
            int num = 0;
            while (SpawnedPawnsPoints < points)
            {
                num++;
                if (num > 1000)
                {
                    Log.Error($"{parent.def.defName} spawn had too many iterations.");
                    break;
                }
                if (!TrySpawnPawn(out Pawn pawn))
                {
                    break;
                }
            }
            CalculateNextPawnSpawnTick();
        }

        private void CalculateNextPawnSpawnTick()
        {
            CalculateNextPawnSpawnTick(Props.pawnSpawnIntervalDays.RandomInRange * 60000f);
        }

        public void CalculateNextPawnSpawnTick(float delayTicks)
        {
            float num = GenMath.LerpDouble(0f, 5f, 1f, 0.5f, (float)spawnedPawns.Count);
            if (Find.Storyteller.difficulty.enemyReproductionRateFactor > 0f)
            {
                nextPawnSpawnTick = Find.TickManager.TicksGame + (int)(delayTicks / (num * Find.Storyteller.difficulty.enemyReproductionRateFactor));
                return;
            }
            nextPawnSpawnTick = Find.TickManager.TicksGame + (int)delayTicks;
        }

        private void FilterOutUnspawnedPawns()
        {
            for (int i = spawnedPawns.Count - 1; i >= 0; i--)
            {
                if (!spawnedPawns[i].Spawned)
                {
                    spawnedPawns.RemoveAt(i);
                }
            }
        }

        private PawnKindDef RandomPawnKindDef()
        {
            float threatP = ThreatPointsHere();
            float curPoints = SpawnedPawnsPoints;
            IEnumerable<PawnKindDef> source = Props.spawnablePawnKinds;
            if (Props.maxThreatScale > -1f)
            {
                source = from x in source
                         where curPoints + x.combatPower <= Props.maxThreatScale * threatP
                         select x;
            }
            if (source.EnumerableNullOrEmpty() && curPoints == 0)
            {
                // Add the cheapest pawn kind possible if no valid option was found and there are no pawns active.
                source = from x in Props.spawnablePawnKinds
                         where x.combatPower == Props.spawnablePawnKinds.Min(y => y.combatPower)
                         select x;
            }

            if (source.TryRandomElement(out PawnKindDef result))
            {
                return result;
            }
            return null;
        }

        private bool TrySpawnPawn(out Pawn pawn)
        {
            if (!canSpawnPawns)
            {
                pawn = null;
                return false;
            }
            if (!Props.chooseSingleTypeToSpawn)
            {
                chosenKind = RandomPawnKindDef();
            }
            if (chosenKind == null)
            {
                pawn = null;
                return false;
            }
            Faction faction = Props.faction != null && FactionUtility.DefaultFactionFrom(Props.faction) != null ? FactionUtility.DefaultFactionFrom(Props.faction) : null;
            if (faction == null)
            {
                Log.Warning($"{this} tried to spawn a pawn but the faction was null. Defaulting to Insects faction as a fallback.");
                faction = Faction.OfInsects;
            }
            PawnGenerationRequest request = new PawnGenerationRequest(chosenKind, faction);
            int index = chosenKind.lifeStages.Count - 1;
            request.FixedBiologicalAge = chosenKind.race.race.lifeStageAges[index].minAge;
            Pawn pawnToCreate = PawnGenerator.GeneratePawn(request);
            spawnedPawns.Add(pawnToCreate);
            GenSpawn.Spawn(pawnToCreate, CellFinder.RandomClosewalkCellNear(parent.Position, parent.Map, Props.pawnSpawnRadius, null), parent.Map, WipeMode.Vanish);
            if (parent.Map != null)
            {
                Lord lord = null;
                if (spawnedPawns.Count > 0)
                {
                    foreach (Pawn lordPawn in spawnedPawns)
                    {
                        if (lordPawn != null)
                        {
                            lordPawn.TryGetLord(out lord);
                            if (lord != null)
                            {
                                break;
                            }
                        }
                    }
                }

                // Invoke constructor LordJob
                lord ??= LordMaker.MakeNewLord(
                    faction,
                    (LordJob)Activator.CreateInstance(Props.lordJob,
                    [parent, Props.wanderRadius, Props.defendRadius, false, false, Props.maxTimeSpentHunting]),
                    parent.Map);

                lord.AddPawn(pawnToCreate);


                pawnToCreate.needs.food.CurLevelPercentage = Rand.Value;
            }
            Props.spawnSound?.PlayOneShot(parent);
            if (pawnsLeftToSpawn > 0)
            {
                pawnsLeftToSpawn--;
            }
            SendMessage();
            pawn = pawnToCreate;
            return true;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && Active && nextPawnSpawnTick == -1)
            {
                if (Props.maxPawnsToSpawn != IntRange.zero)
                {
                    pawnsLeftToSpawn = Props.maxPawnsToSpawn.RandomInRange;
                }
                SpawnInitialPawns();
            }
        }

        public override void CompTick()
        {
            if (Active && parent.Spawned && nextPawnSpawnTick == -1)
            {
                SpawnInitialPawns();
            }
            if (parent.Spawned)
            {
                FilterOutUnspawnedPawns();
                if (Active && Find.TickManager.TicksGame >= nextPawnSpawnTick)
                {
                    float threatMax = ThreatPointMaxNow(Props.maxThreatScale);
                    if ((threatMax < 0f || SpawnedPawnsPoints < threatMax) && Find.Storyteller.difficulty.enemyReproductionRateFactor > 0f && TrySpawnPawn(out Pawn pawn) && pawn.caller != null)
                    {
                        pawn.caller.DoCall();
                    }
                    CalculateNextPawnSpawnTick();
                }
            }
        }

        public void SendMessage()
        {
            if (!Props.spawnMessageKey.NullOrEmpty() && MessagesRepeatAvoider.MessageShowAllowed(Props.spawnMessageKey, 0.1f))
            {
                Messages.Message(Props.spawnMessageKey.Translate(), parent, MessageTypeDefOf.NegativeEvent, true);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Spawn pawn",
                    icon = TexCommand.ReleaseAnimals,
                    action = delegate ()
                    {
                        TrySpawnPawn(out Pawn pawn);
                    }
                };
            }
            yield break;
        }

        public override string CompInspectStringExtra()
        {
            if (!Props.showNextSpawnInInspect || nextPawnSpawnTick <= 0 || chosenKind == null)
            {
                return null;
            }
            if (pawnsLeftToSpawn == 0 && !Props.noPawnsLeftToSpawnKey.NullOrEmpty())
            {
                return Props.noPawnsLeftToSpawnKey.Translate();
            }
            string text;
            if (!Dormant)
            {
                text = (Props.nextSpawnInspectStringKey ?? "SpawningNextPawnIn").Translate(chosenKind.LabelCap, (nextPawnSpawnTick - Find.TickManager.TicksGame).ToStringTicksToDays("F1"));
            }
            else
            {
                if (Props.nextSpawnInspectStringKeyDormant == null)
                {
                    return null;
                }
                text = Props.nextSpawnInspectStringKeyDormant.Translate() + ": " + chosenKind.LabelCap;
            }
            if (pawnsLeftToSpawn > 0 && !Props.pawnsLeftToSpawnKey.NullOrEmpty())
            {
                text = text + ("\n" + Props.pawnsLeftToSpawnKey.Translate() + ": ") + pawnsLeftToSpawn;
            }
            return text;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextPawnSpawnTick, "nextPawnSpawnTick", 0, false);
            Scribe_Values.Look(ref pawnsLeftToSpawn, "pawnsLeftToSpawn", -1, false);
            Scribe_Collections.Look<Pawn>(ref spawnedPawns, "spawnedPawns", LookMode.Reference, Array.Empty<object>());
            Scribe_Values.Look(ref aggressive, "aggressive", false, false);
            Scribe_Values.Look(ref canSpawnPawns, "canSpawnPawns", true, false);
            Scribe_Defs.Look<PawnKindDef>(ref chosenKind, "chosenKind");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                spawnedPawns.RemoveAll((Pawn x) => x == null);
                if (pawnsLeftToSpawn == -1 && Props.maxPawnsToSpawn != IntRange.zero)
                {
                    pawnsLeftToSpawn = Props.maxPawnsToSpawn.RandomInRange;
                }
            }
        }

        public int nextPawnSpawnTick = -1;

        public int pawnsLeftToSpawn = -1;

        public List<Pawn> spawnedPawns = [];

        public bool aggressive = true;

        public bool canSpawnPawns = true;

        private PawnKindDef chosenKind;

        private CompCanBeDormant dormancyCompCached;
    }
}