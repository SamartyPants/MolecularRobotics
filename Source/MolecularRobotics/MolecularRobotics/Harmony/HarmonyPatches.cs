using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.AI;
using System.Reflection.Emit;

namespace NaniteFactory.Harmony
{
    [StaticConstructorOnStartup]
    internal class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);

        static HarmonyPatches()
        {
            HarmonyInstance harmonyInstance = HarmonyInstance.Create(id: "rimworld.samarty_pants.molecularrobotics");

            //Postfix
            //harmonyInstance.Patch(AccessTools.Method(typeof(FactionUIUtility), "DrawFactionRow", new Type[]
            //    {
            //        typeof(Faction),
            //        typeof(float),
            //        typeof(Rect)
            //    }, null), null, new HarmonyMethod(typeof(HarmonyPatches), "DrawFactionRow_WithFactionPoints_Postfix", null), null);


            //Prefix
            //harmonyInstance.Patch(AccessTools.Method(typeof(IncidentWorker), "TryExecute", new Type[]
            //    {
            //        typeof(IncidentParms)
            //    }, null), new HarmonyMethod(typeof(HarmonyPatches), "IncidentWorker_Prefix", null), null, null);

            //Transpiler
            
        }
        

        //public static bool IncidentWorker_Prefix(IncidentWorker __instance, IncidentParms parms, ref bool __result)
        //{
        //    if (__instance.def == null)
        //    {
        //        Traverse.Create(root: __instance).Field(name: "def").SetValue(IncidentDefOf.RaidEnemy);
        //        __instance.def = IncidentDefOf.RaidEnemy;
        //    }
        //    return true;
        //}
        

        //[HarmonyPatch(typeof(IncidentWorker_Ambush_EnemyFaction), "CanFireNowSub", null)]
        //public class CanFireNow_Ambush_EnemyFaction_RemovalPatch
        //{
        //    public static bool Prefix(IncidentWorker_Ambush_EnemyFaction __instance, IncidentParms parms, ref bool __result)
        //    {
        //        __result = false;
        //        return false;
        //    }
        //}        
    }
}
