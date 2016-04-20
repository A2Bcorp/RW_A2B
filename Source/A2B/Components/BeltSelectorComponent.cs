using System;
using System.Collections.Generic;

using RimWorld;
using Verse;
using UnityEngine;

namespace A2B
{
    public class BeltSelectorComponent : BeltComponent
    {

        protected ISlotGroupParent slotParent;

		protected bool hasStorageSettings;
		protected string _mythingID;

        protected Rot4[] inputVectors;
        protected Rot4[] outputOneVectors;
        protected Rot4[] outputTwoVectors;

        protected bool allowOutputOneToGround = false;
        protected bool allowOutputTwoToGround = false;

        protected IntVec3[] inputPos;
        protected IntVec3[] outputOnePos;
        protected IntVec3[] outputTwoPos;

        protected IntVec3 _lastOnePosition;
        protected IntVec3 _lastTwoPosition;

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.LookValue<bool>(ref hasStorageSettings, "hasStorageSettings");
            Scribe_Values.LookValue<bool>(ref allowOutputOneToGround, "allowOutputOneToGround");
            Scribe_Values.LookValue<bool>(ref allowOutputTwoToGround, "allowOutputTwoToGround");
        }

        public override void PostSpawnSetup()
        {
            base.PostSpawnSetup();

            GetIOVectors();

            inputPos = RelativeRotationToPosition( inputVectors );
            outputOnePos = RelativeRotationToPosition( outputOneVectors );
            outputTwoPos = RelativeRotationToPosition( outputTwoVectors );

            _lastOnePosition = outputOnePos[ 0 ];
            _lastTwoPosition = outputTwoPos[ 0 ];

            slotParent = parent as ISlotGroupParent;
            if( slotParent == null )
            {
                throw new InvalidOperationException("parent is not a SlotGroupParent!");
            }

            // we kinda want to not overwrite custom storage settings every save/load...
            if( !hasStorageSettings )
            {
                // First disallow all
                slotParent.GetStoreSettings().filter.SetDisallowAll();
                foreach( var outputOne in outputOnePos )
                {   // Copy from output one vectors if available
                    var slotGroup = outputOne.GetSlotGroup();
                    if( slotGroup != null )
                    {
                        slotParent.GetStoreSettings().CopyFrom( slotGroup.Settings );
                        allowOutputOneToGround = true;
                        break;
                    }
                }
            }

            hasStorageSettings = true;
        }

        private IntVec3[] RelativeRotationToPosition( Rot4[] vectors )
        {
            int count = vectors.GetLength( 0 );
            var dests = new IntVec3[ count ];
            for( int index = 0; index < count; ++index )
            {
                dests[ index ] = this.GetPositionFromRelativeRotation( vectors[ index ] );
            }
            return dests;
        }

        public virtual void GetIOVectors()
        {
            inputVectors = new Rot4[]{ Rot4.South };
            outputOneVectors = new Rot4[]{ Rot4.North };
            outputTwoVectors = new Rot4[]{ Rot4.West, Rot4.East };
        }

        private bool CanOutputToSlotGroup( IntVec3 beltDest, Thing thing )
        {
            var slotGroup = beltDest.GetSlotGroup();
            if( slotGroup == null )
                return true;
            return slotGroup.Settings.AllowedToAccept( thing );
        }

        public override bool CanOutputToNonBelt( IntVec3 beltDest, Thing thing )
        {
            if( allowOutputOneToGround )
                for( int index = 0; index < outputOnePos.GetLength( 0 ); ++index )
                    if( beltDest == outputOnePos[ index ] )
                        return CanOutputToSlotGroup( beltDest, thing );
            if( allowOutputTwoToGround )
                for( int index = 0; index < outputTwoPos.GetLength( 0 ); ++index )
                    if( beltDest == outputTwoPos[ index ] )
                        return CanOutputToSlotGroup( beltDest, thing );
            return false;
        }

        protected virtual IntVec3 GetGroundVector( Thing thing, IntVec3[] vectors, IntVec3 lastVector )
        {
            int limit = vectors.GetLength( 0 );

            // Same item? 
            if(
                ( _mythingID == thing.ThingID )&&
                ( lastVector.NoStorageBlockersIn( thing ) )
            )
            {
                return lastVector;
            }

            // New item
            _mythingID = thing.ThingID;

            // Vector index
            int index = Math.Max( 0, Array.FindIndex( vectors, dest => dest == lastVector ) );
            int lastIndex = index;

            // Try vectors until all have been checked
            do
            {
                // Try next vector
                index = ( index + 1 ) % limit;
                if( vectors[ index ].NoStorageBlockersIn( thing ) )
                {
                    return vectors[ index ];
                }
            }while( index != lastIndex );

            // Can't find a free one
            return IntVec3.Invalid;
        }

