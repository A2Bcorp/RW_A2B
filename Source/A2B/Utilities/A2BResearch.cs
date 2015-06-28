using System;
using System.Collections.Generic;

using Verse;

namespace A2B
{

    public struct A2B_Climatization
    {
        public bool isResearched;
        public float FreezeTemperature;
    }

    public struct A2B_Durability
    {
        public bool isResearched;
        public float DeteriorateChance;
    }

    public struct A2B_Reliability
    {
        public bool isResearched;
        public float FlatRateThreshold;
        public float StartThreshold;
    }

    public struct A2B_BeltSpeed
    {
        public bool isResearched;
        public int TicksToMove;
    }

    public static class A2BData
    {

        public static A2BDataDef            def;

        public static Version               Version;

        public static A2B_BeltSpeed         BeltSpeed;
        public static A2B_Durability        Durability;
        public static A2B_Climatization     Climatization;
        public static A2B_Reliability       Reliability;

        static A2BData()
        {
            def = DefDatabase<A2BDataDef>.GetNamed("A2BCore");
            if (def == null) {
                Log.ErrorOnce("A2B - Unable to load core data!", 0);
                return;
            }

            BeltSpeed.isResearched = false;
            BeltSpeed.TicksToMove = def.BeltSpeedBase;

            Climatization.isResearched = false;
            Climatization.FreezeTemperature = def.ClimatizationMinTemperatureBase;

            Durability.isResearched = false;
            Durability.DeteriorateChance = def.DurabilityBase;

            Reliability.isResearched = false;
            Reliability.StartThreshold = def.ReliabilityStartThresholdBase;
            Reliability.FlatRateThreshold = def.ReliabilityFlatRateThresholdBase;

            Version = new Version(def.Version);
        }

        public static bool IsReady
        {
            get { return def != null; }
        }

        public static bool AllResearchDone
        {
            get
            {
                return (BeltSpeed.isResearched &&
                        Climatization.isResearched &&
                        Durability.isResearched &&
                        Reliability.isResearched);
            }
        }

        public static bool IsVersionSupported(Version version)
        {
            return (Version.Major == version.Major
                 && Version.Minor == version.Minor
                 && Version.Build >= version.Build);
        }

        public static void CheckVersion(Version version)
        {
            var msg = String.Format("A2B Version not supported: required {0} but {1} is loaded", version, A2BData.Version);
            if (!IsVersionSupported(version))
                throw new NotSupportedException(msg);
        }

    }

    public static class A2BResearch
    {
        static A2BResearch()
        {
            A2BMonitor.RegisterTickAction("A2BResearch.PerformResearchChecks", PerformResearchChecks);
        }

        public static void PerformResearchChecks()
        {
            if (!A2BData.IsReady)
                return;

            if (A2BData.AllResearchDone)
                return;

            if ((A2BData.BeltSpeed.isResearched == false) &&
                (A2BResearch.ResearchGroupComplete(A2BData.def.BeltSpeedResearch))) {
                A2BData.BeltSpeed.TicksToMove += A2BData.def.BeltSpeedOffset;
                A2BData.BeltSpeed.isResearched = true;
            }

            if ((A2BData.Climatization.isResearched == false) &&
                (A2BResearch.ResearchGroupComplete(A2BData.def.ClimatizationResearch))) {
                A2BData.Climatization.FreezeTemperature += A2BData.def.ClimatizationMinTemperatureOffset;
                A2BData.Climatization.isResearched = true;
            }

            if ((A2BData.Durability.isResearched == false) &&
                (A2BResearch.ResearchGroupComplete(A2BData.def.DurabilityResearch))) {
                A2BData.Durability.DeteriorateChance += A2BData.def.DurabilityOffset;
                A2BData.Durability.isResearched = true;
            }

            if ((A2BData.Reliability.isResearched == false) &&
                (A2BResearch.ResearchGroupComplete(A2BData.def.ReliabilityResearch))) {
                A2BData.Reliability.FlatRateThreshold += A2BData.def.ReliabilityFlatRateThresholdOffset;
                A2BData.Reliability.StartThreshold += A2BData.def.ReliabilityStartThresholdOffset;
                A2BData.Reliability.isResearched = true;
            }
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
