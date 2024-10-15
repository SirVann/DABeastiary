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
        private CompProperties_BurrowSpawner Props
        {
            get
            {
                return (CompProperties_BurrowSpawner)this.props;
            }
        }

        private float SpawnedPawnsPoints
        {
            get
            {
                this.FilterOutUnspawnedPawns();
                float num = 0f;
                for (int i = 0; i < this.spawnedPawns.Count; i++)
                {
                    num += this.spawnedPawns[i].kindDef.combatPower;
                }
                return num;
            }
        }

        public bool Active
        {
            get
            {
                return this.pawnsLeftToSpawn != 0 && !this.Dormant;
            }
        }

        public CompCanBeDormant DormancyComp
        {
            get
            {
                CompCanBeDormant result;
                if ((result = this.dormancyCompCached) == null)
                {
                    result = (this.dormancyCompCached = this.parent.TryGetComp<CompCanBeDormant>());
                }
                return result;
            }
        }

        public bool Dormant
        {
            get
            {
                return this.DormancyComp != null && !this.DormancyComp.Awake;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            this.chosenKind ??= this.RandomPawnKindDef();
            if (this.Props.maxPawnsToSpawn != IntRange.zero)
            {
                this.pawnsLeftToSpawn = this.Props.maxPawnsToSpawn.RandomInRange;
            }
        }
        

        private void SpawnInitialPawns()
        {
            int num = 0;
            while (num < this.Props.initialPawnsCount && this.TrySpawnPawn(out Pawn pawn))
            {
                num++;
            }
            this.SpawnPawnsUntilPoints(this.Props.initialPawnsPoints);
            this.CalculateNextPawnSpawnTick();
        }

        public void SpawnPawnsUntilPoints(float points)
        {
            int num = 0;
            while (this.SpawnedPawnsPoints < points)
            {
                num++;
                if (num > 1000)
                {
                    Log.Error("Too many iterations.");
                    break;
                }
                if (!this.TrySpawnPawn(out Pawn pawn))
                {
                    break;
                }
            }
            this.CalculateNextPawnSpawnTick();
        }

        private void CalculateNextPawnSpawnTick()
        {
            this.CalculateNextPawnSpawnTick(this.Props.pawnSpawnIntervalDays.RandomInRange * 60000f);
        }

        public void CalculateNextPawnSpawnTick(float delayTicks)
        {
            float num = GenMath.LerpDouble(0f, 5f, 1f, 0.5f, (float)this.spawnedPawns.Count);
            if (Find.Storyteller.difficulty.enemyReproductionRateFactor > 0f)
            {
                this.nextPawnSpawnTick = Find.TickManager.TicksGame + (int)(delayTicks / (num * Find.Storyteller.difficulty.enemyReproductionRateFactor));
                return;
            }
            this.nextPawnSpawnTick = Find.TickManager.TicksGame + (int)delayTicks;
        }

        private void FilterOutUnspawnedPawns()
        {
            for (int i = this.spawnedPawns.Count - 1; i >= 0; i--)
            {
                if (!this.spawnedPawns[i].Spawned)
                {
                    this.spawnedPawns.RemoveAt(i);
                }
            }
        }

        private PawnKindDef RandomPawnKindDef()
        {
            float curPoints = this.SpawnedPawnsPoints;
            IEnumerable<PawnKindDef> source = this.Props.spawnablePawnKinds;
            if (this.Props.maxSpawnedPawnsPoints > -1f)
            {
                source = from x in source
                         where curPoints + x.combatPower <= this.Props.maxSpawnedPawnsPoints
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
            if (!this.canSpawnPawns)
            {
                pawn = null;
                return false;
            }
            if (!this.Props.chooseSingleTypeToSpawn)
            {
                this.chosenKind = this.RandomPawnKindDef();
            }
            if (this.chosenKind == null)
            {
                pawn = null;
                return false;
            }
            Faction faction = Props.faction != null && FactionUtility.DefaultFactionFrom(Props.faction) != null ? FactionUtility.DefaultFactionFrom(Props.faction) : null;
            PawnGenerationRequest request = new (this.chosenKind, faction, PawnGenerationContext.NonPlayer, -1, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false);
            int index = this.chosenKind.lifeStages.Count - 1;
            request.FixedBiologicalAge = new float?(this.chosenKind.race.race.lifeStageAges[index].minAge);
            Pawn pawnToCreate = PawnGenerator.GeneratePawn(request);
            this.spawnedPawns.Add(pawnToCreate);
            GenSpawn.Spawn(pawnToCreate, CellFinder.RandomClosewalkCellNear(this.parent.Position, this.parent.Map, this.Props.pawnSpawnRadius, null), this.parent.Map, WipeMode.Vanish);
            if (this.parent.Map != null)
            {
                Lord lord = (Lord)null;
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
                lord ??= LordMaker.MakeNewLord(faction, (LordJob)new LordJob_DefendPoint(this.parent.Position, this.Props.defendRadius, this.Props.defendRadius), this.parent.Map);
                lord.AddPawn(pawnToCreate);
            }
            this.Props.spawnSound?.PlayOneShot(this.parent);
            if (this.pawnsLeftToSpawn > 0)
            {
                this.pawnsLeftToSpawn--;
            }
            this.SendMessage();
            pawn = pawnToCreate;
            return true;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && this.Active && this.nextPawnSpawnTick == -1)
            {
                this.SpawnInitialPawns();
            }
        }

        public override void CompTick()
        {
            if (this.Active && this.parent.Spawned && this.nextPawnSpawnTick == -1)
            {
                this.SpawnInitialPawns();
            }
            if (this.parent.Spawned)
            {
                this.FilterOutUnspawnedPawns();
                if (this.Active && Find.TickManager.TicksGame >= this.nextPawnSpawnTick)
                {
                    if ((this.Props.maxSpawnedPawnsPoints < 0f || this.SpawnedPawnsPoints < this.Props.maxSpawnedPawnsPoints) && Find.Storyteller.difficulty.enemyReproductionRateFactor > 0f && this.TrySpawnPawn(out Pawn pawn) && pawn.caller != null)
                    {
                        pawn.caller.DoCall();
                    }
                    this.CalculateNextPawnSpawnTick();
                }
            }
        }

        public void SendMessage()
        {
            if (!this.Props.spawnMessageKey.NullOrEmpty() && MessagesRepeatAvoider.MessageShowAllowed(this.Props.spawnMessageKey, 0.1f))
            {
                Messages.Message(this.Props.spawnMessageKey.Translate(), this.parent, MessageTypeDefOf.NegativeEvent, true);
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
                        this.TrySpawnPawn(out Pawn pawn);
                    }
                };
            }
            yield break;
        }

        public override string CompInspectStringExtra()
        {
            if (!this.Props.showNextSpawnInInspect || this.nextPawnSpawnTick <= 0 || this.chosenKind == null)
            {
                return null;
            }
            if (this.pawnsLeftToSpawn == 0 && !this.Props.noPawnsLeftToSpawnKey.NullOrEmpty())
            {
                return this.Props.noPawnsLeftToSpawnKey.Translate();
            }
            string text;
            if (!this.Dormant)
            {
                text = (this.Props.nextSpawnInspectStringKey ?? "SpawningNextPawnIn").Translate(this.chosenKind.LabelCap, (this.nextPawnSpawnTick - Find.TickManager.TicksGame).ToStringTicksToDays("F1"));
            }
            else
            {
                if (this.Props.nextSpawnInspectStringKeyDormant == null)
                {
                    return null;
                }
                text = this.Props.nextSpawnInspectStringKeyDormant.Translate() + ": " + this.chosenKind.LabelCap;
            }
            if (this.pawnsLeftToSpawn > 0 && !this.Props.pawnsLeftToSpawnKey.NullOrEmpty())
            {
                text = text + ("\n" + this.Props.pawnsLeftToSpawnKey.Translate() + ": ") + this.pawnsLeftToSpawn;
            }
            return text;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.nextPawnSpawnTick, "nextPawnSpawnTick", 0, false);
            Scribe_Values.Look<int>(ref this.pawnsLeftToSpawn, "pawnsLeftToSpawn", -1, false);
            Scribe_Collections.Look<Pawn>(ref this.spawnedPawns, "spawnedPawns", LookMode.Reference, Array.Empty<object>());
            Scribe_Values.Look<bool>(ref this.aggressive, "aggressive", false, false);
            Scribe_Values.Look<bool>(ref this.canSpawnPawns, "canSpawnPawns", true, false);
            Scribe_Defs.Look<PawnKindDef>(ref this.chosenKind, "chosenKind");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.spawnedPawns.RemoveAll((Pawn x) => x == null);
                if (this.pawnsLeftToSpawn == -1 && this.Props.maxPawnsToSpawn != IntRange.zero)
                {
                    this.pawnsLeftToSpawn = this.Props.maxPawnsToSpawn.RandomInRange;
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