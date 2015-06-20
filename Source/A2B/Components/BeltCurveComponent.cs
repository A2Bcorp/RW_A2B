using Verse;

namespace A2B
{
    public class BeltCurveComponent : BeltComponent
    {
        public override IntVec3 GetDestinationForThing(Thing thing)
        {
            var beltDestA = parent.Position - parent.Rotation.FacingCell;
            var beltDestB = parent.Position +
                            new IntVec3(-parent.Rotation.FacingCell.z, parent.Rotation.FacingCell.y, parent.Rotation.FacingCell.x);

            return ThingOrigin == beltDestA ? beltDestB : beltDestA;
        }

        public override bool CanAcceptFrom(Rot4 direction)
        {
            return (direction == Rot4.South || direction == Rot4.West);
        }
    }
}