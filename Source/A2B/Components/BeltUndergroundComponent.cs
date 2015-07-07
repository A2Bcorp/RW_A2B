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
        public Rot4 inputDirection;
        public Rot4 outputDirection;

        // Who we're sucking power off of
        private BeltUndergroundComponent _PowerHead = null;
        protected BeltUndergroundComponent PowerHead {
            get{ return _PowerHead; }
            set{ _PowerHead = value;
                if( _PowerHead != null )
                    PowerComponent = _PowerHead.parent.GetComp<CompPowerTrader>();
                else
                    PowerComponent = null;
            }
        }

        // Who's sucking power off us
        protected List< BeltUndergroundComponent > poweredBelts = null;
        protected int poweredCount
        {   // Make sure we only return undercover count, slides require a powered lift but do not draw power
            get { return poweredBelts == null ? 0 : 
                poweredBelts.FindAll( b => ( ( b as BeltUndercoverComponent ) != null ) ).Count; }
        }


        public BeltUndergroundComponent()
        {
            _processLevel = Level.Underground;
            _inputLevel = Level.Underground;
            _outputLevel = Level.Underground;
            inputDirection = Rot4.Invalid;
            outputDirection = Rot4.Invalid;
        }

        #region Infered Power Stuff

        public void RegisterInferedPowerComponent( BeltUndergroundComponent belt )
        {
            if( poweredBelts == null )
                poweredBelts = new List< BeltUndergroundComponent >();
            else if( poweredBelts.Contains( belt ) )
                return;
            poweredBelts.Add( belt );
            belt.InferedPowerCallback( this, Constants.msgPowerConnect );
        }

        public void UnregisterInferedPowerComponent( BeltUndergroundComponent belt )
        {
            if( ( poweredBelts == null )||
                ( !poweredBelts.Contains( belt ) ) )
                return;
            poweredBelts.Remove( belt );
            belt.InferedPowerCallback( this, Constants.msgPowerDisconnect );
        }

        public virtual void InferedPowerCallback( BeltUndergroundComponent caller, string Message )
        {
            if( Message == Constants.msgPowerConnect )
            {
                PowerHead = caller;
                _beltPhase = PowerComponent.PowerOn == true ? Phase.Active : Phase.Offline;
            }
            if( Message == Constants.msgPowerDisconnect )
            {
                PowerHead = null;
                _beltPhase = Phase.Offline;
            }
        }

        #endregion

        #region Callbacks (Core)

        public override void PostDestroy(DestroyMode mode = DestroyMode.Vanish)
        {
            ItemContainer.Destroy();

            // Disconnect all infered power users
            if( poweredBelts != null )
                foreach( var b in poweredBelts )
                    b.UnregisterInferedPowerComponent( b );

            base.PostDestroy(mode);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.LookValue( ref inputDirection, "inputDirection" );
            Scribe_Values.LookValue( ref outputDirection, "outputDirection" );
        }

        public override string CompInspectStringExtra()
        {
            string statusText = base.CompInspectStringExtra();
            if( this.IsUndercover() && !this.Empty )
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

        #endregion

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

    }
}

