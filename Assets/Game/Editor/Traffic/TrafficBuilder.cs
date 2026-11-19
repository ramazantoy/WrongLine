using System.IO;
using AF.Core;
using UnityEditor;
using UnityEngine;

namespace WrongLine.Traffic.Editor
{
    /// <summary>
    /// <c>WrongLine/Traffic/Rebuild Traffic</c>: makes the traffic's assets from the Meshy models in <c>Traffic/Art/Meshy</c> and wires the
    /// root scope to them. Run it again when a model's files change; nothing it writes is edited by hand.
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

        /// <summary>Bumper to bumper, per model, in metres (the catalog's length; a Meshy model is scaled to it).</summary>
        private static readonly float[] Lengths = { 3.88f, 4.58f, 5.24f, 7.72f };

        /// <summary>The widest a Meshy model may be, per model, in metres (the lane is 3 m wide).</summary>
        private static readonly float[] MaxWidths = { 2.1f, 2.1f, 2.2f, 2.5f };

        /// <summary>The length each Meshy model came out at (after the width limit), for the catalog.</summary>
        private static readonly System.Collections.Generic.Dictionary<string, float> MeshyLengths = new System.Collections.Generic.Dictionary<string, float>();

        /// <summary>The texture size of each level of a Meshy model.</summary>
        private static readonly int[] MeshyTextureSizes = { 1024, 512, 512 };

        /// <summary>The axles of the Meshy models made in this run (they have no wheel nodes to read them from).</summary>
        private static readonly System.Collections.Generic.Dictionary<string, WheelContactEstimate.Result> MeshyAxles = new System.Collections.Generic.Dictionary<string, WheelContactEstimate.Result>();

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
            Material blobMaterial = MakeBlobMaterial();
            Mesh blobMesh = MakeBlobMesh();
            var prefabs = new TrafficCarView[Models.Length];
            for (int i = 0; i < Models.Length; i++)
            {
                prefabs[i] = MakeMeshyPrefab(Models[i].Name, Lengths[i], MaxWidths[i], blobMaterial, blobMesh);
            }

            TrafficCatalog catalog = FillCatalog(prefabs);
            TrafficConfig config = EnsureConfig();
            Wire(catalog, config);
            AssetDatabase.SaveAssets();

