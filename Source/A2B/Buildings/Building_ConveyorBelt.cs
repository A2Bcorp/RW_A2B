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

        public override Graphic Graphic
        {
            get
            {
                BeltComponent belt = GetComp<BeltComponent>();

                if (belt.BeltPhase == Phase.Active)
                    return base.Graphic;

                AnimatedGraphic animation = (AnimatedGraphic) base.Graphic;
                return animation.DefaultGraphic;
            }
        }

        private int prevFrame = 0;

        public override void SpawnSetup()
        {
            base.SpawnSetup();

            AnimatedGraphic animation = (AnimatedGraphic) base.Graphic;
            animation.DefaultFrame = 11;
        }

        public override void Tick()
        {
            base.Tick();

            CompPowerTrader power = GetComp<CompPowerTrader>();
            BeltComponent belt = GetComp<BeltComponent>();

            if (Graphic.GetType() == typeof(AnimatedGraphic))
            {
                AnimatedGraphic animation = (AnimatedGraphic)Graphic;

                if (animation.CurrentFrame != prevFrame)
                {
                    Find.MapDrawer.MapChanged(Position, MapChangeType.Things, true, false);
                    prevFrame = animation.CurrentFrame;
                }
            }

        }

    }
}
