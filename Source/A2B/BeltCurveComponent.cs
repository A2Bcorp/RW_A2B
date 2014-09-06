using Verse;

namespace A2B
{
    public class BeltCurveComponent : BeltComponent
    {
        public override IntVec3 GetDestinationForThing(Thing thing)
        {
            var beltDestA = parent.Position - parent.rotation.FacingSquare;
            var beltDestB = parent.Position +
                            new IntVec3(-parent.rotation.FacingSquare.z, parent.rotation.FacingSquare.y, parent.rotation.FacingSquare.x);

            return ThingOrigin == beltDestA ? beltDestB : beltDestA;
        }
    }
}