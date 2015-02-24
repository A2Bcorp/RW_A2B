using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;

namespace A2B
{
    public class Building_Teleporter : Building
    {
        private int prevFrame;

        public override void Tick()
        {
            base.Tick();

            CompPowerTrader power = GetComp<CompPowerTrader>();
            BeltComponent belt = GetComp<BeltComponent>();

            AnimatedGraphic animation = (AnimatedGraphic) Graphic;

            // No power, no service.
            animation.IsAnimating = (power != null && power.PowerOn && belt != null);

            if (animation.CurrentFrame != prevFrame)
            {
                Find.MapDrawer.MapChanged(Position, MapChangeType.Things, true, false);
                prevFrame = animation.CurrentFrame;
            }
        }
    }
}
