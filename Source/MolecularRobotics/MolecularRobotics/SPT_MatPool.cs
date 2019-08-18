using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace NaniteFactory
{
    [StaticConstructorOnStartup]
    public static class SPT_MatPool
    {
        /* Preload and archived graphics/textures */
        
        //public static readonly Material blackLightning = MaterialPool.MatFrom("Other/ArcaneBolt", true);
        public static readonly Texture2D Icon_Construct = ContentFinder<Texture2D>.Get("Icons/construct", true);
        public static readonly Texture2D Icon_Deconstruct = ContentFinder<Texture2D>.Get("Icons/deconstruct", true);
        public static readonly Texture2D Icon_Repair = ContentFinder<Texture2D>.Get("Icons/repair", true);
        public static readonly Texture2D Icon_Heal = ContentFinder<Texture2D>.Get("Icons/heal", true);
    }
}