#region Usings

using System;
using System.Collections.Generic;
using A2B.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

#endregion

namespace A2B
{

    [UsedImplicitly]
	public class BeltLiftComponent : BeltUndertakerComponent
	{

        public override void PostSpawnSetup()
        {   base.PostSpawnSetup();

            // This component is already correct, set the operation mode
            inputDirection = parent.Rotation;
            outputDirection = parent.Rotation.OppositeOf();

            _processLevel = Level.Surface;
            _inputLevel = Level.Underground;
            _outputLevel = Level.Surface;

            // Power
            PowerHead = this;
            PowerDirection = outputDirection;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if( Scribe.mode == LoadSaveMode.ResolvingCrossRefs ){
                // Power
                PowerHead = this;
                PowerDirection = outputDirection;
            }
        }

        public override float GetBasePowerConsumption()
        {
            if( PowerComponent == null )
            {
                return 0f;
            }
            // Powered lifts use additional power based
            // on how many components it's driving
            return PowerComponent.Props.basePowerConsumption + poweredCount * A2BData.PowerPerUndercover;
        }

        public override bool MovingThings()
        {
            // Is the lift itself moving an item?
            if( ItemContainer.MovingThings() )
            {
                return true;
            }
            if( poweredBelts.NullOrEmpty() )
                return false;
            
            // Check all the undercovers the lift is pulling, don't include slides in the scan
            foreach( var undercover in poweredBelts.FindAll( b => b is BeltUndercoverComponent ) )
            {
                if( undercover.MovingThings() )
                {
                    return true;
                }
            }
            return false;
        }

		public override void OnItemTransfer(Thing item, BeltComponent other)
		{
			// Do potential belt deterioration
			if (Rand.Range(0.0f, 1.0f) < A2BData.Durability.DeteriorateChance)
				parent.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, Rand.RangeInclusive(0, 2), parent));
		}

		public override IntVec3 GetDestinationForThing( Thing thing )
		{
			return this.GetPositionFromRelativeRotation( Rot4.South );
		}

        public override bool CanAcceptFrom(Rot4 direction)
        {
            return (direction == Rot4.North);
        }

        protected override void MoveThingTo([NotNull] Thing thing, IntVec3 beltDest)
        {
            OnBeginMove(thing, beltDest);

            // Lifts only output to surface
            var belt = beltDest.GetBeltSurfaceComponent();

            //  Check if there is a belt, if it can accept this thing
            if( belt == null || !belt.CanAcceptThing( thing ) )
            {
                return;
            }

            ItemContainer.TransferItem(thing, belt.ItemContainer);

            // Need to check if it is a receiver or not ...
            belt.ThingOrigin = parent.Position;
        }

	}
}
