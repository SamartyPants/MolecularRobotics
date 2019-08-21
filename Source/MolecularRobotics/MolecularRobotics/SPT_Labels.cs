using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace NaniteFactory
{
    public static class SPT_Labels
    {

        //Labels for the buttons
        public static readonly String deconstruction_label = "SPT_deconstructionNanitesEnabled".Translate();
        public static readonly String deconstruction_desc = "SPT_deconstructionNanitesEnabledDesc".Translate();

        public static readonly String construction_label = "SPT_constructionNanitesEnabled".Translate();
        public static readonly String construction_desc = "SPT_constructionNanitesEnabledDesc".Translate();

        public static readonly String repair_label = "SPT_repairNanitesEnabled".Translate();
        public static readonly String repair_desc = "SPT_repairNanitesEnabledDesc".Translate();

        public static readonly String heal_label = "SPT_healNanitesEnabled".Translate();
        public static readonly String heal_desc = "SPT_HealNanitesEnabledDesc".Translate();

        public static readonly String stockpile_desc = "SPT_MakeStockpileDesc".Translate();
        public static readonly String stockpile_label = "SPT_MakeStockpileLabel".Translate();
    }
}
