using A2B.Annotations;
using RimWorld;
using Verse;

namespace A2B
{
	public class BeltLoader : Building_Storage
    {
        public override void Notify_ReceivedThing([NotNull] Thing newItem)
        {
            Log.Message("Adding item " + newItem);
            GetComp<BeltComponent>().Notify_ReceivedThing(newItem);
        }
    }
}