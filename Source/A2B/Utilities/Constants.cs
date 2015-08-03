using UnityEngine;
using Verse;

namespace A2B
{
    public static class Constants
    {
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

        public static ThingDef DefBeltUndercover = DefDatabase<ThingDef>.GetNamed( "A2BUndercover", true );

        public static Texture2D IconUndercoverCoverToggle = ContentFinder<Texture2D>.Get( "UI/Icons/Commands/UndercoverCoverToggle", true);

        public static DesignationDef DesignationUndercoverCoverToggle = DefDatabase<DesignationDef>.GetNamed( "A2BUndercoverCoverToggleDesignation", true );
        //public static DesignatorDef DesignatorUndercoverCoverToggle = DefDatabase<DesignatorDef>.GetNamed( "A2BUndercoverCoverToggleDesignator", true );

        public static JobDef JobUndercoverCoverToggle = DefDatabase<JobDef>.GetNamed( "A2BUndercoverCoverToggleJob", true );

        public static KeyBindingDef KeyUndercoverCoverToggle = DefDatabase<KeyBindingDef>.GetNamed( "A2BUndercoverCoverToggleKeyBinding", true );

    }
}
