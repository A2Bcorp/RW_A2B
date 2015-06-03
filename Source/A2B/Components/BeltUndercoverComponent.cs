#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
#endregion
namespace A2B
{
	public class BeltUndercoverComponent : BeltComponent
	{
		public Rot4 inputDirection = Rot4.Invalid;
		public Rot4 outputDirection = Rot4.Invalid;

		public BeltUndercoverComponent ()
		{
			_beltLevel = Level.Underground;
		}

		public override void PostExposeData()
		{
			Scribe_Values.LookValue( ref inputDirection, "inputDirection" );
			Scribe_Values.LookValue( ref outputDirection, "outputDirection" );
		}

		public override void OnOccasionalTick()
		{
			// Search for an active powered lift to power this section
			BeltComponent beltHead = null;
			Phase newPhase = Phase.Offline;

			// Empty belt will use any power lift
			if( this.Empty )
			{
				for( int i = 0; i < 4; ++i )
				{
					Rot4 dir = new Rot4( i );
					IntVec3 curPos = parent.Position;
					do {
						curPos = curPos + dir.FacingCell;
						BeltComponent curBelt = curPos.GetBeltComponent( Level.Underground );
						if( curBelt == null )
							break;
						if( curBelt.IsLift() )
						{
							// Regardless if the power is on, select this belt head
							beltHead = curBelt;
							if( curBelt.PowerComponent.PowerOn )
							{
								// If it's on, exit now
								i += 4;
								break;
							}
						}
					} while ( true );
				}
			}
			else
			{
				// Belt is holding something, look in it's direction flow
				// for a powered lift
				IntVec3 curPos = parent.Position;
				do {
					curPos = curPos + outputDirection.FacingCell;
					BeltComponent curBelt = curPos.GetBeltComponent( Level.Underground );
					if( curBelt == null )
						break;
					if( curBelt.IsLift() )
					{
						// Regardless if the power is on, select this belt head
						beltHead = curBelt;
						break;
					}
				} while ( true );
			}

			// Did we find a power head and is it on?
			InferedPowerComponent = beltHead == null ? null : beltHead.PowerComponent;
			if( ( InferedPowerComponent != null )&&( InferedPowerComponent.PowerOn ) )
			{
				// Power head is on
				newPhase = Phase.Active;
			}
			_beltPhase = newPhase;

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
			if (Rand.Range(0.0f, 1.0f) < DeteriorateChance)
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

			// Accepts from any direction, sends it out the opposite,
			// but only from undertakers and other undercovers
			if( ( belt.IsUndercover() )||
				( belt.IsUndertaker() ) )
				return true;

			// Not an undertaker or undercover
			return false;
		}

		private string RotationName( Rot4 r )
		{
			if( r == Rot4.North )
				return Constants.TxtDirectionNorth.Translate();
			if( r == Rot4.East )
				return Constants.TxtDirectionEast.Translate();
			if( r == Rot4.South )
				return Constants.TxtDirectionSouth.Translate();
			if( r == Rot4.West )
				return Constants.TxtDirectionWest.Translate();

			return "Unknown (" + r.ToString() + ")";
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


		public override string CompInspectStringExtra()
		{
			string statusText = base.CompInspectStringExtra();
			if( !this.Empty )
			{
				if( statusText != "" )
					statusText += "\n";
				
				statusText += Constants.TxtUndertakerFlow.Translate() 
					+ " " + RotationName( inputDirection )
					+ " " + Constants.TxtUndertakerFlowTo.Translate() 
					+ " " + RotationName( outputDirection );
			}

			return statusText;
		}
	}
}

