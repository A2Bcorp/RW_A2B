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
            var beltDestL = parent.Position +
                            new IntVec3(-parent.rotation.FacingSquare.z, parent.rotation.FacingSquare.y, parent.rotation.FacingSquare.x);
            var beltDestR = parent.Position +
                            new IntVec3(+parent.rotation.FacingSquare.z, parent.rotation.FacingSquare.y, -parent.rotation.FacingSquare.x);

            // Do we have a new item ?
            if (_mythingID == thing.ThingID)
            {
                return _splitterDest;
            }
            else
            {
                _mythingID = thing.ThingID;
                if (_splitterDest == beltDestL)
                {
                    // Is the other direction free ? Then switch. Else, don't switch !
                    var destBelt = beltDestR.GetBeltComponent();
                    if (destBelt != null && destBelt.Empty)
                    {
                        _splitterDest = beltDestR;
                        return beltDestR;
                    }

                    return beltDestL;
                }
                else
                {
                    var destBelt = beltDestL.GetBeltComponent();
                    if (destBelt != null && destBelt.Empty)
                    {
                        _splitterDest = beltDestL;
                        return beltDestL;
                    }

                    return beltDestR;
                }
            }
        }
    }
}