        protected virtual IntVec3 GetOutputVector( Thing thing, IntVec3[] vectors, IntVec3 lastVector, bool allowGround )
        {
            int limit = vectors.GetLength( 0 );

            // Same item? 
            if(
                ( _mythingID == thing.ThingID )&&
                ( IsFreeBelt( lastVector ) )
            )
            {
                return lastVector;
            }

            // New item
            _mythingID = thing.ThingID;

            // Vector index
            int index = Math.Max( 0, Array.FindIndex( vectors, dest => dest == lastVector ) );
            int lastIndex = index;

            // Try vectors until all have been checked
            do
            {
                // Try next vector
                index = ( index + 1 ) % limit;
                if( IsFreeBelt( vectors[ index ] ) )
                {
                    return vectors[ index ];
                }
            }while( index != lastIndex );

            // Can't find a free one
            if( !allowGround )
                return IntVec3.Invalid;

            // Check the ground
            return GetGroundVector( thing, vectors, lastVector );
        }

        public override bool CanAcceptFrom( Rot4 direction )
        {
            return Array.Exists( inputVectors, v => v == direction );
        }

        public override IntVec3 GetDestinationForThing(Thing thing)
        {
            // Test the 'selection' idea ...
            if( slotParent == null )
            {
                throw new InvalidOperationException("parent is not a SlotGroupParent!");
            }

            IntVec3 destination;

            // Matches filter?
            var selectionSettings = slotParent.GetStoreSettings();
            if( selectionSettings.AllowedToAccept( thing ) )
            {
                // Send it to the next "1" output
                destination = GetOutputVector( thing, outputOnePos, _lastOnePosition, allowOutputOneToGround );
                if( destination != IntVec3.Invalid )
                    _lastOnePosition = destination;
                return _lastOnePosition;
            }

            // Doesn't match, send it to the next "2" output
            destination = GetOutputVector( thing, outputTwoPos, _lastTwoPosition, allowOutputTwoToGround );
            if( destination != IntVec3.Invalid )
                _lastTwoPosition = destination;
            return _lastTwoPosition;
        }

        protected bool IsFreeBelt( IntVec3 position )
        {
			BeltComponent destBelt = position.GetBeltSurfaceComponent();
            return (
                ( destBelt != null )&&
                ( destBelt.CanAcceptFrom( this ) )&&
                ( destBelt.Empty )
            );
        }
        public override IEnumerable<Command> CompGetGizmosExtra()
        {
            // Show gizmo buttons to allow the player to allow outputing directly to the ground
            Command_Action actionToggleOneToGround = new Command_Action();
            if( actionToggleOneToGround != null )
            {
                if( allowOutputOneToGround )
                {
                    actionToggleOneToGround.icon = Constants.IconSelectorToGroundTrue;
                }
                else
                {
                    actionToggleOneToGround.icon = Constants.IconSelectorToGroundFalse;
                }
                actionToggleOneToGround.defaultDesc = Constants.TxtSelectorToGroundToggleDescription.Translate( "1" );
                actionToggleOneToGround.defaultLabel = Constants.TxtSelectorToGroundToggle.Translate( "1" );
                actionToggleOneToGround.activateSound = Constants.ButtonClick;
                actionToggleOneToGround.action = new Action( delegate()
                {
                    allowOutputOneToGround = !allowOutputOneToGround;
                } );
            }
            yield return actionToggleOneToGround;

            Command_Action actionToggleTwoToGround = new Command_Action();
            if( actionToggleTwoToGround != null )
            {
                if( allowOutputTwoToGround )
                {
                    actionToggleTwoToGround.icon = Constants.IconSelectorToGroundTrue;
                }
                else
                {
                    actionToggleTwoToGround.icon = Constants.IconSelectorToGroundFalse;
                }
                actionToggleTwoToGround.defaultDesc = Constants.TxtSelectorToGroundToggleDescription.Translate( "2" );
                actionToggleTwoToGround.defaultLabel = Constants.TxtSelectorToGroundToggle.Translate( "2" );
                actionToggleTwoToGround.activateSound = Constants.ButtonClick;
                actionToggleTwoToGround.action = new Action( delegate()
                {
                    allowOutputTwoToGround = !allowOutputTwoToGround;
                } );
            }
            yield return actionToggleTwoToGround;

            // No more gizmos
            yield break;
        }

    }
}