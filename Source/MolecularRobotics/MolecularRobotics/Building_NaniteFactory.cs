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
    //
    public class Building_NaniteFactory : Building_WorkTable
    {
        private Thing targetThing = null;
        private LocalTargetInfo infoTarget = null;

        //Unsaved variables
        //How frequently the Nanite Factory checks for related jobs
        //Increasing intervals will reduce processor demand but slow down responsiveness
        //Intervals are offset to limit the amount of processor demand during active ticks
        private int repairInterval = 11;
        private int constructInterval = 125;
        private int deconstructInterval = 29;
        private int healInterval = 13;

        //How effective the nanite factory is at performing jobs
        //Used in utility and quality checks
        private int repairSkill = 1;
        private int constructSkill = 5;
        private int deconstructSkill = 1;
        private int healSkill = 4;

        //Saved Variables - built-in QC during call; use CAP variable whenever possible
        private List<Thing> repairJobs = new List<Thing>();
        public List<Thing> RepairJobs
        {
            get
            {
                if(repairJobs == null)
                {
                    repairJobs = new List<Thing>();
                    repairJobs.Clear();
                }
                if(repairJobs.Count > 0)
                {
                    List<Thing> tmpJobs = new List<Thing>();
                    tmpJobs.Clear();
                    for(int i = 0; i < repairJobs.Count; i++)
                    {
                        if(repairJobs[i].HitPoints < repairJobs[i].MaxHitPoints)
                        {
                            tmpJobs.Add(repairJobs[i]);
                        }
                    }
                    repairJobs = tmpJobs;
                }
                return repairJobs;
            }
            set
            {
                if(repairJobs == null)
                {
                    repairJobs = new List<Thing>();
                    repairJobs.Clear();
                }
                repairJobs = value;
            }
        }

        private List<Thing> constructJobs = new List<Thing>();
        public List<Thing> ConstructJobs
        {
            get
            {
                if (constructJobs == null)
                {
                    constructJobs = new List<Thing>();
                    constructJobs.Clear();
                }
                if (constructJobs.Count > 0)
                {
                    List<Thing> tmpJobs = new List<Thing>();
                    tmpJobs.Clear();
                    for (int i = 0; i < constructJobs.Count; i++)
                    {
                        if (constructJobs[i] != null && !constructJobs[i].IsBurning() && constructJobs[i] is Frame)
                        {
                            tmpJobs.Add(constructJobs[i]);
                        }
                    }
                    constructJobs = tmpJobs;
                }
                return constructJobs;
            }
            set
            {
                if (constructJobs == null)
                {
                    constructJobs = new List<Thing>();
                    constructJobs.Clear();
                }
                constructJobs = value;
            }
        }

        private List<Pawn> healJobs = new List<Pawn>();
        public List<Pawn> HealJobs
        {
            get
            {
                if (healJobs == null)
                {
                    healJobs = new List<Pawn>();
                    healJobs.Clear();
                }
                if (healJobs.Count > 0)
                {
                    List<Pawn> tmpJobs = new List<Pawn>();
                    tmpJobs.Clear();
                    for (int i = 0; i < healJobs.Count; i++)
                    {
                        if (healJobs[i] != null && !healJobs[i].DestroyedOrNull() && healJobs[i].Spawned)
                        {
                            Pawn p = healJobs[i];
                            if (p.health.summaryHealth.SummaryHealthPercent < 1f && p.InBed()) //Tends injuries for pawns in a bed
                            {
                                tmpJobs.Add(p);
                            }
                            else if(false) //helps fight diseases? 
                            {
                                //check for diseases
                            }
                        }
                    }
                    healJobs = tmpJobs;
                }
                return healJobs;
            }
            set
            {
                if (healJobs == null)
                {
                    healJobs = new List<Pawn>();
                    healJobs.Clear();
                }
                healJobs = value;
            }
        }

        private List<Thing> deconstructJobs = new List<Thing>();
        public List<Thing> DeconstructJobs
        {
            get
            {
                if (deconstructJobs == null)
                {
                    deconstructJobs = new List<Thing>();
                    deconstructJobs.Clear();
                }
                if (deconstructJobs.Count > 0)
                {
                    List<Thing> tmpJobs = new List<Thing>();
                    tmpJobs.Clear();
                    for (int i = 0; i < deconstructJobs.Count; i++)
                    {
                        if (deconstructJobs[i] != null && deconstructJobs[i] is Building)
                        {
                            List<Thing> designatedDeconstruct = new List<Thing>();
                            designatedDeconstruct.Clear();
                            using (IEnumerator<Designation> enumerator = this.Map.designationManager.SpawnedDesignationsOfDef(DesignationDefOf.Deconstruct).GetEnumerator())
                            {
                                if (enumerator.MoveNext())
                                {
                                    Designation des = enumerator.Current;
                                    if(des.target != null && des.target.Thing != null)
                                    {
                                        Building b = deconstructJobs[i] as Building;
                                        if (b.DeconstructibleBy(this.Faction))
                                        {
                                            designatedDeconstruct.Add(des.target.Thing);
                                        }
                                    }
                                }
                            }
                            if(designatedDeconstruct.Contains(deconstructJobs[i]))
                            {
                                tmpJobs.Add(deconstructJobs[i]);
                            }
                        }
                    }
                    deconstructJobs = tmpJobs;
                }
                return deconstructJobs;
            }
            set
            {
                if (deconstructJobs == null)
                {
                    deconstructJobs = new List<Thing>();
                    deconstructJobs.Clear();
                }
                deconstructJobs = value;
            }
        }

        //Not sure what this does
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void Tick()
        {        
            base.Tick();

            //Offset execution of each function based on game tick
            if(Find.TickManager.TicksGame % this.repairInterval == 0)
            {
                DoRepairJobs();
            }
            if (Find.TickManager.TicksGame % this.constructInterval == 0)
            {
                DoConstructJobs();
            }
            if (Find.TickManager.TicksGame % this.deconstructInterval == 0)
            {
                DoDeconstructJobs();
            }
            if (Find.TickManager.TicksGame % this.healInterval == 0)
            {
                DoHealJobs();
            }
        }

        //Execute ongoing jobs each interval
        public void DoRepairJobs()
        {
            if(RepairJobs.Count > 0)
            {
                List<Thing> tmpJobs = RepairJobs;
                for(int i =0; i < tmpJobs.Count; i++)
                {
                    Thing jobThing = tmpJobs[i];
                    if(jobThing != null)
                    {
                        jobThing.HitPoints = Mathf.Clamp(jobThing.HitPoints += this.repairSkill, 0, jobThing.MaxHitPoints);
                        Vector3 rndVec = jobThing.DrawPos;
                        rndVec.x += (Rand.Range(-.3f, .3f));
                        rndVec.z += (Rand.Range(-.3f, .3f));
                        SPT_Utility.ThrowGenericMote(SPT_DefOf.SPT_Mote_NaniteWorking, rndVec, jobThing.Map, Rand.Range(.02f, .2f), .1f, .3f, .3f, Rand.Range(-300,300), 0, 0, Rand.Range(0, 360));
                        if(jobThing.HitPoints == jobThing.MaxHitPoints)
                        {
                            RepairJobs.Remove(tmpJobs[i]);
                        }
                    }
                }
            }
        }

        public void DoConstructJobs()
        {

        }

        public void DoDeconstructJobs()
        {

        }

        public void DoHealJobs()
        {

        }

        //I assume gizmo is an "interactive furniture"
        public override IEnumerable<Gizmo> GetGizmos()
        {
            var gizmoList = base.GetGizmos().ToList();
            //Check to make sure research is complete
            //Should require research before construction of the nanite factory?
            //Further research can provide additional functionality?
            if (true)//ResearchProjectDef.Named("SPT_MolecularRobotics").IsFinished)
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
                    icon = SPT_MatPool.Icon_Deconstruct,
                    action = delegate
                    {
                        Log.Message("ACTION 1");
                        sendNanitesDeconstruct();
                    }
                };
                gizmoList.Add(item2);

                Command_Action item3 = new Command_Action
                {
                    defaultLabel = construction_label,
                    defaultDesc = construction_desc,
                    order = 69,
                    icon = SPT_MatPool.Icon_Construct,
                    action = delegate
                    {
                        Log.Message("ACTION 2");
                        sendNanitesConstruct();
                    }
                };
                gizmoList.Add(item3);


                Command_Action item4 = new Command_Action
                {
                    defaultLabel = repair_label,
                    defaultDesc = repair_desc,
                    order = 70,
                    icon = SPT_MatPool.Icon_Repair,
                    action = delegate
                    {
                        Log.Message("ACTION 3");
                        sendNanitesRepair();
                    }
                };
                gizmoList.Add(item4);

                Command_Action item5 = new Command_Action
                {
                    defaultLabel = heal_label,
                    defaultDesc = heal_desc,
                    order = 71,
                    icon = SPT_MatPool.Icon_Heal,
                    action = delegate
                    {
                        Log.Message("ACTION 4");
                        sendNanitesHeal();
                    }
                };
                gizmoList.Add(item5);

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
            List<Thing> repairableBuildings = SPT_Utility.FindRepairBuildings(this.Map, this.Faction);
            if(repairableBuildings != null)
            {
                if(repairableBuildings.Count > 0)
                {
                    Thing targetThing = repairableBuildings.Except(RepairJobs).RandomElement();
                    if (targetThing != null)
                    {
                        RepairJobs.Add(targetThing);
                    }
                    else
                    {
                        Log.Message("Nothing new found to repair");
                    }
                }
                else
                {
                    Log.Message("Nothing found to repair");
                }
            }
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
