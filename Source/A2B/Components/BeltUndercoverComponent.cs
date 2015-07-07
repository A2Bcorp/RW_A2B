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
	public class BeltUndercoverComponent : BeltUndergroundComponent
	{
		public override void OnOccasionalTick()
		{
			if( ( PowerHead == null )||
				( !PowerComponent.PowerOn ) ) {
				_beltPhase = Phase.Offline;

				// Search for an active powered lift to power this section
				BeltUndergroundComponent beltHead = null;

				// Empty belt will use power lift in any direction
				if( this.Empty )
				{
					for( int i = 0; i < 4; ++i )
					{
						Rot4 dir = new Rot4( i );
                        var newHead = this.FindPoweredLiftInDirection( dir, new Rot4( ( dir.AsInt + 2 ) % 4 ) );
                        if( newHead != null ){
                            beltHead = newHead;
                            if( newHead.BeltPhase == Phase.Active )
							    break;
                        }
					}
				}
				else
				{
					// Belt is holding something, look in it's direction flow
					// for a powered lift
                    beltHead = this.FindPoweredLiftInDirection( outputDirection, inputDirection );
				}

				// Changed
				if( beltHead != PowerHead )
				{
					if( PowerHead != null )
						// Unregister with the old head first
						PowerHead.UnregisterInferedPowerComponent( this );

					if( beltHead != null )
						// Register with the new head
						beltHead.RegisterInferedPowerComponent( this );
				}
			}

			// Now let the base do it's thing
			base.OnOccasionalTick();
		}

		public override IntVec3 GetDestinationForThing( Thing thing)
		{
			return this.GetPositionFromRelativeRotation( outputDirection );
		}

		public override void OnItemTransfer(Thing item, BeltComponent other)
		{
			// Tell the undercover which direction the item came from
			if( other.IsUndercover() )
			{
				((BeltUndercoverComponent) other).outputDirection = outputDirection;
				((BeltUndercoverComponent) other).inputDirection = inputDirection;
			}

			// Then, do potential belt deterioration
			if (Rand.Range(0.0f, 1.0f) < A2BData.Durability.DeteriorateChance)
				parent.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, Rand.RangeInclusive(0, 2), parent));

			// Now it's gone
			inputDirection = Rot4.Invalid;
			outputDirection = Rot4.Invalid;
		}

		public override bool CanAcceptFrom( BeltComponent belt, bool onlyCheckConnection = false )
		{
			// If I can't accept from anyone, I certainly can't accept from you.
			if( !onlyCheckConnection && !CanAcceptSomething() )
				return false;

            // This belt isn't on the other belts output level
            if( belt.OutputLevel != this.InputLevel )
                return false;

            // Accepts from any direction, sends it out the opposite
			if( belt.IsUndercover() )
                return true;

            // Check a slides orientation
            if( ( belt.IsSlide() )&&
                ( this.parent.Position == belt.GetPositionFromRelativeRotation( Rot4.North ) ) )
		        return true;
            
			// Not an undercover or slide in the corrent orientation
			return false;
		}

        // Undertaker handler
        protected override void MoveThingTo([NotNull] Thing thing, IntVec3 beltDest)
        {
            OnBeginMove(thing, beltDest);

            BeltComponent belt = null;

            var belts = beltDest.GetBeltUndergroundComponents();
            var lift = belts.Find( b => b.IsLift() && b.outputDirection == this.outputDirection );
            var under = belts.Find( tuc => tuc.IsUndercover() );
            if( ( lift != null )&&
                ( ( lift.BeltPhase == Phase.Active )||
                    ( under == null ) ) )
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

		public override void PostDraw()
		{	
			if (BeltPhase == Phase.Frozen)
			{
				this.DrawIceGraphic();
			}
			if( BeltPhase == Phase.Offline )
			{
				OverlayDrawer.DrawOverlay( parent, OverlayTypes.NeedsPower );
			}
		}


	}
}

