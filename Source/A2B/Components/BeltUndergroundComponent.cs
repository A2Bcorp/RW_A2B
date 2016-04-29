#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using A2B.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

#endregion

namespace A2B
{

    public class BeltUndergroundComponent : BeltComponent
    {
        public Rot4                 inputDirection = Rot4.Invalid;
        public Rot4                 outputDirection = Rot4.Invalid;

        public bool                 MultiVector = false;

        public void                 SetItemFlow( Rot4 input )
        {
            // Only multi-vector components dynamically change
            if( MultiVector == false ){
                return;
            }

            // Did the flow somehow get corrupted?
            if( input == Rot4.Invalid )
            {
                if( PowerDirection != Rot4.Invalid )
                {
                    // Set it to the opposite direction of the powered lift
                    input = PowerDirection.OppositeOf();
                }
            }

            if( input == Rot4.Invalid )
            {
                // What the derp?
                Log.Message( string.Format( "A2B:  Belt {0} has invalid input direction!", this.parent.ThingID ) );
                return;
            }

            // Accept input from any flow and try to route it out
            inputDirection = input;

            // Always try straight first
            var outDir = input.OppositeOf();

            // Current output?
            if( PowerDirection == outDir ){
                outputDirection = outDir;
                return;
            }

            // Check straight first
            var lift = PowerEndPoint( outDir );
            if( lift != null ){
                // Register in direction
                lift.RegisterInferedPowerComponent( this, outDir );
                return;
            }

            // Look at side one
            var v1 = outDir.LeftOf();
            lift = PowerEndPoint( v1 );
            if( lift != null ){
                // Register in direction
                lift.RegisterInferedPowerComponent( this, v1 );
                return;
            }

            // Look at side two
            var v2 = outDir.RightOf();
            lift = PowerEndPoint( v2 );
            if( lift != null ){
                // Register in direction
                lift.RegisterInferedPowerComponent( this, v2 );
                return;
            }

        }

        public virtual bool         CoverIsOn
        {
            get { return true; }
        }

        private bool                gameJustLoaded = false;

        // Belt component of actual power source
        private string                  _headParentString = string.Empty;
        private Thing                   _headParent = null;
        private BeltUndergroundComponent _powerHead = null;
        public BeltUndergroundComponent PowerHead {
            get { return _powerHead; }
            set { // Only allow set if this belt doesn't have CompPowerTrader
                if( value == null ){
                    _headParentString = string.Empty;
                    _beltPhase = Phase.Offline;
                    _headParent = null;
                    _powerHead = null;
                    PowerComponent = null;
                    PowerDirection = Rot4.Invalid;
                }
                else{
                    _powerHead = value;
                    _headParent = _powerHead.parent;
                    PowerComponent = _headParent.TryGetComp<CompPowerTrader>();
                    _beltPhase = PowerComponent.PowerOn ? Phase.Active : Phase.Offline;
                }
            }
        }

        // Direction from us to power source
        protected Rot4              PowerDirection = Rot4.Invalid;

        // Who's sucking power off us
        protected List< BeltUndergroundComponent > poweredBelts = null;
        protected int               poweredCount
        {   // Make sure we only return undercover count, slides require a powered lift but do not draw power
            get { return poweredBelts.NullOrEmpty() ? 0 : 
                poweredBelts.FindAll( b => b is BeltUndercoverComponent ).Count; }
        }


        public BeltUndergroundComponent()
        {
            _processLevel = Level.Underground;
            _inputLevel = Level.Underground;
            _outputLevel = Level.Underground;
        }

        #region Power Stuff

        public bool                 RegisterInferedPowerComponent( BeltUndergroundComponent belt, Rot4 dir )
        {
            if( poweredBelts == null )
                poweredBelts = new List< BeltUndergroundComponent >();
            else if( ( belt.PowerHead != null )&&
                ( belt.PowerHead != this ) )
                belt.PowerHead.UnregisterInferedPowerComponent( belt );
            
            if( !poweredBelts.Contains( belt ) )
                poweredBelts.Add( belt );
            
            belt.PowerHead = this;
            belt.PowerDirection = dir;
            if( belt.MultiVector == true )
                belt.outputDirection = dir;

            return true;
        }

