using RimWorld;
using UnityEngine;
using Verse;

namespace NaniteFactory
{
    public class PlaceWorker_ShowResourceRadius : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol)
        {
            Map currentMap = Find.CurrentMap;
            GenDraw.DrawFieldEdges(Building_NaniteFactory.ResourceCellsAround(center, currentMap));
        }
    }
}
