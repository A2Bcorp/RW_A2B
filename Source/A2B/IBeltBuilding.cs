#region Usings

using RimWorld;
using Verse;

#endregion

namespace A2B
{
    public interface IBeltBuilding
    {
        Phase BeltPhase { get; set; }

        CompGlower GlowerComponent { get; }

        CompPowerTrader PowerComponent { get; }

        int Counter { get; set; }

        int BeltSpeed { get; }

        IntVec3 ThingOrigin { set; }

        IntVec3 GetDestinationForThing(Thing thing);

        bool ShouldMoveItems { get; }
    }
}