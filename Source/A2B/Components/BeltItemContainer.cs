#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using A2B.Annotations;
using RimWorld;
using Verse;
using Verse.Sound;

#endregion

namespace A2B
{
    public enum MovementStatus
    {
        Undefined,
        Moving,
        WaitClear,
        WaitMerge,
        MergingIn,
        MergingOut
    }

    public class ThingStatus : IExposable
    {
        public ThingStatus([NotNull] Thing thing, int counter, MovementStatus status)
        {
            Thing = thing;
            Counter = counter;
            Status = status;
            Merge = null;
        }

        public ThingStatus()
        {
            Thing = null;
            Counter = 0;
            Status = MovementStatus.Undefined;
            Merge = null;
        }

        public void ExposeData()
        {
            var t = Thing;
            var c = Counter;
            var s = Status;
            var m = Merge;
            Scribe_Values.LookValue( ref c, "counter" );
            Scribe_Values.LookValue( ref s, "status" );
            Scribe_References.LookReference( ref m, "merge" );
            Scribe_Deep.LookDeep( ref t, "thing" );
            Thing = t;
            Counter = c;
            Status = s;
            Merge = m;
        }

        public bool IsWaiting
        {
            get
            {
                if(
                    ( Status == MovementStatus.WaitClear )||
                    ( Status == MovementStatus.WaitMerge )
                )
                    return true;
                return false;
            }
        }

        [NotNull]
        public Thing Thing { get; private set; }

        public int Counter { get; set; }

        public MovementStatus Status { get; set; }

        public Thing Merge { get; set; }

    }

    public class BeltItemContainer : IExposable, IThingContainerOwner
    {
        private readonly BeltComponent _parentComponent;

        private List<ThingStatus> _thingStatus;

        //private readonly Dictionary<Thing, int> _thingCount;

        private ThingContainer _container;

        static BeltItemContainer()
        {
            A2BMonitor.RegisterTickAction("BeltItemContainer.DoAtmosphereEffects", DoAtmosphereEffects);
        }

        private static int cycle = 0;
        public static bool DoAtmosphereEffects( object target )
        {
            int cells = (int) (Find.Map.Area * 0.0006f);
           
            for (int i = 0; i < cells; ++i) {
                if (cycle >= Find.Map.Area)
                    cycle = 0;

                var cell = MapCellsInRandomOrder.Get(cycle++);
                var belt = cell.GetBeltSurfaceComponent();

                if (belt != null) {
                    belt.ItemContainer.AtmosphereEffectsTick();
                }
            }

			return false;
        }

        public BeltItemContainer([NotNull] BeltComponent component)
        {
            _parentComponent = component;

            _thingStatus = new List<ThingStatus>();

            _container = new ThingContainer(this);

            //_thingCount = new Dictionary<Thing, int>();
        }

        [NotNull]
        public IEnumerable<Thing> Contents
        {
            //get { return _container; }
            get { return _thingStatus.Select( s => s.Thing ).ToList(); }
        }

        [NotNull]
        public IEnumerable<Thing> ThingsToMove
        {
            //get { return _thingCount.Where(pair => pair.Value >= TicksToMove).Select(pair => pair.Key).ToList(); }
            get { return _thingStatus.Where( s => ( !s.IsWaiting )&&( s.Counter >= TicksToMove ) ).Select( s => s.Thing ).ToList(); }
        }

        public bool WorkToDo
        {
            //get { return _thingCount.Any(pair => pair.Value >= TicksToMove); }
            get { return _thingStatus.Any( s => !s.IsWaiting ); }
        }

        public bool Empty
        {
            //get { return _container.Count < 1 ; }
            get { return _thingStatus.Count < 1; }
        }

        public int TicksToMove {
            get { return _parentComponent.BeltSpeed; }
        }

        [NotNull]
        public IEnumerable<ThingStatus> ThingStatus
        {
            //get { return _container.Select(thing => new ThingStatus(thing, _thingCount[thing])); }
            get { return _thingStatus; }
        }

        #region Saveable Members

