using Verse;

namespace A2B
{
    public class BeltCurveComponent : BeltComponent
    {
        public override IntVec3 GetDestinationForThing(Thing thing)
        {
            var beltDestA = parent.Position - parent.Rotation.FacingSquare;
            var beltDestB = parent.Position +
                            new IntVec3(-parent.Rotation.FacingSquare.z, parent.Rotation.FacingSquare.y, parent.Rotation.FacingSquare.x);

            return ThingOrigin == beltDestA ? beltDestB : beltDestA;
        }

        public override bool CanAcceptFrom(IntRot direction)
        {
            return (direction == IntRot.south || direction == IntRot.west);
        }
    }
}