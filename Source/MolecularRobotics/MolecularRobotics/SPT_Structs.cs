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
    //Path Struct
    public struct EPath
    {
        public List<IntVec3> pathList;
        public int pathParent;
        public int pathParentSplitIndex;
        public bool ended;
        public IntVec3 currentCell;

        public EPath(int parent, int splitIndex, bool end, IntVec3 curCell, List<IntVec3> list)
        {
            pathParent = parent;
            ended = false;
            pathList = list;
            currentCell = curCell;
            pathParentSplitIndex = splitIndex;
        }
    }
}
