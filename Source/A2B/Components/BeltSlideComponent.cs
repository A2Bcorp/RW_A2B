#region Usings

using System;
using System.Collections.Generic;
using A2B.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

#endregion

namespace A2B
{

    [UsedImplicitly]
	public class BeltSlideComponent : BeltUndertakerComponent
	{

        public override void PostSpawnSetup()
        {   base.PostSpawnSetup();

            // This component is already correct, set the operation mode
            inputDirection = new Rot4( ( parent.Rotation.AsInt + 2 ) % 4 );
            outputDirection = parent.Rotation;

            _processLevel = Level.Surface;
            _inputLevel = Level.Surface;
            _outputLevel = Level.Underground;
        }

		public override IntVec3 GetDestinationForThing( Thing thing )
		{
			return this.GetPositionFromRelativeRotation( Rot4.North );
		}

        public override bool CanAcceptFrom(Rot4 direction)
        {
            return (direction == Rot4.South);
        }

        protected override void MoveThingTo([NotNull] Thing thing, IntVec3 beltDest)
        {
            OnBeginMove(thing, beltDest);

            // Slides only output to underground
            BeltComponent belt = null;
            var belts = beltDest.GetBeltUndergroundComponents();
            var lift = belts.Find( b => b.IsLift() && b.outputDirection == this.outputDirection );
            var under = belts.Find( tuc => tuc.IsUndercover() );
            if( ( lift != null )&&
                ( ( lift.BeltPhase == Phase.Active )||
                    ( under == null ) ) )
                // Use the lift unless it's unpowered and there is an undertaker
                belt = lift;
            else
                belt = under;
            
            //  Check if there is a belt, if it is empty, and also check if it is active !
            if (belt == null || !belt.ItemContainer.Empty || belt.BeltPhase != Phase.Active)
            {
                return;
            }

            ItemContainer.TransferItem(thing, belt.ItemContainer);

            // Need to check if it is a receiver or not ...
            belt.ThingOrigin = parent.Position;
        }

	}
}
