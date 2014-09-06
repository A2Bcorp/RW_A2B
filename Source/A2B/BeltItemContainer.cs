#region Usings

using System.Collections.Generic;
using System.Linq;
using A2B.Annotations;
using RimWorld;
using Verse;
using Verse.Sound;

#endregion

namespace A2B
{
    public class ThingStatus
    {
        public ThingStatus([NotNull] Thing thing, int counter)
        {
            Thing = thing;
            Counter = counter;
        }

        [NotNull]
        public Thing Thing { get; private set; }

        public int Counter { get; private set; }
    }

    public class BeltItemContainer : Saveable, ThingContainerGiver
    {
        private readonly BeltComponent _parentComponent;

        private readonly Dictionary<Thing, int> _thingCounter;

        private ThingContainer _container;

        public BeltItemContainer([NotNull] BeltComponent component)
        {
            _parentComponent = component;

            _container = new ThingContainer(this);
            _thingCounter = new Dictionary<Thing, int>();
        }

        [NotNull]
        public IEnumerable<Thing> Contents
        {
            get { return _container.Contents; }
        }

        [NotNull]
        public IEnumerable<Thing> ThingsToMove
        {
            get { return _thingCounter.Where(pair => pair.Value >= _parentComponent.BeltSpeed).Select(pair => pair.Key).ToList(); }
        }

        public bool WorkToDo
        {
            get { return _thingCounter.Any(pair => pair.Value >= _parentComponent.BeltSpeed); }
        }

        public bool Empty
        {
            get { return _container.Empty; }
        }

        [NotNull]
        public IEnumerable<ThingStatus> ThingStatus
        {
            get { return _container.Contents.Select(thing => new ThingStatus(thing, _thingCounter[thing])); }
        }

        #region Saveable Members

        public void ExposeData()
        {
            Scribe_Deep.LookDeep(ref _container, "container");

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Dictionary<string, int> counterDictionary = null;
                Scribe_Fixed.LookDictionary(ref counterDictionary, "thingCounter", LookMode.Value);

                _thingCounter.Clear();
                if (counterDictionary != null)
                {
                    foreach (var pair in counterDictionary)
                    {
                        var thing = _container.Contents.FirstOrDefault(t => t.ThingID == pair.Key);

                        if (thing != null)
                        {
                            _thingCounter.Add(thing, pair.Value);
                        }
                    }
                }
            }
            else if (Scribe.mode == LoadSaveMode.Saving)
            {
                var counterDictionary = _thingCounter.ToDictionary(pair => pair.Key.ThingID, pair => pair.Value);

                Scribe_Fixed.LookDictionary(ref counterDictionary, "thingCounter", LookMode.Value);
            }
        }

        #endregion

        #region ThingContainerGiver Members

        [NotNull]
        ThingContainer ThingContainerGiver.GetContainer()
        {
            return _container;
        }

        #endregion

        public void Tick()
        {
            _container.ThingContainerTick();

            foreach (var thing in Contents.Where(ShouldIncreaseCounter))
            {
                _thingCounter[thing]++;
            }
        }

        private bool ShouldIncreaseCounter([NotNull] Thing thing)
        {
            var currentCounter = _thingCounter[thing];
            if (currentCounter < _parentComponent.BeltSpeed / 2 && !_parentComponent.IsReceiver())
            {
                // Always increase the counter until half the belt speed is reached
                return true;
            }

            // Never go above 100%
            if (currentCounter >= _parentComponent.BeltSpeed)
            {
                return false;
            }

            var destination = _parentComponent.GetDestinationForThing(thing);

            var belt = destination.GetBeltComponent();

            // If no belt items, then move things only if this is an unloader
            if (belt == null)
            {
                if (_parentComponent.IsUnloader())
                {
                    // If this is an unloader always increment the counter
                    // BUG: need to check that space is free
                    return true;
                }

                return false;
            }

            // Teleporter only sends items to receivers with the good orientation (avoid visual problems)
            if (_parentComponent.IsTeleporter() && (!belt.IsReceiver() || _parentComponent.parent.rotation.AsInt != belt.parent.rotation.AsInt))
            {
                return false;
            }

            // Only a teleporter can send items to a receiver ...
            if (!_parentComponent.IsTeleporter() && belt.IsReceiver())
            {
                return false;
            }

            // Check that the next belt component has the good orientation: for belt, splitter and unloaders
            if ((belt.parent.def.defName == "A2BBelt" || belt.parent.def.defName == "A2BSplitter" || belt.parent.def.defName == "A2BUnloader") &&
                !(_parentComponent.parent.Position == belt.parent.Position - belt.parent.rotation.FacingSquare))
            {
                return false;
            }

            // Check that the Teleporter has a good orientation with respect to the current element and is shifted properly
            // (BUG: no check done for splitters, selectors or curves !)
            if (belt.IsTeleporter() && (_parentComponent.parent.def.defName == "A2BBelt" || _parentComponent.parent.def.defName == "A2BLoader") &&
                (belt.parent.rotation.AsInt != _parentComponent.parent.rotation.AsInt))
            {
                return false;
            }

            // BUG: need to check that Teleporter is 'in-line' with belt element, and not shifted sideways.
            // BUG: need to check correct orientation for curves & selectors

            // Belt loaders can only be fed manually
            if (belt.parent.def.defName == "A2BLoader")
            {
                return false;
            }

            // Move beyond 50% only if next component is on ! 
            if (belt.BeltPhase == Phase.Offline)
            {
                return false;
            }

            return belt.Empty;
        }

        public bool AddItem([NotNull] Thing t, int initialCounter = 0)
        {
            if (!_container.TryAdd(t))
            {
                return false;
            }

            _thingCounter[t] = initialCounter;
            return true;
        }

        public void TransferItem([NotNull] Thing item, [NotNull] BeltItemContainer other)
        {
            _container.Remove(item);
            _thingCounter.Remove(item);

            other.AddItem(item);
        }

        public void DropItem([NotNull] Thing item, IntVec3 position)
        {
            var backupSound = item.def.soundDrop;
            item.def.soundDrop = null;

            try
            {
                Thing droppedItem;
                if (!_container.TryDrop(item, position, ThingPlaceMode.Direct, out droppedItem))
                {
                    return;
                }

                // Play the sound as that isn't handled by the ThingContainer anymore...
                if (backupSound != null)
                {
                    backupSound.PlayOneShot(position);
                }

                _thingCounter.Remove(item);

                if (droppedItem is ThingWithComponents)
                {
                    droppedItem.SetForbidden(false);
                }
            }
            finally
            {
                // Stupid hack to make sure the drop sound is not played all the time
                item.def.soundDrop = backupSound;
            }
        }

        public void DropAll(IntVec3 position)
        {
            // Check if there is anything on the belt: yes? -> make it accessible to colonists
            foreach (var thing in _container.Contents.ToList())
            {
                DropItem(thing, position);
            }

            _thingCounter.Clear();
        }

        public void Destroy()
        {
            DropAll(_parentComponent.parent.Position);
            _container.DestroyContents();
        }
    }
}
