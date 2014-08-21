#region Usings

using System.Text;
using RimWorld;
using Verse;

#endregion

namespace A2B
{
    public class Building_Selector : Building_Storage, IBeltBuilding
    {
        // ===================== Variables =====================

        // Work variable

        private Phase _beltPhase;

        private int _counter;

        private bool _destroyedFlag;

        public Building_Selector()
        {
            BeltPhase = Phase.Offline;
            Counter = 0;

            // Speed of the belt - 2x faster than standard belt for efficient sorting !
            BeltSpeed = Constants.DefaultBeltSpeed / 2;
        }

        // Destroyed flag. Most of the time not really needed, but sometimes...


        // ===================== Setup Work =====================

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
            // Test the 'selection' idea ...
            var selectionSettings = GetStoreSettings();
            if (selectionSettings.AllowedToAccept(thing))
            {
                return Position + rotation.FacingSquare;
            }

            return Position + new IntVec3(rotation.FacingSquare.z, rotation.FacingSquare.y, -rotation.FacingSquare.x);
        }

        public bool ShouldMoveItems
        {
            get { return true; }
        }

        #endregion

        /// <summary>
        ///     Do something after the object is spawned
        /// </summary>
        public override void SpawnSetup()
        {
            // Do the work of the base class (Building)
            base.SpawnSetup();

            // Get refferences to the components CompPowerTrader and CompGlower
            PowerComponent = GetComp<CompPowerTrader>();
            GlowerComponent = GetComp<CompGlower>();

            // Try to disguishe it as a hopper really well ...
            settings.priority = StoragePriority.Unstored;
        }

        /// <summary>
        ///     To save and load actual values (savegame-data)
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            // Save and load the work variables, so they don't default after loading
            Scribe_Values.LookValue(ref _beltPhase, "phase");
            Scribe_Values.LookValue(ref _counter, "counter");
        }

        // ===================== Destroy =====================

        /// <summary>
        ///     Clean up when this is destroyed
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            // block further ticker work
            _destroyedFlag = true;

            base.Destroy(mode);
        }

        // ===================== Ticker =====================

        /// <summary>
        ///     This is used, when the Ticker is set to Rare
        ///     This is a tick thats done once every 250 normal Ticks
        /// </summary>
        public override void Tick()
        {
            // Don't forget the base work
            base.Tick();

            if (_destroyedFlag) // Do nothing further, when destroyed (just a safety)
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
