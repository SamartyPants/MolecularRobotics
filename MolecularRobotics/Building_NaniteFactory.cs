using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace NaniteFactory
{
    //test
    [StaticConstructorOnStartup]
    //public static class HelloWorld
    //{
    ////    static HelloWorld() //our constructor
    ////    {
    ////        Log.Message("Hello World!"); //Outputs "Hello World!" to the dev console.
    ////    }
    //}
    public class Building_NaniteFactory : Building_WorkTable
    {
        private Thing targetThing = null;
        private LocalTargetInfo infoTarget = null;

        List<RecipeDef> replicatedRecipes = new List<RecipeDef>();

        //Not sure what this does
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            replicatedRecipes = new List<RecipeDef>();
            replicatedRecipes.Clear();
        }

        //I assume gizmo is an "interactive furniture"
        public override IEnumerable<Gizmo> GetGizmos()
        {
            var gizmoList = base.GetGizmos().ToList();
            //Check to make sure research is complete
            if (ResearchProjectDef.Named("SPT_MolecularRobotics").IsFinished)
            {
                bool canScan = true;
                //Bill stack being... bills created at this table?

                String deconstruction_label = "SPT_deconstructionNanitesEnabled".Translate();
                String deconstruction_desc = "SPT_deconstructionNanitesEnabledDesc".Translate();

                String construction_label = "SPT_constructionNanitesEnabled".Translate();
                String construction_desc = "SPT_constructionNanitesEnabledDesc".Translate();

                String repair_label = "SPT_repairNanitesEnabled".Translate();
                String repair_desc = "SPT_repairNanitesEnabledDesc".Translate();

                String heal_label = "SPT_healNanitesEnabled".Translate();
                String heal_desc = "SPT_HealNanitesEnabledDesc".Translate();

                Command_Action item2 = new Command_Action
                {
                    defaultLabel = deconstruction_label,
                    defaultDesc = deconstruction_desc,
                    order = 68,
                    icon = ContentFinder<Texture2D>.Get("UI/replicateDisabled", true),
                    action = delegate
                    {
                        Log.Message("ACTION 1");
                        sendNanitesDeconstruct();
                    }
                };
            

                Command_Action item3 = new Command_Action
                {
                    defaultLabel = construction_label,
                    defaultDesc = construction_desc,
                    order = 68,
                    icon = ContentFinder<Texture2D>.Get("UI/replicateDisabled", true),
                    action = delegate
                    {
                        Log.Message("ACTION 2");
                        sendNanitesConstruct();
                    }
                };
             

                Command_Action item4 = new Command_Action
                {
                    defaultLabel = repair_label,
                    defaultDesc = repair_desc,
                    order = 68,
                    icon = ContentFinder<Texture2D>.Get("UI/replicateDisabled", true),
                    action = delegate
                    {
                        Log.Message("ACTION 3");
                        sendNanitesRepair();
                    }
                };
            
                Command_Action item5 = new Command_Action
                {
                    defaultLabel = heal_label,
                    defaultDesc = heal_desc,
                    order = 68,
                    icon = ContentFinder<Texture2D>.Get("UI/replicateDisabled", true),
                    action = delegate
                    {
                        Log.Message("ACTION 4");
                        sendNanitesHeal();
                    }
                };
                gizmoList.Add(item2);

            }
            return gizmoList;
        }

        /************************************************************
         * This function is a temporary function that we can use
         * to test the nanite functionality. Pressing the ability
         * on the nanite factory will launch the functionality
         * 
         * Eventually this will be a toggle button that will activate
         * a timer that counts down between operations. Research upgrades 
         * will improve the timer/speed/carry weight of the nanites. 
         * Research will also unlock more abilities such as repair/decon/construct/heal
         */
        private void sendNanitesDeconstruct()
        {

        }
        private void sendNanitesConstruct()
        {

        }
        private void sendNanitesRepair()
        {

        }
        private void sendNanitesHeal()
        {

        }

        //Names and research values/trees subject to change
        // Research dependancies depicted with . (DOT) notation : Research 1.1 is a child of Research 1. Research 1.1.1 is a child of research 1.1 ... etc


        //Research Tab -> Molecular Robotics

        // Research 1 : Nanobot creation  // Unlocks the Nanite Factory // 2000 points to start   
        //Research 1.1 : Nanobot Reconsititution Protocols // Unlocks the nanites ability to repair damaged, claimed structures // 2000
        //Research 1.2 : Nanobot Construction Protocols // Unlocks the nanites ability to construct things // 2000
        //Research 1.3 : Nanobot Mending Protocols // Unlocks the nanites ability to heal colonists // 3000
        //Research 1.2.1 : Nanobot Delivery Protocols // Unlocks the nanites ability to deliver resources to a job site // 3000


        //Also considering research to improve speed/carry/distance etc...


    }

}
