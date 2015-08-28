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
        private bool                EverHadCover = false;
        public bool                 CoverWasDestroyed = false;
        private int                 LastCoverHitPoints;

        private static ThingDef     _undercoverCoverDef = null;
        private static ThingDef     UndercoverCoverDef {
            get{
                if( _undercoverCoverDef == null )
                    _undercoverCoverDef = DefDatabase<ThingDef>.GetNamed( "A2BUndercoverCover" );
                return _undercoverCoverDef; }
        }

        private BeltUndercoverCover Cover {
            get {
                var covers = parent.Position.GetThingList().FindAll( c => ( c.def == UndercoverCoverDef ) );
                if( ( covers != null )&&
                    ( covers.Count > 0 ) )
                    return covers[ 0 ].TryGetComp<BeltUndercoverCover>();
                return null;
            }
        }

        public bool                CoverBlueprint ()
        {
            var blueprints = parent.Position.GetThingList().FindAll( c => ( c.def == UndercoverCoverDef.blueprintDef ) );
            return ( blueprints != null )&&( blueprints.Count > 0 );
        }

        public BeltUndercoverComponent ()
        {
            MultiVector = true;
        }

        public override bool        CoverIsOn {
            get { return ( Cover != null ); }
        }

		public override IntVec3     GetDestinationForThing( Thing thing )
		{
            // Prefer straight
            return this.GetPositionFromRelativeRotation( outputDirection );
		}

		public override bool        CanAcceptFrom( BeltComponent belt, bool onlyCheckConnection = false )
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

        // Undercover handler
        protected override void     MoveThingTo([NotNull] Thing thing, IntVec3 beltDest)
        {
            OnBeginMove(thing, beltDest);

            BeltComponent belt = null;

            var belts = beltDest.GetBeltUndergroundComponents();
            var lift = belts.Find( b => b.IsLift() && b.outputDirection == this.outputDirection );
            var under = belts.Find( u => u.IsUndercover() );
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

        public override void OnOccasionalTick()
        {
            // Call base power check
            base.OnOccasionalTick();

            // Undercovers can jam
            if( BeltPhase == Phase.Active || BeltPhase == Phase.Jammed )
                DoJamCheck();

        }

        public override void        PostSpawnSetup()
        {
            base.PostSpawnSetup();

            // Create cover for the first time
            // Add a cover if it's never had one
            if( ( EverHadCover == false )&&
                ( Cover == null ) )
                ToggleCover();
        }

        public override void        PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.LookValue( ref EverHadCover, "EverHadCover", false, true );
            if( EverHadCover == true ){
                Scribe_Values.LookValue( ref LastCoverHitPoints, "LastCoverHitPoints", -1, true );
                Scribe_Values.LookValue( ref CoverWasDestroyed, "CoverWasDestroyed", false, true );
            }
        }

        public override void        PostDraw()
        {
            if( CoverIsOn == true ){

                if (BeltPhase == Phase.Frozen)
                {
                    this.DrawIceGraphic();
                }
                if( BeltPhase == Phase.Offline )
                {
                    OverlayDrawer.DrawOverlay( parent, OverlayTypes.NeedsPower );
                }
            } else {
                base.PostDraw();

                if (BeltPhase == Phase.Frozen)
                {
                    this.DrawIceGraphic();
                }

                foreach (var status in ItemContainer.ThingStatus)
                {
                    var drawPos = parent.DrawPos + GetOffset(status) + Altitudes.AltIncVect * Altitudes.AltitudeFor(AltitudeLayer.FloorEmplacement);

                    status.Thing.DrawAt(drawPos);

                    DrawGUIOverlay(status, drawPos);
                }

                if( BeltPhase == Phase.Offline )
                {
                    OverlayDrawer.DrawOverlay( parent, OverlayTypes.NeedsPower );
                }
            }
        }

        public override void        PostDeSpawn()
        {
            // Find a cover
            var cover = Cover;

            if( cover != null ) {
                // Remove it
                cover.parent.Destroy( DestroyMode.Vanish );
            }

            base.PostDeSpawn();
        }

        public override IEnumerable<Command> CompGetGizmosExtra()
        {
            // Show a gizmo button to allow the player to remove/replace the cover
            // Not under things (walls, furniture, plants, etc)
            if( CanDesignateToggle() == true )
            {
                Command_Action actionToggleMode = new Command_Action();
                if( actionToggleMode != null )
                {
                    actionToggleMode.icon = Constants.IconUndercoverCoverToggle;
                    actionToggleMode.defaultLabel = Constants.TxtUndercoverCoverToggle.Translate();
                    actionToggleMode.defaultDesc = Constants.TxtUnderUndercoverCoverToggleDesc.Translate();
                    actionToggleMode.activateSound = SoundDef.Named( "Click" );
                    actionToggleMode.hotKey = Constants.KeyUndercoverCoverToggle;
                    actionToggleMode.action = new Action( delegate()
                    {
                            Find.DesignationManager.AddDesignation( new Designation( (TargetInfo)parent, Constants.DesignationUndercoverCoverToggle ) );
                    } );
                    if( actionToggleMode.action != null )
                    {
                        yield return actionToggleMode;
                    }
                }
            }
            // No more gizmos
            yield break;
        }

        public bool CanDesignateToggle()
        {
            return
                ( parent.Position.GetEdifice() == null )&&
                ( CoverBlueprint() == false )&&
                ( Find.DesignationManager.DesignationOn( parent, Constants.DesignationUndercoverCoverToggle ) == null );
        }

        public void                 ToggleCover()
        {
            // Find a cover
            var cover = Cover;

            if( cover == null ) {
                // Get our current position and assign default rotation
                IntVec3 coverPos = parent.Position;
                Rot4 coverRot = Rot4.South;

                if( CoverWasDestroyed == true ){
                    // Spawn a cover blueprint

                    var blueprint = GenConstruct.PlaceBlueprintForBuild( UndercoverCoverDef, coverPos, coverRot, Faction.OfColony, null );

                    LastCoverHitPoints = UndercoverCoverDef.BaseMaxHitPoints;
                }
                else{
                    // First time/respawn cover

                    var thing = ThingMaker.MakeThing( UndercoverCoverDef );
                    cover = thing.TryGetComp<BeltUndercoverCover>();
                    thing.SetFactionDirect( Faction.OfColony );


                    if( ( EverHadCover == true )&&
                        ( LastCoverHitPoints > 0 ) )
                        thing.HitPoints = LastCoverHitPoints;
                    else
                        LastCoverHitPoints = thing.HitPoints;

                    // Spawn the cover/blueprint
                    GenSpawn.Spawn( thing, coverPos, coverRot );

                }

                // Do once
                EverHadCover = true;
            }
            else{
                // Remove it (them)
                while( cover != null ){
                    LastCoverHitPoints = cover.parent.HitPoints;
                    cover.parent.Destroy( DestroyMode.Vanish );
                    cover = Cover;
                }
            }
        }

	}
}
