﻿using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NaniteFactory
{
    [StaticConstructorOnStartup]
    public class SPT_FlyingObject : ThingWithComps
    {

        protected Vector3 origin;
        protected Vector3 destination;
        protected Vector3 trueOrigin;
        protected Vector3 trueDestination;

        public float speed = 30f;
        protected int ticksToImpact;
        protected Thing launcher;
        protected Thing assignedTarget;
        protected Thing flyingThing;

        public ThingDef moteDef = null;
        public int moteFrequency = 0;

        public bool spinning = false;
        public float curveVariance = 0; // 0 = no curve, straight flight
        private List<Vector3> curvePoints = new List<Vector3>();
        private List<IntVec3> impactCells = new List<IntVec3>();
        public float force = 1f;
        private int destinationCurvePoint = 0;
        private float impactRadius = 0;
        private int explosionDamage = 0;
        private bool isExplosive = false;
        private DamageDef impactDamageType = null;
        private bool flyOverhead = false;
        private NaniteDispersal dispersalMethod = NaniteDispersal.Invisible;
        private NaniteActions naniteAction;
        private bool impacted = false;
        private int ticksFollowingImpact = 120;
        private Vector3 sprayVec = default(Vector3);

        private bool earlyImpact = false;
        private float impactForce = 0;

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;
        public bool explosion = false;
        public int weaponDmg = 0;

        Pawn pawn;

        protected int StartingTicksToImpact
        {
            get
            {
                int num = Mathf.RoundToInt((this.origin - this.destination).magnitude / (this.speed / 100f));
                bool flag = num < 1;
                if (flag)
                {
                    num = 1;
                }
                return num;
            }
        }

        protected IntVec3 DestinationCell
        {
            get
            {
                return new IntVec3(this.destination);
            }
        }

        public virtual Vector3 ExactPosition
        {
            get
            {
                Vector3 b = (this.destination - this.origin) * (1f - (float)this.ticksToImpact / (float)this.StartingTicksToImpact);
                return this.origin + b + Vector3.up * this.def.Altitude;
            }
        }

        public virtual Quaternion ExactRotation
        {
            get
            {
                return Quaternion.LookRotation(this.destination - this.origin);
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                return this.ExactPosition;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Vector3>(ref this.origin, "origin", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.destination, "destination", default(Vector3), false);
            Scribe_Values.Look<int>(ref this.ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look<bool>(ref this.damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref this.explosion, "explosion", false, false);
            Scribe_Values.Look<bool>(ref this.impacted, "impacted", false, false);
            Scribe_Values.Look<NaniteActions>(ref this.naniteAction, "naniteAction", NaniteActions.Repair, false);
            Scribe_Values.Look<NaniteDispersal>(ref this.dispersalMethod, "dispersalMethod", NaniteDispersal.Invisible, false);
            Scribe_References.Look<Thing>(ref this.assignedTarget, "assignedTarget", false);
            Scribe_References.Look<Thing>(ref this.launcher, "launcher", false);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                MoteMaker.ThrowDustPuff(pawn.Position, pawn.Map, Rand.Range(1.2f, 1.8f));
            }
            else
            {
                flyingThing.ThingID += Rand.Range(0, 214).ToString();
            }

        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, DamageInfo? impactDamage)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, impactDamage);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, null);
        }
  
        public void AdvancedLaunch(Thing launcher, ThingDef effectMote, int moteFrequencyTicks, float curveAmount, bool shouldSpin, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, int flyingSpeed, bool isExplosion, int _impactDamage, float _impactRadius, NaniteDispersal dispersal, NaniteActions action, DamageDef damageType, DamageInfo? newDamageInfo = null)
        {

            this.naniteAction = action;
            this.dispersalMethod = dispersal;
            this.explosionDamage = _impactDamage;
            this.isExplosive = isExplosion;
            this.impactRadius = _impactRadius;
            this.impactDamageType = damageType;
            this.moteFrequency = moteFrequencyTicks;
            this.moteDef = effectMote;
            this.curveVariance = curveAmount;
            this.spinning = shouldSpin;
            this.speed = flyingSpeed;
            this.curvePoints = new List<Vector3>();
            this.curvePoints.Clear();
            this.Launch(launcher, origin, targ, flyingThing, newDamageInfo);
        }


        public void ExactLaunch(ThingDef effectMote, int moteFrequencyTicks, bool shouldSpin, List<Vector3> travelPath, Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, int flyingSpeed, float _impactRadius, NaniteDispersal dispersal, NaniteActions action)
        {
            this.naniteAction = action;
            this.dispersalMethod = dispersal;
            this.moteFrequency = moteFrequencyTicks;
            this.moteDef = effectMote;
            this.impactRadius = _impactRadius;
            this.spinning = shouldSpin;
            this.speed = flyingSpeed;
            this.curvePoints = travelPath;
            this.curveVariance = 1;
            this.Launch(launcher, origin, targ, flyingThing, null);         
        }
       

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, DamageInfo? newDamageInfo = null)
        {
            //Log.Message("launching object");
            bool spawned = flyingThing.Spawned;
            this.pawn = launcher as Pawn;
            if (spawned)
            {
                flyingThing.DeSpawn();
            }
            this.launcher = launcher; //Bench
            this.trueOrigin = origin; //Bench
            this.trueDestination = targ.Cell.ToVector3(); // Factory
            this.impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;
            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            this.speed = this.speed * this.force;
            this.origin = origin;
            if (this.curveVariance > 0 && this.curvePoints.Count == 0)
            {
                CalculateCurvePoints(this.trueOrigin, this.trueDestination, this.curveVariance);
                this.destinationCurvePoint++;
                this.destination = this.curvePoints[this.destinationCurvePoint];
            }
            else if (this.curveVariance > 0 && this.curvePoints.Count > 0)
            {
                this.destinationCurvePoint++;
                this.destination = this.curvePoints[this.destinationCurvePoint];
            }
            else
            {
                this.destination = this.trueDestination;
            }
            this.ticksToImpact = this.StartingTicksToImpact;
            this.Initialize();
        }
        public void LaunchNoThing(IntVec3 launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, DamageInfo? newDamageInfo = null)
        {
            //Log.Message("launching object");
            bool spawned = flyingThing.Spawned;
            //this.pawn = launcher as Pawn;
            if (spawned)
            {
                flyingThing.DeSpawn();
            }
            this.launcher.Position = launcher; //Bench
            this.trueOrigin = origin; //Bench
            this.trueDestination = targ.Cell.ToVector3(); // Factory
            this.impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;
            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            this.speed = this.speed * this.force;
            this.origin = origin;
            if (this.curveVariance > 0 && this.curvePoints.Count == 0)
            {
                CalculateCurvePoints(this.trueOrigin, this.trueDestination, this.curveVariance);
                this.destinationCurvePoint++;
                this.destination = this.curvePoints[this.destinationCurvePoint];
            }
            else if (this.curveVariance > 0 && this.curvePoints.Count > 0)
            {
                this.destinationCurvePoint++;
                this.destination = this.curvePoints[this.destinationCurvePoint];
            }
            else
            {
                this.destination = this.trueDestination;
            }
            this.ticksToImpact = this.StartingTicksToImpact;
            this.Initialize();
        }

        public void CalculateCurvePoints(Vector3 start, Vector3 end, float variance)
        {
            int variancePoints = 20;
            Vector3 initialVector = SPT_Utility.GetVector(start, end);
            initialVector.y = 0;
            float initialAngle = (initialVector).ToAngleFlat(); //Quaternion.AngleAxis(90, Vector3.up) *
            float curveAngle = 0;
            if (Rand.Chance(.5f))
            {
                curveAngle = variance;
            }
            else
            {
                curveAngle = (-1) * variance;
            }
            //calculate extra distance bolt travels around the ellipse
            float a = .5f * Vector3.Distance(start, end);
            float b = a * Mathf.Sin(.5f * Mathf.Deg2Rad * variance);
            float p = .5f * Mathf.PI * (3 * (a + b) - (Mathf.Sqrt((3 * a + b) * (a + 3 * b))));

            float incrementalDistance = p / variancePoints;
            float incrementalAngle = (curveAngle / variancePoints) * 2;
            this.curvePoints.Add(this.trueOrigin);
            for (int i = 1; i < variancePoints; i++)
            {
                this.curvePoints.Add(this.curvePoints[i - 1] + ((Quaternion.AngleAxis(curveAngle, Vector3.up) * initialVector) * incrementalDistance)); //(Quaternion.AngleAxis(curveAngle, Vector3.up) *
                curveAngle -= incrementalAngle;
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (!this.impacted)
            {
                Vector3 exactPosition = this.ExactPosition;
                if (this.ticksToImpact >= 0 && this.moteDef != null && Find.TickManager.TicksGame % this.moteFrequency == 0)
                {
                    DrawEffects(exactPosition);
                }
                this.ticksToImpact--;
                bool flag = !this.ExactPosition.InBounds(base.Map);
                if (flag)
                {
                    this.ticksToImpact++;
                    base.Position = this.ExactPosition.ToIntVec3();
                    this.Destroy(DestroyMode.Vanish);
                }
                else if (this.dispersalMethod == NaniteDispersal.Spray && !this.ExactPosition.ToIntVec3().GetTransmitter(this.Map).TransmitsPowerNow)
                {
                    this.earlyImpact = true;
                    this.impactForce = (this.DestinationCell - this.ExactPosition.ToIntVec3()).LengthHorizontal + (this.speed * .2f);
                    this.ImpactSomething();
                }
                else
                {
                    base.Position = this.ExactPosition.ToIntVec3();
                    //if (Find.TickManager.TicksGame % 3 == 0)
                    //{
                    //    MoteMaker.ThrowDustPuff(base.Position, base.Map, Rand.Range(0.6f, .8f));
                    //}

                    bool flag2 = this.ticksToImpact <= 0;
                    if (flag2)
                    {
                        if (this.curveVariance > 0)
                        {
                            if ((this.curvePoints.Count() - 1) > this.destinationCurvePoint)
                            {
                                this.origin = curvePoints[destinationCurvePoint];
                                this.destinationCurvePoint++;
                                this.destination = this.curvePoints[this.destinationCurvePoint];
                                this.ticksToImpact = this.StartingTicksToImpact;
                            }
                            else
                            {
                                bool flag3 = this.DestinationCell.InBounds(base.Map);
                                if (flag3)
                                {
                                    base.Position = this.DestinationCell;
                                }
                                this.ImpactSomething();
                            }
                        }
                        else
                        {
                            bool flag3 = this.DestinationCell.InBounds(base.Map);
                            if (flag3)
                            {
                                base.Position = this.DestinationCell;
                            }
                            this.ImpactSomething();
                        }
                    }
                }
            }

            if (this.impacted)
            {
                if (this.ticksFollowingImpact > 0 && Find.TickManager.TicksGame % 2 == 0 && this.dispersalMethod == NaniteDispersal.Spray)
                {
                    //Spray nanites
                  
                    SPT_Utility.ThrowGenericMote(SPT_DefOf.SPT_Mote_NanitesAir, this.ExactPosition, this.launcher.Map, Rand.Range(.8f, 1.2f), .3f, .01f, .3f, Rand.Range(-100, 100), 5, (Quaternion.AngleAxis(Rand.Range(80, 100), Vector3.up) * sprayVec).ToAngleFlat(), Rand.Range(0, 360));
               
                }

                if(this.ticksFollowingImpact > 0 && this.dispersalMethod == NaniteDispersal.ExplosionMist && this.impactRadius > 0 && Find.TickManager.TicksGame % 3 == 0)
                {
                    for(int i =0; i < impactCells.Count; i++)
                    {
                        //Spray nanites
                      
                        SPT_Utility.ThrowGenericMote(SPT_DefOf.SPT_Mote_NanitesAir, impactCells[i].ToVector3Shifted(), this.launcher.Map, Rand.Range(1.5f, 2f), .5f, .01f, Rand.Range(.5f,1), Rand.Range(-500, 500), Rand.Range(2,5), (Quaternion.AngleAxis(Rand.Range(80, 100), Vector3.up) * sprayVec).ToAngleFlat(), Rand.Range(0, 360));
                      
                    }
                }

                this.ticksFollowingImpact--;

                if (this.ticksFollowingImpact < 0)
                {
                    this.Destroy(DestroyMode.Vanish);
                }
            }
        }

        public override void Draw()
        {
            if (!this.impacted)
            {
                bool flag = this.flyingThing != null;
                if (flag)
                {
                    bool flag2 = this.flyingThing is Pawn;
                    if (flag2)
                    {
                        Vector3 arg_2B_0 = this.DrawPos;
                        bool flag4 = !this.DrawPos.ToIntVec3().IsValid;
                        if (flag4)
                        {
                            return;
                        }
                        Pawn pawn = this.flyingThing as Pawn;
                        pawn.Drawer.DrawAt(this.DrawPos);

                    }
                    else
                    {
                        Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 0);
                    }
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 0);
                }
                base.Comps_PostDraw();
            }
        }

        private void DrawEffects(Vector3 effectVec)
        {
            effectVec.x += Rand.Range(-0.2f, 0.2f);
            effectVec.z += Rand.Range(-0.2f, 0.2f);
            SPT_Utility.ThrowGenericMote(this.moteDef, effectVec, this.Map, Rand.Range(.4f, .6f), Rand.Range(.05f, .1f), .03f, Rand.Range(.2f, .3f), Rand.Range(-200, 200), Rand.Range(.5f, 2f), Rand.Range(0, 360), Rand.Range(0, 360));
        }

        private void ImpactSomething()
        {
            bool flag = this.assignedTarget != null;
            if (flag)
            {
                Pawn pawn = this.assignedTarget as Pawn;
                bool flag2 = pawn != null && pawn.GetPosture() != PawnPosture.Standing && (this.origin - this.destination).MagnitudeHorizontalSquared() >= 20.25f && Rand.Value > 0.2f;
                if (flag2)
                {
                    this.Impact(null);
                }
                else
                {
                    this.Impact(this.assignedTarget);
                }
            }
            else
            {
                this.Impact(null);
            }
        }

        protected virtual void Impact(Thing hitThing)
        {
           
            if (this.impactRadius > 0)
            {
                if (this.isExplosive)
                {
                    GenExplosion.DoExplosion(this.ExactPosition.ToIntVec3(), this.Map, this.impactRadius, this.impactDamageType, this.launcher as Pawn, this.explosionDamage, -1, this.impactDamageType.soundExplosion, def, null, null, null, 0f, 1, false, null, 0f, 0, 0.0f, true);
                }
                else
                {
                    this.impactCells = GenRadial.RadialCellsAround(this.Position, this.impactRadius, true).ToList();                   
                }
            }
            
            //From nanite to bench // not null
            //This.Launcher WHEN RETURNING HOME = Bench
            //This.LAUNCHER when being sent to job = NF

            Building_NaniteFactory factory = this.launcher as Building_NaniteFactory; // NULL // THING
            Building_NaniteFactory returnFactory = hitThing as Building_NaniteFactory;


            Log.Message("1");
            if (!factory.DestroyedOrNull())
            {
                Log.Message("2");
                if (dispersalMethod == NaniteDispersal.Spray && !hitThing.DestroyedOrNull() && hitThing.Spawned)
                {
                    Log.Message("3");
                    this.sprayVec = SPT_Utility.GetVector(this.curvePoints[this.curvePoints.Count - 1], hitThing.DrawPos);
                    this.impacted = true;
                    this.ticksFollowingImpact = 35;
                    if (naniteAction == NaniteActions.Repair)
                    {
                        factory.RepairJobs.Add(hitThing);
                    }
                    else if(naniteAction == NaniteActions.Construct)
                    {
                        Frame constructFrame = hitThing as Frame;
                        factory.ConstructJobs.Add(constructFrame);
                    }
                    else if (naniteAction == NaniteActions.Deconstruct)
                    {

                        factory.DeconstructJobs.Add(hitThing);
                    }

                }
                else if (dispersalMethod == NaniteDispersal.ExplosionMist && !hitThing.DestroyedOrNull() && hitThing.Spawned)
                {                 
                    this.sprayVec = SPT_Utility.GetVector(this.curvePoints[this.curvePoints.Count - 1], hitThing.DrawPos);                 
                    this.impacted = true;                 
                    this.ticksFollowingImpact = 15;                
                    if (naniteAction == NaniteActions.Repair)
                    {                       
                        factory.RepairJobs.Add(hitThing);                        
                    }
                    else if (naniteAction == NaniteActions.Construct)
                    {
                        Frame constructFrame = hitThing as Frame;
                        factory.ConstructJobs.Add(constructFrame);
                    }
                    else if (naniteAction == NaniteActions.Deconstruct)
                    {
                  
                        factory.DeconstructJobs.Add(hitThing);
                    }

                }
                
                else
                {
                    Log.Message("5");

                    this.Destroy(DestroyMode.Vanish);
                }
            }
            else if (!returnFactory.DestroyedOrNull() && returnFactory.def == SPT_DefOf.SPT_NaniteFactory)
            {

                //if (naniteAction == NaniteActions.Return)
                //{
                    returnFactory.nanitesTraveling = false;
                    this.Destroy(DestroyMode.Vanish);
                //}
                
            }
            else
            {

                //FIRE FIRE FIRE
                this.Destroy(DestroyMode.Vanish);
                
                
            }
        }
    }
}
