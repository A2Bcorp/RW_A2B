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
    public static class BeltUndergroundComponentExtensions
    {
        public static BeltUndergroundComponent FindPoweredLiftInDirection([NotNull] this BeltUndergroundComponent belt, Rot4 direction, Rot4 orientation )
        {
            BeltUndergroundComponent beltHead = null;
            bool ReachedEnd = false;
            bool foundUndercover;

            IntVec3 curPos = belt.parent.Position;
            do {
                curPos = curPos + direction.FacingCell;

                // Underground belts need more robust checking to handle strange configurations
                var belts = curPos.GetBeltUndergroundComponents();

                foundUndercover = false;

                // Scan for prefered belt (lift) otherwise continue underground
                if( ( belts == null )||
                    ( belts.Count == 0 ) ){
                    ReachedEnd = true;
                } else {
                    var lift = belts.Find( b => b.IsLift() && b.inputDirection == orientation );
                    if( lift != null ) {
                        beltHead = lift;
                        if( lift.BeltPhase == Phase.Active )
                            return beltHead;
                        
                    } else {
                        if( belts.Find( b => b.IsUndercover() ) != null )
                            foundUndercover = true;
                    }
                }
                // This allows us to continue looking if there are more undercovers
                if( ReachedEnd == false )
                    ReachedEnd = !foundUndercover;
            } while ( ReachedEnd == false );

            // Return any head we found
            return beltHead;
        }

    }
}
