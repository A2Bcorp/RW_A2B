#region Usings

using Verse;
using System;

#endregion

namespace A2B
{
    public class BeltSplitterComponent : BeltComponent
    {
		// A list of destinations - indexing modulo 3 lets us cycle them and avoid
		// long chains of if-statements.
		private IntVec3[] _dests;

		private string _mythingID;

		private IntVec3 _splitterDest = IntVec3.Invalid;

		private int _nextDest = 0;

		public override void PostSpawnSetup()
		{
			base.PostSpawnSetup();

			_dests = new IntVec3[3]{
				this.GetPositionFromRelativeRotation(Rot4.West),
				this.GetPositionFromRelativeRotation(Rot4.North),
				this.GetPositionFromRelativeRotation(Rot4.East)
			};
		}

		public override void OnItemTransfer(Thing item, BeltComponent other)
		{
			base.OnItemTransfer( item, other );

			_mythingID = null;

			int index = Math.Max(_nextDest, Array.FindIndex(_dests, dir => (dir == _splitterDest)));
			_nextDest = FindNextDest( index );

		}

        public override IntVec3 GetDestinationForThing(Thing thing)
        {
			// Do we have a thing and is our existing path still usable?
			if (_mythingID == thing.ThingID && IsFreeBelt(_splitterDest))
			{
				// Then use it
				return _splitterDest;
			}

			// New thing
			_mythingID = thing.ThingID;

			// Determine where we are going in the destination list (and default to left)
			//int index = Math.Max(_nextDest, Array.FindIndex(dests, dir => (dir == _splitterDest)));
			int index = _nextDest;

            // Try the next destination
            if (IsFreeBelt(_dests[index]))
            {
                _splitterDest = _dests[index];
                return _splitterDest;
            }

            // Try the one after that
            index = (index + 1) % 3;
            if (IsFreeBelt(_dests[index]))
            {
                _splitterDest = _dests[index];
                return _splitterDest;
            }

            // Force use the last one
			index = (index + 1) % 3;
			_splitterDest = _dests[index];
            return _splitterDest;
        }

		private bool IsFreeBelt(IntVec3 position, bool onlyCheckConnection = false)
        {
            BeltComponent destBelt = position.GetBeltSurfaceComponent();
			return (destBelt != null && destBelt.CanAcceptFrom(this, onlyCheckConnection));
        }

		private int FindNextDest( int index )
		{
			// Try to find a different destination for the next item
			// Prevents sparsely populated belts from picking the same
			// path each time
			if( IsFreeBelt(_dests[ ( index + 1 ) % 3 ], true) ) return ( index + 1 ) % 3;
			if( IsFreeBelt(_dests[ ( index + 2 ) % 3 ], true) ) return ( index + 2 ) % 3;
			return index;
		}

    }
}
