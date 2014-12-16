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
    }
}