        public void ExposeData()
        {
            Scribe_Collections.LookList( ref _thingStatus, "contents", LookMode.Deep );

            if(
                ( Scribe.mode == LoadSaveMode.LoadingVars )&&
                ( _thingStatus == null )
            )
            {
                _thingStatus = new List<A2B.ThingStatus>();
            }

            /*
            Scribe_Deep.LookDeep(ref _container, "container");

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Dictionary<string, int> counterDictionary = null;
                Scribe_Fixed.LookDictionary(ref counterDictionary, "thingCounter", LookMode.Value);

                _thingCount.Clear();
                if (counterDictionary != null)
                {
                    foreach (var pair in counterDictionary)
                    {
                        var thing = _container.FirstOrDefault(t => t.ThingID == pair.Key);

                        if (thing != null)
                        {
                            _thingCount.Add(thing, pair.Value);
                        }
                    }
                }
            }
            else if (Scribe.mode == LoadSaveMode.Saving)
            {
                var counterDictionary = _thingCount.ToDictionary(pair => pair.Key.ThingID, pair => pair.Value);

                Scribe_Fixed.LookDictionary(ref counterDictionary, "thingCounter", LookMode.Value);
            }
            */
        }

        #endregion

        #region ThingContainerOwner Members

        public string ContentsString
        {
            get
            {
                if( _thingStatus.Count == 0 )
                    return "NothingLower".Translate();
                StringBuilder stringBuilder = new StringBuilder();
                for( int index = 0; index < _thingStatus.Count; ++index )
                {
                    if( index != 0 )
                        stringBuilder.Append( ", " );
                    stringBuilder.Append( _thingStatus[ index ].Thing.Label );
                    #if DEBUG
                    stringBuilder.Append( " " + _thingStatus[ index ].Status );
                    stringBuilder.Append( " " + _thingStatus[ index ].Counter );
                    #endif
                }
                return stringBuilder.ToString();
            }
        }

        [NotNull]
        ThingContainer IThingContainerOwner.GetContainer()
        {
            return _container;
        }

		[NotNull]
		IntVec3 IThingContainerOwner.GetPosition()
		{
			return _parentComponent.parent.PositionHeld;
		}

        [NotNull]
        bool IThingContainerOwner.Spawned
        {
            get
            {
                return this._parentComponent.parent.Spawned;
            }
        }

        #endregion

        public bool MovingThings()
        {
            return _thingStatus.Count( s => !s.IsWaiting ) > 0 ;
        }

