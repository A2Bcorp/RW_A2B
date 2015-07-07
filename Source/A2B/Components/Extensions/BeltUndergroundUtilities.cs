#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using A2B.Annotations;
using RimWorld;
using Verse;
using UnityEngine;

#endregion

namespace A2B
{
    public static class BeltUndergroundUtilities
    {

        [CanBeNull]
        public static List<BeltUndergroundComponent> GetBeltUndergroundComponents(this IntVec3 position)
        {
            // CHANGE: Belts now have a level (underground and surface), this function returns a list of underground component

            // This returns a list for special case scenarios involving underground components

            try {
                return Find.ThingGrid.ThingsAt(position)                                // All things at a given position
                    .OfType<ThingWithComps>()                                           // Only ones that can be converted to ThingWithComps
                    .Select(tc => tc.TryGetComp<BeltUndergroundComponent>())            // Grab the BeltUndergroundComponent from each one
                    .Where(b => b != null && (b.InputLevel & Level.Underground) != 0 )   // Get the all components at the proper level
                    .ToList();                                                          // To a nice list for us to work with
            } catch (InvalidOperationException) {
                return null;                                                            // Didn't find even one
            }
        }

    }
}
