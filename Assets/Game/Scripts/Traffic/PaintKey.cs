using System;

namespace WrongLine.Traffic
{
    /// <summary>
    /// Finds a car's body paint in a painted texture by its colour. A traffic car is drawn with its body in one flat, saturated
    /// magenta that no other part wears (glass is grey, lights are white, red and amber, tyres are black), so a texel's colour says how
    /// much of it is body paint; the game then paints it with the car's own colour. The shade (how dark the texel is) is kept, so the
    /// body's shading survives the change of colour.
    /// </summary>
    public static class PaintKey
    {
        /// <summary>The grey the key colour is turned into: light, so that the paint colour multiplied into it stays itself.</summary>
        public const float NeutralGrey = 0.8f;

        private const float FullKeyness = 0.55f;
        private const float NoKeyness = 0.15f;

        /// <summary>
        /// How much of the texel is body paint, 0 to 1. Magenta is red and blue high with green low: the measure is how far green
        /// lies below the lesser of red and blue, relative to the texel's brightness, so a dark magenta (a shaded body) keys as well as a bright one.
        /// </summary>
        public static float Weight(float red, float green, float blue)
        {
            float top = Math.Max(red, Math.Max(green, blue));
            if (top < 0.05f)
            {
                return 0f;
            }

            float keyness = (Math.Min(red, blue) - green) / top;
            return Smooth((keyness - NoKeyness) / (FullKeyness - NoKeyness));
        }

        /// <summary>
        /// The texel's shade: how bright it is against the key colour at full brightness (the lesser of red and blue stands for the
        /// magenta, which is lit equally in both). 0 to 1.
        /// </summary>
        public static float Shade(float red, float blue)
        {
            return Math.Min(1f, Math.Max(0f, Math.Min(red, blue)));
        }

        /// <summary>Where a texel's colour goes in the base texture: the key texel becomes a grey of its shade, the rest is unchanged.</summary>
        public static void NeutralColour(float red, float green, float blue, out float outRed, out float outGreen, out float outBlue)
        {
            float weight = Weight(red, green, blue);
            float grey = Shade(red, blue) * NeutralGrey;
            outRed = Lerp(red, grey, weight);
            outGreen = Lerp(green, grey, weight);
            outBlue = Lerp(blue, grey, weight);
        }

        private static float Smooth(float t)
        {
            t = Math.Min(1f, Math.Max(0f, t));
            return t * t * (3f - 2f * t);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
