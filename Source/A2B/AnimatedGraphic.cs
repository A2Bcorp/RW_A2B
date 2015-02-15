using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using VerseBase;
using RimWorld;

namespace A2B
{
    public class AnimatedGraphic : Graphic_Collection
    {
        private float animationRate = 1.0f;
        private bool isAnimating = true;
        private int defaultFrame = 0;

        public static AnimatedGraphic FromSingleFrame(Graphic frame)
        {
            int finalSlash = frame.initPath.LastIndexOf('/');
            string path = frame.initPath.Substring(0, finalSlash);

            AnimatedGraphic anim = new AnimatedGraphic(path, frame.shader, frame.overdraw, frame.color);
            anim.DefaultFrame = Array.FindIndex(anim.subGraphics, row => row == frame);

            return anim;
        }

        public AnimatedGraphic(string folderPath, Shader shader, bool overdraw, Color color) : base(folderPath, shader, overdraw, color)
        {
            List<string> files = Directory.GetFiles(Path.Combine(ModUtilities.GetTexturePath(), folderPath)).ToList();
            files.Sort((a, b) => a.CompareTo(b));

            int i = 0;
            foreach (string file in files)
            {
                string fileRelative = folderPath + "/" + Path.GetFileNameWithoutExtension(file);
                //Log.Message("adding graphic " + i + " from file '" + fileRelative + "'");
                subGraphics[i++] = GraphicDatabase.Get_Single(fileRelative, shader, overdraw, color);
            }
        }

        public bool IsAnimating
        {
            get
            {
                return isAnimating;
            }

            set
            {
                isAnimating = value;
            }
        }

        public int DefaultFrame
        {
            get
            {
                return defaultFrame;
            }

            set
            {
                defaultFrame = value;
            }
        }

        public override Material MatSingle
        {
            get
            {
                return CurrentGraphic.MatSingle;
            }
        }

        public override Material MatFront
        {
            get
            {
                return CurrentGraphic.MatFront;
            }
        }

        public override Material MatSide
        {
            get
            {
                return CurrentGraphic.MatSide;
            }
        }

        public override Material MatBack
        {
            get
            {
                return CurrentGraphic.MatBack;
            }
        }

        public override bool ShouldDrawRotated
        {
            get
            {
                return true;
            }
        }

        public Graphic CurrentGraphic
        {
            get
            {
                return subGraphics[CurrentFrame];
            }
        }

        public int CurrentFrame
        {
            get
            {
                if (IsAnimating)
                    return ((int) (Time.fixedTime * subGraphics.Length / animationRate)) % subGraphics.Length;

                return 0;
            }
        }
    }
}
