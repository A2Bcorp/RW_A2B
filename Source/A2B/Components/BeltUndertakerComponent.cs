#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
#endregion
namespace A2B
{
	public class BeltUndertakerComponent : BeltComponent
	{

		private IntVec3 OutertakerPos;
		private static List<BeltUndertakerComponent> Undertakers = new List<BeltUndertakerComponent>();
		private static float basePowerConsumption = 0f;

        protected override void DoFreezeCheck()
        {
            // Undertakers don't freeze.
        }

		public override void PostSpawnSetup()
		{
			base.PostSpawnSetup();
            if (basePowerConsumption == 0f)
					basePowerConsumption = PowerComponent.PowerOutput;

			Undertakers.Add(this);
			// Try to assign pairs of undertakers
			Undertakers.ForEach(e => e.GetOutertakerPos());

		}

		private void GetOutertakerPos()
		{
			List<BeltUndertakerComponent> OutertakerList = new List<BeltUndertakerComponent>();
			switch (parent.Rotation.AsInt)
			{
			case 0:
				OutertakerList = Undertakers.Where(e => e.parent.Position.x == this.parent.Position.x && e.parent.Rotation.AsInt == 2
				                               && e.parent.Position.z > this.parent.Position.z).OrderBy(e => e.parent.Position.z).ToList();
				break;
			case 1:
				OutertakerList = Undertakers.Where(e => e.parent.Position.z == this.parent.Position.z && e.parent.Rotation.AsInt == 3
				                               && e.parent.Position.x > this.parent.Position.x).OrderBy(e => e.parent.Position.x).ToList();
				break;
			case 2:
				OutertakerList = Undertakers.Where(e => e.parent.Position.x == this.parent.Position.x && e.parent.Rotation.AsInt == 0
				                               && e.parent.Position.z < this.parent.Position.z).OrderByDescending(e => e.parent.Position.z).ToList();
				break;
			case 3:
				OutertakerList = Undertakers.Where(e => e.parent.Position.z == this.parent.Position.z && e.parent.Rotation.AsInt == 1
				                               && e.parent.Position.x < this.parent.Position.x).OrderByDescending(e => e.parent.Position.x).ToList();
				break;
			}
			if (OutertakerList.Count > 0)
			{
				OutertakerPos = OutertakerList[0].parent.Position;
			}
			else
			{
				OutertakerPos = IntVec3.Zero;
			}
		}

		public override IntVec3 GetDestinationForThing(Thing thing)
		{
			// If we are coming from above ground, then send to Outeraker.
			// Else, send to the next tile overground.
			var beltDestUp = parent.Position - parent.Rotation.FacingCell;
			var beltDestDown = OutertakerPos;
			
			return ThingOrigin == beltDestUp ? beltDestDown : beltDestUp;
		}

		// Check that two Undertakers are connected by Undercovers all the way.
		public bool IsConnectedUnder (BeltComponent belt)
		{
			var direction = (belt.parent.Position - this.parent.Position);
			var step = direction.ToVector3();
			step.Normalize();

			bool IsUndercover = false;

			// First, check if the two belts are in line
			if (belt.parent.Position.x == this.parent.Position.x || belt.parent.Position.z == this.parent.Position.z) 
			{
				var myPosition = this.parent.Position.ToVector3() + step;
				// If this is already the destination, then there is no Undercover gap at all !
				if (myPosition.ToIntVec3() == belt.parent.Position)
					return true;

				do
				{
					IsUndercover = false;

					foreach (var target in Find.Map.thingGrid.ThingsAt(myPosition.ToIntVec3()))
					{
						// Check to see if this is an Undercover element.
						if (target.def.defName == "A2BUndercover")
						{
							// Also need to check if it is connected to power !
							PowerComponent = target.TryGetComp<CompPowerTrader>();
							if (PowerComponent.PowerOn)
								IsUndercover = true;
						}
					}

					myPosition += step;

				} while ( (myPosition.ToIntVec3() != belt.parent.Position) && (IsUndercover));

				return IsUndercover;
			} 
			return false;
		}

        public override bool CanAcceptFrom (BeltComponent belt)
		{
			// If this is an Undertaker, make sure we are connected underground AND have the good oriantation !
			if (belt.IsUndertaker() && IsConnectedUnder(belt)) 
			{
				if (parent.Rotation.AsInt == 0)
					return ((belt.parent.Rotation.AsInt == 2) && (belt.parent.Position.x == this.parent.Position.x) && (belt.parent.Position.z > this.parent.Position.z));
				if (parent.Rotation.AsInt == 1)
					return ((belt.parent.Rotation.AsInt == 3) && (belt.parent.Position.z == this.parent.Position.z) && (belt.parent.Position.x > this.parent.Position.x));
				if (parent.Rotation.AsInt == 2)
					return ((belt.parent.Rotation.AsInt == 0) && (belt.parent.Position.x == this.parent.Position.x) && (belt.parent.Position.z < this.parent.Position.z));
				if (parent.Rotation.AsInt == 3)
					return ((belt.parent.Rotation.AsInt == 1) && (belt.parent.Position.z == this.parent.Position.z) && (belt.parent.Position.x < this.parent.Position.x));
				else
					return false; // Should never get here ...
			}
			else 
			{
				return this.GetPositionFromRelativeRotation(Rot4.South) == belt.parent.Position;
			}
        }

		public override void PostDraw()
		{	
			foreach (var status in ItemContainer.ThingStatus)
			{
				var drawPos = parent.DrawPos + GetOffset(status);
				drawPos += - Altitudes.AltIncVect*parent.DrawPos.y + Altitudes.AltIncVect*(Altitudes.AltitudeFor(AltitudeLayer.Waist) + 0.03f) ;
				
				status.Thing.DrawAt(drawPos);
				
				DrawGUIOverlay(status, drawPos);
			}
		}
		
		protected override Vector3 GetOffset (ThingStatus status)
		{
			var destination = GetDestinationForThing (status.Thing);

			var progress = (float)status.Counter / BeltSpeed;

			// Are we going under or getting out ?
			if (ThingOrigin == (parent.Position - parent.Rotation.FacingCell)) 
			{
				if (progress < 0.5)
				{
					var myDir = parent.Position - ThingOrigin;
					return myDir.ToVector3()*(progress-0.5f);
				}
				else
				{
					return parent.Position.ToVector3();
				}
			} 
			else 
			{
				
				if (progress < 0.5)
				{
					return parent.Position.ToVector3();
				}
				else
				{
					var myDir = destination - parent.Position;
					return myDir.ToVector3()*(progress-0.5f);
				}

			}

		}
		
		public override void PostDeSpawn()
		{
			Undertakers.Remove(this);
			Undertakers.ForEach(e => e.GetOutertakerPos());
			base.PostDeSpawn();
		}


	}
}
