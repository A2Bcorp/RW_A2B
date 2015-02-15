#region Usings

using Verse;

#endregion

namespace A2B
{
    public class BeltSplitterComponent : BeltComponent
    {
		private string _mythingID;

        private IntVec3 _splitterDest;

        public override IntVec3 GetDestinationForThing(Thing thing)
        {
            // A list of destinations - indexing modulo 3 lets us cycle them and avoid
            // long chains of if-statements.
            IntVec3[] dests = {
                BeltUtilities.GetPositionFromRelativeRotation(this, IntRot.west),
                BeltUtilities.GetPositionFromRelativeRotation(this, IntRot.north),
                BeltUtilities.GetPositionFromRelativeRotation(this, IntRot.east)
            };

            // Determine where we are going in the destination list
            int index;
            for (index = 0; index < 3; ++index)
            {
                if (_splitterDest == dests[index])
                {
                    break;
                }
            }

            // Do we have a new item ?
            if (_mythingID == thing.ThingID)
            {
                return _splitterDest;
            }
            else
            {
                _mythingID = thing.ThingID;

                // Try the next destination
                index = (index + 1) % 3;
                if (IsFreeBelt(dests[index]))
                {
                    _splitterDest = dests[index];
                    return _splitterDest;
                }

                // Try the one after that
                index = (index + 1) % 3;
                if (IsFreeBelt(dests[index]))
                {
                    _splitterDest = dests[index];
                    return _splitterDest;
                }

                // Give up and use our current destination
                return _splitterDest;
            }
        }

        private bool IsFreeBelt(IntVec3 position)
        {
            var destBelt = position.GetBeltComponent();
            return (destBelt != null && destBelt.Empty && destBelt.CanAcceptFrom(this));
        }
    }
}
