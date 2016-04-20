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

    public class BeltUndercoverCover : ThingComp
    {
        private static ThingDef     _undercoverDef = null;
        private static ThingDef     UndercoverDef {
            get{
                if( _undercoverDef == null )
                    _undercoverDef = DefDatabase<ThingDef>.GetNamed( "A2BUndercover" );
                return _undercoverDef; }
        }

        private BeltUndercoverComponent    _parentBelt;
        public BeltUndercoverComponent    ParentBelt {
            get {
                if( _parentBelt == null ){
                    var belts = parent.Position.GetThingList().FindAll( c => ( c.def == UndercoverDef ) );
                    if( ( belts != null )&&
                        ( belts.Count > 0 ) )
                            _parentBelt = belts[ 0 ].TryGetComp<BeltUndercoverComponent>();
                }
                return _parentBelt;
            }
        }

        public override void PostSpawnSetup()
        {
            // Tell our parent undercover belt that we're done being built
            if( ParentBelt != null )
                ParentBelt.CoverWasDestroyed = false;
        }

        public override void PostDestroy( DestroyMode mode, bool wasSpawned )
        {
            // If we were destroyed, tell our parent undercover belt
            if( ( mode == DestroyMode.Kill )&&
                ( ParentBelt != null ) ){
                ParentBelt.CoverWasDestroyed = true;
            }
            base.PostDestroy( mode, wasSpawned );
        }

    }
}

