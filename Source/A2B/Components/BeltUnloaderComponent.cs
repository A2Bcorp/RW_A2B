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
