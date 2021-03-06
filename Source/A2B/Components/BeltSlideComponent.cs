﻿#region Usings

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
            inputDirection = parent.Rotation.OppositeOf();
            outputDirection = parent.Rotation;

            _processLevel = Level.Surface;
            _inputLevel = Level.Surface;
            _outputLevel = Level.Underground;
        }

        public override bool        AllowLowPowerMode()
        {
            // Powered lifts will handle this for us
            return false;
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
            if(
                ( lift != null )&&
                (
                    ( lift.BeltPhase == Phase.Active )||
                    ( under == null )
                )
            )
            {   // Use the lift unless it's unpowered and there is an undercover
                belt = lift;
            }
            else
                belt = under;
            
            //  Check if there is a belt, if it can accept this thing
            if( belt == null || !belt.CanAcceptThing( thing ) )
            {
                return;
            }

            ItemContainer.TransferItem(thing, belt.ItemContainer);

            // Need to check if it is a receiver or not ...
            belt.ThingOrigin = parent.Position;
        }

	}
}
