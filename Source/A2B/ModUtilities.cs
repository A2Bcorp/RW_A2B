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
    class ModUtilities
    {

        public static Mod CurrentMod 
        {
            get
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                return LoadedModManager.LoadedMods.Single(mod => mod.assemblies.loadedAssemblies.Contains(asm));
            }
        }

        public static string GetTexturePath(Mod mod = null)
        {
            if (mod == null)
                mod = CurrentMod;

            return mod.RootFolder + "/" + GenFilePaths.ContentPath<Texture2D>();
        }

        public static string GetSoundPath(Mod mod = null)
        {
            if (mod == null)
                mod = CurrentMod;

            return mod.RootFolder + "/" + GenFilePaths.ContentPath<AudioClip>();
        }

        public static string GetStringPath(Mod mod = null)
        {
            if (mod == null)
                mod = CurrentMod;

            return mod.RootFolder + "/" + GenFilePaths.ContentPath<string>();
        }
    }
}
