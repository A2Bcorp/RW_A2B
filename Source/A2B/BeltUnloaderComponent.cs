using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;

namespace A2B
{
    public class BeltUnloaderComponent : BeltComponent
    {
        public override bool CanOutputToNonBelt()
        {
            return true;
        }
    }
}
