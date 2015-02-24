using System;

namespace A2B
{
    public static class MathUtilities
    {
        /**
         * Transforms a value in [0, 1] to a value in [min, max].
         **/
        public static float LinearTransform(float x, float min, float max)
        {
            return min + (max - min) * x;
        }

        /**
         * Transforms a value in [min, max] to a value in [0, 1].
         **/
        public static float LinearTransformInv(float x, float min, float max)
        {
            return (x - min) / (max - min);
        }
    }
}
