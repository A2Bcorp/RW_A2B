using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace A2B
{

    public class WorkGiver_UndercoverCoverToggle : WorkGiver_Scanner
    {

        public override ThingRequest PotentialWorkThingRequest{
            get { return ThingRequest.ForDef( Constants.DefBeltUndercover ); }
        }

        public override IEnumerable<IntVec3> PotentialWorkCellsGlobal( Pawn pawn )
        {
            // Create a list of potential cells to toggle covers
            List<IntVec3> targets = new List<IntVec3>();
            foreach( var d in Find.DesignationManager.DesignationsOfDef( Constants.DesignationUndercoverCoverToggle ) )
            {
                IntVec3 c = d.target.Cell;
                if( ( c.InBounds() )&&( pawn.CanReserveAndReach( d.target, PathEndMode.OnCell, DangerUtility.NormalMaxDanger( pawn ) ) ) )
                    targets.Add( c );
            }
            return targets.AsEnumerable<IntVec3>();
        }

        public override bool HasJobOnThing( Pawn pawn, Thing t )
        {
            return ( Find.DesignationManager.DesignationOn( t, Constants.DesignationUndercoverCoverToggle ) != null )&&( ReservationUtility.CanReserveAndReach( pawn, (TargetInfo) t, PathEndMode.Touch, DangerUtility.NormalMaxDanger( pawn ), 1 ) );
        }

        public override Job JobOnThing(Pawn pawn, Thing t )
        {
            // Precursor job:
            // Cut plants
            var plant = t.Position.GetPlant();
            if( ( plant != null )&&
                ( ReservationUtility.CanReserveAndReach( pawn, (TargetInfo) plant, PathEndMode.ClosestTouch, DangerUtility.NormalMaxDanger( pawn ), 1 ) ) )
                return new Job( JobDefOf.CutPlant, (TargetInfo) plant );

            // Precursor job:
            // Move haulables
            var haulable = t.Position.GetFirstHaulable();
            if( ( haulable != null )&&
                ( ReservationUtility.CanReserveAndReach( pawn, (TargetInfo) haulable, PathEndMode.ClosestTouch, DangerUtility.NormalMaxDanger( pawn ), 1 ) ) )
                return HaulAIUtility.HaulAsideJobFor( pawn, haulable );

            // Precursor job:
            // Clean floor
            var filth = t.Position.GetThingList().Find( f => ( f.def.category == ThingCategory.Filth ) );
            if( ( filth != null )&&
                ( ReservationUtility.CanReserveAndReach( pawn, (TargetInfo) filth, PathEndMode.ClosestTouch, DangerUtility.NormalMaxDanger( pawn ), 1 ) ) )
                return new Job( JobDefOf.Clean, (TargetInfo) filth );

            // Actual job:
            // Deal with the cover after everything else is complete
            if( ( plant == null )&&
                ( haulable == null )&&
                ( filth == null ) )
                return new Job( Constants.JobUndercoverCoverToggle, (TargetInfo) t );

            // Cell not ready for cover to be removed, nothing for now
            return null;
        }
    }
}