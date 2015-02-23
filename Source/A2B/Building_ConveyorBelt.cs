using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace A2B
{
    public class Building_ConveyorBelt : Building
    {

        private int prevFrame = 0;

        public override void Tick()
        {
            base.Tick();

            CompPowerTrader power = GetComp<CompPowerTrader>();
            BeltComponent belt = GetComp<BeltComponent>();

            AnimatedGraphic animation = (AnimatedGraphic) Graphic;

            // No power, no service.
            animation.IsAnimating = belt.BeltPhase == Phase.Active; //(power != null && power.PowerOn && belt != null);

            if (animation.CurrentFrame != prevFrame)
            {
                Find.MapDrawer.MapChanged(Position, MapChangeType.Things, true, false);
                prevFrame = animation.CurrentFrame;
            }
        }

    }
}
