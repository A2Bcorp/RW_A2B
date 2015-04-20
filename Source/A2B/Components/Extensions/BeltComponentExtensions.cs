#region Usings

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
		public static bool IsUndertaker([NotNull] this BeltComponent belt)
		{
			return belt.parent.def.defName == "A2BUndertaker";
		}


    }
}
