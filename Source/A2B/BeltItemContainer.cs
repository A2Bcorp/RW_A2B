#region Usings

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using Verse;

#endregion

namespace A2B
{
    public class ThingStatus
    {
        public ThingStatus(Thing thing, int counter)
        {
            Thing = thing;
            Counter = counter;
        }

        public Thing Thing { get; private set; }

        public int Counter { get; private set; }
    }

    public class BeltItemContainer : Saveable, ThingContainerGiver
    {
        private readonly BeltComponent _parentComponent;

        private readonly Dictionary<Thing, int> _thingCounter;

        private ThingContainer _container;

        public BeltItemContainer(BeltComponent component)
        {
            _parentComponent = component;

            _container = new ThingContainer(this);
            _thingCounter = new Dictionary<Thing, int>();
        }

        public IEnumerable<Thing> Contents
        {
            get { return _container.Contents; }
        }

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

        private bool ShouldIncreaseCounter(Thing thing)
        {
            var currentCounter = _thingCounter[thing];
            if (currentCounter < _parentComponent.BeltSpeed / 2)
            {
                // Always increase the counter until half the belt speed is reached
                return true;
            }

            if (currentCounter >= _parentComponent.BeltSpeed)
            {
                return false;
            }

            var destination = _parentComponent.GetDestinationForThing(thing);

            var belt = destination.GetBeltComponent();

            if (belt == null)
            {
                return _parentComponent.IsUnloader;
            }

            return belt.Empty;
        }

        public bool AddItem(Thing t, int initialCounter = 0)
        {
            if (!_container.TryAdd(t))
            {
                return false;
            }

            _thingCounter[t] = initialCounter;
            return true;
        }

        public void TransferItem(Thing item, BeltItemContainer other)
        {
            _container.Remove(item);
            _thingCounter.Remove(item);

            other.AddItem(item);
        }

        public void DropItem(Thing item, IntVec3 position)
        {
            Thing droppedItem;
            if (!_container.TryDrop(item, position, ThingPlaceMode.Direct, out droppedItem))
            {
                return;
            }

            _thingCounter.Remove(item);

            if (droppedItem is ThingWithComponents)
            {
                droppedItem.SetForbidden(false);
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
