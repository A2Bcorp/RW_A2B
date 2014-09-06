#region Usings

using A2B.Annotations;
using UnityEngine;
using Verse;

#endregion

namespace A2B
{
    public class BeltTeleporterComponent : BeltComponent
    {
        public override void CompSpawnSetup()
        {
            base.CompSpawnSetup();

            if (!this.IsReceiver())
            {
                BeltSpeed = 3 * Constants.DefaultBeltSpeed;
            }
        }

        public override IntVec3 GetDestinationForThing(Thing thing)
        {
            if (this.IsReceiver())
            {
                return base.GetDestinationForThing(thing);
            }

            return parent.Position +
                   new IntVec3(3 * parent.rotation.FacingSquare.x, parent.rotation.FacingSquare.y, 3 * parent.rotation.FacingSquare.z);
        }

        public override void CompDraw()
        {
            foreach (var status in ItemContainer.ThingStatus)
            {
                var posOffset = parent.DrawPos;
                var mySize = parent.RotatedSize.ToVector3();

                switch (parent.rotation.AsInt)
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
            if (ThingOrigin.HasValue && !this.IsReceiver())
            {
                direction = destination - ThingOrigin.Value;
                midDirection = parent.Position + parent.rotation.FacingSquare - ThingOrigin.Value;
            }
            else
            {
                if (!this.IsReceiver())
                {
                    direction = new IntVec3(3 * parent.rotation.FacingSquare.x, parent.rotation.FacingSquare.y, 3 * parent.rotation.FacingSquare.z);
                    midDirection = parent.rotation.FacingSquare;
                }
                else
                {
                    direction = parent.rotation.FacingSquare;
                    midDirection = parent.rotation.FacingSquare; // Should never be used in principle ...
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
    }
}
