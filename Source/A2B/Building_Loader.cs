#region Usings

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
// Always needed
//using VerseBase;   // Material/Graphics handling functions are found here
// RimWorld specific functions are found here (like 'Building_Battery')
// RimWorld universal objects are here (like 'Building')
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.Noise;

#endregion

namespace A2B
{
    /// <summary>
    ///     Define a special class for the Belt Selector - it's a hopper with some tweaks ...
    /// </summary>
    public class Building_Loader : Building, SlotGroupParent, IBeltBuilding
    {
        private Phase _beltPhase;

        private int _counter;

        private List<IntVec3> cachedOccupiedSquares;

        private bool destroyedFlag = false;

        public StorageSettings settings;

        public SlotGroup slotGroup;

        public bool ShouldMoveItems
        {
            get { return true; }
        }

        public Building_Loader()
        {
            BeltPhase = Phase.Offline;
            Counter = 0;
            BeltSpeed = Constants.DefaultBeltSpeed;
        }

        #region IBeltBuilding Members

        public Phase BeltPhase
        {
            get { return _beltPhase; }
            set { _beltPhase = value; }
        }

        public CompGlower GlowerComponent { get; private set; }

        public CompPowerTrader PowerComponent { get; private set; }

        public int Counter
        {
            get { return _counter; }
            set { _counter = value; }
        }

        public int BeltSpeed { get; private set; }

        public IntVec3 ThingOrigin { set; private get; }

        public IntVec3 GetDestinationForThing(Thing thing)
        {
            return Position + rotation.FacingSquare;
        }

        #endregion

        //=======================================================================
        //========================== SlotGrouParent interface=======================
        //=======================================================================

        #region SlotGroupParent Members

        public SlotGroup GetSlotGroup()
        {
            return slotGroup;
        }

        public virtual void Notify_ReceivedThing(Thing newItem)
        {
/*Nothing by default*/
        }

        public virtual void Notify_LostThing(Thing newItem)
        {
/*Nothing by default*/
        }

        public virtual IEnumerable<IntVec3> AllSlotCells()
        {
            return GenAdj.CellsOccupiedBy(this);
        }

        public List<IntVec3> AllSlotCellsListFast()
        {
            return cachedOccupiedSquares;
        }

        public StorageSettings GetStoreSettings()
        {
            return settings;
        }

        public StorageSettings GetParentStoreSettings()
        {
            return def.building.defaultStorageSettings;
        }

        public string SlotYielderLabel()
        {
            return Label;
        }

        #endregion

        //=======================================================================
        //============================== Other stuff ============================
        //=======================================================================

        public override void PostMake()
        {
            base.PostMake();

            settings = new StorageSettings(this);

            if (def.building.defaultStorageSettings != null)
            {
                settings.CopyFrom(def.building.defaultStorageSettings);
            }
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            slotGroup = new SlotGroup(this);

            cachedOccupiedSquares = AllSlotCells().ToList();

            // Get references to the components CompPowerTrader and CompGlower
            PowerComponent = GetComp<CompPowerTrader>();
            GlowerComponent = GetComp<CompGlower>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.LookDeep(ref settings, "settings", this);
            Scribe_Values.LookValue(ref _beltPhase, "phase");
            Scribe_Values.LookValue(ref _counter, "counter");
        }

        public void Destroy()
        {
            slotGroup.Notify_ParentDestroying();
            base.Destroy();

            destroyedFlag = true;
        }

        // ===================== More Variables =====================

        // Work variable

        // ===================== Ticker =====================

        /// <summary>
        ///     This is used, when the Ticker is set to Rare
        ///     This is a tick thats done once every 250 normal Ticks
        /// </summary>
        //public override void TickRare()
        public override void Tick()
        {
            // Don't forget the base work
            base.Tick();

            if (destroyedFlag) // Do nothing further, when destroyed (just a safety)
            {
                return;
            }

            // Call work function
            this.DoBeltTick();
        }

        // ===================== Inspections =====================

        /// <summary>
        ///     This string will be shown when the object is selected (focus)
        /// </summary>
        /// <returns></returns>
        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();

            // Add the inspections string from the base
            stringBuilder.Append(base.GetInspectString());

            stringBuilder.Append(this.GetInspectionString());

            // return the complete string
            return stringBuilder.ToString();
        }
    }
}
