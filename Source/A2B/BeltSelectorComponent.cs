using System;
using RimWorld;
using Verse;

namespace A2B
{
    public class BeltSelectorComponent : BeltComponent
    {
        public override IntVec3 GetDestinationForThing(Thing thing)
        {
            // Test the 'selection' idea ...
            var slotParent = parent as SlotGroupParent;
            if (slotParent == null)
            {
                throw new InvalidOperationException("parent is not a SlotGroupParent!");
            }

            var selectionSettings = slotParent.GetStoreSettings();
            if (selectionSettings.AllowedToAccept(thing))
            {
                return parent.Position + parent.Rotation.FacingSquare;
            }

            return parent.Position +
                   new IntVec3(parent.Rotation.FacingSquare.z, parent.Rotation.FacingSquare.y, -parent.Rotation.FacingSquare.x);
        }

        public override bool CanAcceptFrom(IntRot direction)
        {
            return (direction == IntRot.south || direction == IntRot.west);
        }
    }
}