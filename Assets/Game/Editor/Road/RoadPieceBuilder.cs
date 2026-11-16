using System;
using System.Collections.Generic;
using AF.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace WrongLine.Road.Editor
{
    /// <summary>
    /// Rebuilds every road piece from its recipe asset: the mesh, the prefab and the catalog. Idempotent: running it twice
    /// changes no file. Recipe assets that are missing are created from <see cref="RoadPieceSpecs"/>; existing ones are never
    /// overwritten, so Inspector tweaks survive.
    /// </summary>
    public static class RoadPieceBuilder
    {
        private const string ROOT = "Assets/Game/Road";
        private const string RECIPES = ROOT + "/Recipes";
        private const string MESHES = ROOT + "/Meshes";
        private const string PREFABS = ROOT + "/Prefabs";
        private const string MATERIALS = ROOT + "/Materials";
        private const string CATALOG_PATH = ROOT + "/RoadCatalog.asset";
        private const string LIT_SHADER = "Universal Render Pipeline/Lit";
        private const int MeshRingStride = 2;

        private static readonly (string Name, Color Color)[] _materialSpecs =
        {
            ("RoadAsphalt", new Color(0.18f, 0.18f, 0.2f)),
            ("RoadCurb", new Color(0.62f, 0.62f, 0.6f)),
            ("RoadLine", new Color(0.92f, 0.92f, 0.88f)),
            ("RoadBody", new Color(0.32f, 0.32f, 0.34f)),
        };

        [MenuItem("WrongLine/Road/Rebuild Pieces")]
        public static void Rebuild()
        {
            EnsureFolders();
            Material[] materials = EnsureMaterials();
            EnsureRecipes();

            var recipes = new List<RoadPieceRecipe>();
            foreach (string guid in AssetDatabase.FindAssets("t:RoadPieceRecipe", new[] { RECIPES }))
            {
                recipes.Add(AssetDatabase.LoadAssetAtPath<RoadPieceRecipe>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            recipes.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            var views = new List<RoadPieceView>();
            RoadPieceView start = null;
            foreach (RoadPieceRecipe recipe in recipes)
            {
                RoadPieceView view = BuildPiece(recipe, materials);
                if (recipe.IsStart)
                {
                    start = view;
                }
                else
                {
                    views.Add(view);
                }
            }

            WriteCatalog(start, views);
            Log.Info($"[Road] Rebuilt {recipes.Count} pieces ({views.Count} selectable, start: {(start != null ? start.name : "none")}).");
        }

        private static RoadPieceView BuildPiece(RoadPieceRecipe recipe, Material[] materials)
        {
            TrackShape shape = recipe.Shape;
            bool straight = (recipe.Tags & ~RoadTags.Straight) == RoadTags.None;
            Mesh built = RoadMeshBuilder.Build(shape, straight ? shape.Count - 1 : MeshRingStride);
            built.name = recipe.name;

            Mesh mesh = WriteMesh($"{MESHES}/{recipe.name}.asset", built);
            if (mesh != built)
            {
                UnityEngine.Object.DestroyImmediate(built); // Copied into the existing asset; a new asset adopted `built` itself.
            }

            float gap = RoadMeshBuilder.ExitGap(mesh, shape);
            Log.Info($"[Road] {recipe.name}: {recipe.Recipe.Length} m, {shape.Count} frames, {mesh.vertexCount} vertices, tags {recipe.Tags}, exit {shape.Exit.Position}, mesh-to-exit gap {gap:F4} m.");

            var root = new GameObject(recipe.name);
            try
            {
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

                var view = root.AddComponent<RoadPieceView>();
                var serialized = new SerializedObject(view);
                serialized.FindProperty("_recipe").objectReferenceValue = recipe;
                serialized.FindProperty("_bakedHash").intValue = recipe.Recipe.StableHash();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PREFABS}/{recipe.name}.prefab");
                return prefab.GetComponent<RoadPieceView>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Mesh WriteMesh(string path, Mesh source)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(source, path);
                AssetDatabase.SaveAssetIfDirty(source);
                return source;
            }

            existing.Clear();
            EditorUtility.CopySerialized(source, existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            return existing;
        }

        private static void WriteCatalog(RoadPieceView start, List<RoadPieceView> pieces)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RoadCatalog>(CATALOG_PATH);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RoadCatalog>();
                AssetDatabase.CreateAsset(catalog, CATALOG_PATH);
            }

            var serialized = new SerializedObject(catalog);
            serialized.FindProperty("_start").objectReferenceValue = start;
            SerializedProperty list = serialized.FindProperty("_pieces");
            list.arraySize = pieces.Count;
            for (int i = 0; i < pieces.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = pieces[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(catalog);
        }

        private static void EnsureRecipes()
        {
            foreach (RoadPieceSpecs.Spec spec in RoadPieceSpecs.Defaults)
            {
                string path = $"{RECIPES}/{spec.Name}.asset";
                if (AssetDatabase.LoadAssetAtPath<RoadPieceRecipe>(path) != null)
                {
                    continue;
                }

                var recipe = ScriptableObject.CreateInstance<RoadPieceRecipe>();
                AssetDatabase.CreateAsset(recipe, path);

                var serialized = new SerializedObject(recipe);
                serialized.FindProperty("_length").floatValue = spec.Recipe.Length;
                serialized.FindProperty("_yawDegrees").floatValue = spec.Recipe.YawDegrees;
                serialized.FindProperty("_yawProfile").enumValueIndex = (int)spec.Recipe.YawProfile;
                serialized.FindProperty("_pitchDegrees").floatValue = spec.Recipe.PitchDegrees;
                serialized.FindProperty("_pitchProfile").enumValueIndex = (int)spec.Recipe.PitchProfile;
                serialized.FindProperty("_bankDegrees").floatValue = spec.Recipe.BankDegrees;
                serialized.FindProperty("_isStart").boolValue = spec.IsStart;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(recipe);
            }
        }

        private static Material[] EnsureMaterials()
        {
            var materials = new Material[_materialSpecs.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                string path = $"{MATERIALS}/{_materialSpecs[i].Name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    Shader shader = Shader.Find(LIT_SHADER);
                    if (shader == null)
                    {
                        throw new InvalidOperationException($"Shader '{LIT_SHADER}' not found.");
                    }

                    material = new Material(shader);
                    material.SetColor("_BaseColor", _materialSpecs[i].Color);
                    material.SetFloat("_Smoothness", 0.1f);
                    AssetDatabase.CreateAsset(material, path);
                    AssetDatabase.SaveAssetIfDirty(material);
                }

                materials[i] = material;
            }

            return materials;
        }

        private static void EnsureFolders()
        {
            foreach (string folder in new[] { ROOT, RECIPES, MESHES, PREFABS, MATERIALS })
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                int split = folder.LastIndexOf('/');
                AssetDatabase.CreateFolder(folder.Substring(0, split), folder.Substring(split + 1));
            }
        }
    }
}
