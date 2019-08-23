using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace NaniteFactory
{
  
    [StaticConstructorOnStartup]

    //***************************************************************************************
    //  NOTES:
    // Names and research values/trees subject to change
    // Research dependancies depicted with . (DOT) notation : Research 1.1 is a child of Research 1. Research 1.1.1 is a child of research 1.1 ... etc
    // Research Tab -> Molecular Robotics

    // Research 1 : Nanobot creation  // Unlocks the Nanite Factory // 2000 points to start   
    // Research 1.1 : Nanobot Reconsititution Protocols // Unlocks the nanites ability to repair damaged, claimed structures // 2000
    // Research 1.2 : Nanobot Construction Protocols // Unlocks the nanites ability to construct things // 2000
    // Research 1.3 : Nanobot Mending Protocols // Unlocks the nanites ability to heal colonists // 3000
    // Research 1.2.1 : Nanobot Delivery Protocols // Unlocks the nanites ability to deliver resources to a job site // 3000


    //Also considering research to improve speed/carry/distance etc...
    //**************************************************************************************

    public class Building_NaniteFactory : Building_WorkTable
    {
        private Thing targetThing = null;
        private LocalTargetInfo infoTarget = null;
        private Effecter effecter;

        //Used to determine available resources within a designated zone
        private static List<IntVec3> resourceCells = new List<IntVec3>();

        //How frequently the Nanite Factory checks for related jobs
        //Increasing intervals will reduce processor demand but slow down responsiveness
        //Intervals are offset to limit the amount of processor demand during active ticks
        private int repairInterval = 11;
        private int constructInterval = 125;
        private int deconstructInterval = 25;
        private int healInterval = 25;

        //Indicators which task the factory is currently working on
        private bool isRepairing = false;
        private bool isConstructing = false;
        private bool isDeconstructing = false;
        private bool isHealing = false;

        //Current Job for Coloring
        private Thing currentThingJob;
        private Frame currentFrameJob;
        private Pawn currentPawnJob;
        private Hediff_Injury lastBodyPartHealed;

        //How effective the nanite factory is at performing jobs
        //Used in utility and quality checks
        private int repairSkill = 1;
        private int constructSkill = 5;
        private int deconstructSkill = 1;
        private float healSkill = .1F;


        //Saved Variables - built-in QC during call; use CAP variable whenever possible
        //Repair requires -> ResearchProjectDef.Named("SPT_NaniteReconstitutionProtocols").IsFinished)
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

        //Construction requires -> ResearchProjectDef.Named("SPT_NaniteConstructionProtocols").IsFinished)
        private List<Frame> constructJobs = new List<Frame>();
        public List<Frame> ConstructJobs
        {
            get
            {
                if (constructJobs == null)
                {
                    constructJobs = new List<Frame>();
                    constructJobs.Clear();
                }
                if (constructJobs.Count > 0)
                {
                    List<Frame> tmpJobs = new List<Frame>();
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
                    constructJobs = new List<Frame>();
                    constructJobs.Clear();
                }
                constructJobs = value;
            }
        }

        //Healing requires -> ResearchProjectDef.Named("SPT_NaniteMendingProtocols").IsFinished)
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
                            if (p.health.summaryHealth.SummaryHealthPercent < 1f) //Tends injuries for pawns in a bed
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

        //Deconstruction should be the first ability by default. This should not require any additional research
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

      
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void Tick()
        {        
            base.Tick();

            //The following draws the mote every game tick.
            //This was so the user can better see what is being worked on for the jobs that take longer (constructing and deconstructing)
            //we may want to lower the amount a bit so that its not fireing every tick, but ever 4 or 10.
            if(isRepairing == true)
            {
                //set color of nanites
                //Draw Visual On thing (Blue sparkles temporary)
                if(currentThingJob != null)
                {
                    Vector3 rndVec = currentThingJob.DrawPos;
                    float size = .3f;
                    //test expanding area of motes based on building size...
                    if (currentThingJob is Building)
                    {
                        Building b = currentThingJob as Building;
                        size = b.def.Size.Magnitude;
                    }
                    rndVec.x += (Rand.Range(-size, size));
                    rndVec.z += (Rand.Range(-size, size));
                    SPT_Utility.ThrowGenericMote(SPT_DefOf.SPT_Mote_NaniteRepairing, rndVec, currentThingJob.Map, Rand.Range(.02f, .2f), .1f, .3f, .3f, Rand.Range(-300, 300), 0, 0, Rand.Range(0, 360));

                }

            }
            else if(isConstructing == true)
            {
                //Draw Visual On thing (Blue sparkles temporary)
                if(currentFrameJob != null)
                {
                    Vector3 rndVec = currentFrameJob.DrawPos;
                    rndVec.x += (Rand.Range(-.3f, .3f));
                    rndVec.z += (Rand.Range(-.3f, .3f));
                    SPT_Utility.ThrowGenericMote(SPT_DefOf.SPT_Mote_NaniteConstructing, rndVec, currentFrameJob.Map, Rand.Range(.02f, .2f), .1f, .3f, .3f, Rand.Range(-300, 300), 0, 0, Rand.Range(0, 360));

                }

            }
            else if (isDeconstructing == true)
            {
                //Draw Visual On thing (Red sparkles temporary)
                if(currentThingJob != null)
                {
                    Vector3 rndVec = currentThingJob.DrawPos;
                    rndVec.x += (Rand.Range(-.3f, .3f));
                    rndVec.z += (Rand.Range(-.3f, .3f));
                    SPT_Utility.ThrowGenericMote(SPT_DefOf.SPT_Mote_NaniteWorking, rndVec, currentThingJob.Map, Rand.Range(.02f, .2f), .1f, .3f, .3f, Rand.Range(-300, 300), 0, 0, Rand.Range(0, 360));

                }

            }
            else if (isHealing == true)
            {
                if (currentPawnJob != null)
                {
                    Vector3 rndVec = currentPawnJob.DrawPos;
                    rndVec.x += (Rand.Range(-.3f, .3f));
                    rndVec.z += (Rand.Range(-.3f, .3f));
                    SPT_Utility.ThrowGenericMote(SPT_DefOf.SPT_Mote_NaniteHealing, rndVec, currentPawnJob.Map, Rand.Range(.02f, .2f), .1f, .3f, .3f, Rand.Range(-300, 300), 0, 0, Rand.Range(0, 360));

                }
            }

            //Offset execution of each function based on game tick
            if (Find.TickManager.TicksGame % this.repairInterval == 0)
            {
                if((this.UseFixedConstruction && this.UseFixedDeconstruction && this.UseFixedHealing) != true && this.UseFixedRepairing == true)
                {
                    sendNanitesRepair();
                    DoRepairJobs();
                }
              
            }
            if (Find.TickManager.TicksGame % this.constructInterval == 0)
            {
                if ((this.UseFixedRepairing && this.UseFixedDeconstruction && this.UseFixedHealing) != true && this.UseFixedConstruction == true)
                {
                    sendNanitesConstruct();
                    DoConstructJobs();
                }
            }
            if (Find.TickManager.TicksGame % this.deconstructInterval == 0)
            {
                if ((this.UseFixedConstruction && this.UseFixedRepairing && this.UseFixedHealing) != true && this.UseFixedDeconstruction == true)
                {
                    sendNanitesDeconstruct();
                    DoDeconstructJobs();
                }
            }
            if (Find.TickManager.TicksGame % this.healInterval == 0)
            {
                if ((this.UseFixedConstruction && this.UseFixedDeconstruction && this.UseFixedRepairing) != true && this.UseFixedHealing == true)
                {
                    sendNanitesHeal();
                    DoHealJobs();
                }
            }
        }

        //Execute ongoing jobs each interval
        public void DoRepairJobs()
        {
            //If there are more than 0 repair jobs on the map
            if(RepairJobs.Count > 0)
            {   
                //Store those jobs in a temporary thing list to iterate through
                List<Thing> tmpJobs = RepairJobs;
                for(int i =0; i < tmpJobs.Count; i++)
                {
                    //Cast current iteration to a thing
                    Thing jobThing = tmpJobs[i];
               
                    //if that thing is not null (error checking)
                    if (jobThing != null)
                    {                     
                        jobThing.HitPoints = Mathf.Clamp(jobThing.HitPoints += this.repairSkill, 0, jobThing.MaxHitPoints);

                        //Draw Visual On thing (Red sparkles temporary)
                        Vector3 rndVec = jobThing.DrawPos;
                        rndVec.x += (Rand.Range(-.3f, .3f));
                        rndVec.z += (Rand.Range(-.3f, .3f));
                        SPT_Utility.ThrowGenericMote(SPT_DefOf.SPT_Mote_NaniteRepairing, rndVec, jobThing.Map, Rand.Range(.02f, .2f), .1f, .3f, .3f, Rand.Range(-300,300), 0, 0, Rand.Range(0, 360));

                        //Once thing is max health, remove thing from job list
                        if(jobThing.HitPoints == jobThing.MaxHitPoints)
                        {
                            RepairJobs.Remove(tmpJobs[i]);
                            isRepairing = false;
                        }
                    }
                }
            }
        }

        public void DoConstructJobs()
        {
            //If there are more than 0 construction jobs on the map
            if (ConstructJobs.Count > 0)
            {
                //Store those jobs in a temporary thing list to iterate through
                List<Frame> tmpJobs = ConstructJobs;
                for (int i = 0; i < tmpJobs.Count; i++)
                {
                    //Cast current iteration to a thing
                    Frame jobThing = tmpJobs[i];
                
                    //if that thing is not null (error checking)
                    if (jobThing != null)
                    {
                        //Need to check for materials needed            
                        if(jobThing.MaterialsNeeded().Count == 0)
                        {
                            jobThing.workDone = Mathf.Clamp(jobThing.workDone += this.constructSkill, 0, jobThing.WorkToBuild);

                            //Draw Visual On thing (Blue sparkles temporary)
                            Vector3 rndVec = jobThing.DrawPos;
                            rndVec.x += (Rand.Range(-.3f, .3f));
                            rndVec.z += (Rand.Range(-.3f, .3f));
                            SPT_Utility.ThrowGenericMote(SPT_DefOf.SPT_Mote_NaniteConstructing, rndVec, jobThing.Map, Rand.Range(.02f, .2f), .1f, .3f, .3f, Rand.Range(-300, 300), 0, 0, Rand.Range(0, 360));

                            //Once thing is max work, remove thing from job list
                            if (jobThing.WorkLeft == 0.0)
                            {
                                jobThing.CompleteConstruction(this.Map.mapPawns.AllPawnsSpawned.RandomElement() as Pawn);
                                ConstructJobs.Remove(tmpJobs[i]);
                                isConstructing = false;
                            }
                        }
                    }
                }
            }

        }

        public void DoDeconstructJobs()
        {
            //If there are more than 0 construction jobs on the map
            if (DeconstructJobs.Count > 0)
            {
                //Store those jobs in a temporary thing list to iterate through
                List<Thing> tmpJobs = DeconstructJobs;
                for (int i = 0; i < tmpJobs.Count; i++)
                {
                    //Cast current iteration to a thing
                    Thing jobThing = tmpJobs[i];

                    //if that thing is not null (error checking)
                    if (jobThing != null)
                    {
                            //There is nothing fancy here yet, we need to revisit how we handle this later.                     
                            jobThing.Destroy(DestroyMode.Deconstruct);
                            //Draw Visual On thing (Red sparkles temporary)
                            Vector3 rndVec = jobThing.DrawPos;
                            rndVec.x += (Rand.Range(-.3f, .3f));
                            rndVec.z += (Rand.Range(-.3f, .3f));
                            SPT_Utility.ThrowGenericMote(SPT_DefOf.SPT_Mote_NaniteWorking, rndVec, jobThing.Map, Rand.Range(.02f, .2f), .1f, .3f, .3f, Rand.Range(-300, 300), 0, 0, Rand.Range(0, 360));

                            DeconstructJobs.Remove(tmpJobs[i]);
                            isDeconstructing = false;       
                    }
                }
            }
        }

        public void DoHealJobs()
        {         
            //If there are more than 0 construction jobs on the map
            if (HealJobs.Count > 0)
            {             
                //Store those jobs in a temporary thing list to iterate through
                List<Pawn> tmpPawns = HealJobs;
                for (int i = 0; i < tmpPawns.Count; i++)
                {
                    //Cast current iteration to a thing
                    Pawn jobPawn = tmpPawns[i];
                 
                    //if that thing is not null (error checking)
                    if (jobPawn != null)
                    {               
                       
                        //Looping through this again ( i know we defined a function for this in Utility, however I want the nanites to be able to focus 1 body part at a time
                        using (IEnumerator<BodyPartRecord> enumerator = jobPawn.health.hediffSet.GetInjuredParts().GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                BodyPartRecord rec = enumerator.Current;
                                IEnumerable<Hediff_Injury> arg_BB_0 = jobPawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                                Func<Hediff_Injury, bool> arg_BB_1;
                                arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);
                              
                                foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                                {
                                    //Check if we have started healing a pawn yet, null == starting on a new pawn
                                    if (lastBodyPartHealed == null)
                                    {                                     
                                        bool flag5 = current.CanHealNaturally() && !current.IsPermanent() && current.BleedRate > 0;
                                        if (flag5)
                                        {                                                                               
                                            //set current body part being worked on
                                            lastBodyPartHealed = current;
                                            current.Heal(this.healSkill);
                                           
                                            //current.Tended(.5F, 1);
                                            if (current.Bleeding == false)
                                            {
                                                //body part is healed, reset lastBodyPartHealed                                           
                                                lastBodyPartHealed = null;
                                               
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //If we have healed a body part previously
                                        if (lastBodyPartHealed == current)
                                        {                                       
                                           // Get the same body part we healed last interval
                                            bool flag5 = current.CanHealNaturally() && !current.IsPermanent() && current.BleedRate > 0;
                                            if (flag5)
                                            {                                      
                                                lastBodyPartHealed = current;
                                                current.Heal(this.healSkill);                                                                                     
                                                if (current.Bleeding == false)
                                                {       
                                                    //body part is healed, reset lastBodyPartHealed
                                                    lastBodyPartHealed = null;                                    
                                                }
                                            }
                                        }
                                    }                                             
                                }
                            }
                        }
                   
                        //Draw Visual On thing (Red sparkles temporary)
                        Vector3 rndVec = jobPawn.DrawPos;
                        rndVec.x += (Rand.Range(-.3f, .3f));
                        rndVec.z += (Rand.Range(-.3f, .3f));
                        SPT_Utility.ThrowGenericMote(SPT_DefOf.SPT_Mote_NaniteHealing, rndVec, jobPawn.Map, Rand.Range(.02f, .2f), .1f, .3f, .3f, Rand.Range(-300, 300), 0, 0, Rand.Range(0, 360));

                     
                        if (!SPT_Utility.IsPawnInjured(jobPawn, 0))
                        {
                           
                            HealJobs.Remove(jobPawn);
                            isHealing = false;
                        }

                    }
                }
            }
        }

        public void ShowProgressBar()
        {
            
            //bool flag4 = this.effecter == null;
            //if (flag4)
            //{
            //    EffecterDef progressBar = EffecterDefOf.ProgressBar;
            //    this.effecter = progressBar.Spawn();
            //}
            //else
            //{
            //    LocalTargetInfo localTargetInfo = this.parent;
            //    bool spawned2 = base.parent.Spawned;
            //    if (spawned2)
            //    {
            //        this.effecter.EffectTick(this.parent, TargetInfo.Invalid);
            //    }
            //    MoteProgressBar mote = ((SubEffecter_ProgressBar)this.effecter.children[0]).mote;
            //    bool flag5 = mote != null;
            //    if (flag5)
            //    {
            //        float value = 1f - (float)(this.TicksToDestroy - this.ticksLeft) / (float)this.TicksToDestroy;
            //        mote.progress = Mathf.Clamp01(value);
            //        mote.offsetZ = -0.5f;
            //    }
            //}
            //bool flag = this.effecter != null && this.effecter.children != null;
            //if (flag)
            //{
            //    this.effecter.Cleanup();
            //}
        }

     
        public void ToggleUseFixed(int flag)
        {
            switch (flag)
            {
                //Deconstruction
                case 1:
                    this.UseFixedDeconstruction = true;
                    this.UseFixedConstruction = false;
                    this.UseFixedHealing = false;
                    this.UseFixedRepairing = false;
                    break;
                //Construction
                case 2:
                    this.UseFixedDeconstruction = false;
                    this.UseFixedConstruction = true;
                    this.UseFixedHealing = false;
                    this.UseFixedRepairing = false;
                    break;
                //healing
                case 3:
                    this.UseFixedDeconstruction = false;
                    this.UseFixedConstruction = false;
                    this.UseFixedHealing = true;
                    this.UseFixedRepairing = false;
                    break;
                //repairing
                case 4:
                    this.UseFixedDeconstruction = false;
                    this.UseFixedConstruction = false;
                    this.UseFixedHealing = false;
                    this.UseFixedRepairing = true;
                    break;
                default:
                    //Something went wrong...
                    this.UseFixedDeconstruction = false;
                    this.UseFixedConstruction = false;
                    this.UseFixedHealing = false;
                    this.UseFixedRepairing = false;
                    break;
            }
            
            
        }
     
        public override IEnumerable<Gizmo> GetGizmos()
        {
          
            var gizmoList = base.GetGizmos().ToList();
            if (true)//ResearchProjectDef.Named("SPT_MolecularRobotics").IsFinished)
            {

                if ((this as Building).Faction == Faction.OfPlayer)
                {
                    gizmoList.Add( new Command_Toggle
                    {
                        //hotKey = KeyBindingDefOf.Command_TogglePower,
                        icon = SPT_MatPool.Icon_Deconstruct,
                        defaultLabel = SPT_Labels.deconstruction_label,
                        defaultDesc = SPT_Labels.deconstruction_desc,
                        isActive = (() => (this.UseFixedDeconstruction == true)),
                        toggleAction = delegate
                        {
                            //1 = deconstruct
                            ToggleUseFixed(1);                        
                            if (Find.TickManager.TicksGame % this.deconstructInterval == 0)
                            {
                                sendNanitesDeconstruct();
                            }
                                                            
                        }
                    });
                    gizmoList.Add(new Command_Toggle
                    {
                        //hotKey = KeyBindingDefOf.Command_TogglePower,
                        icon = SPT_MatPool.Icon_Construct,
                        defaultLabel = SPT_Labels.construction_label,
                        defaultDesc = SPT_Labels.construction_desc,
                        isActive = (() => (this.UseFixedConstruction == true)),
                        toggleAction = delegate
                        {
                            //2 = construct
                            ToggleUseFixed(2);
                            if (Find.TickManager.TicksGame % this.constructInterval == 0)
                            {
                                sendNanitesConstruct();
                            }
                                                            
                        }
                    });
                    gizmoList.Add(new Command_Toggle
                    {
                        //hotKey = KeyBindingDefOf.Command_TogglePower,
                        icon = SPT_MatPool.Icon_Heal,
                        defaultLabel = SPT_Labels.heal_label,
                        defaultDesc = SPT_Labels.heal_desc,
                        isActive = (() => (this.UseFixedHealing == true)),
                        toggleAction = delegate
                        {
                            //3 = heal
                            ToggleUseFixed(3);
                            if (Find.TickManager.TicksGame % this.healInterval == 0)
                            {
                                sendNanitesHeal();
                            }

                        }
                    });
                    gizmoList.Add(new Command_Toggle
                    {
                        //hotKey = KeyBindingDefOf.Command_TogglePower,
                        icon = SPT_MatPool.Icon_Repair,
                        defaultLabel = SPT_Labels.repair_label,
                        defaultDesc = SPT_Labels.repair_desc,
                        isActive = (() => (this.UseFixedRepairing == true)),
                        toggleAction = delegate
                        {
                            //4 = repair
                            ToggleUseFixed(4);
                                if (Find.TickManager.TicksGame % this.repairInterval == 0)
                                {
                                    sendNanitesRepair();
                                }
                            
                           
                        }
                    });
                   
                }

                //Stockpile creation button
                if (DesignatorUtility.FindAllowedDesignator<Designator_ZoneAddStockpile_Resources>() != null)
                {
                    Command_Action item0 = new Command_Action
                    {

                        action = new Action(this.MakeMatchingStockpile),
                        hotKey = KeyBindingDefOf.Misc3,
                        order = 50,
                        defaultDesc = SPT_Labels.stockpile_desc,
                        icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Stockpile", true),
                        defaultLabel = SPT_Labels.stockpile_label
                    };
                    gizmoList.Add(item0);
                }

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
            List<Thing> deconstructableThings = SPT_Utility.FindDeconstructionBuildings(this.Map, this.Faction);
            if (deconstructableThings != null)
            {
                if (deconstructableThings.Count > 0)
                {

                    Thing targetThing = deconstructableThings.Except(DeconstructJobs).RandomElement();
                    if (targetThing != null)
                    {
                        currentThingJob = targetThing;
                        DeconstructJobs.Add(targetThing);
                    }
                    else
                    {
                        Log.Message("Nothing new found to deconstruct");
                    }

                }
            }
        }
        private void sendNanitesConstruct()
        {
            List<Frame> constructableBuildings = SPT_Utility.FindConstructionBuildings(this.Map, this.Faction);
            if (constructableBuildings != null)
            {
                if (constructableBuildings.Count > 0)
                {
                
                        Frame targetThing = constructableBuildings.Except(ConstructJobs).RandomElement();
                        if (targetThing != null)
                        {
                            currentFrameJob = targetThing;
                            ConstructJobs.Add(targetThing);
                        }
                        else
                        {
                            Log.Message("Nothing new found to construct");
                        }
                                   
                }
            }
                  
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
                        //Need to check if the "Wireless" research is researched
                        List<IntVec3> ePath = SPT_Utility.FindElectricPath(this, targetThing, this.InteractionCell);
                     
                        if (ePath.Count > 0)
                        {
                            //Delivery And Tracking                    
                            if(SPT_DefOf.SPT_NaniteWirelessAdaptation.IsFinished)
                            {
                                //Do wireless delivery method...
                            }
                            else
                            {
                                //Do wired delivery method
                                NaniteDelivery_Wired(targetThing, SPT_Utility.IntVec3List_To_Vector3List(ePath));
                            }
                        }
                        else
                        {
                            Log.Message("no path found");
                        }
                       
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
            List<Pawn> healablePawns = SPT_Utility.FindHurtPawns(this.Map, this.Faction);
            if (healablePawns != null)
            {
                if (healablePawns.Count > 0)
                {
                    Pawn targetPawn = healablePawns.Except(HealJobs).RandomElement();
                    if (targetPawn != null)
                    {
                        currentPawnJob = targetPawn;
                        HealJobs.Add(targetPawn);    
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

        public void NaniteDelivery_Wired(LocalTargetInfo target, List<Vector3> path)
        {
            LocalTargetInfo t = target;
            bool flag = t.Cell != default(IntVec3);
            if (flag)
            {
                Thing launchedThing = new Thing()
                {
                    def = SPT_DefOf.SPT_FlyingObject
                };
                Thing launcher = this;
                SPT_FlyingObject flyingObject = (SPT_FlyingObject)GenSpawn.Spawn(SPT_DefOf.SPT_FlyingObject, this.Position, this.Map);  //replace this.Position with this.interactionCell (Building)
                flyingObject.ExactLaunch(null, 0, false, path, launcher, path[0], t, launchedThing, 10, 0);
                //LongEventHandler.QueueLongEvent(delegate
                //{
                    
                //}, "LaunchingFlyer", false, null);
            }
        }



        //Create resource stockpile
        public IEnumerable<IntVec3> ResourceCells
        {
            get
            {
                return Building_NaniteFactory.ResourceCellsAround(base.Position, base.Map);
            }
        }

        public bool UseFixedDeconstruction { get; private set; }
        public bool UseFixedConstruction { get; private set; }
        public bool UseFixedRepairing { get; private set; }
        public bool UseFixedHealing { get; private set; }

        private void MakeMatchingStockpile()
        {
            Designator des = DesignatorUtility.FindAllowedDesignator<Designator_ZoneAddStockpile_Resources>();

            des.DesignateMultiCell(from c in this.ResourceCells
                                   where des.CanDesignateCell(c).Accepted
                                   select c);
        }

        public static List<IntVec3> ResourceCellsAround(IntVec3 pos, Map map)
        {
            Building_NaniteFactory.resourceCells.Clear();
            if (!pos.InBounds(map))
            {
                return Building_NaniteFactory.resourceCells;

            }
            Region region = pos.GetRegion(map, RegionType.Set_Passable);
            if (region == null)
            {
                return Building_NaniteFactory.resourceCells;
            }
            RegionTraverser.BreadthFirstTraverse(region, (Region from, Region r) => r.door == null, delegate (Region r)
            {
                foreach (IntVec3 current in r.Cells)
                {
                    if (current.InHorDistOf(pos, 3.99f))
                    {
                        Building_NaniteFactory.resourceCells.Add(current);
                    }
                }
                return false;
            }, 13, RegionType.Set_Passable);
            return Building_NaniteFactory.resourceCells;
        }

    }

}
