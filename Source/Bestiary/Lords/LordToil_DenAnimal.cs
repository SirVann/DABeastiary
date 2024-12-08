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
    public class LordToil_DenAnimalToil : LordToil
    {
        private bool allowSatisfyLongNeeds = true;

        protected LordToilData_DefendPoint Data => (LordToilData_DefendPoint)data;

        public override IntVec3 FlagLoc => Data.defendPoint;

        public override bool AllowSatisfyLongNeeds => allowSatisfyLongNeeds;

        public LordToil_DenAnimalToil(bool canSatisfyLongNeeds = true)
        {
            allowSatisfyLongNeeds = canSatisfyLongNeeds;
            data = new LordToilData_DefendPoint();
        }

        public LordToil_DenAnimalToil(IntVec3 defendPoint, float? defendRadius = null, float? wanderRadius = null)
            : this()
        {
            Data.defendPoint = defendPoint;
            Data.defendRadius = defendRadius ?? 28f;
            Data.wanderRadius = wanderRadius;
        }

        public override void UpdateAllDuties()
        {
            LordToilData_DefendPoint lordToilData_DefendPoint = Data;
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];
                if (pawn?.mindState != null)
                {
                    pawn.mindState.duty = new PawnDuty(DADefOfs.DA_DenDwellerDuty, lordToilData_DefendPoint.defendPoint)
                    {
                        focusSecond = lordToilData_DefendPoint.defendPoint,
                        radius = ((pawn.kindDef.defendPointRadius >= 0f) ? pawn.kindDef.defendPointRadius : lordToilData_DefendPoint.defendRadius),
                        wanderRadius = lordToilData_DefendPoint.wanderRadius
                    };
                }
            }
        }
    }
}
