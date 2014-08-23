#region Usings

using System;
using RimWorld;
using Verse;

#endregion

namespace A2B
{
    public static class BeltBuildingExtensions
    {
        public static void DoBeltTick(this IBeltBuilding belt)
        {
            var beltBuilding = belt as Building;

            if (beltBuilding == null)
            {
                throw new InvalidOperationException("belt must be a building!");
            }

            if (belt.PowerComponent.PowerOn)
            {
                // Power is on -> do work
                // ----------------------
                // phase == offline
                if (belt.BeltPhase == Phase.Offline)
                {
                    // Turn on, incl. 'system online' glow
                    belt.BeltPhase = Phase.Active;
                    belt.GlowerComponent.Lit = true;

                    // Check if there is anything on the belt: yes? -> make it inaccessible to colonists
                    foreach (var target in Find.Map.thingGrid.ThingsAt(beltBuilding.Position))
                    {
                        // Check and make sure this is not a Pawn, and not the belt itself !
                        if ((target.def.category == EntityCategory.Item) && (target != beltBuilding) && target is ThingWithComponents)
                        {
                            // Forbid an item when it should be moved
                            target.SetForbidden(belt.ShouldMoveItems);
                        }
                    }

                    //glowerComp.def.glowColor = new ColorInt(255,200,0,0); // Hum ... that changes ALL the belt ... not what I want ...
                    return;
                }

                // phase == active
                if (belt.BeltPhase != Phase.Active)
                {
                    return;
                }

                // Active 'yellow' color
                belt.GlowerComponent.Lit = true; // in principle not required (should be already ON ...)

                if (!belt.ShouldMoveItems)
                {
                    // Unforbidd each item
                    foreach (var target in Find.Map.thingGrid.ThingsAt(beltBuilding.Position))
                    {
                        // Check and make sure this is not a Pawn, and not the belt itself !
                        if ((target.def.category == EntityCategory.Item) && (target != beltBuilding) && target is ThingWithComponents)
                        {
                            target.SetForbidden(false);
                        }
                    }

                    return;
                }

                // Check if there is anything on the belt: yes? -> try to move it
                foreach (var target in Find.Map.thingGrid.ThingsAt(beltBuilding.Position))
                {
                    // Check and make sure this is not a Pawn, and not the belt itself !
                    if ((target.def.category != EntityCategory.Item) || (target == beltBuilding))
                    {
                        continue;
                    }

                    if (target is ThingWithComponents)
                    {
                        // Make sure it is Forbidden ...
                        target.SetForbidden(true);
                    }

                    if (belt.Counter < belt.BeltSpeed)
                    {
                        belt.Counter += 1;
                    }

                    // Have we waited long enough to do something ?
                    if (belt.Counter < belt.BeltSpeed)
                    {
                        continue;
                    }

                    // Alright, I have something to move. Where to ?
                    var beltDest = belt.GetDestinationForThing(target);

                    // Check if there is a conveyor belt element there ...
                    var buildDest = Find.Map.buildingGrid.BuildingAt(beltDest);
                    if (buildDest == null)
                    {
                        continue;
                    }

                    var beltBuildingInstance = buildDest as IBeltBuilding;

                    // Does the target position contain something ? no -> move the stuff there ...
                    if (Find.Map.thingGrid.CellContains(beltDest, EntityCategory.Item))
                    {
                        foreach (var thing in Find.Map.thingGrid.ThingsAt(beltDest))
                        {
                            if (thing.TryAbsorbStack(target))
                            {
                                belt.Counter = 0;
                            }
                        }

                        continue;
                    }

                    // Move
                    target.Position = beltDest;
                    belt.Counter = 0; // reset counter to 0

                    if (beltBuildingInstance != null)
                    {
                        beltBuildingInstance.ThingOrigin = beltBuilding.Position;
                    }
                }
            }
            else
            {
                // Power off -> reset everything
                // Let's be smart: check this only once, set the item to 'Unforbidden', and then, let the player choose what he wants to do
                // i.e. forbid or unforbid them ...
                if (belt.BeltPhase != Phase.Active)
                {
                    return;
                }

                belt.GlowerComponent.Lit = false;
                belt.Counter = 0;
                belt.BeltPhase = Phase.Offline;

                // Check if there is anything on the belt: yes? -> make it accessible to colonists
                foreach (var target in Find.Map.thingGrid.ThingsAt(beltBuilding.Position))
                {
                    // Check and make sure this is not a Pawn, and not the belt itself !
                    if ((target.def.category == EntityCategory.Item) && (target != beltBuilding) && target is ThingWithComponents)
                    {
                        target.SetForbidden(false);
                    }
                }
            }
        }

        public static string GetInspectionString(this IBeltBuilding belt)
        {
            switch (belt.BeltPhase)
            {
                case Phase.Offline:
                    return Constants.TxtStatus.Translate() + " " + Constants.TxtOffline.Translate();
                case Phase.Active:
                    return Constants.TxtStatus.Translate() + " " + Constants.TxtActive.Translate();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
