using System;
using RimWorld;
using Verse;

namespace NaniteFactory
{
    [DefOf]
    public class SPT_DefOf
    {       
        /* Repository of Defs for precaching and efficient access */

        public static ThingDef SPT_NaniteFactory;
        public static ThingDef SPT_FlyingObject;

        //Motes
        public static ThingDef SPT_Mote_NaniteWorking;
        public static ThingDef SPT_Mote_NaniteRepairing;
        public static ThingDef SPT_Mote_NaniteHealing;
        public static ThingDef SPT_Mote_NaniteConstructing;
        public static ThingDef SPT_Mote_NanitesAir;

        //Research Defs
        public static ResearchProjectDef SPT_MolecularRobotics;
        public static ResearchProjectDef SPT_NaniteReconstitutionProtocols;
        public static ResearchProjectDef SPT_NaniteConstructionProtocols;
        public static ResearchProjectDef SPT_NaniteMendingProtocols;
        public static ResearchProjectDef SPT_NaniteWirelessAdaptation;

        //Sounds
        public static SoundDef Mortar_LaunchA; //Default RimWorld sound

    }
}
