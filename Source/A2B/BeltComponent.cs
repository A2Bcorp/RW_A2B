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
        public Phase _beltPhase;

        private BeltItemContainer _itemContainer;

        public BeltComponent()
        {
            _beltPhase = Phase.Offline;

            _itemContainer = new BeltItemContainer(this);
            ThingOrigin = null;
        }

        private CompGlower GlowerComponent { get; set; }

        private CompPowerTrader PowerComponent { get; set; }

        private MovementType MovementType { get; set; }

        public int BeltSpeed { get; private set; }

        private IntVec3? ThingOrigin { set; get; }

		// Used for the splitter only for now ...
		private string mythingID;
		private IntVec3 splitterDest;

        public bool IsUnloader { get; private set; }

        public bool Empty
        {
            get { return _itemContainer.Empty; }
        }

        public override void CompDestroy(DestroyMode mode = DestroyMode.Vanish)
        {
            _itemContainer.Destroy();

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

            // Speed things up a little and only do this once
            IsUnloader = parent.def.defName == "A2BUnloader";
        }

        public override void CompExposeData()
        {
            Scribe_Values.LookValue(ref _beltPhase, "phase");

            Scribe_Deep.LookDeep(ref _itemContainer, "container", this);
        }

        public override void CompDraw()
        {
            foreach (var status in _itemContainer.ThingStatus)
            {
                var drawPos = parent.DrawPos + GetOffset(status) + Altitudes.AltIncVect * Altitudes.AltitudeFor(AltitudeLayer.Item);

                status.Thing.DrawAt(drawPos);

                DrawGUIOverlay(status, drawPos);
            }
        }

        private static void DrawGUIOverlay(ThingStatus status, Vector3 drawPos)
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

        private Vector3 GetOffset(ThingStatus status)
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

            _itemContainer.Tick();
        }

        public IntVec3 GetDestinationForThing(Thing thing)
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

				case A2B.MovementType.Splitter:
					var beltDestL = parent.Position +
									new IntVec3(-parent.rotation.FacingSquare.z, parent.rotation.FacingSquare.y, parent.rotation.FacingSquare.x);
					var beltDestR = parent.Position +
									new IntVec3(+parent.rotation.FacingSquare.z, parent.rotation.FacingSquare.y, -parent.rotation.FacingSquare.x);
	
					// Do we have a new item ?
					if (mythingID == thing.ThingID)
					{
						return splitterDest;
					}
					else	
					{	
						mythingID = thing.ThingID;
						if (splitterDest == beltDestL) 
						{
							
							splitterDest = beltDestR;
							return beltDestR;
						}
					
						splitterDest = beltDestL;
						return beltDestL;
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
                            _itemContainer.AddItem(target, BeltSpeed / 2);
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

                _itemContainer.Tick();

                if (!_itemContainer.WorkToDo)
                {
                    return;
                }

                foreach (var thing in _itemContainer.ThingsToMove)
                {
                    if (IsUnloader)
                    {
                        var beltDest = GetDestinationForThing(thing);

                        if (Find.ThingGrid.CellContains(beltDest, EntityCategory.Building))
                        {
                            continue;
                        }

                        if (Find.ThingGrid.CellContains(beltDest, EntityCategory.Item))
                        {
                            continue;
                        }

                        if (Find.ThingGrid.CellContains(beltDest, EntityCategory.Terrain))
                        {
                            continue;
                        }

                        _itemContainer.DropItem(thing, beltDest);
                    }
                    else
                    {
                        // Alright, I have something to move. Where to ?
                        var beltDest = GetDestinationForThing(thing);

                        var beltComponent = beltDest.GetBeltComponent();

						//  Check if there is a belt, if it is empty, and also check if it is active !
                        if (beltComponent == null || !beltComponent._itemContainer.Empty || beltComponent._beltPhase == Phase.Offline)
                        {
                            continue;
                        }

                        _itemContainer.TransferItem(thing, beltComponent._itemContainer);
                        beltComponent.ThingOrigin = parent.Position;
                    }
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
                _itemContainer.DropAll(parent.Position);
            }
        }

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

            if (_itemContainer.Empty)
            {
                return statusText;
            }

            return statusText + "\nContents: " + ((ThingContainerGiver) _itemContainer).GetContainer().ContentsString;
        }

        public void Notify_ReceivedThing(Thing newItem)
        {
            _itemContainer.AddItem(newItem, BeltSpeed / 2);
        }
    }
}
