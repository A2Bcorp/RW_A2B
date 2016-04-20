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
    public static class BeltUtilities
    {

        private static Graphic _iceGraphic = null;
        public static Graphic IceGraphic
        {
            get
            {
                if (_iceGraphic == null)
                {
                    Color color = new Color(1.0f, 1.0f, 1.0f, 0.4f);
					_iceGraphic = GraphicDatabase.Get<Graphic_Single>("Effects/ice_64", ShaderDatabase.MetaOverlay, IntVec2.One.ToVector2(), color);
                }

                return _iceGraphic;
            }
        }

        public static void DrawIceGraphic(this BeltComponent belt)
        {
            IceGraphic.Draw(belt.parent.DrawPos, belt.parent.Rotation, belt.parent);
        }

        private static Material     _undercoverMaterial = null;
        public static Material      UndercoverFrame
        {
            get
            {
                if( _undercoverMaterial == null )
                {
                    _undercoverMaterial = MaterialPool.MatFrom( "Things/Building/UndergroundFrame", ShaderDatabase.TransparentPostLight );
                }

                return _undercoverMaterial;
            }
        }

        public static void DrawUndercoverFrame( this BeltUndercoverComponent belt )
        {
            // Compute the render matrix
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS( belt.parent.DrawPos + Altitudes.AltIncVect * 2.0f, (0f).ToQuat(), new Vector3( 1.0f, 1.0f, 1.0f ) );
            // Render the mesh
            Graphics.DrawMesh( MeshPool.plane10, matrix, UndercoverFrame, 0 );
        }

        [CanBeNull]
        public static BeltComponent GetBeltComponent( this IntVec3 position, BeltComponent source )
        {
            switch( source.OutputLevel )
            {
            case Level.Surface:
                return position.GetBeltSurfaceComponent();

            case Level.Underground:
                var belts = position.GetBeltUndergroundComponents();

                // Scan for prefered belt (lift) otherwise continue underground
                if( ( belts != null )&&
                   ( belts.Count > 0 ) )
                {

                    //var p = _parentComponent as BeltUndergroundComponent;
                    var lift = belts.Find( b => b.IsLift() && b.CanAcceptFrom( source ) );
                    var under = belts.Find( b => b.IsUndercover() );
                    if(
                        ( lift != null )&&
                        (
                            ( lift.BeltPhase == Phase.Active )||
                            ( under == null )
                        )
                    )
                        return lift;
                    else
                        return under;
                }
                return null;

            default:
                return null;
            }
        }

        [CanBeNull]
        public static BeltComponent GetBeltSurfaceComponent(this IntVec3 position )
        {
            // BUGFIX: Previously, this function would grab the first building it saw at a given position. This is a problem
            // if a power conduit was on the same tile, as it was possible to miss the BeltComponent entirely. This is a more
            // robust method of identifying BeltComponents at a given location because it first finds ALL buildings on a tile.

            // CHANGE: Belts now have a level (underground and surface), this function returns a surface component

            // Since this query is lazily evaluated, it is much faster than using ThingsListAt.

            try {
                return Find.ThingGrid.ThingsAt(position)                                // All things at a given position
                           .OfType<ThingWithComps>()                                   // Only ones that can be converted to ThingWithComps
                           .Select(tc => tc.TryGetComp<BeltComponent>())               // Grab the BeltComponent from each one
                           .First(b => (b != null)&&(b.InputLevel & Level.Surface)!= 0);// Get the first non-null entry on the surface
            } catch (InvalidOperationException) {
                return null;                                                            // Didn't find one at all
            }
        }

        public static bool NoStorageBlockersIn(this IntVec3 c, Thing thing)
        {
            List<Thing> list = Find.ThingGrid.ThingsListAt( c );
            for( int index = 0; index < list.Count; ++index )
            {
                Thing thing1 = list[index];
                if(
                    (
                        ( thing1.def.EverStoreable )&&
                        (
                            ( thing1.def != thing.def )||
                            ( thing1.stackCount >= thing.def.stackLimit )
                        )
                    )||
                    (
                        ( thing1.def.entityDefToBuild != null )&&
                        ( thing1.def.entityDefToBuild.passability != Traversability.Standable )
                    )||
                    (
                        ( thing1.def.surfaceType == SurfaceType.None )&&
                        ( thing1.def.passability != Traversability.Standable )
                    )
                )
                {
                    return false;
                }
            }
            return true;
        }

		/**
         * Get the position corresponding to a rotation relative to the Thing's
         * current rotation. Used as a convenient way to specify left/right/front/back
         * without worrying about where the belt is currently facing. 'rotation' must be
         * one of IntRot.north, IntRot.south, IntRot.east, or IntRot.west.
         **/
        public static IntVec3 GetPositionFromRelativeRotation(this BeltComponent belt, Rot4 rotation)
        {
            Rot4 rotTotal = new Rot4((belt.parent.Rotation.AsInt + rotation.AsInt) % 4);

            return belt.parent.Position + rotTotal.FacingCell;
        }

        /**
         * Calculates the chance for this BeltComponent to freeze per check at a given temperature
         **/
        public static float FreezeChance(this BeltComponent belt, float currentTemp)
        {
			float delta = A2BData.Climatization.FreezeTemperature - currentTemp;

            const float MIN_CHANCE          = 0.20f;
            const float MAX_CHANCE          = 1.00f;
            const float FLAT_RATE_THRESHOLD = 20.0f;

            // No chance to freeze above the freezing temp
            if (delta < 0)
                return 0;

            // Flat rate past a certain point
            if (delta >= FLAT_RATE_THRESHOLD)
                return MAX_CHANCE;

            // Transform to [0, 1] (a percentage of the range)
            float percent = MathUtilities.LinearTransformInv(delta, 0, FLAT_RATE_THRESHOLD);

            // Transform to [MIN_CHANCE, MAX_CHANCE]
            return MathUtilities.LinearTransform(percent, MIN_CHANCE, MAX_CHANCE);
        }

        /**
        * Calculates the chance for this BeltComponent to jam per check at a given health percentage
        **/
        public static float JamChance(this BeltComponent belt, float health)
        {
            float delta = 1.0f - health;

            const float MIN_CHANCE = 0.01f;
            const float MAX_CHANCE = 1.00f;
            //const float FLAT_RATE_THRESHOLD = 10.0f;
            //const float START_THRESHOLD = 0.40f;

            // No chance to jam above the start threshold
			if (delta < A2BData.Reliability.StartThreshold)
                return 0;

            // Flat rate past a certain point
			if (delta >= A2BData.Reliability.FlatRateThreshold)
				return MAX_CHANCE;

            // Transform to [0, 1] (a percentage of the range)
			float percent = MathUtilities.LinearTransformInv(delta, 0, A2BData.Reliability.FlatRateThreshold);

            // Transform to [MIN_CHANCE, MAX_CHANCE]
			return MathUtilities.LinearTransform(percent, MIN_CHANCE, MAX_CHANCE);
        }


    }
}
