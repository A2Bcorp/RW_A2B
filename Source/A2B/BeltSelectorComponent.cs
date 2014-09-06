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
                return parent.Position + parent.rotation.FacingSquare;
            }

            return parent.Position +
                   new IntVec3(parent.rotation.FacingSquare.z, parent.rotation.FacingSquare.y, -parent.rotation.FacingSquare.x);
        }
    }
}