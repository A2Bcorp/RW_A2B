using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace A2B
{
    public class BeltMergerComponent : BeltComponent
    {
        public override bool CanAcceptFrom(Rot4 direction)
        {
            return (direction != Rot4.North);
        }
    }
}
