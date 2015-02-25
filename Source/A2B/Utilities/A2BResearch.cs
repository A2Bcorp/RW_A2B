using System;
using Verse;
using RimWorld;

namespace A2B
{
    public static class A2BResearch
    {

        public const string 
            Climatization   = "A2B_Climatization",
            TeleporterHeat  = "A2B_TeleporterHeat",
            Durability      = "A2B_Durability",
            Reliability     = "A2B_Reliability";

        public static bool IsResearched(this string name)
        {
            return Find.ResearchManager.IsFinished(ResearchProjectDef.Named(name));
        }

    }
}
