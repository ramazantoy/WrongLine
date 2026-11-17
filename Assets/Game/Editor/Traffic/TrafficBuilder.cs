using System.Collections.Generic;
using System.IO;
using AF.Core;
using UnityEditor;
using UnityEngine;

namespace WrongLine.Traffic.Editor
{
    /// <summary>
    /// <c>WrongLine/Traffic/Rebuild Traffic</c>: makes the traffic's assets from the artist's models in <c>Traffic/Art</c> and wires the
    /// root scope to them. Run it again when the artist's files change; nothing it writes is edited by hand.
    /// </summary>
    public static class TrafficBuilder
    {
        public const string ROOT = "Assets/Game/Traffic";
        public const string ART = ROOT + "/Art";
        public const string MATERIALS = ROOT + "/Materials";
        public const string PREFABS = ROOT + "/Prefabs";
        public const string MESHES = ROOT + "/Meshes";
        public const string CATALOG_PATH = ROOT + "/TrafficCatalog.asset";
        public const string CONFIG_PATH = ROOT + "/TrafficConfig.asset";
        public const string ROOT_SCOPE_PATH = "Assets/Game/Prefabs/RootScope.prefab";

        private const string SHADER = "WrongLine/CurvedLit";

        /// <summary>The model files, with how often each comes up.</summary>
        private static readonly (string Name, float Weight)[] Models =
        {
            ("Hatchback", 1f),
            ("Sedan", 1f),
            ("Van", 0.6f),
            ("Truck", 0.4f),
        };

        /// <summary>Paint colours, multiplied into the body's neutral light grey (the paint mask picks the body).</summary>
        private static readonly Color[] Paints =
        {
            new Color(0.80f, 0.80f, 0.83f),
            new Color(0.16f, 0.16f, 0.19f),
            new Color(0.72f, 0.10f, 0.10f),
            new Color(0.14f, 0.30f, 0.68f),
            new Color(0.92f, 0.76f, 0.16f),
            new Color(0.14f, 0.48f, 0.30f),
            new Color(0.92f, 0.50f, 0.14f),
            new Color(0.97f, 0.97f, 0.97f),
        };

        [MenuItem("WrongLine/Traffic/Rebuild Traffic")]
        public static void Rebuild()
        {
            EnsureFolder(ART);
            EnsureFolder(MATERIALS);
            EnsureFolder(PREFABS);

            EnsureFolder(MESHES);
            Texture2D emission = WriteEmission();
            Material[] paints = MakePaints(emission);
            Material blobMaterial = MakeBlobMaterial();
            Mesh blobMesh = MakeBlobMesh();
            Material signalMaterial = MakeSignalMaterial();
            Mesh lampMesh = MakeLampMesh();
            var mask = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            mask.LoadImage(File.ReadAllBytes($"{ART}/Traffic_LightMask.png"));
            var prefabs = new TrafficCarView[Models.Length];
            for (int i = 0; i < Models.Length; i++)
            {
                prefabs[i] = MakePrefab(Models[i].Name, paints[0], blobMaterial, blobMesh, signalMaterial, lampMesh, mask);
            }

            Object.DestroyImmediate(mask);

            TrafficCatalog catalog = FillCatalog(prefabs, paints);
            TrafficConfig config = EnsureConfig();
            Wire(catalog, config);
            AssetDatabase.SaveAssets();
            Log.Info($"[Traffic] Rebuilt {Models.Length} models and {paints.Length} paints.");
        }

        /// <summary>
        /// The light mask keeps one light per channel (R headlights, G brake lights, B and A the signals). The shader's emission
        /// map is a colour, so the headlights are made white and the brake lights red here; both light with the headlights.
        /// </summary>
        private static Texture2D WriteEmission()
        {
            string maskPath = $"{ART}/Traffic_LightMask.png";
            string outPath = $"{ART}/Traffic_Emission.png";
            var mask = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            mask.LoadImage(File.ReadAllBytes(maskPath));
            Color32[] pixels = mask.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                float head = pixels[i].r / 255f;
                float stop = pixels[i].g / 255f;
                var color = new Color(
                    Mathf.Clamp01(head + stop),
                    Mathf.Clamp01(head * 0.96f + stop * 0.05f),
                    Mathf.Clamp01(head * 0.80f + stop * 0.03f),
                    1f);
                pixels[i] = color;
            }

            var result = new Texture2D(mask.width, mask.height, TextureFormat.RGBA32, false, false);
            result.SetPixels32(pixels);
            byte[] png = result.EncodeToPNG();
            Object.DestroyImmediate(mask);
            Object.DestroyImmediate(result);

