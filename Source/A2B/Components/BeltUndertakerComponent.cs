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
	public class BeltUndertakerComponent : BeltUndergroundComponent
	{

		private bool forcedMode = false;

        public override void PostSpawnSetup()
        {   base.PostSpawnSetup();

            // Set to surface for initial detection
            inputDirection = parent.Rotation;
            outputDirection = parent.Rotation.OppositeOf();

            _processLevel = Level.Surface;
            _inputLevel = Level.Surface;
            _outputLevel = Level.Surface;
        }

		public override void PostExposeData()
		{
            base.PostExposeData();
			Scribe_Values.LookValue( ref forcedMode, "forcedMode" );
		}

		private bool modeReset()
		{
			// If the undertaker hasn't been forced into a specific operation mode,
			// do an auto-check to determine it's mode
			if( forcedMode )
				return false;
			
			// Get the top belt connection
			BeltComponent topBelt = this.GetPositionFromRelativeRotation( Rot4.South ).GetBeltComponent();

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
				}
			}
			return false;
		}

		// Stub
        /*
        protected virtual void DoPowerCheck()
		{
			return;
        }
        */

		public override void OnOccasionalTick()
		{
            // Check for operational mode change
			if( modeReset() )
				return;
			
            // Abort if still in config mode
            if( ( !this.IsSlide() )&&
                ( !this.IsLift() ) )
                return;

            // Configured, now process
            base.OnOccasionalTick();

			//DoPowerCheck();

            if( this.IsLift() )
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
				beltDefName = "A2BLift";
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

			// Remove this belt
			parent.Destroy( DestroyMode.Vanish );

			// Spawn new belt
			GenSpawn.Spawn( beltThing, beltPos, beltRot );
		}

        public override IntVec3 GetDestinationForThing( Thing thing)
        {
            return IntVec3.Invalid;
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
            if( ( ( !this.IsSlide() )&&( !this.IsLift() ) )||
                ( ( this.IsSlide() )&&( BeltPhase == Phase.Offline ) ) )
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

            if( this.IsLift() ){
				statusText += Constants.TxtUndertakerModeLift.Translate();
                statusText += " (" + Translator.Translate( Constants.TxtLiftDrivingComponents, poweredCount ) + ")";
            }else if( this.IsSlide() )
				statusText += Constants.TxtUndertakerModeSlide.Translate();
			else
			    statusText += Constants.TxtUndertakerModeUndefined.Translate();

			if( forcedMode )
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
                if( this.IsLift() )
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
