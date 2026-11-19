using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using WrongLine.Car;
using WrongLine.Car.Editor;

namespace WrongLine.Traffic.Editor
{
    /// <summary>
    /// Makes one level of detail of a traffic car from a Meshy model: the mesh turned into the car's frame (found from the painted
    /// lights and tyres, <see cref="CarFrameEstimate"/>), scaled to the car's length and set on the road; the texture split into the
    /// grey body, the paint mask (the key magenta, <see cref="PaintKey"/>) and the light mask; and the material that wears them.
    /// Inputs sit in <c>Traffic/Art/Meshy/&lt;Model&gt;/</c>: <c>&lt;Model&gt;_LOD&lt;n&gt;.fbx</c> and <c>&lt;Model&gt;_LOD&lt;n&gt;_Source.png</c>.
    /// </summary>
    public static class MeshyTrafficLevelBuilder
    {
        private const float DarkLuminance = 0.15f;

        /// <summary>A surface this far side-on is a wheel rim or a door, not a headlight (see <see cref="CarLightMaskBuilder"/>).</summary>
        private const float MaxHeadlightSideways = 0.85f;

        public sealed class Level
        {
            public Mesh Mesh;
            public Material Material;
            public string Report;
        }

        public static string Folder(string model)
        {
            return $"{TrafficBuilder.ART}/Meshy/{model}";
        }

        public static bool Has(string model, int level)
        {
            return File.Exists($"{Folder(model)}/{model}_LOD{level}.fbx") && File.Exists($"{Folder(model)}/{model}_LOD{level}_Source.png");
        }

        public static Level Build(string model, int level, float targetLength, float maxWidth, int textureSize)
        {
            string folder = Folder(model);
            string stem = $"{model}_LOD{level}";
            var report = new StringBuilder();

            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            source.LoadImage(File.ReadAllBytes($"{folder}/{stem}_Source.png"));

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{stem}.fbx");
            Mesh src = fbx.GetComponentInChildren<MeshFilter>().sharedMesh;
            Vector3[] vertices = src.vertices;
            Vector3[] normals = src.normals;
            Vector2[] uvs = src.uv;
            int[] triangles = src.triangles;

            // 1. Which way it faces, from the painted lights and the dark tyres.
            Bounds bounds = src.bounds;
            Vector3 headSum = Vector3.zero;
            Vector3 stopSum = Vector3.zero;
            Vector3 darkSum = Vector3.zero;
            int heads = 0;
            int stops = 0;
            int darks = 0;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 middle = (vertices[triangles[t]] + vertices[triangles[t + 1]] + vertices[triangles[t + 2]]) / 3f;
                Vector2 uv = (uvs[triangles[t]] + uvs[triangles[t + 1]] + uvs[triangles[t + 2]]) / 3f;
                Color texel = source.GetPixelBilinear(uv.x, uv.y);
                switch (LightMaskKey.Classify(texel.r, texel.g, texel.b))
                {
                    case LightPanel.Headlight:
                        headSum += middle;
                        heads++;
                        break;
                    case LightPanel.Stop:
                        stopSum += middle;
                        stops++;
                        break;
                    default:
                        if (texel.r * 0.3f + texel.g * 0.59f + texel.b * 0.11f < DarkLuminance)
                        {
                            darkSum += middle;
                            darks++;
                        }

                        break;
                }
            }

            report.Append($"{stem}: {triangles.Length / 3} tris, head {heads}, stop {stops}, dark {darks} triangles; ");
            if (heads == 0 || stops == 0 || darks == 0)
            {
                Object.DestroyImmediate(source);
                throw new System.InvalidOperationException($"{stem}: no {(heads == 0 ? "headlight " : string.Empty)}{(stops == 0 ? "brake light " : string.Empty)}{(darks == 0 ? "dark part " : string.Empty)}colour found in the texture; the model's frame cannot be told.");
            }

            if (!CarFrameEstimate.TryEstimate(bounds.center, bounds.size, headSum / heads, stopSum / stops, darkSum / darks, out Vector3 right, out Vector3 up, out Vector3 forward))
            {
                Object.DestroyImmediate(source);
                throw new System.InvalidOperationException($"{stem}: the lights and the tyres do not say which way the model faces.");
            }

            report.Append($"forward {forward}, up {up}; ");

            // 2. Into the car's frame, scaled to its length, centred on the pivot, wheels on the road.
            var local = new Vector3[vertices.Length];
            var localNormals = new Vector3[vertices.Length];
            var low = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var high = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < vertices.Length; i++)
            {
                local[i] = new Vector3(Vector3.Dot(vertices[i], right), Vector3.Dot(vertices[i], up), Vector3.Dot(vertices[i], forward));
                localNormals[i] = normals.Length == vertices.Length ? new Vector3(Vector3.Dot(normals[i], right), Vector3.Dot(normals[i], up), Vector3.Dot(normals[i], forward)) : Vector3.up;
                low = Vector3.Min(low, local[i]);
                high = Vector3.Max(high, local[i]);
            }

