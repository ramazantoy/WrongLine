using System;
using System.Collections.Generic;
using AF.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace WrongLine.Scenery.Editor
{
    /// <summary>
    /// Builds every roadside prop's prefab from the scenery catalog: the artist's model (<c>Scenery/Art/&lt;Biome&gt;/&lt;Name&gt;.fbx</c>,
    /// drawn with the biome's material) when it is there, else a stand-in shape. Writes each prefab back into the catalog.
    /// Makes the catalog and the config the first time, from <see cref="ScenerySpecs"/>. Idempotent.
    /// </summary>
    public static class SceneryPropBuilder
    {
        private const string SHADER = "WrongLine/CurvedLit";

        [MenuItem("WrongLine/Scenery/Rebuild Props")]
        public static void Rebuild()
        {
            foreach (string folder in new[] { SceneryAssets.ART, SceneryAssets.PREFABS, SceneryAssets.MESHES, SceneryAssets.MATERIALS })
            {
                SceneryAssets.EnsureFolder(folder);
            }

            SceneryAssets.EnsureConfig();
            SceneryCatalog catalog = EnsureCatalog();

            var serialized = new SerializedObject(catalog);
            int built = 0;
            int art = 0;
            SerializedProperty shared = serialized.FindProperty("_shared");
            for (int i = 0; i < shared.arraySize; i++)
            {
                art += BuildEntry(SceneryCatalog.SHARED_NAME, shared.GetArrayElementAtIndex(i)) ? 1 : 0;
                built++;
            }

            SerializedProperty biomes = serialized.FindProperty("_biomes");
            for (int b = 0; b < biomes.arraySize; b++)
            {
                SerializedProperty biome = biomes.GetArrayElementAtIndex(b);
                string biomeName = biome.FindPropertyRelative("_name").stringValue;
                SerializedProperty props = biome.FindPropertyRelative("_props");
                for (int i = 0; i < props.arraySize; i++)
                {
                    art += BuildEntry(biomeName, props.GetArrayElementAtIndex(i)) ? 1 : 0;
                    built++;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(catalog);
            Log.Info($"[Scenery] Rebuilt {built} props ({art} from the artist's models, {built - art} stand-ins) in {biomes.arraySize} biomes.");
        }

        /// <returns>Whether the artist's model was used.</returns>
        private static bool BuildEntry(string biome, SerializedProperty entry)
        {
            string name = entry.FindPropertyRelative("_name").stringValue;
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException($"A prop in biome '{biome}' has no name.");
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SceneryAssets.ArtModelPath(biome, name));
            var shadows = entry.FindPropertyRelative("_castShadows").boolValue ? ShadowCastingMode.On : ShadowCastingMode.Off;
            var root = new GameObject(name);
            try
            {
                if (model != null)
                {
                    Material material = EnsureBiomeMaterial(biome);
                    foreach (MeshFilter source in model.GetComponentsInChildren<MeshFilter>(true))
                    {
                        // The model's meshes, placed as in the model, all drawn with the biome's material.
                        var part = new GameObject(source.name);
                        part.transform.SetParent(root.transform, false);
                        Matrix4x4 local = model.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
                        part.transform.SetLocalPositionAndRotation(local.GetPosition(), local.rotation);
                        part.transform.localScale = local.lossyScale;
                        part.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                        var materials = new Material[Math.Max(1, source.sharedMesh.subMeshCount)];
                        for (int i = 0; i < materials.Length; i++)
                        {
                            materials[i] = material;
                        }

                        Configure(part.AddComponent<MeshRenderer>(), materials, shadows);
                    }
                }
                else
                {
                    var shape = (PlaceholderShape)entry.FindPropertyRelative("_placeholder").enumValueIndex;
                    Mesh built = PlaceholderMeshes.Build(shape);
                    built.name = name;
                    Mesh mesh = WriteMesh($"{SceneryAssets.MESHES}/{name}.asset", built);
                    root.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var materials = new Material[2];
                    materials[PlaceholderMeshes.FrameSubmesh] = EnsureColorMaterial(entry.FindPropertyRelative("_frameColor").colorValue);
                    materials[PlaceholderMeshes.MainSubmesh] = EnsureColorMaterial(entry.FindPropertyRelative("_mainColor").colorValue);
                    Configure(root.AddComponent<MeshRenderer>(), materials, shadows);
                }

                root.AddComponent<SceneryPropView>();
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{SceneryAssets.PREFABS}/{name}.prefab");
                entry.FindPropertyRelative("_prefab").objectReferenceValue = prefab.GetComponent<SceneryPropView>();
                return model != null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void Configure(MeshRenderer renderer, Material[] materials, ShadowCastingMode shadows)
        {
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = shadows;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static SceneryCatalog EnsureCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SceneryCatalog>(SceneryAssets.CATALOG_PATH);
            if (catalog != null)
            {
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<SceneryCatalog>();
            AssetDatabase.CreateAsset(catalog, SceneryAssets.CATALOG_PATH);
            var serialized = new SerializedObject(catalog);
            var byBiome = new Dictionary<string, SerializedProperty>();
            SerializedProperty biomes = serialized.FindProperty("_biomes");
            biomes.arraySize = ScenerySpecs.Biomes.Length;
            for (int b = 0; b < ScenerySpecs.Biomes.Length; b++)
            {
                SerializedProperty biome = biomes.GetArrayElementAtIndex(b);
                biome.FindPropertyRelative("_name").stringValue = ScenerySpecs.Biomes[b].Name;
                biome.FindPropertyRelative("_groundColor").colorValue = ScenerySpecs.Biomes[b].Ground;
                SerializedProperty props = biome.FindPropertyRelative("_props");
                props.arraySize = 0;
                byBiome.Add(ScenerySpecs.Biomes[b].Name, props);
            }

            SerializedProperty shared = serialized.FindProperty("_shared");
            shared.arraySize = 0;
            byBiome.Add(SceneryCatalog.SHARED_NAME, shared);

            foreach (ScenerySpecs.Prop spec in ScenerySpecs.Props)
            {
                SerializedProperty list = byBiome[spec.Biome];
                list.arraySize++;
                SerializedProperty entry = list.GetArrayElementAtIndex(list.arraySize - 1);
                entry.FindPropertyRelative("_name").stringValue = spec.Name;
                entry.FindPropertyRelative("_prefab").objectReferenceValue = null;
                entry.FindPropertyRelative("_placeholder").enumValueIndex = (int)spec.Shape;
                entry.FindPropertyRelative("_mainColor").colorValue = spec.Main;
                entry.FindPropertyRelative("_frameColor").colorValue = spec.Frame;
                entry.FindPropertyRelative("_mode").enumValueIndex = (int)spec.Mode;
                entry.FindPropertyRelative("_density").floatValue = spec.Mode == PropMode.Scatter ? spec.DensityOrSpacing : 0f;
                entry.FindPropertyRelative("_spacing").floatValue = spec.Mode == PropMode.Row ? spec.DensityOrSpacing : 0f;
                entry.FindPropertyRelative("_side").enumValueIndex = (int)spec.Side;
                entry.FindPropertyRelative("_lateralMin").floatValue = spec.LateralMin;
                entry.FindPropertyRelative("_lateralMax").floatValue = spec.LateralMax;
                entry.FindPropertyRelative("_footprint").floatValue = spec.Footprint;
                entry.FindPropertyRelative("_scaleRange").vector2Value = spec.Scale;
                entry.FindPropertyRelative("_castShadows").boolValue = spec.Shadows;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(catalog);
            return catalog;
        }

        /// <summary>The material every model of a biome shares: the curved shader with the biome's palette texture.</summary>
        private static Material EnsureBiomeMaterial(string biome)
        {
            string path = $"{SceneryAssets.MATERIALS}/{biome}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(FindShader());
                AssetDatabase.CreateAsset(material, path);
            }

            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(SceneryAssets.ArtAtlasPath(biome));
            if (atlas != null && material.GetTexture("_BaseMap") != atlas)
            {
                material.SetTexture("_BaseMap", atlas);
                EditorUtility.SetDirty(material);
            }

            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        /// <summary>A plain coloured material for the stand-ins, one per colour, shared by every stand-in that uses it.</summary>
        private static Material EnsureColorMaterial(Color color)
        {
            string path = $"{SceneryAssets.MATERIALS}/Placeholder_{ColorUtility.ToHtmlStringRGB(color)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(FindShader());
                material.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(material, path);
                AssetDatabase.SaveAssetIfDirty(material);
            }

            return material;
        }

        private static Shader FindShader()
        {
            return Shader.Find(SHADER) ?? throw new InvalidOperationException($"Shader '{SHADER}' not found.");
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
            UnityEngine.Object.DestroyImmediate(source);
            return existing;
        }
    }
}
