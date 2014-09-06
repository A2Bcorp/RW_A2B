#region Usings

using System;
using A2B.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

#endregion

namespace A2B
{
    [UsedImplicitly]
    public class BeltComponent : ThingComp
    {
        //Changed from private to public for access from BeltItemContainer

        protected BeltItemContainer ItemContainer;

        private Phase _beltPhase;

        private string _mythingID;

        private IntVec3 _splitterDest;

        public BeltComponent()
        {
            _beltPhase = Phase.Offline;

            ItemContainer = new BeltItemContainer(this);
            ThingOrigin = null;
        }

        public Phase BeltPhase
        {
            get { return _beltPhase; }
        }

        [NotNull]
        protected CompGlower GlowerComponent { get; set; }

        [NotNull]
        protected CompPowerTrader PowerComponent { get; set; }

        protected MovementType MovementType { get; set; }

        public int BeltSpeed { get; protected set; }

        protected IntVec3? ThingOrigin { set; get; }

        public bool Empty
        {
            get { return ItemContainer.Empty; }
        }

        public bool IsLoader { get; private set; }

        public override void CompDestroy(DestroyMode mode = DestroyMode.Vanish)
        {
            ItemContainer.Destroy();

            base.CompDestroy(mode);
        }

        public override void CompSpawnSetup()
        {
            GlowerComponent = parent.GetComp<CompGlower>();
            PowerComponent = parent.GetComp<CompPowerTrader>();

            switch (parent.def.defName)
            {
                case "A2BCurve":
                    MovementType = MovementType.Curve;
                    break;
                case "A2BSelector":
                    MovementType = MovementType.Selector;
                    break;
                case "A2BSplitter":
                    MovementType = MovementType.Splitter;
                    break;
                default:
                    MovementType = MovementType.Straight;
                    break;
            }

            switch (MovementType)
            {
                case MovementType.Straight:
                    BeltSpeed = Constants.DefaultBeltSpeed;
                    break;
                case MovementType.Curve:
                    BeltSpeed = Constants.DefaultBeltSpeed;
                    break;
                case MovementType.Selector:
                    BeltSpeed = Constants.DefaultBeltSpeed;
                    break;
                case MovementType.Splitter:
                    BeltSpeed = Constants.DefaultBeltSpeed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            IsLoader = parent.def.defName == "A2BLoader";
        }

        public override void CompExposeData()
        {
            Scribe_Values.LookValue(ref _beltPhase, "phase");

            Scribe_Deep.LookDeep(ref ItemContainer, "container", this);
        }

        public override void CompDraw()
        {
            foreach (var status in ItemContainer.ThingStatus)
            {
                var drawPos = parent.DrawPos + GetOffset(status) + Altitudes.AltIncVect * Altitudes.AltitudeFor(AltitudeLayer.Item);

                status.Thing.DrawAt(drawPos);

                DrawGUIOverlay(status, drawPos);
            }
        }

        protected static void DrawGUIOverlay([NotNull] ThingStatus status, Vector3 drawPos)
        {
            if (Find.CameraMap.CurrentZoom != CameraZoomRange.Closest)
            {
                return;
            }

            drawPos.z -= 0.4f;

            var screenPos = Find.CameraMap.camera.WorldToScreenPoint(drawPos);
            screenPos.y = Screen.height - screenPos.y;

            GenWorldUI.DrawThingLabel(new Vector2(screenPos.x, screenPos.y), GenString.NumberString(status.Thing.stackCount),
                new Color(1f, 1f, 1f, 0.75f));
        }

        protected virtual Vector3 GetOffset([NotNull] ThingStatus status)
        {
            var destination = GetDestinationForThing(status.Thing);

            IntVec3 direction;
            if (ThingOrigin.HasValue)
            {
                direction = destination - ThingOrigin.Value;
            }
            else
            {
                direction = parent.rotation.FacingSquare;
            }

            var progress = (float) status.Counter / BeltSpeed;

            if (Math.Abs(direction.x) == 1 && Math.Abs(direction.z) == 1 && ThingOrigin.HasValue)
            {
                // Diagonal movement
                var origin = ThingOrigin.Value;

                var incoming = (parent.Position - origin).ToVector3();
                var outgoing = (destination - parent.Position).ToVector3();

                // Now adjust the vectors.
                // Both need to be half the length so they only reach the edge of out square

                // The incoming vector also needs to be negated as it points in the wrong direction

                incoming = (-incoming) / 2;
                outgoing = outgoing / 2;

                // This is a funny function that should give me a quarter circle:
                // -sqrt(1 - (x - 1)^2) + 1
                var f = (progress - 1);
                var incomingVal = -Mathf.Sqrt(1 - f * f) + 1;

                var outgoingVal = -Mathf.Sqrt(1 - progress * progress) + 1;

                return incoming * incomingVal + outgoing * outgoingVal;
            }

            var dir = direction.ToVector3();
            dir.Normalize();

            var scaleFactor = progress - .5f;

            return dir * scaleFactor;
        }

        public override void CompTick()
        {
            DoBeltTick();

            ItemContainer.Tick();
        }

        public virtual IntVec3 GetDestinationForThing([NotNull] Thing thing)
        {
            switch (MovementType)
            {
                case MovementType.Straight:
                    return parent.Position + parent.rotation.FacingSquare;

                case MovementType.Curve:
                    var beltDestA = parent.Position - parent.rotation.FacingSquare;
                    var beltDestB = parent.Position +
                                    new IntVec3(-parent.rotation.FacingSquare.z, parent.rotation.FacingSquare.y, parent.rotation.FacingSquare.x);

                    return ThingOrigin == beltDestA ? beltDestB : beltDestA;

                case MovementType.Selector:
                    // Test the 'selection' idea ...
                    var slotParent = parent as SlotGroupParent;
                    if (slotParent == null)
                    {
                        throw new InvalidOperationException("parent is not a SlotGroupParent!");
                    }

                    var selectionSettings = slotParent.GetStoreSettings();
                    if (selectionSettings.AllowedToAccept(thing))
                    {
                        return parent.Position + parent.rotation.FacingSquare;
                    }

                    return parent.Position +
                           new IntVec3(parent.rotation.FacingSquare.z, parent.rotation.FacingSquare.y, -parent.rotation.FacingSquare.x);

                case MovementType.Splitter:
                    var beltDestL = parent.Position +
                                    new IntVec3(-parent.rotation.FacingSquare.z, parent.rotation.FacingSquare.y, parent.rotation.FacingSquare.x);
                    var beltDestR = parent.Position +
                                    new IntVec3(+parent.rotation.FacingSquare.z, parent.rotation.FacingSquare.y, -parent.rotation.FacingSquare.x);

                    // Do we have a new item ?
                    if (_mythingID == thing.ThingID)
                    {
                        return _splitterDest;
                    }
                    else
                    {
                        _mythingID = thing.ThingID;
                        if (_splitterDest == beltDestL)
                        {
                            // Is the other direction free ? Then switch. Else, don't switch !
                            var dest_belt = beltDestR.GetBeltComponent();
                            if (dest_belt.Empty)
                            {
                                _splitterDest = beltDestR;
                                return beltDestR;
                            }

                            return beltDestL;
                        }
                        else
                        {
                            var dest_belt = beltDestL.GetBeltComponent();
                            if (dest_belt.Empty)
                            {
                                _splitterDest = beltDestL;
                                return beltDestL;
                            }

                            return beltDestR;
                        }
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DoBeltTick()
        {
            if (PowerComponent.PowerOn)
            {
                // Power is on -> do work
                // ----------------------
                // phase == offline

                if (_beltPhase == Phase.Offline)
                {
                    // Turn on, incl. 'system online' glow
                    _beltPhase = Phase.Active;
                    GlowerComponent.Lit = true;

                    // Check if there is anything on the belt: yes? -> add it to our container
                    //foreach (var target in Find.Map.thingGrid.ThingsListAt(parent.Position))
                    foreach (var target in Find.Map.thingGrid.ThingsAt(parent.Position))
                    {
                        // Check and make sure this is not a Pawn, and not the belt itself !
                        if ((target.def.category == EntityCategory.Item) && (target != parent))
                        {
                            ItemContainer.AddItem(target, BeltSpeed / 2);
                        }
                    }

                    //glowerComp.def.glowColor = new ColorInt(255,200,0,0); // Hum ... that changes ALL the belt ... not what I want ...
                    return;
                }

                // phase == active
                if (_beltPhase != Phase.Active)
                {
                    return;
                }

                // Active 'yellow' color
                GlowerComponent.Lit = true; // in principle not required (should be already ON ...)

                ItemContainer.Tick();

                if (IsLoader)
                {
                    // If this is a loader check the things that are on the ground at our position
                    // If the thing can be moved and the destination is empty it can be added to our container
                    // This should fix the "pawn carry items to the loader all the time"-bug
                    foreach (var thing in Find.ThingGrid.ThingsAt(parent.Position))
                    {
                        if ((thing.def.category == EntityCategory.Item) && (thing != parent))
                        {
                            var destination = GetDestinationForThing(thing);
                            var destBelt = destination.GetBeltComponent();

                            if (destBelt == null)
                            {
                                continue;
                            }

                            if (!destBelt.Empty)
                            {
                                continue;
                            }

                            _itemContainer.AddItem(thing, BeltSpeed / 2);
                        }
                    }
                }

                if (!_itemContainer.WorkToDo)
                {
                    return;
                }

                foreach (var thing in ItemContainer.ThingsToMove)
                {
                    // Alright, I have something to move. Where to ?
                    var beltDest = GetDestinationForThing(thing);

                    MoveThingTo(thing, beltDest);
                }
            }
            else
            {
                // Power off -> reset everything
                // Let's be smart: check this only once, set the item to 'Unforbidden', and then, let the player choose what he wants to do
                // i.e. forbid or unforbid them ...
                if (_beltPhase != Phase.Active)
                {
                    return;
                }

                GlowerComponent.Lit = false;
                _beltPhase = Phase.Offline;
                ItemContainer.DropAll(parent.Position);
            }
        }

        protected virtual void MoveThingTo([NotNull] Thing thing, IntVec3 beltDest)
        {
            if (this.IsUnloader())
            {
                ItemContainer.DropItem(thing, beltDest);
            }
            else
            {
                var beltComponent = beltDest.GetBeltComponent();

                //  Check if there is a belt, if it is empty, and also check if it is active !
                if (beltComponent == null || !beltComponent.ItemContainer.Empty || beltComponent._beltPhase == Phase.Offline)
                {
                    return;
                }

                ItemContainer.TransferItem(thing, beltComponent.ItemContainer);

                // Need to check if it is a receiver or not ...
                beltComponent.ThingOrigin = parent.Position;
            }
        }

        [NotNull]
        public override string CompInspectString()
        {
            string statusText;
            switch (_beltPhase)
            {
                case Phase.Offline:
                    statusText = Constants.TxtStatus.Translate() + " " + Constants.TxtOffline.Translate();
                    break;
                case Phase.Active:
                    statusText = Constants.TxtStatus.Translate() + " " + Constants.TxtActive.Translate();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (ItemContainer.Empty)
            {
                return statusText;
            }

            return statusText + "\nContents: " + ((ThingContainerGiver) ItemContainer).GetContainer().ContentsString;
        }

        public void Notify_ReceivedThing([NotNull] Thing newItem)
        {
        }
    }
}