            // Scaled to the length, unless that makes it wider than a lane takes (a generated van or truck can be stubby).
            float scale = Mathf.Min(targetLength / (high.z - low.z), maxWidth / (high.x - low.x));
            var shift = new Vector3((low.x + high.x) * 0.5f, low.y, (low.z + high.z) * 0.5f);
            for (int i = 0; i < local.Length; i++)
            {
                local[i] = (local[i] - shift) * scale;
            }

            var mesh = new Mesh { name = stem };
            mesh.indexFormat = vertices.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = local;
            mesh.normals = localNormals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            report.Append($"size {mesh.bounds.size.ToString("F2")}; ");

            string meshPath = $"{TrafficBuilder.MESHES}/{stem}.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null)
            {
                AssetDatabase.DeleteAsset(meshPath);
            }

            AssetDatabase.CreateAsset(mesh, meshPath);
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

            // 3. The texture: the key magenta becomes grey (shaded as it was) and the paint mask; the rest is unchanged.
            int width = source.width;
            int height = source.height;
            Color32[] pixels = source.GetPixels32();
            var grey = new Color32[pixels.Length];
            var paintMask = new Color32[pixels.Length];
            int keyed = 0;
            int signals = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                float r = pixels[i].r / 255f;
                float g = pixels[i].g / 255f;
                float b = pixels[i].b / 255f;
                float weight = PaintKey.Weight(r, g, b);
                PaintKey.NeutralColour(r, g, b, out float nr, out float ng, out float nb);
                grey[i] = new Color32((byte)(nr * 255f + 0.5f), (byte)(ng * 255f + 0.5f), (byte)(nb * 255f + 0.5f), 255);
                byte w = (byte)(weight * 255f + 0.5f);
                paintMask[i] = new Color32(w, w, w, 255);
                if (weight > 0.5f)
                {
                    keyed++;
                }

                if (LightMaskKey.Classify(r, g, b) == LightPanel.Signal)
                {
                    signals++;
                }
            }

            report.Append($"paint {100f * keyed / pixels.Length:F0}% of the texture, signal texels {signals}; ");
            Object.DestroyImmediate(source);

            int ratio = Mathf.Max(1, width / textureSize);
            string basePath = $"{folder}/{stem}_Base.png";
            string paintPath = $"{folder}/{stem}_PaintMask.png";
            WritePng(basePath, Downsample(grey, width, height, ratio), width / ratio, height / ratio);
            WritePng(paintPath, Downsample(paintMask, width, height, ratio), width / ratio, height / ratio);
            ImportTexture(basePath, true);
            ImportTexture(paintPath, false);

            int size = width / ratio;
            string maskPath = $"{folder}/{stem}_LightMask.png";
            Texture2D lightMask = CarLightMaskBuilder.Build(mesh, basePath, maskPath, size, MaxHeadlightSideways);

            // 4. The material.
            string materialPath = $"{TrafficBuilder.MATERIALS}/Traffic_{stem}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("WrongLine/CurvedLit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.shader = Shader.Find("WrongLine/CurvedLit");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(basePath));
            material.SetTexture("_PaintMask", AssetDatabase.LoadAssetAtPath<Texture2D>(paintPath));
            material.SetTexture("_LightMask", lightMask);
            material.SetColor("_EmissionColor", new Color(1.6f, 1.6f, 1.6f, 1f));
            material.SetFloat("_PaintMaskOn", 1f);
            material.SetFloat("_LightMaskOn", 1f);
            material.SetFloat("_TrafficOn", 1f);
            material.SetFloat("_Cull", 0f);
            material.EnableKeyword("_PAINTMASK_ON");
            material.EnableKeyword("_LIGHTMASK_ON");
            material.EnableKeyword("_TRAFFIC_ON");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);

            return new Level { Mesh = mesh, Material = material, Report = report.ToString() };
        }

        /// <summary>The average of every <paramref name="ratio"/> by <paramref name="ratio"/> block (the sizes are powers of two).</summary>
        private static Color32[] Downsample(Color32[] pixels, int width, int height, int ratio)
        {
            if (ratio == 1)
            {
                return pixels;
            }

            int outWidth = width / ratio;
            int outHeight = height / ratio;
            var result = new Color32[outWidth * outHeight];
            int count = ratio * ratio;
            for (int y = 0; y < outHeight; y++)
            {
                for (int x = 0; x < outWidth; x++)
                {
                    int r = 0;
                    int g = 0;
                    int b = 0;
                    for (int dy = 0; dy < ratio; dy++)
                    {
                        for (int dx = 0; dx < ratio; dx++)
                        {
                            Color32 p = pixels[(y * ratio + dy) * width + x * ratio + dx];
                            r += p.r;
                            g += p.g;
                            b += p.b;
                        }
                    }

                    result[y * outWidth + x] = new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 255);
                }
            }

            return result;
        }

        private static void WritePng(string path, Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static void ImportTexture(string path, bool colour)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = colour;
            importer.mipmapEnabled = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.SaveAndReimport();
        }
    }
}
