#region Usings

using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using A2B;
using A2B.Annotations;

#endregion

namespace A2B
{
    public static class BeltComponentExtensions
    {
        public static bool IsReceiver([NotNull] this BeltComponent belt)
        {
            return belt.parent.def.defName == "A2BReceiver";
        }

        public static bool IsTeleporter([NotNull] this BeltComponent belt)
        {
            return belt.parent.def.defName == "A2BTeleporter";
        }

        public static bool IsUnloader([NotNull] this BeltComponent belt)
        {
            return belt.parent.def.defName == "A2BUnloader";
        }

        public static bool IsLoader([NotNull] this BeltComponent belt)
        {
            return belt.parent.def.defName == "A2BLoader";
        }

        public static bool IsUnderground([NotNull] this BeltComponent belt)
        {
            return belt.parent.def.defName == "A2BUnderground";
        }

		public static bool IsUndertaker([NotNull] this BeltComponent belt)
		{
            return belt.parent.def.defName == "A2BUndertaker";
		}

		public static bool IsLift([NotNull] this BeltComponent belt)
		{
            return belt.parent.def.defName == "A2BLift";
		}

		public static bool IsSlide([NotNull] this BeltComponent belt)
		{
            return belt.parent.def.defName == "A2BSlide";
		}

		public static bool IsUndercover([NotNull] this BeltComponent belt)
		{
            return belt.parent.def.defName == "A2BUndercover";
		}

    }
}
