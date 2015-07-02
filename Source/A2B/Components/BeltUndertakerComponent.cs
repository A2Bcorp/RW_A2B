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

	public class BeltUndertakerComponent : BeltComponent
	{
		private float powerPerUndercover = 10f;

		private UndertakerMode operationMode;
		private bool forcedMode;

		// Lifts use additional power
		// based on how many undercovers it's pulling from
		private int undercoverCount;

		private Level OutputTo
		{
			get{
				switch (operationMode){
				case UndertakerMode.PoweredLift    : return Level.Surface;
				case UndertakerMode.UnpoweredSlide : return Level.Underground;
				}
				return Level.Both;
			}
		}

		public BeltUndertakerComponent()
		{
			_beltLevel = Level.Both;
			operationMode = UndertakerMode.Undefined;
			forcedMode = false;
			undercoverCount = 0;
		}

		public override void PostExposeData()
		{
			Scribe_Values.LookValue( ref operationMode, "undertakerMode" );
			Scribe_Values.LookValue( ref forcedMode, "forcedMode" );
			Scribe_Values.LookValue( ref undercoverCount, "undercoversAttached" );
		}

		public override void PostSpawnSetup()
		{
			base.PostSpawnSetup();

			// Reset power usage for attached undercovers
			if( ( operationMode == UndertakerMode.PoweredLift )&&( undercoverCount > 0 ) )
				PowerComponent.PowerOutput = -( PowerComponent.props.basePowerConsumption + undercoverCount * powerPerUndercover );
		}

		private bool modeReset()
		{
			// If the undertaker hasn't been forced into a specific operation mode,
			// do an auto-check to determine it's mode
			if( forcedMode )
				return false;
			
			// Get the top belt connection
			BeltComponent topBelt = this.GetPositionFromRelativeRotation( Rot4.South ).GetBeltComponent( Level.Surface );

			if( topBelt != null )
			{
				// We have a belt connected to the top
				if( topBelt.CanAcceptFrom( this, true ) )
				{
					// The top belt can accept from this belt,
					// therefore this is a powered lift
					if( !this.IsLift() )
					{
						// Make sure it's a powered version
						ChangeOperationalMode( UndertakerMode.PoweredLift );

						// Force exit now to allow change
						return true;
					}
					// This component is already correct, set the operation mode
					operationMode = UndertakerMode.PoweredLift;
				}
				else
				{
					// This undertaker can't feed the top belt, assume the top
					// belt feeds the undertaker and operate as a slide
					if( !this.IsSlide() )
					{
						// Slides don't use power and we don't want
						// them to transmit to allow segmenting the
						// power network used by the conveyors
						ChangeOperationalMode( UndertakerMode.UnpoweredSlide );

						// Force exit now to allow change
						return true;
					}
					// This component is already correct, set the operation mode
					operationMode = UndertakerMode.UnpoweredSlide;
				}
			}
			return false;
		}

		private void DoPowerCheck()
		{
			switch (operationMode){
			case UndertakerMode.Undefined:
				return;
			case UndertakerMode.PoweredLift :
				// Powered lifts use additional power based
				// on how many undercovers it's pulling
				undercoverCount = CountUndercoversDirection( new Rot4( ( parent.Rotation.AsInt + Rot4.North.AsInt ) % 4 ) );
				PowerComponent.PowerOutput = -( PowerComponent.props.basePowerConsumption + undercoverCount * powerPerUndercover );
				return;
			case UndertakerMode.UnpoweredSlide :
				// Search for an active powered lift to power this section
				BeltComponent beltHead = null;
				Phase newPhase = Phase.Offline;
				bool ReachedEnd = false;

				IntVec3 curPos = parent.Position;
				do {
					curPos = curPos + parent.Rotation.FacingCell;

					// Get all underground components
					List<BeltComponent> belts = curPos.GetBeltComponents( Level.Underground );

					// None?
					if( belts == null )
						break;

					bool foundUndercover = false;
					for( int i = 0; i < belts.Count; ++i ){
						var b = belts[ i ];
						if( b.IsLift() )
						{
							// Is it the right orientation?
							if( b.parent.Rotation.AsInt == ( parent.Rotation.AsInt + Rot4.North.AsInt ) )
							{
								// Select this belt head
								beltHead = b;

								// If it's on, we'll accept it
								if( b.BeltPhase == Phase.Active ){
									i += belts.Count;
									ReachedEnd = true;
									break;
								}
							}
						} else if( b.IsUndercover() ) {
							foundUndercover = true;
						}
					}
					// This allows us to continue looking if there are more undercovers
					if( ReachedEnd == false )
						ReachedEnd = !foundUndercover;
				} while ( ReachedEnd == false );

				// Did we find a power head and is it on?
				InferedPowerComponent = beltHead == null ? null : beltHead.PowerComponent;
				if( ( InferedPowerComponent != null )&&( InferedPowerComponent.PowerOn ) )
				{
					// Power head is on
					newPhase = Phase.Active;
				}
				_beltPhase = newPhase;
				return;
			}
		}

		public override void OnOccasionalTick()
		{
			// Check for operational mode change
			if( modeReset() )
				return;
			
			DoPowerCheck();

			if( operationMode == UndertakerMode.PoweredLift )
			{
				// Powered lifts can freeze up
				DoFreezeCheck();
			}

			// Lifts and slides can jam
			if( BeltPhase == Phase.Active || BeltPhase == Phase.Jammed )
				DoJamCheck();

		}

		private void ChangeOperationalMode( UndertakerMode newMode, bool forced = false )
		{
			// Get the def we need
			string beltDefName;
			switch ( newMode )
			{
			case UndertakerMode.PoweredLift:
				beltDefName = "A2BUndertaker";
				break;
			case UndertakerMode.UnpoweredSlide:
				beltDefName = "A2BSlide";
				break;
			default:
				return; // Invalid mode change
			}

			// Get the thing def for the belt
			ThingDef beltDef = DefDatabase<ThingDef>.GetNamed( beltDefName );

			// Get our current position and rotation
			IntVec3 beltPos = parent.Position;
			Rot4 beltRot = parent.Rotation;

			// Make the new belt
			Thing beltThing = ThingMaker.MakeThing( beltDef );
			beltThing.SetFactionDirect( Faction.OfColony );
			beltThing.HitPoints = parent.HitPoints;

			// Set the new belt mode
			BeltUndertakerComponent beltComp = beltThing.TryGetComp<BeltUndertakerComponent>();
			beltComp.forcedMode = forced;
			beltComp.operationMode = newMode;

			// Remove this belt
			parent.Destroy( DestroyMode.Vanish );

			// Spawn new belt
			GenSpawn.Spawn( beltThing, beltPos, beltRot );
		}

		private int CountUndercoversDirection( Rot4 rot )
		{
			// Count all the undercovers in a line, powered 
			// lifts power all the undercovers they pull
			IntVec3 curPos = parent.Position;
			int count = 0;
			do {
				curPos = curPos + rot.FacingCell;
				BeltComponent curBelt = curPos.GetBeltComponent( Level.Underground );
				if( ( curBelt == null )||
					( !curBelt.IsUndercover() ) )
					break;
				count++;
			} while ( true );
			return count;
		}

		public override void OnItemTransfer(Thing item, BeltComponent other)
		{
			// Tell the undercover which direction the item came from
			if( other.IsUndercover() )
			{
				// Input is opposite side as output
				((BeltUndercoverComponent) other).outputDirection = parent.Rotation;
				((BeltUndercoverComponent) other).inputDirection = new Rot4( ( parent.Rotation.AsInt + 2 ) % 4 );
			}

			// Do potential belt deterioration
			if (Rand.Range(0.0f, 1.0f) < A2BData.Durability.DeteriorateChance)
				parent.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, Rand.RangeInclusive(0, 2), parent));
		}

		public override IntVec3 GetDestinationForThing( Thing thing)
		{
			// If it's a slide, it only sends to the "North"
			if( operationMode == UndertakerMode.UnpoweredSlide )
				return this.GetPositionFromRelativeRotation( Rot4.North );

			// If it's a lift, it only sends to the "South"
			if( operationMode == UndertakerMode.PoweredLift )
				return this.GetPositionFromRelativeRotation( Rot4.South );

			// Undefined operation means there is an invalid input connection
			// This should never happen though but we'll output south by default
			return this.GetPositionFromRelativeRotation( Rot4.South );
		}

		public override bool CanAcceptFrom( BeltComponent belt, bool onlyCheckConnection = false )
		{
			// If I can't accept from anyone, I certainly can't accept from you.
			if( !onlyCheckConnection && !CanAcceptSomething() )
				return false;

			// Undefined operation means there is an invalid input connection
			if( operationMode == UndertakerMode.Undefined )
				return false;

			// If it's a slide, it only accepts from "South"
			if( ( operationMode == UndertakerMode.UnpoweredSlide )&&
				( belt.parent.Position == this.GetPositionFromRelativeRotation( Rot4.South ) ) )
				return true;

			// If it's a lift, it only accepts from "North"
			if( ( operationMode == UndertakerMode.PoweredLift )&&
				( belt.parent.Position == this.GetPositionFromRelativeRotation( Rot4.North ) ) )
				return true;

			// Invalid input flow
			return false;

        }

		public override void PostDraw()
		{	
			if (BeltPhase == Phase.Frozen)
			{
				this.DrawIceGraphic();
			}
			foreach (var status in ItemContainer.ThingStatus)
			{
				var drawPos = parent.DrawPos + GetOffset(status);
				drawPos += - Altitudes.AltIncVect*parent.DrawPos.y + Altitudes.AltIncVect*(Altitudes.AltitudeFor(AltitudeLayer.Waist) + 0.03f) ;
				
				status.Thing.DrawAt(drawPos);
				
				DrawGUIOverlay(status, drawPos);
			}
			if( ( operationMode == UndertakerMode.UnpoweredSlide )&&
				( BeltPhase == Phase.Offline ) )
			{
				OverlayDrawer.DrawOverlay( parent, OverlayTypes.NeedsPower );
			}
		}

		protected override Vector3 GetOffset (ThingStatus status)
		{
			var destination = GetDestinationForThing (status.Thing);

			var progress = (float)status.Counter / A2BData.BeltSpeed.TicksToMove;

			// Are we going under or getting out ?
			if (ThingOrigin == (parent.Position - parent.Rotation.FacingCell)) 
			{
				if (progress < 0.5)
				{
					var myDir = parent.Position - ThingOrigin;
					return myDir.ToVector3()*(progress-0.5f);
				}
				else
				{
					return parent.Position.ToVector3();
				}
			} 
			else 
			{
				
				if (progress < 0.5)
				{
					return parent.Position.ToVector3();
				}
				else
				{
					var myDir = destination - parent.Position;
					return myDir.ToVector3()*(progress-0.5f);
				}

			}

		}

		public override string CompInspectStringExtra()
		{
			string statusText = Constants.TxtUndertakerMode.Translate() + " ";

			switch (operationMode)
			{
			case UndertakerMode.Undefined:
				statusText += Constants.TxtUndertakerModeUndefined.Translate();
				break;
			case UndertakerMode.PoweredLift:
				statusText += Constants.TxtUndertakerModeLift.Translate();
				break;
			case UndertakerMode.UnpoweredSlide:
				statusText += Constants.TxtUndertakerModeSlide.Translate();
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}

			if( ( operationMode != UndertakerMode.Undefined )&&( forcedMode ) )
				statusText += " (" + Constants.TxtForced.Translate() + ")";

			return statusText
				+ "\n"
				+ base.CompInspectStringExtra();
		}

		public override IEnumerable<Command> CompGetGizmosExtra()
		{
			// Show a gizmo button to allow the player to force the operational mode
			Command_Action actionToggleMode = new Command_Action();
			if( actionToggleMode != null )
			{
				actionToggleMode.icon = ContentFinder<Texture2D>.Get( "UI/Icons/Commands/UndertakerMode", true);;
				actionToggleMode.defaultLabel = Constants.TxtUndertakerModeToggle.Translate();
				actionToggleMode.activateSound = SoundDef.Named( "Click" );
				if( operationMode == UndertakerMode.PoweredLift )
				{
					actionToggleMode.defaultDesc = Constants.TxtUndertakerModeSlide.Translate();
					actionToggleMode.action = new Action( delegate()
						{
							ChangeOperationalMode( UndertakerMode.UnpoweredSlide, true );
						} );
				}
				else
				{
					actionToggleMode.defaultDesc = Constants.TxtUndertakerModeLift.Translate();
					actionToggleMode.action = new Action( delegate()
						{
							ChangeOperationalMode( UndertakerMode.PoweredLift, true );
						} );
				}
				if( actionToggleMode.action != null )
				{
					yield return actionToggleMode;
				}
			}
			// No more gizmos
			yield break;
		}
	}
}
