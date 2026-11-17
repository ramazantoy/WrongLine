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

            Texture2D emission = WriteEmission();
            Material[] paints = MakePaints(emission);
            var prefabs = new TrafficCarView[Models.Length];
            for (int i = 0; i < Models.Length; i++)
            {
                prefabs[i] = MakePrefab(Models[i].Name, paints[0]);
            }

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

        private static TrafficCarView MakePrefab(string name, Material paint)
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

                var view = root.AddComponent<TrafficCarView>();
                var serialized = new SerializedObject(view);
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