        public void MoveTick()
        {
            if( _parentComponent.AllowLowPowerMode() )
            {
                if( _parentComponent.MovingThings() )
                {
                    _parentComponent.PowerComponent.PowerOutput = -_parentComponent.GetBasePowerConsumption();
                }
                else
                {
                    _parentComponent.PowerComponent.PowerOutput = -_parentComponent.GetBasePowerConsumption() * A2BData.LowPowerFactor;
                }
            }

            foreach( var thingStatus in _thingStatus.Where(ShouldIncreaseCounter))
            {
                thingStatus.Counter++;
            }

            if( !_parentComponent.parent.IsHashIntervalTick( 30 ) )
            {
                // Only do a status update twice per second
                return;
            }

            for( int index = _thingStatus.Count - 1; index >= 0; --index )
            {
                var thingStatus = _thingStatus[ index ];

                if( thingStatus.Status == MovementStatus.Moving )
                {
                    // Item it moving
                    if( thingStatus.Counter >= TicksToMove / 2 )
                    {
                        // It's at least half way, get destination for item
                        var destination = _parentComponent.GetDestinationForThing( thingStatus.Thing );
                        var belt = destination.GetBeltComponent( _parentComponent );
                        if(
                            (
                                ( belt != null )&&
                                ( !belt.Empty )
                            )||
                            (
                                ( belt == null )&&
                                (
                                    ( !_parentComponent.CanOutputToNonBelt( destination, thingStatus.Thing ) )||
                                    ( !destination.NoStorageBlockersIn( thingStatus.Thing ) )
                                )
                            )
                        )
                        {
                            // Belt isn't empty or no belt and can't output to non-belt or non-belt is blocked
                            thingStatus.Status = MovementStatus.WaitClear;
                        }
                    }
                }
                else if( thingStatus.Status == MovementStatus.MergingIn )
                {
                    var mergeStatus = GetWaitMerge();
                    if(
                        ( mergeStatus != null )&&
                        ( mergeStatus.Merge == thingStatus.Thing )&&
                        ( thingStatus.Merge == mergeStatus.Thing )
                    )
                    {
                        // Merging item into existing stack when it reaches it
                        if( thingStatus.Counter >= mergeStatus.Counter )
                        {
                            // Try absorb incoming stack
                            if( mergeStatus.Thing.TryAbsorbStack( thingStatus.Thing, true ) )
                            {
                                // Merged stacks, change status of waiting stack
                                mergeStatus.Merge = null;
                                mergeStatus.Status = MovementStatus.WaitClear;
                                _thingStatus.Remove( thingStatus );
                                break;
                            }
                            // Make the left-overs(!?) wait
                            thingStatus.Merge = null;
                            thingStatus.Status = MovementStatus.WaitClear;
                        }
                    }
                    else
                    {
                        // Could find merge wait?
                        Log.Error( string.Format( "Merge source {0} ({1}) does not match merge target {2} ({3})!", thingStatus.Thing.ThingID, thingStatus.Merge?.ThingID, mergeStatus.Thing.ThingID, mergeStatus.Merge?.ThingID ) );
                        thingStatus.Status = MovementStatus.WaitClear;
                    }
                }
                else if( thingStatus.Status == MovementStatus.WaitClear )
                {
                    if( !MovingThings() )
                    {
                        // Nothing moving on this belt

                        // Get destination for thing
                        var destination = _parentComponent.GetDestinationForThing( thingStatus.Thing );
                        var belt = destination.GetBeltComponent( _parentComponent );
                        if(
                            ( belt != null )&&
                            ( belt.CanAcceptThing( thingStatus.Thing ) )
                        )
                        {
                            if( belt.Empty )
                            {
                                // Next belt is ready
                                thingStatus.Status = MovementStatus.Moving;
                            }
                            else
                            {
                                var mergeStatus = belt.ItemContainer.GetWaitMerge();
                                if(
                                    ( mergeStatus.Merge == null )&&
                                    ( belt.ItemContainer.GetWantedDef() == thingStatus.Thing.def )
                                )
                                {
                                    // Merge this with the next belts stack

                                    var count = belt.ItemContainer.GetWantedCount();
                                    if( thingStatus.Thing.stackCount > count )
                                    {
                                        // Split the off the amount needed
                                        var newStack = thingStatus.Thing.SplitOff( count );
                                        if( newStack != null )
                                        {
                                            AddItem( newStack, thingStatus.Counter, MovementStatus.MergingOut, mergeStatus.Thing );
                                            mergeStatus.Merge = newStack;
                                        }
                                    }
                                    else
                                    {
                                        // Merge whole stack
                                        mergeStatus.Merge = thingStatus.Thing;
                                        thingStatus.Merge = mergeStatus.Thing;
                                        thingStatus.Status = MovementStatus.MergingOut;
                                    }
                                    break;
                                }
                            }
                        }
                        else if(
                            ( belt == null )&&
                            ( _parentComponent.CanOutputToNonBelt( destination, thingStatus.Thing ) )&&
                            ( destination.NoStorageBlockersIn( thingStatus.Thing ) )
                        )
                        {
                            // Dumping on floor which is now clear
                            thingStatus.Status = MovementStatus.Moving;
                        }
                    }
                }
                else if( thingStatus.Status == MovementStatus.WaitMerge )
                {
                    // Waiting for something to come in
                    if( thingStatus.Merge == null )
                    {
                        // No source for merge???
                        thingStatus.Status = MovementStatus.WaitClear;
                    }
                }
            }
        }

        public void AtmosphereEffectsTick()
        {
            var SteadyAtmosphereEffects = typeof(SteadyAtmosphereEffects);
            foreach (var thing in Contents.ToList()) {
                SteadyAtmosphereEffects.Call("TryDoDeteriorate", thing, _parentComponent.parent.Position, false);
            }
        }

        #region Tickers for items on the belt

        public void Tick()
        {
            for( int index = _thingStatus.Count - 1; index >= 0; --index )
            {
                if( _thingStatus[ index ].Thing.def.tickerType == TickerType.Normal )
                    _thingStatus[ index ].Thing.Tick();
            }
        }