        public bool                 UnregisterInferedPowerComponent( BeltUndergroundComponent belt )
        {

            if(
                ( poweredBelts.NullOrEmpty() )||
                ( !poweredBelts.Contains( belt ) )
            )
            {
                return false;
            }

            poweredBelts.Remove( belt );

            // Clear vars
            belt.PowerHead = null;
            if( belt.MultiVector == true )
                belt.outputDirection = Rot4.Invalid;

            // Unregistered
            return true;
        }

        private Rot4                    _lastLookDir = Rot4.Invalid;
        private BeltUndergroundComponent PowerEndPoint( Rot4 lookDir )
        {
            // Last looked in this direction, must have looped around
            if( lookDir == _lastLookDir ){
                return null;
            }
            _lastLookDir = lookDir;

            // Compute cell to examine
            IntVec3 checkPos = parent.Position + lookDir.FacingCell;

            // Get list of underground components in cell
            var belts = checkPos.GetBeltUndergroundComponents();
            if( ( belts == null )||
                ( belts.Count == 0 ) ){
                // Nothing here
                _lastLookDir = Rot4.Invalid;
                return null;
            }

            // Is there a valid lift there?
            var lift = belts.Find( b => ( b.IsLift() == true )&&( b.PowerDirection == lookDir ) );
            if( lift != null ){
                _lastLookDir = Rot4.Invalid;
                return lift;
            }

            // Check multivector
            var under = belts.Find( b => ( b.IsUndercover() == true ) );
            if( under != null ){
                
                // Check straight first
                lift = under.PowerEndPoint( lookDir );
                if( lift != null ){
                    // Register in direction
                    lift.RegisterInferedPowerComponent( under, lookDir );
                    _lastLookDir = Rot4.Invalid;
                    return lift;
                }

                // Look at side one
                var v1 = lookDir.LeftOf();
                lift = under.PowerEndPoint( v1 );
                if( lift != null ){
                    // Register in direction
                    lift.RegisterInferedPowerComponent( under, v1 );
                    _lastLookDir = Rot4.Invalid;
                    return lift;
                }

                // Look at side two
                var v2 = lookDir.RightOf();
                lift = under.PowerEndPoint( v2 );
                if( lift != null ){
                    // Register in direction
                    lift.RegisterInferedPowerComponent( under, v2 );
                    _lastLookDir = Rot4.Invalid;
                    return lift;
                }
            }

            // No end point reached
            _lastLookDir = Rot4.Invalid;
            return null;
        }

        #endregion

        public override void        OnOccasionalTick()
        {
            // Lifts don't need to worry about power detection
            if( this.IsLift() )
                return;

            // RimWorld load bug work around:
            // Sometimes on load we lose our power component reference
            if( ( _powerHead != null )&&
                ( PowerComponent == null ) )
                PowerComponent = _headParent.TryGetComp<CompPowerTrader>();

            // If this component has no power head, or;
            // The head parent is not this components parent, and;
            // The power head is off
            if( ( _powerHead == null )||
                ( ( _headParent != this.parent )&&
                    ( PowerComponent.PowerOn == false ) ) )
            {
                // Check power link
                BeltUndergroundComponent lift = null;

                _beltPhase = Phase.Offline;

                if( outputDirection != Rot4.Invalid ){
                    // Single vector/targetted output first
                    lift = PowerEndPoint( outputDirection );
                    if( lift != null ){
                        // Register in direction
                        lift.RegisterInferedPowerComponent( this, outputDirection );
                        return;
                    }
                }

                if( ItemContainer.Empty == false ){
                    // Power lost, re-target
                    if( inputDirection != Rot4.Invalid ){
                        SetItemFlow( inputDirection );
                        return;
                    }
                }

                // Now check multi-vector alts
                if( MultiVector == true ){

                    // Look in all directions
                    for( int i = 0; i < 4; ++i ){
                        var d = new Rot4( i );
                        lift = PowerEndPoint( d );
                        if( lift != null ){
                            // Registered in direction
                            lift.RegisterInferedPowerComponent( this, d );
                            return;
                        }
                    }

                }
            }
        }

