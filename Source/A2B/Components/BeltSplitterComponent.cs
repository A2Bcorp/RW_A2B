#region Usings

using Verse;
using System;

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
                this.GetPositionFromRelativeRotation(Rot4.West),
                this.GetPositionFromRelativeRotation(Rot4.North),
                this.GetPositionFromRelativeRotation(Rot4.East)
            };

            // Determine where we are going in the destination list (and default to left)
            int index = Math.Max(0, Array.FindIndex(dests, dir => (dir == _splitterDest)));

            // Do we have a new item ?
            if (_mythingID == thing.ThingID && IsFreeBelt(_splitterDest))
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
            BeltComponent destBelt = position.GetBeltComponent();
            return (destBelt != null && destBelt.CanAcceptFrom(this));
        }
    }
}
