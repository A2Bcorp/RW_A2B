using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace A2B
{
    public class BeltMergerComponent : BeltComponent
    {
        public override bool CanAcceptFrom(IntRot direction)
        {
            return (direction != IntRot.north);
        }
    }
}