            if (!File.Exists(outPath) || !FileEquals(outPath, png))
            {
                File.WriteAllBytes(outPath, png);
            }

            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(outPath);
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }

        private static bool FileEquals(string path, byte[] bytes)
        {
            byte[] existing = File.ReadAllBytes(path);
            if (existing.Length != bytes.Length)
            {
                return false;
            }

            for (int i = 0; i < bytes.Length; i++)
            {
                if (existing[i] != bytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static Material[] MakePaints(Texture2D emission)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ART}/Traffic_Atlas.png");
            var paintMask = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ART}/Traffic_PaintMask.png");
            Shader shader = Shader.Find(SHADER);
            var materials = new Material[Paints.Length];
            for (int i = 0; i < Paints.Length; i++)
            {
                string path = $"{MATERIALS}/TrafficPaint_{i:00}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }

                material.shader = shader;
                material.SetTexture("_BaseMap", atlas);
                material.SetTexture("_PaintMask", paintMask);
                material.SetTexture("_EmissionMap", emission);
                material.SetColor("_PaintColor", Paints[i]);
                material.SetColor("_EmissionColor", Color.white);
                material.SetVector("_EmissionLevelMask", new Vector4(0f, 1f, 0f, 0f));
                material.SetFloat("_PaintMaskOn", 1f);
                material.SetFloat("_EmissionOn", 1f);
                // Double-sided like the player's car: a one-layer body shows its inside through any open face (the truck's box).
                material.SetFloat("_Cull", 0f);
                material.EnableKeyword("_PAINTMASK_ON");
                material.EnableKeyword("_EMISSION");
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
                materials[i] = material;
            }

            return materials;
        }

