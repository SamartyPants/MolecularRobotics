﻿using System;
using RimWorld;
using Verse;

namespace NaniteFactory
{
    [DefOf]
    public class SPT_DefOf : Def
    {       
        /* Repository of Defs for precaching and efficient access */

        public static ThingDef TableNaniteFactory;
        public static ThingDef SPT_FlyingObject;

        //Motes
        public static ThingDef SPT_Mote_NaniteWorking;
        public static ThingDef SPT_Mote_NaniteRepairing;
        public static ThingDef SPT_Mote_NaniteHealing;
        public static ThingDef SPT_Mote_NaniteConstructing;
    }
}
