#region Usings

using System;
using A2B.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using VerseBase;

#endregion

namespace A2B
{
    [UsedImplicitly]
    public class BeltComponent : ThingComp
    {
        //Changed from private to public for access from BeltItemContainer

        protected BeltItemContainer ItemContainer;

        private Phase _beltPhase;

        private IntVec3 _thingOrigin;

        public BeltComponent()
        {
            _beltPhase = Phase.Offline;

            ItemContainer = new BeltItemContainer(this);
            ThingOrigin = IntVec3.Invalid;

            BeltSpeed = Constants.DefaultBeltSpeed;
        }

        public Phase BeltPhase
        {
            get { return _beltPhase; }
        }

        [NotNull]
        protected CompGlower GlowerComponent { get; set; }

        [NotNull]
        protected CompPowerTrader PowerComponent { get; set; }

        public int BeltSpeed { get; protected set; }

        protected IntVec3 ThingOrigin
        {
            set { _thingOrigin = value; }
            get { return _thingOrigin; }
        }

        public bool Empty
        {
            get { return ItemContainer.Empty; }
        }

        public override void PostDestroy(DestroyMode mode = DestroyMode.Vanish)
        {
            ItemContainer.Destroy();

            base.PostDestroy(mode);
        }

        public override void PostSpawnSetup()
        {
            GlowerComponent = parent.GetComp<CompGlower>();
            PowerComponent = parent.GetComp<CompPowerTrader>();
        }

        public override void PostExposeData()
        {
            Scribe_Values.LookValue(ref _beltPhase, "phase");

            Scribe_Deep.LookDeep(ref ItemContainer, "container", this);

            Scribe_Values.LookValue(ref _thingOrigin, "thingOrigin", IntVec3.Invalid);
        }

        public override void PostDraw()
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
            if (ThingOrigin != IntVec3.Invalid)
            {
                direction = destination - ThingOrigin;
            }
            else
            {
                direction = parent.Rotation.FacingSquare;
            }

            var progress = (float) status.Counter / BeltSpeed;

            if (Math.Abs(direction.x) == 1 && Math.Abs(direction.z) == 1 && ThingOrigin != IntVec3.Invalid)
            {
                // Diagonal movement
                var incoming = (parent.Position - ThingOrigin).ToVector3();
                var outgoing = (destination - parent.Position).ToVector3();

                // Now adjust the vectors.
                // Both need to be half the length so they only reach the edge of out square

                // The incoming vector also needs to be negated as it points in the wrong direction

                incoming = (-incoming) / 2;
                outgoing = outgoing / 2;

                var angle = progress * Mathf.PI / 2;

                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);

                return incoming * (1 - sin) + outgoing * (1 - cos);
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
            //return parent.Position + parent.Rotation.FacingSquare;
            return this.GetPositionFromRelativeRotation(IntRot.north);
        }

        public virtual bool CanAcceptFrom(BeltComponent belt)
        {
            // If I can't accept from anyone, I certainly can't accept from you.
            if (!CanAcceptSomething())
                return false;

            for (int i = 0; i < 4; ++i)
            {
                IntRot dir = new IntRot(i);
                if (CanAcceptFrom(dir) && belt.parent.Position == this.GetPositionFromRelativeRotation(dir))
                    return true;
            }

            return false;
        }

        /**
         *  This method assumes that the component can accept in general - i.e. If it can accept at all, can
         *  it accept from the given direction? (If it accepts from the south, but it's currently clogged, this
         *  method still returns true)
         **/
        public virtual bool CanAcceptFrom(IntRot direction)
        {
            return (direction == IntRot.south);
        }

        public virtual bool CanAcceptSomething()
        {
            return (Empty && BeltPhase == Phase.Active);
        }

        public virtual bool CanOutputToNonBelt()
        {
            return false;
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

                PostItemContainerTick();

                if (!ItemContainer.WorkToDo)
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

        protected virtual void PostItemContainerTick()
        {
            // stub
        }

        protected virtual void MoveThingTo([NotNull] Thing thing, IntVec3 beltDest)
        {
            if (CanOutputToNonBelt() && Find.TerrainGrid.TerrainAt(beltDest).changeable)
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
        public override string CompInspectStringExtra()
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
    }
}
