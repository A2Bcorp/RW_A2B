using UnityEngine;
using Verse;

namespace A2B
{

    [StaticConstructorOnStartup]
    public static class Constants
    {
        
        #region Text Keys
		
        public const string msgPowerDisconnect = "PowerDisconnect";
		public const string msgPowerConnect = "PowerConnect";

        public const string TxtActive = "A2B_Active";
        public const string TxtOffline = "A2B_Offline";
        public const string TxtFrozen = "A2B_Frozen";
        public const string TxtJammed = "A2B_Jammed";

		public const string TxtStatus = "A2B_Status";

        public const string TxtFrozenMsg = "A2B_Frozen_Message";
        public const string TxtJammedMsg = "A2B_Jammed_Message";

		public const string TxtContents = "A2B_Contents";
		public const string TxtForced = "A2B_Forced";

        public const string TxtLiftDrivingComponents = "A2B_LiftDrivingComponents";

        public const string TxtSelectorToGroundToggleDescription = "A2B_Selector_To_Ground_Toggle_Desc";
        public const string TxtSelectorToGroundToggle = "A2B_Selector_To_Ground_Toggle";

		public const string TxtUndertakerMode = "A2B_Undertaker_Mode";
		public const string TxtUndertakerModeToggle = "A2B_Undertaker_Mode_Toggle";
		public const string TxtUndertakerModeUndefined = "A2B_Undertaker_Mode_Undefined";
		public const string TxtUndertakerModeLift = "A2B_Undertaker_Mode_PoweredLift";
		public const string TxtUndertakerModeSlide = "A2B_Undertaker_Mode_UnpoweredSlide";

		public const string TxtUndertakerFlow = "A2B_Undertaker_Flow";
		public const string TxtUndertakerFlowTo = "A2B_Undertaker_FlowTo";

        public const string TxtUndercoverCoverToggle = "A2B_UndercoverCover_Toggle";
        public const string TxtUnderUndercoverCoverToggleDesc = "A2B_UnderUndercoverCover_Toggle_Desc";
        public const string TxtUnderUndercoverCoverToggleDesignateOnly = "A2B_UnderUndercoverCover_Toggle_DesignateOnly";

		public const string TxtDirectionNorth = "A2B_Direction_North";
		public const string TxtDirectionEast = "A2B_Direction_East";
		public const string TxtDirectionSouth = "A2B_Direction_South";
		public const string TxtDirectionWest = "A2B_Direction_West";

        #endregion

        #region Key Defs

        public static ThingDef DefBeltUndercover = DefDatabase<ThingDef>.GetNamed( "A2BUndercover", true );
        public static ThingDef DefBeltLift = DefDatabase<ThingDef>.GetNamed( "A2BLift", true );
        public static ThingDef DefBeltSlide = DefDatabase<ThingDef>.GetNamed( "A2BSlide", true );

        public static DesignationDef DesignationUndercoverCoverToggle = DefDatabase<DesignationDef>.GetNamed( "A2BUndercoverCoverToggleDesignation", true );

        public static JobDef JobUndercoverCoverToggle = DefDatabase<JobDef>.GetNamed( "A2BUndercoverCoverToggleJob", true );

        public static KeyBindingDef KeyUndercoverCoverToggle = DefDatabase<KeyBindingDef>.GetNamed( "A2BUndercoverCoverToggleKeyBinding", true );

        public static SoundDef ButtonClick = DefDatabase<SoundDef>.GetNamed( "Click", true );

        #endregion

        #region Icon Paths

        public static string PathIconSelectorToGroundTrue = "UI/Icons/Commands/UndertakerMode";
        public static string PathIconSelectorToGroundFalse = "UI/Icons/Commands/UndercoverCoverToggle";

        public static string PathIconUndertakerToggle = "UI/Icons/Commands/UndertakerMode";
        public static string PathIconUndercoverCoverToggle = "UI/Icons/Commands/UndercoverCoverToggle";

        #endregion

        #region Icon Textures

        public static Texture2D IconSelectorToGroundTrue;
        public static Texture2D IconSelectorToGroundFalse;
        public static Texture2D IconUndertakerToggle;
        public static Texture2D IconUndercoverCoverToggle;

        #endregion

        //public static Material UndercoverFrame = MaterialPool.MatFrom( "Things/Building/UndergroundFrame" );

        static Constants()
        {
            LongEventHandler.ExecuteWhenFinished( LoadIcons );
        }

        static void LoadIcons()
        {
            IconSelectorToGroundTrue = ContentFinder<Texture2D>.Get( PathIconSelectorToGroundTrue, true );
            IconSelectorToGroundFalse = ContentFinder<Texture2D>.Get( PathIconSelectorToGroundFalse, true );
            IconSelectorToGroundFalse = ContentFinder<Texture2D>.Get( PathIconSelectorToGroundFalse, true );
            IconUndercoverCoverToggle = ContentFinder<Texture2D>.Get( PathIconUndercoverCoverToggle, true );
        }
    }
}