        /// <summary>A soft black disc fading to nothing at the edge (alpha is the shadow).</summary>
        private static Material MakeBlobMaterial()
        {
            const int Size = 64;
            string texturePath = $"{ART}/Traffic_Blob.png";
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x + 0.5f) / Size * 2f - 1f;
                    float dy = (y + 0.5f) / Size * 2f - 1f;
                    float t = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * Size + x] = new Color(0f, 0f, 0f, t * t * (3f - 2f * t));
                }
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(pixels);
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            if (!File.Exists(texturePath) || !FileEquals(texturePath, png))
            {
                File.WriteAllBytes(texturePath, png);
            }

            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();

            string path = $"{MATERIALS}/TrafficBlob.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(SHADER));
                AssetDatabase.CreateAsset(material, path);
            }

            // Like CarGlass: alpha blended, no depth write, no shadow or depth pass. The offset keeps it in front of the asphalt.
            material.shader = Shader.Find(SHADER);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0.6f));
            material.SetFloat("_SrcBlend", 5f);
            material.SetFloat("_DstBlend", 10f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_OffsetFactor", -1f);
            material.SetFloat("_OffsetUnits", -2f);
            material.renderQueue = 3000;
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("DepthOnly", false);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static void FillObjects(SerializedProperty list, List<GameObject> objects)
        {
            list.arraySize = objects.Count;
            for (int i = 0; i < objects.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
            }
        }

        private static GameObject MakeLamp(Transform parent, string name, Vector3 at, Material material, Mesh mesh)
        {
            var lamp = new GameObject(name);
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = at;
            lamp.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = lamp.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            lamp.SetActive(false);
            return lamp;
        }

        /// <summary>
        /// Where a signal lamp goes: the middle of the body's vertices whose light-mask texel is lit in the signal's channel (blue is
        /// the left signal, alpha the right), at the front (z above 0) or the rear.
        /// </summary>
        private static bool TryLampPosition(Mesh body, Texture2D mask, bool left, bool front, out Vector3 position)
        {
            Vector3[] vertices = body.vertices;
            Vector2[] uvs = body.uv;
            int[] triangles = body.triangles;
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];
                Vector3 middle = (vertices[a] + vertices[b] + vertices[c]) / 3f;
                if (front != (middle.z > 0f))
                {
                    continue;
                }

                // The texel under the triangle's middle, and under each corner pulled towards it.
                Vector2 uv = (uvs[a] + uvs[b] + uvs[c]) / 3f;
                Color texel = mask.GetPixelBilinear(uv.x, uv.y);
                if ((left ? texel.b : texel.a) > 0.5f)
                {
                    sum += middle;
                    count++;
                }
            }

            position = count > 0 ? sum / count : Vector3.zero;
            return count > 0;
        }

        /// <summary>Where a signal lamp goes when the mask has none: a rear/front corner of the body, at a headlight's height.</summary>
        private static Vector3 CornerLamp(Mesh body, bool left, bool front)
        {
            Bounds bounds = body.bounds;
            return new Vector3(
                (left ? -1f : 1f) * (bounds.extents.x - 0.22f),
                Mathf.Min(0.8f, bounds.max.y * 0.45f),
                (front ? 1f : -1f) * (bounds.extents.z - 0.04f));
        }

        /// <summary>A small box (0.22 x 0.14 x 0.10 m) with its own normals on every face.</summary>
        private static Mesh MakeLampMesh()
        {
            string path = $"{MESHES}/SignalLamp.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = "SignalLamp" };
                AssetDatabase.CreateAsset(mesh, path);
            }

            var half = new Vector3(0.11f, 0.07f, 0.05f);
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 normal = axes[axis] * sign;
                    Vector3 u = axes[(axis + 1) % 3];
                    Vector3 v = axes[(axis + 2) % 3];
                    int first = vertices.Count;
                    foreach (Vector2 corner in new[] { new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1) })
                    {
                        Vector3 offset = normal + u * corner.x + v * corner.y;
                        vertices.Add(Vector3.Scale(offset, half));
                        normals.Add(normal);
                        uvs.Add(new Vector2(0.5f, 0.5f));
                    }

                    // Wound so the face looks along its normal: flip when the corner order runs the other way round.
                    bool flip = Vector3.Dot(Vector3.Cross(u, v), normal) < 0f;
                    triangles.AddRange(flip ? new[] { first, first + 2, first + 1, first, first + 3, first + 2 } : new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(mesh);
            return mesh;
        }

        /// <summary>Amber, lit at any hour: the fourth light level is always 1, and this material picks only it.</summary>
        private static Material MakeSignalMaterial()
        {
            string texturePath = $"{ART}/Traffic_White.png";
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false, false);
            var white = new Color32[16];
            for (int i = 0; i < white.Length; i++)
            {
                white[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(white);
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            if (!File.Exists(texturePath) || !FileEquals(texturePath, png))
            {
                File.WriteAllBytes(texturePath, png);
            }

            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);

            string path = $"{MATERIALS}/TrafficSignal.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(SHADER));
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find(SHADER);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", new Color(1f, 0.55f, 0.05f, 1f));
            material.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.08f, 1f));
            material.SetVector("_EmissionLevelMask", new Vector4(0f, 0f, 0f, 1f));
            material.SetFloat("_EmissionOn", 1f);
            material.SetFloat("_Cull", 2f);
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        /// <summary>A unit square lying flat (x and z from -0.5 to 0.5), facing up.</summary>
        private static Mesh MakeBlobMesh()
        {
            string path = $"{MESHES}/BlobQuad.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = "BlobQuad" };
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();
            mesh.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(mesh);
            return mesh;
        }

        private static TrafficCarView MakePrefab(string name, Material paint, Material blobMaterial, Mesh blobMesh, Material signalMaterial, Mesh lampMesh, Texture2D mask)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ART}/{name}.fbx");
            var root = new GameObject($"Traffic_{name}");
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);

                // Unity makes a LODGroup of the *_LOD0/1/2 nodes on import. It would cull the levels by screen size over the
                // distance rule's head (the near levels vanished), so it is switched off: TrafficLodRule picks the level.
                foreach (LODGroup group in instance.GetComponentsInChildren<LODGroup>(true))
                {
                    group.enabled = false;
                }

                var levels = new GameObject[TrafficCarView.LevelCount];
                for (int level = 0; level < levels.Length; level++)
                {
                    Transform part = instance.transform.Find($"{name}_LOD{level}");
                    levels[level] = part.gameObject;
                    foreach (MeshRenderer renderer in part.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        renderer.sharedMaterial = paint;
                        // Only the nearest level throws a shadow: the others are too far for it to show.
                        renderer.shadowCastingMode = level == 0 ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                        renderer.receiveShadows = true;
                    }

                    part.gameObject.SetActive(false);
                }

                // The far level has no wheels, so its lowest point is above the road: sink it until it stands on it.
                Transform far = levels[2].transform;
                far.localPosition = new Vector3(0f, -far.GetComponent<MeshFilter>().sharedMesh.bounds.min.y, 0f);

                // The contact shadow, sized from the near level's mesh.
                Bounds body = levels[1].GetComponent<MeshFilter>().sharedMesh.bounds;
                var blob = new GameObject("Blob");
                blob.transform.SetParent(root.transform, false);
                blob.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                blob.transform.localScale = new Vector3(body.size.x * 1.3f, 1f, body.size.z * 1.2f);
                blob.AddComponent<MeshFilter>().sharedMesh = blobMesh;
                var blobRenderer = blob.AddComponent<MeshRenderer>();
                blobRenderer.sharedMaterial = blobMaterial;
                blobRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                blobRenderer.receiveShadows = false;
                blob.SetActive(false);

                // The turn signal lamps, where the light mask has them (blue: left, alpha: right), at the front and the rear.
                var signals = new GameObject("Signals");
                signals.transform.SetParent(root.transform, false);
                Mesh bodyMesh = levels[0].GetComponent<MeshFilter>().sharedMesh;
                var left = new List<GameObject>();
                var right = new List<GameObject>();
                foreach (bool front in new[] { true, false })
                {
                    string end = front ? "F" : "R";
                    if (!TryLampPosition(bodyMesh, mask, true, front, out Vector3 leftAt))
                    {
                        leftAt = CornerLamp(bodyMesh, true, front);
                        Log.Warning($"[Traffic] {name}: the light mask has no left signal at the {end}; a lamp is put on the corner.");
                    }

                    if (!TryLampPosition(bodyMesh, mask, false, front, out Vector3 rightAt))
                    {
                        rightAt = CornerLamp(bodyMesh, false, front);
                        Log.Warning($"[Traffic] {name}: the light mask has no right signal at the {end}; a lamp is put on the corner.");
                    }

                    left.Add(MakeLamp(signals.transform, "Signal_L_" + end, leftAt, signalMaterial, lampMesh));
                    right.Add(MakeLamp(signals.transform, "Signal_R_" + end, rightAt, signalMaterial, lampMesh));
                }

                Log.Info($"[Traffic] {name}: {left.Count} left and {right.Count} right signal lamps.");

                var view = root.AddComponent<TrafficCarView>();
                var serialized = new SerializedObject(view);
                serialized.FindProperty("_blob").objectReferenceValue = blob;
                serialized.FindProperty("_signalRoot").objectReferenceValue = signals.transform;
                FillObjects(serialized.FindProperty("_leftLamps"), left);
                FillObjects(serialized.FindProperty("_rightLamps"), right);
                SerializedProperty list = serialized.FindProperty("_levels");
                list.arraySize = levels.Length;
                for (int i = 0; i < levels.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                string path = $"{PREFABS}/Traffic_{name}.prefab";
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                return saved.GetComponent<TrafficCarView>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static TrafficCatalog FillCatalog(TrafficCarView[] prefabs, Material[] paints)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TrafficCatalog>(CATALOG_PATH);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<TrafficCatalog>();
                AssetDatabase.CreateAsset(catalog, CATALOG_PATH);
            }

            float[] lengths = { 3.88f, 4.58f, 5.24f, 7.72f };
            var serialized = new SerializedObject(catalog);
            SerializedProperty models = serialized.FindProperty("_models");
            models.arraySize = Models.Length;
            for (int i = 0; i < Models.Length; i++)
            {
                SerializedProperty entry = models.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_name").stringValue = Models[i].Name;
                entry.FindPropertyRelative("_prefab").objectReferenceValue = prefabs[i];
                entry.FindPropertyRelative("_length").floatValue = lengths[i];
                entry.FindPropertyRelative("_weight").floatValue = Models[i].Weight;
            }

            SerializedProperty paintList = serialized.FindProperty("_paints");
            paintList.arraySize = paints.Length;
            for (int i = 0; i < paints.Length; i++)
            {
                paintList.GetArrayElementAtIndex(i).objectReferenceValue = paints[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return catalog;
        }

        private static TrafficConfig EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<TrafficConfig>(CONFIG_PATH);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<TrafficConfig>();
                AssetDatabase.CreateAsset(config, CONFIG_PATH);
                AssetDatabase.SaveAssetIfDirty(config);
            }

            return config;
        }

        private static void Wire(TrafficCatalog catalog, TrafficConfig config)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(ROOT_SCOPE_PATH);
            try
            {
                // The editor assembly does not reference VContainer, so the scope is found by its type name.
                Component scope = null;
                foreach (Component component in contents.GetComponents<Component>())
                {
                    if (component != null && component.GetType().Name == "GameRootScope")
                    {
                        scope = component;
                    }
                }

                if (scope == null)
                {
                    throw new System.InvalidOperationException($"{ROOT_SCOPE_PATH} has no GameRootScope.");
                }

                var serialized = new SerializedObject(scope);
                serialized.FindProperty("_trafficCatalog").objectReferenceValue = catalog;
                serialized.FindProperty("_trafficConfig").objectReferenceValue = config;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, ROOT_SCOPE_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            int split = folder.LastIndexOf('/');
            EnsureFolder(folder.Substring(0, split));
            AssetDatabase.CreateFolder(folder.Substring(0, split), folder.Substring(split + 1));
        }
    }
}
