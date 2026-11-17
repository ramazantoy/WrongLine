using System;
using System.Collections.Generic;
using AF.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using WrongLine.Scenery;
using WrongLine.Scenery.Editor;

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
        private const string ART = ROOT + "/Art";
        private const string ROAD_SHADER = "WrongLine/CurvedLit";
        private const string GROUND_CHILD = "Ground";
        private const string GROUND_MATERIAL_PATH = MATERIALS + "/Ground.mat";
        private const int MeshRingStride = 2;

        // Colour is the fallback while the slot has no texture in Art/.
        // The lane line's texture holds one dash and one gap with a transparent gap: it is cut by alpha and tiled so one repeat
        // covers LineDashPeriod (the mesh UV repeats every TileLength, so the material scales v by their ratio).
        /// <summary>The longest slice a flat straight keeps, in metres: with rings this far apart the bend's sag between them is ~0.6 cm (Strength 0.0004).</summary>
        private const float RefineStepMeters = 8f;

        private static readonly (string Name, Color Color, string Texture, bool AlphaClip, float TilingV)[] _materialSpecs =
        {
            ("RoadAsphalt", new Color(0.18f, 0.18f, 0.2f), "Road_Asphalt_Base", false, 1f),
            ("RoadCurb", new Color(0.62f, 0.62f, 0.6f), "Road_Curb_Base", false, 1f),
            ("RoadLine", new Color(0.92f, 0.92f, 0.88f), "Road_Line_Base", true, RoadSection.TileLength / RoadSection.LineDashPeriod),
            ("RoadBody", new Color(0.32f, 0.32f, 0.34f), "Road_Body_Base", false, 1f),
        };

        [MenuItem("WrongLine/Road/Rebuild Pieces")]
        public static void Rebuild()
        {
            EnsureFolders();
            Material[] materials = EnsureMaterials();
            Material ground = EnsureGroundMaterial();
            SceneryConfig sceneryConfig = SceneryAssets.EnsureConfig();
            if (sceneryConfig.BackdropMaterial != ground)
            {
                // The backdrop disc under the camera draws with the ground's material.
                var serializedConfig = new SerializedObject(sceneryConfig);
                serializedConfig.FindProperty("_backdropMaterial").objectReferenceValue = ground;
                serializedConfig.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(sceneryConfig);
            }

            GroundSettings groundSettings = sceneryConfig.Ground;
            groundSettings.Validate();
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
                RoadPieceView view = BuildPiece(recipe, materials, ground, groundSettings);
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

        private static RoadPieceView BuildPiece(RoadPieceRecipe recipe, Material[] materials, Material groundMaterial, GroundSettings groundSettings)
        {
            Mesh builtGround = GroundMeshBuilder.Build(recipe.Shape, groundSettings);
            builtGround.name = $"{recipe.name}_Ground";
            Mesh groundMesh = WriteMesh($"{MESHES}/{recipe.name}_Ground.asset", builtGround);
            if (groundMesh != builtGround)
            {
                UnityEngine.Object.DestroyImmediate(builtGround);
            }

            TrackShape shape = recipe.Shape;
            Mesh mesh = LoadArtMesh(recipe.name);
            if (mesh != null)
            {
                // Art made for another cross-section (an older road width) would put the road out of line with the car and the
                // rules: until the artist redelivers, the generated mesh stands in (the validator warns about it).
                List<string> problems = RoadMeshContractCheck.Check(mesh, shape);
                if (problems.Count > 0)
                {
                    Log.Warning($"[Road] {recipe.name}: the art mesh breaks the road piece contract, the generated mesh is used instead. {problems[0]}");
                    mesh = null;
                }
            }

            if (mesh != null)
            {
                // The real art wins; the validator measures its shape against the recipe (RoadMeshContractCheck).
                // A flat straight is cut into slices first: the curve shader bends vertices only, so its road sags between the two
                // rings under the cars and props that follow the true bend (RoadMeshRefiner).
                // Only the flat straights: every other piece has a ring every few metres already (measured: no sag), and on a bent
                // piece the middle of a split edge is off the surface the contract measures.
                bool flatStraight = (recipe.Tags & ~RoadTags.Straight) == RoadTags.None;
                Mesh refined = flatStraight ? RoadMeshRefiner.SliceAlongRoad(mesh, RefineStepMeters) : mesh;
                if (refined != mesh)
                {
                    refined.name = recipe.name;
                    Mesh written = WriteMesh(RefinedArtMeshPath(recipe.name), refined);
                    if (written != refined)
                    {
                        UnityEngine.Object.DestroyImmediate(refined);
                    }

                    Log.Info($"[Road] {recipe.name}: art mesh {mesh.vertexCount} -> {written.vertexCount} vertices (sliced to {RefineStepMeters} m).");
                    mesh = written;
                }

                Log.Info($"[Road] {recipe.name}: {recipe.Recipe.Length} m, mesh {mesh.vertexCount} vertices, tags {recipe.Tags}, exit {shape.Exit.Position}.");
            }
            else
            {
                bool straight = (recipe.Tags & ~RoadTags.Straight) == RoadTags.None;
                Mesh built = RoadMeshBuilder.Build(shape, straight ? shape.Count - 1 : MeshRingStride);
                built.name = recipe.name;

                mesh = WriteMesh($"{MESHES}/{recipe.name}.asset", built);
                if (mesh != built)
                {
                    UnityEngine.Object.DestroyImmediate(built); // Copied into the existing asset; a new asset adopted `built` itself.
                }

                float gap = RoadMeshBuilder.ExitGap(mesh, shape);
                Log.Info($"[Road] {recipe.name}: {recipe.Recipe.Length} m, {shape.Count} frames, {mesh.vertexCount} vertices, tags {recipe.Tags}, exit {shape.Exit.Position}, mesh-to-exit gap {gap:F4} m.");
            }

            var root = new GameObject(recipe.name);
            try
            {
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

                // The land beside the road comes and goes with the piece (see GroundProfile).
                var ground = new GameObject(GROUND_CHILD);
                ground.transform.SetParent(root.transform, false);
                ground.AddComponent<MeshFilter>().sharedMesh = groundMesh;
                var groundRenderer = ground.AddComponent<MeshRenderer>();
                groundRenderer.sharedMaterial = groundMaterial;
                groundRenderer.shadowCastingMode = ShadowCastingMode.Off;
                groundRenderer.lightProbeUsage = LightProbeUsage.Off;
                groundRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

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

        internal static string ArtModelPath(string pieceName) => $"{ART}/{pieceName}.fbx";

        /// <summary>Where the artist's mesh of a piece goes once its long edges are split (see <see cref="RoadMeshRefiner"/>); the validator counts it as the art.</summary>
        internal static string RefinedArtMeshPath(string pieceName) => $"{MESHES}/{pieceName}_Art.asset";

        /// <summary>The mesh of <c>Art/&lt;piece&gt;.fbx</c> when the artist has delivered it, else null (the grey box stands in).</summary>
        private static Mesh LoadArtMesh(string pieceName)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ArtModelPath(pieceName));
            if (model == null)
            {
                return null;
            }

            var filter = model.GetComponentInChildren<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
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
                Shader shader = Shader.Find(ROAD_SHADER);
                if (shader == null)
                {
                    throw new InvalidOperationException($"Shader '{ROAD_SHADER}' not found.");
                }

                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    material.SetColor("_BaseColor", _materialSpecs[i].Color);
                    AssetDatabase.CreateAsset(material, path);
                }

                // An older material (the stand-in lit shader) moves to the curved one; a texture in Art/ is bound once,
                // and the colour then multiplies it, so it goes white. Anything tuned by hand afterwards stays.
                bool changed = false;
                if (material.shader != shader)
                {
                    material.shader = shader;
                    changed = true;
                }

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ART}/{_materialSpecs[i].Texture}.png");
                if (texture != null && material.GetTexture("_BaseMap") == null)
                {
                    material.SetTexture("_BaseMap", texture);
                    material.SetColor("_BaseColor", Color.white);
                    changed = true;
                }

                // How the material cuts and tiles its texture belongs to the road contract, not to taste: kept in step every run.
                bool clip = _materialSpecs[i].AlphaClip;
                if (material.IsKeywordEnabled("_ALPHATEST_ON") != clip || material.GetFloat("_AlphaClip") != (clip ? 1f : 0f))
                {
                    material.SetFloat("_AlphaClip", clip ? 1f : 0f);
                    if (clip)
                    {
                        material.EnableKeyword("_ALPHATEST_ON");
                    }
                    else
                    {
                        material.DisableKeyword("_ALPHATEST_ON");
                    }

                    changed = true;
                }

                // The lane line material (the alpha clipped one) keeps its thin strips whole at a distance: see CurvedLit.shader.
                if (clip && ConfigureLineMaterial(material))
                {
                    changed = true;
                }

                // The road takes a pool of light under the street lamps at night.
                changed |= EnableLampPool(material);

                var wanted = new Vector2(1f, _materialSpecs[i].TilingV);
                if (material.GetTextureScale("_BaseMap") != wanted)
                {
                    material.SetTextureScale("_BaseMap", wanted);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(material);
                }

                AssetDatabase.SaveAssetIfDirty(material);
                materials[i] = material;
            }

            return materials;
        }

        /// <summary>The ground's material: the curved shader with the biome blend on, the detail texture mapped from above.</summary>
        private static Material EnsureGroundMaterial()
        {
            Shader shader = Shader.Find(ROAD_SHADER) ?? throw new InvalidOperationException($"Shader '{ROAD_SHADER}' not found.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(GROUND_MATERIAL_PATH);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, GROUND_MATERIAL_PATH);
            }

            bool changed = false;
            if (material.shader != shader)
            {
                material.shader = shader;
                changed = true;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SceneryAssets.GROUND_TEXTURE_PATH);
            if (texture != null && material.GetTexture("_BaseMap") == null)
            {
                material.SetTexture("_BaseMap", texture);
                changed = true;
            }

            changed |= SetFloat(material, "_GroundBlend", 1f);
            changed |= SetFloat(material, "_Cull", 0f); // Both sides: the ground folds inside tight bends (GroundMeshBuilder).
            if (!material.IsKeywordEnabled("_GROUNDBLEND_ON"))
            {
                material.EnableKeyword("_GROUNDBLEND_ON");
                changed = true;
            }

            changed |= EnableLampPool(material);

            // The shader maps the texture by world metres: the scale is one repeat per tile.
            var wanted = new Vector2(1f / SceneryAssets.GroundTileMeters, 1f / SceneryAssets.GroundTileMeters);
            if (material.GetTextureScale("_BaseMap") != wanted)
            {
                material.SetTextureScale("_BaseMap", wanted);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(material);
            }

            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        /// <returns>Whether anything changed.</returns>
        private static bool ConfigureLineMaterial(Material material)
        {
            bool changed = false;
            changed |= SetFloat(material, "_LineWiden", 1f);
            changed |= SetFloat(material, "_LineWidth", RoadSection.LineWidth);
            changed |= SetFloat(material, "_LineSpacing", RoadSection.LaneWidth);
            changed |= SetFloat(material, "_LineMinPixels", 1.5f);
            changed |= SetFloat(material, "_OffsetFactor", -1f);
            changed |= SetFloat(material, "_OffsetUnits", -2f); // In depth-buffer steps, so it wins at any distance.
            if (!material.IsKeywordEnabled("_LINEWIDEN_ON"))
            {
                material.EnableKeyword("_LINEWIDEN_ON");
                changed = true;
            }

            return changed;
        }

        /// <returns>Whether anything changed.</returns>
        private static bool EnableLampPool(Material material)
        {
            bool changed = SetFloat(material, "_LampPool", 1f);
            if (!material.IsKeywordEnabled("_LAMPPOOL_ON"))
            {
                material.EnableKeyword("_LAMPPOOL_ON");
                changed = true;
            }

            return changed;
        }

        private static bool SetFloat(Material material, string name, float value)
        {
            if (Mathf.Approximately(material.GetFloat(name), value))
            {
                return false;
            }

            material.SetFloat(name, value);
            return true;
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
