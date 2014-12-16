#region Usings
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
#endregion
namespace A2B
{
	public class BeltTeleporterComponent : BeltComponent
	{
		private IntVec3 ReceiverPos;
		private static List<BeltTeleporterComponent> Receivers = new List<BeltTeleporterComponent>();
		private static List<BeltTeleporterComponent> Teleporters = new List<BeltTeleporterComponent>();
		private static float basePowerConsumption = 0f;
		public override void PostSpawnSetup()
		{
			base.PostSpawnSetup();
			if (!this.IsReceiver())
			{
				BeltSpeed = 3 * Constants.DefaultBeltSpeed;
				Teleporters.Add(this);
				// Ensure that Teleporters have minimum energy cost when no receiver exist
				if (basePowerConsumption == 0f)
					basePowerConsumption = PowerComponent.powerOutput;
				GetReceiverPos();

			}
			else
			{
				if (basePowerConsumption == 0f)
					basePowerConsumption = PowerComponent.powerOutput;
				Receivers.Add(this);
				Teleporters.ForEach(e => e.GetReceiverPos());
			}
		}
		private void GetReceiverPos()
		{
			List<BeltTeleporterComponent> ReceiverList = new List<BeltTeleporterComponent>();
			switch (parent.Rotation.AsInt)
			{
			case 0:
				ReceiverList = Receivers.Where(e => e.parent.Position.x == this.parent.Position.x && e.parent.Rotation.AsInt == 0
				                               && e.parent.Position.z > this.parent.Position.z).OrderBy(e => e.parent.Position.z).ToList();
				break;
			case 1:
				ReceiverList = Receivers.Where(e => e.parent.Position.z == this.parent.Position.z && e.parent.Rotation.AsInt == 1
				                               && e.parent.Position.x > this.parent.Position.x).OrderBy(e => e.parent.Position.x).ToList();
				break;
			case 2:
				ReceiverList = Receivers.Where(e => e.parent.Position.x == this.parent.Position.x && e.parent.Rotation.AsInt == 2
				                               && e.parent.Position.z < this.parent.Position.z).OrderByDescending(e => e.parent.Position.z).ToList();
				break;
			case 3:
				ReceiverList = Receivers.Where(e => e.parent.Position.z == this.parent.Position.z && e.parent.Rotation.AsInt == 3
				                               && e.parent.Position.x < this.parent.Position.x).OrderByDescending(e => e.parent.Position.x).ToList();
				break;
			}
			if (ReceiverList.Count > 0)
			{
				ReceiverPos = ReceiverList[0].parent.Position;
				PowerComponent.powerOutput = Vector3.Distance(ReceiverPos.ToVector3(), parent.Position.ToVector3()) * basePowerConsumption;
			}
			else
			{
				ReceiverPos = IntVec3.zero;
				PowerComponent.powerOutput = basePowerConsumption;
			}
		}
		public override IntVec3 GetDestinationForThing(Thing thing)
		{
			if (this.IsReceiver())
			{
				return base.GetDestinationForThing(thing);
			}
			return ReceiverPos;
		}
		public override void PostDraw()
		{
			foreach (var status in ItemContainer.ThingStatus)
			{
				var posOffset = parent.DrawPos;
				var mySize = parent.RotatedSize.ToVector3();
				switch (parent.Rotation.AsInt)
				{
				case 0:
					posOffset = new Vector3((parent.DrawPos.x - 0.5f * (mySize.x - 1.0f)), parent.DrawPos.y,
					                        (parent.DrawPos.z - 0.5f * (mySize.z - 1.0f)));
					break;
				case 1:
					posOffset = new Vector3((parent.DrawPos.x - 0.5f * (mySize.x - 1.0f)), parent.DrawPos.y,
					                        1.0f + (parent.DrawPos.z - 0.5f * (mySize.z - 1.0f)));
					break;
				case 2:
					if (!this.IsReceiver())
					{
						posOffset = new Vector3((parent.DrawPos.x - 0.5f * (mySize.x - 1.0f)) + 1.0f, parent.DrawPos.y,
						                        1.0f + (parent.DrawPos.z - 0.5f * (mySize.z - 1.0f)));
					}
					else
					{
						posOffset = new Vector3((parent.DrawPos.x - 0.5f * (mySize.x - 1.0f)) + 1.0f, parent.DrawPos.y,
						                        (parent.DrawPos.z - 0.5f * (mySize.z - 1.0f)));
					}
					break;
				case 3:
					if (!this.IsReceiver())
					{
						posOffset = new Vector3((parent.DrawPos.x - 0.5f * (mySize.x - 1.0f)) + 1.0f, parent.DrawPos.y,
						                        (parent.DrawPos.z - 0.5f * (mySize.z - 1.0f)));
					}
					else
					{
						posOffset = new Vector3((parent.DrawPos.x - 0.5f * (mySize.x - 1.0f)), parent.DrawPos.y,
						                        (parent.DrawPos.z - 0.5f * (mySize.z - 1.0f)));
					}
					break;
				}
				var drawPos = posOffset + GetOffset(status) + Altitudes.AltIncVect * Altitudes.AltitudeFor(AltitudeLayer.Item);
				status.Thing.DrawAt(drawPos);
				DrawGUIOverlay(status, drawPos);
			}
		}
		protected override Vector3 GetOffset(ThingStatus status)
		{
			var destination = GetDestinationForThing(status.Thing);
			IntVec3 direction;
			IntVec3 midDirection;
			if (ThingOrigin != IntVec3.Invalid && !this.IsReceiver())
			{
				direction = destination - ThingOrigin;
				midDirection = parent.Position + parent.Rotation.FacingSquare - ThingOrigin;
			}
			else
			{
				if (!this.IsReceiver())
				{
					direction = new IntVec3(3 * parent.Rotation.FacingSquare.x, parent.Rotation.FacingSquare.y, 3 * parent.Rotation.FacingSquare.z);
					midDirection = parent.Rotation.FacingSquare;
				}
				else
				{
					direction = parent.Rotation.FacingSquare;
					midDirection = parent.Rotation.FacingSquare; // Should never be used in principle ...
				}
			}
			var progress = (float) status.Counter / BeltSpeed;
			// In case we use the teleporter
			if (this.IsReceiver())
			{
				var myDir = direction.ToVector3();
				return myDir * progress * 0.5f;
			}
			if (progress < 0.5)
			{
				// Slew item to teleportation pad
				var midDir = midDirection.ToVector3();
				var midDirOff = new Vector3(0.5f * midDir.normalized.x, 0.0f, 0.5f * midDir.normalized.z);
				var midDirCorr = midDir.normalized + midDirOff;
				var midScaleFactor = progress / 0.5f;
				return (midDirCorr * midScaleFactor) - midDirOff;
			}
			// Start the teleportation
			var randomNumber = Random.Range(0.0f, 1.0f);
			if (randomNumber > 2 * (progress - 0.5f))
			{
				var finDira = midDirection.ToVector3();
				finDira.Normalize();
				return finDira;
			}
			var finDir = direction.ToVector3();
			var finDirNorm = direction.ToVector3();
			finDirNorm.Normalize(); // Can't normalize, or it doesn't go anywhere ... !
			return (finDir - finDirNorm);
		}
		public override void PostDeSpawn()
		{
			Receivers.Remove(this);
			Teleporters.ForEach(e => e.GetReceiverPos());
			base.PostDeSpawn();
		}
	}
}
