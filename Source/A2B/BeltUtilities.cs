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
			var building = (Building)Find.ThingGrid.ThingAt(position, EntityCategory.Building);

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
        public static IntVec3 GetPositionFromRelativeRotation(Thing thing, IntRot rotation)
        {
            IntRot rotTotal = new IntRot((thing.Rotation.AsInt + rotation.AsInt) % 4);
            
            return thing.Position + rotTotal.FacingSquare;
        }
    }
}
