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

    public struct UndertakerToggleData
    {
        public BeltUndertakerComponent     target;
        public bool                        forced;

        public UndertakerToggleData( BeltUndertakerComponent target, bool forced )
        {
            this.target = target;
            this.forced = forced;
        }
    }

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
			
            // Needs mode toggle, register for update
            if(
                ( !this.IsSlide() )&&
                ( !this.IsLift() )
            )
            {
                A2BMonitor.RegisterTickAction( this.parent.ThingID, UndertakerToggleMode, new UndertakerToggleData( this, false ) );
                return true;
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
				
				//DrawGUIOverlay(status, drawPos);
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
                actionToggleMode.icon = Constants.IconUndertakerToggle;
				actionToggleMode.defaultLabel = Constants.TxtUndertakerModeToggle.Translate();
                actionToggleMode.activateSound = Constants.ButtonClick;
                if( this.IsLift() )
				{
					actionToggleMode.defaultDesc = Constants.TxtUndertakerModeSlide.Translate();
				}
				else
				{
					actionToggleMode.defaultDesc = Constants.TxtUndertakerModeLift.Translate();
				}
                actionToggleMode.action = new Action( delegate()
                    {
                    A2BMonitor.RegisterTickAction( this.parent.ThingID, UndertakerToggleMode, new UndertakerToggleData( this, true ) );
                    } );
				if( actionToggleMode.action != null )
				{
					yield return actionToggleMode;
				}
			}
			// No more gizmos
			yield break;
		}

        #region Static Callbacks

        public static bool UndertakerToggleMode( object target )
        {
            var toggleData = (UndertakerToggleData) target;
            UndertakerMode toggleMode = UndertakerMode.Undefined;
            if( toggleData.target.IsLift() )
            {
                toggleMode = UndertakerMode.UnpoweredSlide;
            }
            else if( toggleData.target.IsSlide() )
            {
                toggleMode = UndertakerMode.PoweredLift;
            }
            else
            {
                toggleMode = UndertakerMode.AutoDetect;
            }
            return ChangeOperationalMode( toggleData.target, toggleMode, toggleData.forced );
        }

        private static bool ChangeOperationalMode( BeltUndertakerComponent undertaker, UndertakerMode newMode, bool forced )
        {
            // Get the new def
            ThingDef beltDef;
            switch ( newMode )
            {
            case UndertakerMode.PoweredLift:
                beltDef = Constants.DefBeltLift;
                break;
            case UndertakerMode.UnpoweredSlide:
                beltDef = Constants.DefBeltSlide;
                break;
            case UndertakerMode.AutoDetect:
                BeltComponent topBelt = undertaker.GetPositionFromRelativeRotation( Rot4.South ).GetBeltSurfaceComponent();

                if( topBelt != null )
                {
                    // We have a belt connected to the top
                    if( topBelt.CanAcceptFrom( undertaker, true ) )
                    {
                        // The top belt can accept from this belt,
                        // therefore this is a powered lift
                        if( undertaker.IsLift() )
                        {
                            // Already a lift
                            return true;
                        }

                        // Switch this to a lift
                        beltDef = Constants.DefBeltLift;
                        break;
                    }
                    else
                    {
                        // This undertaker can't feed the top belt, assume the top
                        // belt feeds the undertaker and operate as a slide
                        if( undertaker.IsSlide() )
                        {
                            // Already a slide
                            return true;
                        }

                        // Switch this to a slide
                        beltDef = Constants.DefBeltSlide;
                        break;
                    }
                }
                // No top belt, don't do anything yet
                return false;
            default:
                // Invalid mode change, deregister to allow for a proper mode change
                return true;
            }

            // Get target position and rotation
            IntVec3 beltPos = undertaker.parent.Position;
            Rot4 beltRot = undertaker.parent.Rotation;

            // Make the new belt
            Thing beltThing = ThingMaker.MakeThing( beltDef );
            beltThing.SetFactionDirect( Faction.OfColony );
            beltThing.HitPoints = undertaker.parent.HitPoints;

            // Set the new belt mode
            BeltUndertakerComponent beltComp = beltThing.TryGetComp<BeltUndertakerComponent>();
            beltComp.forcedMode = forced;

            // Remove the existing belt
            undertaker.parent.Destroy();

            // Spawn new belt
            GenSpawn.Spawn( beltThing, beltPos, beltRot );
            return true;
        }

        #endregion

	}
}
