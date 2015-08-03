using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace A2B
{

    public class JobDriver_UndercoverCoverToggle : JobDriver
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Fail conditions on Undercover if:
            // Destroyed,
            // On fire
            // Designator removed
            this.FailOnDestroyed( TargetIndex.A );
            this.FailOnBurningImmobile( TargetIndex.A );
            this.FailOnThingMissingDesignation( TargetIndex.A, Constants.DesignationUndercoverCoverToggle );
            // Reserve the target
            yield return Toils_Reserve.Reserve( TargetIndex.A );
            // Go to the target
            var toilGoto = Toils_Goto.GotoCell( TargetIndex.A, PathEndMode.OnCell );
            // Fail going to the target if it becomes unreachable
            toilGoto.FailOn( ( Func< bool > )(() =>
                {
                    if( Reachability.CanReach( pawn, (TargetInfo)TargetLocA, PathEndMode.OnCell, DangerUtility.NormalMaxDanger( pawn ) ) )
                        return false;
                    return true;
                }));
            yield return toilGoto;

            // "Work" toil
            var toilWork = new Toil
            {
                // Instant complete
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 200,
            };
            // Single action
            toilWork.AddFinishAction( new Action(() =>
            {
                // Toggle the belt cover
                var belt = TargetThingA.TryGetComp<BeltUndercoverComponent>();
                belt.ToggleCover();
            } ) );
            yield return toilWork;
            // Remove designator
            yield return Toils_General.RemoveDesignationsOnThing( TargetIndex.A, Constants.DesignationUndercoverCoverToggle );
            // And we're done.
            yield break;
        }
    }
}

