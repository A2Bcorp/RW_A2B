using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace A2B
{

    public class Designator_ToggleUndercoverCover : Designator
    {
        public Designator_ToggleUndercoverCover()
        {
            this.icon = Constants.IconUndercoverCoverToggle;
            this.defaultLabel = Constants.TxtUndercoverCoverToggle.Translate();
            this.defaultDesc = Constants.TxtUnderUndercoverCoverToggleDesc.Translate();
            this.useMouseIcon = true;
            this.soundDragSustain = SoundDefOf.DesignateDragStandard;
            this.soundDragChanged = SoundDefOf.DesignateDragStandardChanged;
            this.soundSucceeded = SoundDef.Named( "Click" );
            this.hotKey = Constants.KeyUndercoverCoverToggle;
        }

        public override bool DragDrawMeasurements {
            get { return true; }
        }

        public override int DraggableDimensions {
            get { return 2; }
        }

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            var belt = t.TryGetComp<BeltUndercoverComponent>();
            if( ( belt != null )&&
                ( belt.CanDesignateToggle() == true ) )
                return AcceptanceReport.WasAccepted;
            return (AcceptanceReport)Constants.TxtUnderUndercoverCoverToggleDesignateOnly.Translate();
        }

        public override AcceptanceReport CanDesignateCell ( IntVec3 loc )
        {
            var l = loc.GetBeltUndergroundComponents();
            if( l != null ){
                var belt = l.Find( b => ( b.IsUndercover() == true ) );
                if( belt != null )
                    return CanDesignateThing( belt.parent );
            }
            return (AcceptanceReport)Constants.TxtUnderUndercoverCoverToggleDesignateOnly.Translate();
        }

        public override void DesignateSingleCell( IntVec3 c )
        {
            var l = c.GetBeltUndergroundComponents();
            foreach( var b in l )
                if( b.IsUndercover() == true )
                    DesignateThing( b.parent );
        }

        public override void DesignateThing( Thing t )
        {
            var belt = t.TryGetComp<BeltUndercoverComponent>();
            if( ( belt != null )&&
                ( belt.CanDesignateToggle() == true ) )
                Find.DesignationManager.AddDesignation( new Designation( (TargetInfo)t, Constants.DesignationUndercoverCoverToggle ) );
        }
    }
}
