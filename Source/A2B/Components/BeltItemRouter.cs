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

        private List<Rot4> _inputs = GU.List(Rot4.South);
        private List<Rot4> _outputs = GU.List(Rot4.North);

        public List<Rot4> Inputs
        {
            get
            {
                return _inputs;
            }
        }

        public List<Rot4> Outputs
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
        public virtual Rot4 GetDirectionForItem(Thing thing, Rot4 inDir)
        {
            return Rot4.North;
        }

    }
}
