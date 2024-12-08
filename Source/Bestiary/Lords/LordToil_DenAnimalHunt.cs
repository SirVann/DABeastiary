using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Bestiary
{
    public class LordToilData_DenAnimalHunt : LordToilData
    {
        public IntVec3 denPoint = IntVec3.Invalid;
        public float? wanderRadius;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref denPoint, "denPoint");
            Scribe_Values.Look(ref wanderRadius, "wanderRadius");
        }
    }

    public class LordToil_DenAnimalHunt : LordToil
    {
        private bool allowSatisfyLongNeeds = false;

        protected LordToilData_DenAnimalHunt Data => (LordToilData_DenAnimalHunt)data;

        /// <summary>
        /// Animals will return prey to this point.
        /// </summary>
        public override IntVec3 FlagLoc => Data.denPoint;

        public override bool AllowSatisfyLongNeeds => allowSatisfyLongNeeds;

        public LordToil_DenAnimalHunt(bool canSatisfyLongNeeds = true)
        {
            allowSatisfyLongNeeds = canSatisfyLongNeeds;
            data = new LordToilData_DenAnimalHunt();
        }

        public LordToil_DenAnimalHunt(IntVec3 defendPoint, float? defendRadius = null, float? wanderRadius = null)
            : this()
        {
            Data.denPoint = defendPoint;
            Data.wanderRadius = wanderRadius;
        }

        public override void UpdateAllDuties()
        {
            LordToilData_DenAnimalHunt lordToilData_DefendPoint = Data;
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];
                if (pawn?.mindState != null)
                {
                    // Cancell all previous jobs.
                    pawn.jobs.StopAll(ifLayingKeepLaying:true);
                    pawn.mindState.duty = new PawnDuty(DADefOfs.DA_HunterDuty, FlagLoc)
                    {
                        focusSecond = lordToilData_DefendPoint.denPoint,
                        wanderRadius = lordToilData_DefendPoint.wanderRadius,
                    };
                }
            }
        }

        public void SetDefendPoint(IntVec3 defendPoint)
        {
            Data.denPoint = defendPoint;
        }
    }
}
