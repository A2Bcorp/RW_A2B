using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace A2B
{
    public class AnimatedGraphic : Graphic_Collection
    {
		public static float animationRate = 1.0f;
        private bool isAnimating = true;
        private int defaultFrame = 0;

        public static AnimatedGraphic FromSingleFrame(Graphic frame)
        {
            int finalSlash = frame.path.LastIndexOf('/');
            string path = frame.path.Substring(0, finalSlash);

            AnimatedGraphic anim = (AnimatedGraphic) GraphicDatabase.Get<AnimatedGraphic>(path, frame.Shader, frame.drawSize, frame.color, frame.colorTwo);
            anim.DefaultFrame = Array.FindIndex(anim.subGraphics, row => row == frame);
            return anim;
        }

        // Unfortunately the method used to populate a Graphic_Collection does not actually put the graphics in order
        // based on their filename. It's pretty difficult to make an interesting animation where the order of the frames
        // doesn't matter, so we have to fix this ourselves.

        public override void Init(GraphicRequest req)
        {
            List<string> files = Directory.GetFiles(Path.Combine(ModUtilities.GetTexturePath(), req.path)).ToList();
            files.Sort((a, b) => a.CompareTo(b));

            subGraphics = new Graphic_Single[files.Count];
            path = req.path;
            //Shader = req.shader;
            color = req.color;
            drawSize = req.drawSize;

            int i = 0;
            foreach (string file in files)
            {
                string fileRelative = req.path + "/" + Path.GetFileNameWithoutExtension(file);
                subGraphics[i++] = GraphicDatabase.Get<Graphic_Single>(fileRelative, req.shader, drawSize, color);
            }
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return CurrentGraphic.GetColoredVersion(newShader, newColor, newColorTwo);
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

        public Graphic DefaultGraphic
        {
            get
            {
                return subGraphics[DefaultFrame];
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

				return defaultFrame;
            }
        }
    }
}
