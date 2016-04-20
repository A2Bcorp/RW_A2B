using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace A2B
{
    
	public class A2BDataDef : Def
	{
		// Core mod versioning
		public string           Version;

		// Occasional ticks
		public int				OccasionalTicks;

		// Belt Speed data
		public int				BeltSpeedBase;
		public int				BeltSpeedOffset;
		public List< string >	BeltSpeedResearch;

		// Climatization data
		public float			ClimatizationMinTemperatureBase;
		public float			ClimatizationMinTemperatureOffset;
		public List< string >	ClimatizationResearch;

		// Durability data
		public float			DurabilityBase;
		public float			DurabilityOffset;
		public List< string >	DurabilityResearch;

		// Reliability data
		public float			ReliabilityFlatRateThresholdBase;
		public float			ReliabilityFlatRateThresholdOffset;
		public float			ReliabilityStartThresholdBase;
		public float			ReliabilityStartThresholdOffset;
		public List< string >	ReliabilityResearch;

        // Power data
        public float            LowPowerFactor = 0.1f;
        public float            PowerPerUndercover = 10f;

	}

}