        public override void CompTick ()
        {
            if( gameJustLoaded )
            {
                if( _headParentString != string.Empty )
                {
                    if( PowerDirection == Rot4.Invalid )
                    {
                        Log.Error( parent.ThingID + " :: PowerDirection is invalid!" );
                    }
                    else
                    {
                        var headThing = Find.ListerThings.AllThings.FirstOrDefault( thing => thing.ThingID == _headParentString );
                        if( headThing == null )
                        {
                            Log.Error( parent.ThingID + " :: Unable to get Thing from ThingID " + _headParentString );
                        }
                        else
                        {
                            var parentBelt = headThing.TryGetComp<BeltUndergroundComponent>();
                            if( parentBelt == null )
                            {
                                Log.Error( parent.ThingID + " :: " + _headParentString + " is not a valid underground belt component!" );
                            }
                            else
                            {
                                parentBelt.RegisterInferedPowerComponent( this, PowerDirection );
                            }
                        }
                    }
                }
                gameJustLoaded = false;
            }
            base.CompTick();
        }

        public override void        OnItemTransfer(Thing item, BeltComponent other)
        {
            // Tell the undercover which direction the item came from
            if( other.IsUndercover() )
            {
                ((BeltUndercoverComponent) other).SetItemFlow( outputDirection.OppositeOf() );
            }

            // Then, do potential belt deterioration
            if (Rand.Range(0.0f, 1.0f) < A2BData.Durability.DeteriorateChance)
                parent.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, Rand.RangeInclusive(0, 2), parent));

            // Now it's gone
            if( this.IsUndercover() ){
                inputDirection = Rot4.Invalid;
                outputDirection = Rot4.Invalid;
            }
        }

        #region Callbacks (Core)

        public override void        PostExposeData()
        {
            base.PostExposeData();

            // Current targetting vector
            Scribe_Values.LookValue( ref inputDirection, "inputDirection", Rot4.Invalid, true );
            Scribe_Values.LookValue( ref outputDirection, "outputDirection", Rot4.Invalid, true );

            if( this.IsLift() == false )
            {
                Scribe_Values.LookValue( ref PowerDirection, "powerDirection", Rot4.Invalid, true );

                if(
                    ( Scribe.mode == LoadSaveMode.Saving )&&
                    ( _headParent != null )
                )
                {
                    _headParentString = _headParent.ThingID;
                }

                Scribe_Values.LookValue<string>( ref _headParentString, "powerHead", string.Empty, true );

                if( Scribe.mode == LoadSaveMode.ResolvingCrossRefs )
                {
                    gameJustLoaded = true;
                }
            }
        }

        public override void        PostDeSpawn()
        {
            ItemContainer.Destroy();

            // Disconnect all infered power users
            if( !poweredBelts.NullOrEmpty() )
                for( int index = poweredBelts.Count - 1; index >= 0; index-- )
                    UnregisterInferedPowerComponent( poweredBelts[ index ] );

            // Disconnect from power head
            if( ( PowerHead != null )&&
                ( PowerHead != this ) )
                PowerHead.UnregisterInferedPowerComponent( this );

            base.PostDeSpawn();
        }

        public override string      CompInspectStringExtra()
        {
            string statusText = base.CompInspectStringExtra();
            if( this.IsUndercover() && !this.Empty )
            {
                if( statusText != "" )
                    statusText += "\n";

                statusText += Constants.TxtUndertakerFlow.Translate()
                    + " " + inputDirection.Name()
                    + " " + Constants.TxtUndertakerFlowTo.Translate()
                    + " " + outputDirection.Name();
            }

            return statusText;
        }

        #endregion

    }
}
