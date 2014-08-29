using Verse;

namespace A2B
{
    public static class BeltUtilities
    {
        public static BeltComponent GetBeltComponent(this IntVec3 position)
        {
            var building = Find.BuildingGrid.BuildingAt(position);

            return building == null ? null : building.GetComp<BeltComponent>();
        }
    }
}