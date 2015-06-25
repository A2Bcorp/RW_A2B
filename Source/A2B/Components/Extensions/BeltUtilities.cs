#region Usings

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

        [CanBeNull]
		public static BeltComponent GetBeltComponent(this IntVec3 position, Level lookLevel = Level.Surface )
        {
            // BUGFIX: Previously, this function would grab the first building it saw at a given position. This is a problem
            // if a power conduit was on the same tile, as it was possible to miss the BeltComponent entirely. This is a more
            // robust method of identifying BeltComponents at a given location because it first finds ALL buildings on a tile.
			// CHANGE: Belts now have a level (underground and surface), this function now looks for a component on an individual level.

            //var building = (Building) Find.ThingGrid.ThingsListAt(position).Find(thing => (thing.TryGetComp<BeltComponent>() != null));

			foreach( Thing t in Find.ThingGrid.ThingsListAt( position ) )
			{
				ThingWithComps tc = t as ThingWithComps;
				if( tc != null )
				{
					BeltComponent b = tc.TryGetComp<BeltComponent>();
					if( b != null )
					{
						if( ( lookLevel & b.BeltLevel ) != 0 )
						{
							return b;
						}
					}
				}
			}
			
			return null;
        }

        public static bool CanPlaceThing(this IntVec3 position, [NotNull] Thing thing)
        {
			var usable = PlaceSpotPlacability( position, thing );

			if( usable == PlaceSpotUsability.Usable )
            {
                return true;
            }

            var slotGroup = Find.ThingGrid.ThingAt( position, ThingCategory.Building ) as ISlotGroupParent;
            if( slotGroup != null )
            {
				return slotGroup.GetStoreSettings().AllowedToAccept(thing);
            }

            return false;
        }

		public enum PlaceSpotUsability : byte
		{
			Unusable,
			Usable,
		}

		public static PlaceSpotUsability PlaceSpotPlacability( IntVec3 c, Thing thing )
		{
			if( !GenGrid.InBounds( c ) || !GenGrid.Walkable( c ) )
				return PlaceSpotUsability.Unusable;
			
			List< Thing > list = Find.ThingGrid.ThingsListAt( c );
			for( int index = 0; index < list.Count; ++index )
			{
				Thing thing1 = list[ index ];

				if( thing.def.saveCompressible && thing1.def.saveCompressible )
					return PlaceSpotUsability.Unusable;
				
				if( thing1.def.category == ThingCategory.Item )
					return thing1.def == thing.def && thing1.stackCount < thing.def.stackLimit ? PlaceSpotUsability.Usable : PlaceSpotUsability.Unusable;
				
			}
			return PlaceSpotUsability.Usable;
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