        // We still want items to rot while sitting on the belts, but ThingContainer doesn't call
        // TickRare on its contents, which is where the rotting mechanic takes place.
        public void TickRare()
        {
            for( int index = _thingStatus.Count - 1; index >= 0; --index )
            {
                var thingStatus = _thingStatus[ index ];
                if( thingStatus.Thing.def.tickerType == TickerType.Rare )
                {
                    thingStatus.Thing.TickRare();
                    if( thingStatus.Thing.Destroyed )
                    {
                        // Tick action destroyed item, remove it from the belt
                        if( thingStatus.Merge != null )
                        {
                            ReleaseMergeTarget( thingStatus.Merge );
                        }
                        _thingStatus.RemoveAt( index );
                    }
                }
            }
        }

        #endregion

        public ThingStatus GetStatusForThing( [NotNull] Thing thing )
        {
            foreach( var thingStatus in _thingStatus )
            {
                if( thingStatus.Thing == thing )
                    return thingStatus;
            }
            return null;
        }

        private ThingStatus GetWaitMerge()
        {
            foreach( var thingStatus in _thingStatus )
            {
                if( thingStatus.Status == MovementStatus.WaitMerge )
                {
                    return thingStatus;
                }
            }
            return null;
        }

        private int GetWantedCount()
        {
            if( _thingStatus.Count < 1 )
            {
                return 0;
            }
            var thingStatus = GetWaitMerge();
            if( thingStatus != null )
            {
                return thingStatus.Thing.def.stackLimit - thingStatus.Thing.stackCount;
            }
            return 0;
        }

        private ThingDef GetWantedDef()
        {
            if( _thingStatus.Count < 1 )
            {
                return null;
            }
            var thingStatus = GetWaitMerge();
            if( thingStatus != null )
            {
                return thingStatus.Thing.def;
            }
            return null;
        }

        private bool ShouldIncreaseCounter([NotNull] ThingStatus thingStatus)
        {
            if( thingStatus.IsWaiting )
            {
                return false;
            }

            if( thingStatus.Counter < TicksToMove / 2 )//&& !_parentComponent.IsReceiver())
            {
                // Always increase the counter until half the belt speed is reached
                return true;
            }

            // Never go above 100%
            if( thingStatus.Counter >= TicksToMove )
            {
                return false;
            }

            if( thingStatus.Status == MovementStatus.MergingIn )
            {
                // Keep moving item until it merges with it's target
                return true;
            }

            // Get destination for thing
            var destination = _parentComponent.GetDestinationForThing( thingStatus.Thing );
            var belt = destination.GetBeltComponent( _parentComponent );

            // If no belt items, then move things only if this can output to non-belts
            if (belt == null)
            {
                return (_parentComponent.CanOutputToNonBelt( destination, thingStatus.Thing ) && destination.NoStorageBlockersIn( thingStatus.Thing ));
            }

            // There is a belt, only move things if it can accept them from us
            if( !belt.CanAcceptFrom( _parentComponent, true ) )
                return false;

            // An empty belt accepts all
            if( belt.Empty )
                return true;
        
            // Only move the stack if the next belt wants it
            ThingDef wantedDef = belt.ItemContainer.GetWantedDef();
            return wantedDef == thingStatus.Thing.def;
        }

        public bool AddItem([NotNull] Thing t, int initialCounter = 0, MovementStatus initialStatus = MovementStatus.Moving, Thing mergeTarget = null )
        {
            var thingStatus = new ThingStatus( t, initialCounter, initialStatus );
            if( thingStatus == null )
                return false;

            thingStatus.Merge = mergeTarget;
            _thingStatus.Add( thingStatus );

            SlotGroupUtility.Notify_TakingThing( t );
            if( t.Spawned )
                t.DeSpawn();
            if( t.HasAttachment(ThingDefOf.Fire ) )
                t.GetAttachment(ThingDefOf.Fire ).Destroy( DestroyMode.Vanish );

            t.holder = _container;

            /*
            if (!_container.TryAdd(t))
            {
                return false;
            }

            _thingCount[t] = initialCounter;
            */
            return true;
        }

        public void TransferItem([NotNull] Thing item, [NotNull] BeltItemContainer other)
        {
            if (!_parentComponent.PreItemTransfer(item, other._parentComponent))
                return;

            var thingStatus = GetStatusForThing( item );
            _thingStatus.Remove( thingStatus );

            if( thingStatus.Status == MovementStatus.MergingOut )
            {
                thingStatus.Status = MovementStatus.MergingIn;
            }
            other.AddItem( item, 0, thingStatus.Status, thingStatus.Merge );

            _parentComponent.OnItemTransfer(item, other._parentComponent);
            other._parentComponent.OnItemReceived(item, _parentComponent);
        }

