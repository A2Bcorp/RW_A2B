using System;
using System.Collections.Generic;

using RimWorld;
using Verse;

namespace A2B
{

    public struct A2B_Climatization
    {
		public bool		isResearched;
        public float	FreezeTemperature;
    }

    public struct A2B_Durability
    {
		public bool		isResearched;
        public float	DeteriorateChance;
    }

    public struct A2B_Reliability
    {
		public bool		isResearched;
        public float	FlatRateThreshold;
        public float	StartThreshold;
    }

	public struct A2B_BeltSpeed
	{
		public bool		isResearched;
		public int		TicksToMove;
	}

    public static class A2BData
    {

        private static A2BDataDef           _def;

        public static Version               Version;

		public static int					OccasionalTicks;

        public static A2B_BeltSpeed         BeltSpeed;
        public static A2B_Durability        Durability;
        public static A2B_Climatization     Climatization;
        public static A2B_Reliability       Reliability;

        // Power for underground powered belts
        public static float                 powerPerUndercover = 1000f;

		public static A2BDataDef def
		{
			get {
				if( _def == null )
					_def = DefDatabase<A2BDataDef>.GetNamed("A2BCore");
				if( _def == null )
					_def = DefDatabase<A2BDataDef>.GetRandom();
				return _def;
			}
		}

        static A2BData()
        {
            if (def == null) {
				Log.ErrorOnce( "A2B - Unable to load core data!", 0 );
				return;
            }
			Version = new Version(def.Version);

			OccasionalTicks = def.OccasionalTicks;

			BeltSpeed.isResearched = false;

			Climatization.isResearched = false;
            Climatization.FreezeTemperature = def.ClimatizationMinTemperatureBase;

			Durability.isResearched = false;
			Durability.DeteriorateChance = def.DurabilityBase;

			Reliability.isResearched = false;
			Reliability.StartThreshold = def.ReliabilityStartThresholdBase;
            Reliability.FlatRateThreshold = def.ReliabilityFlatRateThresholdBase;

            A2BMonitor.RegisterTickAction( "A2BResearch.UndercoverPowerInit", A2BResearch.UndercoverPowerInit );
            A2BMonitor.RegisterTickAction( "A2BResearch.BeltSpeedInit", A2BResearch.BeltSpeedInit );
		    
            A2BMonitor.RegisterOccasionalAction( "A2BResearch.BeltSpeed", A2BResearch.BeltSpeed );
			A2BMonitor.RegisterOccasionalAction( "A2BResearch.Climatization", A2BResearch.Climatization );
			A2BMonitor.RegisterOccasionalAction( "A2BResearch.Durability", A2BResearch.Durability );
			A2BMonitor.RegisterOccasionalAction( "A2BResearch.Reliability", A2BResearch.Reliability );

			Log.Message( "A2B Initialized" );

        }

		public static bool IsReady
		{
			get { return def != null; }
		}

        public static bool IsVersionSupported(Version version)
        {
            return (Version.Major == version.Major
                 && Version.Minor == version.Minor
                 && Version.Build >= version.Build);
        }

        public static void CheckVersion(Version version)
        {
			if (!IsVersionSupported(version)){
				var msg = String.Format("A2B Version not supported: required {0} but {1} is loaded", version, A2BData.Version);
                throw new NotSupportedException(msg);
			}
        }

    }

    public static class A2BResearch
    {

        public static bool UndercoverPowerInit()
        {
            var baseBelt = DefDatabase<ThingDef>.GetNamed( "A2BBelt" );
            if( baseBelt != null ){
                var beltComps = baseBelt.CompDefFor<CompPowerTrader>();
                if( beltComps != null )
                    A2BData.powerPerUndercover = beltComps.basePowerConsumption;
            }
            return true;
        }

        public static bool BeltSpeedInit()
        {
            A2BData.BeltSpeed.TicksToMove = A2BData.def.BeltSpeedBase;
            AnimatedGraphic.animationRate = ( (float)A2BData.BeltSpeed.TicksToMove / 90.0f);
            return true;
        }

        public static bool BeltSpeed()
        {
			if (A2BResearch.ResearchGroupComplete(A2BData.def.BeltSpeedResearch)) {
				A2BData.BeltSpeed.TicksToMove += A2BData.def.BeltSpeedOffset;
				AnimatedGraphic.animationRate = ( (float)A2BData.BeltSpeed.TicksToMove / 90.0f);
				A2BData.BeltSpeed.isResearched = true;
				return true;
			}
			return false;
		}

		public static bool Climatization()
		{
			if (A2BResearch.ResearchGroupComplete(A2BData.def.ClimatizationResearch)) {
				A2BData.Climatization.FreezeTemperature += A2BData.def.ClimatizationMinTemperatureOffset;
				A2BData.Climatization.isResearched = true;
				return true;
			}
			return false;
		}

		public static bool Durability()
		{
            if (A2BResearch.ResearchGroupComplete(A2BData.def.DurabilityResearch)) {
                A2BData.Durability.DeteriorateChance += A2BData.def.DurabilityOffset;
				A2BData.Durability.isResearched = true;
				return true;
            }
			return false;
		}

		public static bool Reliability()
		{
            if (A2BResearch.ResearchGroupComplete(A2BData.def.ReliabilityResearch)) {
                A2BData.Reliability.FlatRateThreshold += A2BData.def.ReliabilityFlatRateThresholdOffset;
                A2BData.Reliability.StartThreshold += A2BData.def.ReliabilityStartThresholdOffset;
				A2BData.Reliability.isResearched = true;
				return true;
            }
			return false;
        }

        public static bool ResearchGroupComplete(List<string> research)
        {
            foreach (var r in research) {
                if (!ResearchProjectDef.Named(r).IsFinished)
                    return false;
            }

            return true;
        }

    }
}