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
            outputDirection = new Rot4( ( parent.Rotation.AsInt + 2 ) % 4 );

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
            var belt = beltDest.GetBeltComponent();

            //  Check if there is a belt, if it is empty, and also check if it is active !
            if (belt == null || !belt.ItemContainer.Empty || belt.BeltPhase != Phase.Active)
            {
                return;
            }

            ItemContainer.TransferItem(thing, belt.ItemContainer);

            // Need to check if it is a receiver or not ...
            belt.ThingOrigin = parent.Position;
        }

	}
}