        private bool TryDrop( ThingStatus thingStatus, IntVec3 dropLoc, ThingPlaceMode mode, out Thing lastResultingThing )
        {
            lastResultingThing = null;
            if(
                ( thingStatus == null )||
                ( thingStatus.Thing == null )
            )
            {
                return false;
            }
            if( thingStatus.Merge != null )
            {
                // Make sure any items which are merging release their lock on their target
                ReleaseMergeTarget( thingStatus.Merge );
            }
            return GenDrop.TryDropSpawn( thingStatus.Thing, dropLoc, mode, out lastResultingThing );
        }

        private void ReleaseMergeTarget( Thing target )
        {
            var belt = target.PositionHeld.GetBeltComponent( this._parentComponent );
            if( belt == null )
            {
                Log.Error( string.Format( "Can not find belt for merge target {0}!", target ) );
            }
            else
            {
                var mergeStatus = belt.ItemContainer.GetStatusForThing( target );
                if( mergeStatus == null )
                {
                    Log.Error( string.Format( "Can not get ThingStatus for merge target {0} from belt {1}!", target, belt.parent ) );
                }
                else
                {
                    mergeStatus.Merge = null;
                    mergeStatus.Status = MovementStatus.WaitClear;
                }
            }
        }

        public void DropItem([NotNull] Thing item, IntVec3 position, bool forced = false)
        {
            var backupSound = item.def.soundDrop;

            try
            {
                item.def.soundDrop = null;

                var thingStatus = GetStatusForThing( item );
                if( thingStatus == null )
                {
                    Log.Message( string.Format( "Unable to find ThingStatus for {0} in {1}!", item, _parentComponent.parent ) );
                    return;
                }

                bool droppedAll = false;
                Thing droppedItem;
                if( !forced )
                {
                    droppedAll = TryDrop( thingStatus, position, ThingPlaceMode.Direct, out droppedItem );
                }
                // Might prevent those "has null owner" errors...
                else
                {
                    droppedAll = TryDrop( thingStatus, position, ThingPlaceMode.Near, out droppedItem );
                    if( !droppedAll )
                    {
                        Log.Message( string.Format( "Unable to drop {0} near {1}!", thingStatus.Thing, position ) );
                    }
                }
                if( droppedAll )
                {
                    _thingStatus.Remove( thingStatus );
                }

                if( droppedItem == null )
                {
                    return;
                }

                droppedItem.holder = null;

                // Play the drop sound
                if( backupSound != null )
                {
                    backupSound.PlayOneShot( position );
                    droppedItem.def.soundDrop = backupSound;
                }

                if( droppedItem is ThingWithComps )
                {
                    droppedItem.SetForbidden( false );
                }

                if(
                    ( droppedItem.def.defName.Contains( "Chunk" ) )&&
                    ( Find.DesignationManager.DesignationOn( droppedItem, DesignationDefOf.Haul ) == null )
                )
                {
                    // If this is a chunk AND not already haulable ("designated twice" warning) make it haulable
                    Find.DesignationManager.AddDesignation( new Designation( droppedItem, DesignationDefOf.Haul ) );
                }
            }
            finally
            {
                // Stupid hack to make sure the drop sound is not played all the time
                item.def.soundDrop = backupSound;
            }
        }

        public void DropAll(IntVec3 position, bool forced = false)
        {
            // Check if there is anything on the belt: yes? -> make it accessible to colonists
            for( int index = _thingStatus.Count - 1; index >= 0; index-- )
            {
                var thingStatus = _thingStatus[ index ];
                DropItem( thingStatus.Thing, position, forced );
            }
            if( _thingStatus.Count > 0 )
                Log.Error( "A2B: Tried DropAll but items remain!" );
            /*
            foreach (var thing in _container.ToList())
            {
                DropItem(thing, position, forced);
            }

            _thingCount.Clear();
            */
        }

        public void Destroy()
        {
            DropAll(_parentComponent.parent.Position, true);
            //_container.ClearAndDestroyContents();
        }
    }
}
