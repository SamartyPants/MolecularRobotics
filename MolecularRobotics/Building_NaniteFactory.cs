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

                String label = "SPT_DeconstructionNanitesEnabled".Translate();
                String desc = "SPT_DeconstructionNanitesEnabledDesc".Translate();

                Command_Action item2 = new Command_Action
                {
                    defaultLabel = label,
                    defaultDesc = desc,
                    order = 68,
                    icon = ContentFinder<Texture2D>.Get("UI/replicateDisabled", true),
                    action = delegate
                    {
                        Log.Message("ACTION!!!");
                        sendNanites();
                    }
                };
                gizmoList.Add(item2);

            }
            return gizmoList;
        }

        private void sendNanites()
        {

        }



    }

}
