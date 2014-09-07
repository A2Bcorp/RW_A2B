#region Usings

using System.Linq;
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
            var building = Find.BuildingGrid.BuildingAt(position);

            return building == null ? null : building.GetComp<BeltComponent>();
        }

        public static bool CanPlaceThing(this IntVec3 position, [NotNull] Thing thing)
        {
            var quality = GenPlace.PlaceSpotQualityAt(position, thing, position);

            return quality > PlaceSpotQuality.Okay;
        }
    }
}
