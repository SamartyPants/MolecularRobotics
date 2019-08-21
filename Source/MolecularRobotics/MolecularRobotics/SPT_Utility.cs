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

        public static List<IntVec3> FindElectricPath(Thing from, Thing to)
        {
            //use a structure to record path parameters
            //look for any transmitter nearby 
            //nearby transmitters are considered a possible path
            //add nearby path to list of paths
            //multiple nearby transmitters create a new path structure that gets enumerated
            //terminate a paths if no nearby transmitter is found

            Map map = from.Map;
            List<IntVec3> allCells = new List<IntVec3>();
            List<IntVec3> startList = new List<IntVec3>();
            List<IntVec3> bestPath = new List<IntVec3>();
            List<EPath> pathFinder = new List<EPath>();

            allCells.Clear();
            pathFinder.Clear();

            startList.Add(from.Position);                 
            pathFinder.Add(new EPath(0, 0, false, from.Position, startList));

            bool pathFound = false;
            int bestPathIndex = 0;

            for (int i = 0; i < 200; i++) //fail after 200 path attempts
            {
                for (int j = 0; j < pathFinder.Count; j++)
                {
                    if (!pathFinder[j].ended)
                    {
                        List<IntVec3> cellList = GenRadial.RadialCellsAround(pathFinder[j].currentCell, 1f, true).ToList();
                        List<IntVec3> validCells = new List<IntVec3>();
                        validCells.Clear();
                        for (int k = 0; k < cellList.Count; k++)
                        {
                            if (!allCells.Contains(cellList[k]) && CellHasConduit(cellList[k], map))
                            {
                                allCells.Add(cellList[k]);
                                validCells.Add(cellList[k]);
                            }
                        }
                        if (validCells.Count > 0)
                        {
                            for (int k = 0; k < validCells.Count; k++)
                            {
                                if (k == 0)
                                {
                                    //continue path in a single direction; additional possible paths create a branch
                                    pathFinder[j].pathList.Add(validCells[k]);
                                    pathFinder[j] = new EPath(pathFinder[j].pathParent, pathFinder[j].pathParentSplitIndex, false, validCells[k], pathFinder[j].pathList);
                                    if ((to.Position - validCells[k]).LengthHorizontal <= 4)
                                    {
                                        pathFound = true;
                                        bestPathIndex = j;
                                    }
                                }
                                else
                                {
                                    //create new paths
                                    List<IntVec3> newList = new List<IntVec3>();
                                    newList.Clear();
                                    newList.Add(validCells[k]);
                                    pathFinder.Add(new EPath(j, pathFinder[j].pathList.Count, false, validCells[k], newList));
                                    if ((to.Position - validCells[k]).LengthHorizontal <= 4)
                                    {
                                        pathFound = true;
                                        bestPathIndex = j;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //end path
                            pathFinder[j] = new EPath(pathFinder[j].pathParent, pathFinder[j].pathParentSplitIndex, true, pathFinder[j].currentCell, pathFinder[j].pathList);
                        }
                    }
                }

                if (pathFound)
                {
                    //Evaluate best path, reverse, and return
                    bestPath = GetBestPath(pathFinder, bestPathIndex);
                    bestPath.Reverse();
                    break;
                }
            }

            return bestPath;
        }

        public static List<IntVec3> GetBestPath(List<EPath> pathFinder, int index)
        {
            //Evaluates path structure from ending cell to start cell
            //First evaluated path always uses full list
            //Following paths can be branched; eliminate excess cells from those lists 
            //by recording when the valid path branches
            List<IntVec3> tracebackList = new List<IntVec3>();
            tracebackList.Clear();
            bool tracebackComplete = false;
            int parentIndexCount = pathFinder[index].pathList.Count;

            while (!tracebackComplete)
            {
                //ignore index 0 (starting point)
                if (index != 0)
                {
                    for (int i = 0; i < parentIndexCount; i++)
                    {
                        //construct the reverse path
                        tracebackList.Add(pathFinder[index].pathList[i]);
                    }
                }

                if (index == 0)
                {
                    //finished return path
                    tracebackComplete = true;
                }
                else
                {
                    //construct valid reverse path from point path branches from parent
                    parentIndexCount = pathFinder[index].pathParentSplitIndex;
                    index = pathFinder[index].pathParent;
                }
            }
            return tracebackList;
        }

        public static bool CellHasConduit(IntVec3 cell, Map map)
        {
            //Determines if a cell has an active transmitter
            bool hasConduit = false;
            if (cell != default(IntVec3) && cell.InBounds(map))
            {
                Building transmitter = cell.GetTransmitter(map);
                if (transmitter != null && transmitter.TransmitsPowerNow)
                {
                    hasConduit = true;
                }
            }
            return hasConduit;
        }

    }
}
