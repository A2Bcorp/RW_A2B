using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;

namespace A2B
{
    public class BeltUnloaderComponent : BeltComponent
    {
        public override bool CanOutputToNonBelt( IntVec3 beltDest, Thing thing )
        {
            return true;
        }

        /**
         * Belt unloaders don't freeze.
         **/
        protected override void Freeze()
        {
            // stub
        }

        /**
         * Belt unloaders don't jam.
         **/
        public override void Jam()
        {
            // stub
        }

    }
}
