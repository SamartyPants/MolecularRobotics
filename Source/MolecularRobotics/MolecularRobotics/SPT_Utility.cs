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
    public static class SPT_Utility
    {
        /* Reusable methods */

        public static void ThrowGenericMote(ThingDef moteDef, Vector3 loc, Map map, float scale, float solidTime, float fadeIn, float fadeOut, int rotationRate, float velocity, float velocityAngle, float lookAngle)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(moteDef, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = rotationRate;
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity(velocityAngle, velocity);
            moteThrown.exactRotation = lookAngle;
            moteThrown.def.mote.solidTime = solidTime;
            moteThrown.def.mote.fadeInTime = fadeIn;
            moteThrown.def.mote.fadeOutTime = fadeOut;
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        //Method responsible for finding buildings that are repairable in home area
        public static List<Thing> FindRepairBuildings(Map map, Faction faction)
        {
            List<Thing> startingList = map.listerBuildingsRepairable.RepairableBuildings(faction);
            List<Thing> tmpThing = new List<Thing>();
            tmpThing.Clear();
            for(int i = 0; i < startingList.Count; i++)
            {
                if(map.areaManager.Home != null && map.areaManager.Home.ActiveCells != null && map.areaManager.Home.ActiveCells.ToList().Contains(startingList[i].Position))
                {
                    tmpThing.Add(startingList[i]);
                }
                
            }
            return tmpThing;
        }
        //Method responsible for finding deconstruction designations within home area
        public static List<Thing> FindDeconstructionBuildings(Map map, Faction faction)
        {

            List<Designation> startingList = map.designationManager.SpawnedDesignationsOfDef(DesignationDefOf.Deconstruct).ToList();   
            List<Thing> tmpThing = new List<Thing>();
            tmpThing.Clear();
            for (int i = 0; i < startingList.Count; i++)
            {   
                if (map.areaManager.Home != null && map.areaManager.Home.ActiveCells != null && map.areaManager.Home.ActiveCells.ToList().Contains(startingList[i].target.Thing.Position) && startingList[i].target.Thing is Thing)
                {
                    tmpThing.Add(startingList[i].target.Thing as Thing);
                }

            }
            return tmpThing;
        }
        //Method responsible for finding construction frames within home area
        public static List<Frame> FindConstructionBuildings(Map map, Faction faction)
        {

            List<Building> startingList = map.listerBuildings.allBuildingsColonist;
            List<Frame> tmpFrame = new List<Frame>();
            tmpFrame.Clear();    
            for (int i = 0; i < startingList.Count; i++)
            {         
                if (map.areaManager.Home != null && map.areaManager.Home.ActiveCells != null && map.areaManager.Home.ActiveCells.ToList().Contains(startingList[i].Position) && startingList[i] is Frame)
                {
                    tmpFrame.Add(startingList[i] as Frame);
                }

            }
            return tmpFrame;
        }
        public static List<Pawn> FindHurtPawns(Map map, Faction faction)
        {
            List<Pawn> startingList = map.mapPawns.PawnsInFaction(Faction.OfPlayer).ToList();
            List<Pawn> tmpPawn = new List<Pawn>();
            tmpPawn.Clear();
            for (int i = 0; i < startingList.Count; i++)
            {

                if (IsPawnInjured(startingList[i], 0))
                {
                    tmpPawn.Add(startingList[i]);
                }

            }
            return tmpPawn;
        }

        public static bool IsPawnInjured(Pawn targetPawn, float minInjurySeverity = 0)
        {
            float injurySeverity = 0;
            using (IEnumerator<BodyPartRecord> enumerator = targetPawn.health.hediffSet.GetInjuredParts().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    BodyPartRecord rec = enumerator.Current;
                    IEnumerable<Hediff_Injury> arg_BB_0 = targetPawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                    Func<Hediff_Injury, bool> arg_BB_1;
                    arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                    foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                    {
                        bool flag5 = current.CanHealNaturally() && !current.IsPermanent() && current.BleedRate > 0;
                        if (flag5)
                        {
                            injurySeverity += current.Severity;
                        }
                    }
                }
            }
            Log.Message(injurySeverity.ToString());
         

            return injurySeverity > minInjurySeverity;
        }

    }
}
