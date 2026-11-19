using System.IO;
using UnityEditor;
using UnityEngine;

namespace WrongLine.Car.Editor
{
    /// <summary>
    /// Cuts the car's light mask out of its painted texture: every texel the model's UV covers is keyed by colour
    /// (<see cref="LightMaskKey"/>), and a signal panel goes to the left or the right channel by which side of the car its surface is on.
    /// Channels: R headlight, G stop light, B left signal, A right signal. The model's UV is a mosaic of small islands, so the mask is
    /// made by drawing each triangle into UV space rather than by cutting the texture itself.
    /// </summary>
    public static class CarLightMaskBuilder
    {
        /// <param name="body">The body mesh as the car uses it (the car's own axes: +X is its right).</param>
        /// <param name="maxSidewaysNormal">A surface facing sideways more than this (the normal's |x|, 0 to 1) is never a headlight: a silver wheel rim is as white as a lamp. 1 = no limit.</param>
        /// <param name="baseTexturePath">The painted texture's file.</param>
        /// <param name="maskPath">Where the mask PNG is written.</param>
        public static Texture2D Build(Mesh body, string baseTexturePath, string maskPath, int size, float maxSidewaysNormal = 1f)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(baseTexturePath));

            var pixels = new Color32[size * size];
            Vector3[] vertices = body.vertices;
            Vector2[] uv = body.uv;
            int[] triangles = body.triangles;

            for (int t = 0; t < triangles.Length; t += 3)
            {
                int ia = triangles[t];
                int ib = triangles[t + 1];
                int ic = triangles[t + 2];
                bool left = (vertices[ia].x + vertices[ib].x + vertices[ic].x) < 0f;
                Vector3[] normals = body.normals;
                bool sideways = normals.Length == vertices.Length && Mathf.Abs((normals[ia].x + normals[ib].x + normals[ic].x) / 3f) > maxSidewaysNormal;
                Draw(pixels, size, source, uv[ia] * size, uv[ib] * size, uv[ic] * size, left, sideways);
            }

            var mask = new Texture2D(size, size, TextureFormat.RGBA32, false);
            mask.SetPixels32(pixels);
            File.WriteAllBytes(maskPath, mask.EncodeToPNG());
            Object.DestroyImmediate(mask);
            Object.DestroyImmediate(source);

            AssetDatabase.ImportAsset(maskPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(maskPath);
            importer.sRGBTexture = false;
            importer.mipmapEnabled = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
        }

        private static void Draw(Color32[] pixels, int size, Texture2D source, Vector2 a, Vector2 b, Vector2 c, bool left, bool sideways)
        {
            float area = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
            if (Mathf.Abs(area) < 1e-6f)
            {
                return;
            }

            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float w0 = ((b.x - p.x) * (c.y - p.y) - (c.x - p.x) * (b.y - p.y)) / area;
                    float w1 = ((c.x - p.x) * (a.y - p.y) - (a.x - p.x) * (c.y - p.y)) / area;
                    float w2 = 1f - w0 - w1;
                    if (w0 < 0f || w1 < 0f || w2 < 0f)
                    {
                        continue;
                    }

                    Color texel = source.GetPixelBilinear(p.x / size, p.y / size);
                    LightPanel panel = LightMaskKey.Classify(texel.r, texel.g, texel.b);
                    if (panel == LightPanel.None || (panel == LightPanel.Headlight && sideways))
                    {
                        continue;
                    }

                    Color32 pixel = pixels[y * size + x];
                    switch (panel)
                    {
                        case LightPanel.Headlight:
                            pixel.r = 255;
                            break;
                        case LightPanel.Stop:
                            pixel.g = 255;
                            break;
                        default:
                            if (left)
                            {
                                pixel.b = 255;
                            }
                            else
                            {
                                pixel.a = 255;
                            }

                            break;
                    }

                    pixels[y * size + x] = pixel;
                }
            }
        }
    }
}
