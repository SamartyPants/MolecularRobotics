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

    }
}
