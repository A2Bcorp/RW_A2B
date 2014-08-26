using RimWorld;
using Verse;

namespace A2B
{
    public class BeltLoader : Building_Storage
    {
        public override void Notify_ReceivedThing(Thing newItem)
        {
            GetComp<BeltComponent>().Notify_ReceivedThing(newItem);
        }
    }
}