using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

using GU = A2B.GeneralUtilities;

namespace A2B
{

    public class BeltItemRouter
    {
        private BeltComponent _parent;

        private List<IntRot> _inputs = GU.List(IntRot.south);
        private List<IntRot> _outputs = GU.List(IntRot.north);

        public List<IntRot> Inputs
        {
            get
            {
                return _inputs;
            }
        }

        public List<IntRot> Outputs
        {
            get
            {
                return _outputs;
            }
        }

        public BeltComponent Parent
        {
            get
            {
                return _parent;
            }
        }

        public BeltItemRouter(BeltComponent parent)
        {
            _parent = parent;
        }

        /**
         * Gets which direction the item should go, given that it came from
         * a particular input direction.
         **/
        public virtual IntRot GetDirectionForItem(Thing thing, IntRot inDir)
        {
            return IntRot.north;
        }

    }
}
