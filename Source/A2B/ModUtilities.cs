using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using Verse;
using VerseBase;
using RimWorld;

namespace A2B
{
    public static class ModUtilities
    {

        public static Mod CurrentMod 
        {
            get
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                return LoadedModManager.LoadedMods.Single(mod => mod.assemblies.loadedAssemblies.Contains(asm));
            }
        }

        public static string GetTexturePath(this Mod mod)
        {
            return mod.RootFolder + "/" + GenFilePaths.ContentPath<Texture2D>();
        }

        public static string GetTexturePath()
        {
            return CurrentMod.GetTexturePath();
        }

        public static string GetSoundPath(this Mod mod)
        {
            return mod.RootFolder + "/" + GenFilePaths.ContentPath<AudioClip>();
        }

        public static string GetSoundPath()
        {
            return CurrentMod.GetSoundPath();
        }

        public static string GetStringPath(this Mod mod)
        {
            return mod.RootFolder + "/" + GenFilePaths.ContentPath<string>();
        }

        public static string GetStringPath()
        {
            return CurrentMod.GetStringPath();
        }
    }
}