            // A prefab saved in the same run as the meshes it points at can be imported before them and keep empty references.
            foreach (TrafficCarView prefab in prefabs)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(prefab), ImportAssetOptions.ForceUpdate);
            }

            Log.Info($"[Traffic] Rebuilt {Models.Length} models and {Paints.Length} paints.");
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

        /// <summary>The contact shadow, the view component and the prefab asset, for a car whose levels are made.</summary>
        private static TrafficCarView FinishPrefab(GameObject root, string name, GameObject[] levels, Bounds body, Material blobMaterial, Mesh blobMesh)
        {
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

            // One spot light at the nose (settings, colour and range come at run time from the lights config).
            var lamp = new GameObject("Headlight");
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localPosition = new Vector3(0f, Mathf.Clamp(body.size.y * 0.25f, 0.55f, 0.95f), body.max.z - 0.15f);
            lamp.transform.localRotation = Quaternion.Euler(3f, 0f, 0f);
            var headlight = lamp.AddComponent<Light>();
            headlight.type = LightType.Spot;
            headlight.shadows = LightShadows.None;
            headlight.enabled = false;

            var view = root.AddComponent<TrafficCarView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_blob").objectReferenceValue = blob;
            serialized.FindProperty("_headlight").objectReferenceValue = headlight;
            SerializedProperty list = serialized.FindProperty("_levels");
            list.arraySize = levels.Length;
            for (int i = 0; i < levels.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, $"{PREFABS}/Traffic_{name}.prefab");
            return saved.GetComponent<TrafficCarView>();
        }

        /// <summary>
        /// A car from a Meshy model: one mesh and one material per level (the wheels are part of the mesh, so the axles are read off its
        /// tyre contacts). A level without a file of its own (not made yet) shows the level above it.
        /// </summary>
        private static TrafficCarView MakeMeshyPrefab(string name, float length, float maxWidth, Material blobMaterial, Mesh blobMesh)
        {
            var root = new GameObject($"Traffic_{name}");
            try
            {
                var levels = new GameObject[TrafficCarView.LevelCount];
                MeshyTrafficLevelBuilder.Level previous = null;
                for (int level = 0; level < levels.Length; level++)
                {
                    MeshyTrafficLevelBuilder.Level made = MeshyTrafficLevelBuilder.Has(name, level)
                        ? MeshyTrafficLevelBuilder.Build(name, level, length, maxWidth, MeshyTextureSizes[level])
                        : previous;
                    if (made == null)
                    {
                        throw new System.InvalidOperationException($"{name}: the nearest level has no Meshy model.");
                    }

                    Log.Info($"[Traffic] {made.Report}");
                    previous = made;

                    var part = new GameObject($"{name}_LOD{level}");
                    part.transform.SetParent(root.transform, false);
                    part.AddComponent<MeshFilter>().sharedMesh = made.Mesh;
                    var renderer = part.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = made.Material;
                    // Only the nearest level throws a shadow: the others are too far for it to show.
                    renderer.shadowCastingMode = level == 0 ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = true;
                    part.SetActive(false);
                    levels[level] = part;
                }

                Mesh near = levels[0].GetComponent<MeshFilter>().sharedMesh;
                if (!WheelContactEstimate.TryEstimate(near.vertices, 0.05f, out WheelContactEstimate.Result axles))
                {
                    Bounds b = near.bounds;
                    axles = new WheelContactEstimate.Result(b.extents.z * 0.62f, -b.extents.z * 0.62f, b.extents.x * 0.85f);
                    Log.Warning($"[Traffic] {name}: no four tyre contacts found near the road; the axles are guessed from the size.");
                }

                MeshyAxles[name] = axles;
                MeshyLengths[name] = near.bounds.size.z;
                Log.Info($"[Traffic] {name}: axles {axles.FrontAxle:F2} / {axles.RearAxle:F2}, half track {axles.HalfTrack:F2}.");
                return FinishPrefab(root, name, levels, levels[0].GetComponent<MeshFilter>().sharedMesh.bounds, blobMaterial, blobMesh);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static TrafficCatalog FillCatalog(TrafficCarView[] prefabs)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TrafficCatalog>(CATALOG_PATH);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<TrafficCatalog>();
                AssetDatabase.CreateAsset(catalog, CATALOG_PATH);
            }

            var serialized = new SerializedObject(catalog);
            SerializedProperty models = serialized.FindProperty("_models");
            models.arraySize = Models.Length;
            for (int i = 0; i < Models.Length; i++)
            {
                SerializedProperty entry = models.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_name").stringValue = Models[i].Name;
                entry.FindPropertyRelative("_prefab").objectReferenceValue = prefabs[i];
                entry.FindPropertyRelative("_length").floatValue = MeshyLengths.TryGetValue(Models[i].Name, out float made) ? made : Lengths[i];
                entry.FindPropertyRelative("_weight").floatValue = Models[i].Weight;
                WriteAxles(entry, Models[i].Name);
            }

            SerializedProperty paintList = serialized.FindProperty("_paintColors");
            paintList.arraySize = Paints.Length;
            for (int i = 0; i < Paints.Length; i++)
            {
                paintList.GetArrayElementAtIndex(i).colorValue = Paints[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return catalog;
        }

        /// <summary>The axles and the half track the model's tyre contacts gave (see <see cref="MakeMeshyPrefab"/>).</summary>
        private static void WriteAxles(SerializedProperty entry, string name)
        {
            WheelContactEstimate.Result axles = MeshyAxles[name];
            entry.FindPropertyRelative("_frontAxle").floatValue = axles.FrontAxle;
            entry.FindPropertyRelative("_rearAxle").floatValue = axles.RearAxle;
            entry.FindPropertyRelative("_halfTrack").floatValue = axles.HalfTrack;
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
