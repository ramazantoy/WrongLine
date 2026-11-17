using System.IO;
using UnityEditor;
using UnityEngine;

namespace WrongLine.Road.Editor
{
    /// <summary>
    /// The lane line texture, painted across the road: a texel's u says which line it belongs to (the nearest one). The line
    /// between the two directions is solid yellow, the lines between lanes of one direction are dashed white. The mesh keeps
    /// the contract's UVs (u across the asphalt, v along it), so the same texture serves every piece. Along v one repeat is one
    /// <see cref="RoadSection.LineDashPeriod"/>: the dash first, then the gap (transparent, cut by the material's alpha clip).
    /// </summary>
    public static class RoadLineTexture
    {
        public const string PATH = "Assets/Game/Road/Art/Road_Line_Base.png";
        public const int Width = 128;
        public const int Height = 256;

        public static readonly Color32 White = new Color32(255, 255, 255, 255);
        public static readonly Color32 Yellow = new Color32(255, 201, 60, 255);

        /// <summary>Rows (from the bottom) that are painted in a dashed line; the rest of the repeat is the gap.</summary>
        public static int DashRows(int height) => Mathf.RoundToInt(height * RoadSection.LineDashLength / RoadSection.LineDashPeriod);

        /// <summary>The u (0..1 across the asphalt) of lane line <paramref name="index"/>.</summary>
        public static float LineU(int index) => (RoadSection.LineX(index) + RoadSection.HalfWidth) / (2f * RoadSection.HalfWidth);

        /// <summary>Which line a column belongs to: the one whose u is nearest.</summary>
        public static int LineAt(float u)
        {
            int nearest = 0;
            for (int i = 1; i < RoadSection.LineCount; i++)
            {
                if (Mathf.Abs(u - LineU(i)) < Mathf.Abs(u - LineU(nearest)))
                {
                    nearest = i;
                }
            }

            return nearest;
        }

        /// <summary>The pixels, row by row from the bottom (the order of <c>Texture2D.SetPixels32</c>).</summary>
        public static Color32[] Build(int width, int height)
        {
            var pixels = new Color32[width * height];
            int dashRows = DashRows(height);
            for (int x = 0; x < width; x++)
            {
                bool center = LineAt((x + 0.5f) / width) == RoadSection.CenterLineIndex;
                Color32 paint = center ? Yellow : White;
                Color32 gap = new Color32(paint.r, paint.g, paint.b, 0); // The colour stays, so filtering does not darken the dash edge.
                for (int y = 0; y < height; y++)
                {
                    pixels[y * width + x] = center || y < dashRows ? paint : gap;
                }
            }

            return pixels;
        }

        [MenuItem("WrongLine/Road/Write Line Texture")]
        public static void Write()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(Build(Width, Height));
                File.WriteAllBytes(PATH, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(PATH, ImportAssetOptions.ForceUpdate);
        }
    }
}
