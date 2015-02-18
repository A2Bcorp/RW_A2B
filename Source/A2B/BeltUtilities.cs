#region Usings

using A2B.Annotations;
using RimWorld;
using Verse;

#endregion

namespace A2B
{
    public static class BeltUtilities
    {
        [CanBeNull]
        public static BeltComponent GetBeltComponent(this IntVec3 position)
        {
            // BUGFIX: Previously, this function would grab the first building it saw at a given position. This is a problem
            // if a power conduit was on the same tile, as it was possible to miss the BeltComponent entirely. This is a more
            // robust method of identifying BeltComponents at a given location because it first finds ALL buildings on a tile.

            var building = (Building) Find.ThingGrid.ThingsListAt(position).Find(thing => (thing.TryGetComp<BeltComponent>() != null));

            return building == null ? null : building.GetComp<BeltComponent>();
        }

        public static bool CanPlaceThing(this IntVec3 position, [NotNull] Thing thing)
        {
            var quality = GenPlace.PlaceSpotQualityAt(position, thing, position);

            if (quality >= PlaceSpotQuality.Okay)
            {
                return true;
            }

            var slotGroup = Find.ThingGrid.ThingAt(position, EntityCategory.Building) as SlotGroupParent;
            if (slotGroup != null)
            {
                return slotGroup.GetStoreSettings().AllowedToAccept(thing);
            }

            return false;
        }

        /**
         * Get the position corresponding to a rotation relative to the Thing's
         * current rotation. Used as a convenient way to specify left/right/front/back
         * without worrying about where the belt is currently facing. 'rotation' must be
         * one of IntRot.north, IntRot.south, IntRot.east, or IntRot.west.
         **/
        public static IntVec3 GetPositionFromRelativeRotation(this BeltComponent belt, IntRot rotation)
        {
            IntRot rotTotal = new IntRot((belt.parent.Rotation.AsInt + rotation.AsInt) % 4);

            return belt.parent.Position + rotTotal.FacingSquare;
        }
    }
}
