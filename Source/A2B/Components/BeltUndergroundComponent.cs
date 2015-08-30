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

        // Belt component of actual power source
        private Thing                   _headParent = null;
        private BeltUndergroundComponent _powerHead = null;
        public BeltUndergroundComponent PowerHead {
            get { return _powerHead; }
            set { // Only allow set if this belt doesn't have CompPowerTrader
                if( value == null ){
                    _headParent = null;
                    _powerHead = null;
                    PowerComponent = null;
                    PowerDirection = Rot4.Invalid;
                }
                else{
                    _powerHead = value;
                    _headParent = _powerHead.parent;
                    PowerComponent = _headParent.TryGetComp<CompPowerTrader>();
                }
            }
        }

        // Direction from us to power source
        protected Rot4              PowerDirection = Rot4.Invalid;

        // Who's sucking power off us
        protected List< BeltUndergroundComponent > poweredBelts = null;
        protected int               poweredCount
        {   // Make sure we only return undercover count, slides require a powered lift but do not draw power
            get { return poweredBelts == null ? 0 : 
                poweredBelts.FindAll( b => ( ( b as BeltUndercoverComponent ) != null ) ).Count; }
        }


        public BeltUndergroundComponent()
        {
            _processLevel = Level.Underground;
            _inputLevel = Level.Underground;
            _outputLevel = Level.Underground;
        }

        #region Infered Power Stuff

        public void                 RecomputerPower()
        {
            if( this.IsLift() ){
                // Powered lifts use additional power based
                // on how many components it's driving
                PowerComponent.PowerOutput = -( PowerComponent.props.basePowerConsumption + poweredCount * A2BData.powerPerUndercover );
            }
        }

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

            RecomputerPower();
            return true;
        }

        public bool                 UnregisterInferedPowerComponent( BeltUndergroundComponent belt )
        {

            if( ( poweredBelts == null )||
                ( !poweredBelts.Contains( belt ) ) ){
                return false;
            }

            poweredBelts.Remove( belt );

            // Clear vars
            belt.PowerHead = null;
            if( belt.MultiVector == true )
                belt.outputDirection = Rot4.Invalid;

            // Unregistered
            RecomputerPower();
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

            if( this.IsLift() == false ){
                Scribe_Values.LookValue( ref PowerDirection, "powerDirection", Rot4.Invalid, true );

                Scribe_References.LookReference<Thing>( ref _headParent, "powerHead" );

                if( Scribe.mode == LoadSaveMode.PostLoadInit ){
                    if( ( _headParent != null )&&
                        ( PowerDirection != Rot4.Invalid ) ){
                        var p = _headParent.TryGetComp<BeltUndergroundComponent>();
                        if( p != null ){
                            p.RegisterInferedPowerComponent( this, PowerDirection );
                        }
                    } else {
                        PowerHead = null;
                    }
                }
            }
        }

        public override void        PostDestroy(DestroyMode mode = DestroyMode.Vanish)
        {
            ItemContainer.Destroy();

            // Disconnect all infered power users
            if( poweredBelts != null )
                foreach( var b in poweredBelts )
                    UnregisterInferedPowerComponent( b );

            // Disconnect from power head
            if( ( PowerHead != null )&&
                ( PowerHead != this ) )
                PowerHead.UnregisterInferedPowerComponent( this );

            base.PostDestroy(mode);
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
