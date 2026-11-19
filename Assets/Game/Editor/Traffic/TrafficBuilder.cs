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
            Texture2D signalMask = WriteSignalMask();
            Material bodyMaterial = MakeBodyMaterial(emission, signalMask);
            Material blobMaterial = MakeBlobMaterial();
            Mesh blobMesh = MakeBlobMesh();
            var prefabs = new TrafficCarView[Models.Length];
            for (int i = 0; i < Models.Length; i++)
            {
                prefabs[i] = MakePrefab(Models[i].Name, bodyMaterial, blobMaterial, blobMesh);
            }

            TrafficCatalog catalog = FillCatalog(prefabs);
            TrafficConfig config = EnsureConfig();
            Wire(catalog, config);
            DeleteObsolete();
            AssetDatabase.SaveAssets();
            Log.Info($"[Traffic] Rebuilt {Models.Length} models and {Paints.Length} paints.");
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

        /// <summary>The light mask without its headlight and stop channels: only the turn signals (B left, A right) are left.</summary>
        private static Texture2D WriteSignalMask()
        {
            string maskPath = $"{ART}/Traffic_LightMask.png";
            string outPath = $"{ART}/Traffic_SignalMask.png";
            var mask = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            mask.LoadImage(File.ReadAllBytes(maskPath));
            Color32[] pixels = mask.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].r = 0;
                pixels[i].g = 0;
            }

            var result = new Texture2D(mask.width, mask.height, TextureFormat.RGBA32, false, true);
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
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }

        /// <summary>
        /// The one material of the artist's models (a shared atlas): the paint colour and the turn signals come from each car's
        /// renderer user value (<c>_TRAFFIC_ON</c>), the headlights and brake lights from the emission map as before.
        /// </summary>
        private static Material MakeBodyMaterial(Texture2D emission, Texture2D signalMask)
        {
            string path = $"{MATERIALS}/TrafficBody.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(SHADER));
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find(SHADER);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{ART}/Traffic_Atlas.png"));
            material.SetTexture("_PaintMask", AssetDatabase.LoadAssetAtPath<Texture2D>($"{ART}/Traffic_PaintMask.png"));
            material.SetTexture("_EmissionMap", emission);
            material.SetTexture("_LightMask", signalMask);
            material.SetColor("_EmissionColor", Color.white);
            material.SetVector("_EmissionLevelMask", new Vector4(0f, 1f, 0f, 0f));
            material.SetFloat("_PaintMaskOn", 1f);
            material.SetFloat("_EmissionOn", 1f);
            material.SetFloat("_LightMaskOn", 1f);
            material.SetFloat("_TrafficOn", 1f);
            // Double-sided like the player's car: a one-layer body shows its inside through any open face (the truck's box).
            material.SetFloat("_Cull", 0f);
            material.EnableKeyword("_PAINTMASK_ON");
            material.EnableKeyword("_EMISSION");
            material.EnableKeyword("_LIGHTMASK_ON");
            material.EnableKeyword("_TRAFFIC_ON");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        /// <summary>What the paint materials and the lamp boxes left behind: the paint is a palette now, the signals a light mask.</summary>
        private static void DeleteObsolete()
        {
            for (int i = 0; i < 8; i++)
            {
                AssetDatabase.DeleteAsset($"{MATERIALS}/TrafficPaint_{i:00}.mat");
            }

            AssetDatabase.DeleteAsset($"{MATERIALS}/TrafficSignal.mat");
            AssetDatabase.DeleteAsset($"{MESHES}/SignalLamp.asset");
            AssetDatabase.DeleteAsset($"{ART}/Traffic_White.png");
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

        private static TrafficCarView MakePrefab(string name, Material body, Material blobMaterial, Mesh blobMesh)
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
                        renderer.sharedMaterial = body;
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
                Bounds bounds = levels[1].GetComponent<MeshFilter>().sharedMesh.bounds;
                var blob = new GameObject("Blob");
                blob.transform.SetParent(root.transform, false);
                blob.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                blob.transform.localScale = new Vector3(bounds.size.x * 1.3f, 1f, bounds.size.z * 1.2f);
                blob.AddComponent<MeshFilter>().sharedMesh = blobMesh;
                var blobRenderer = blob.AddComponent<MeshRenderer>();
                blobRenderer.sharedMaterial = blobMaterial;
                blobRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                blobRenderer.receiveShadows = false;
                blob.SetActive(false);

                var view = root.AddComponent<TrafficCarView>();
                var serialized = new SerializedObject(view);
                serialized.FindProperty("_blob").objectReferenceValue = blob;
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

        private static TrafficCatalog FillCatalog(TrafficCarView[] prefabs)
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

        /// <summary>The axles and the half track from the model's four wheel nodes (their pivots are the axle centres).</summary>
        private static void WriteAxles(SerializedProperty entry, string name)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ART}/{name}.fbx");
            Transform body = model.transform.Find($"{name}_LOD0");
            Vector3 frontLeft = body.Find($"{name}_LOD0_Wheel_FL").localPosition;
            Vector3 frontRight = body.Find($"{name}_LOD0_Wheel_FR").localPosition;
            Vector3 rearLeft = body.Find($"{name}_LOD0_Wheel_RL").localPosition;
            entry.FindPropertyRelative("_frontAxle").floatValue = frontLeft.z;
            entry.FindPropertyRelative("_rearAxle").floatValue = rearLeft.z;
            entry.FindPropertyRelative("_halfTrack").floatValue = Mathf.Abs(frontRight.x - frontLeft.x) * 0.5f;
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
