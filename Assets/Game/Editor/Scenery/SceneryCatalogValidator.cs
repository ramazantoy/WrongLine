using System;
using System.Collections.Generic;
using AF.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace WrongLine.Scenery.Editor
{
    /// <summary>
    /// A scenery catalog or config that would only fail when a run starts, or a prop that could never be placed (standing too
    /// near the road for its footprint), fails the validation instead.
    /// </summary>
    public sealed class SceneryCatalogValidator : IValidator
    {
        private const string GAME_FOLDER = "Assets/Game";

        public void ValidateAssets(ValidationReport report)
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(SceneryCatalog)}", new[] { GAME_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var catalog = AssetDatabase.LoadAssetAtPath<SceneryCatalog>(path);
                if (catalog != null)
                {
                    Check(catalog, path, report);
                }
            }

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(SceneryConfig)}", new[] { GAME_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<SceneryConfig>(path);
                if (config == null)
                {
                    continue;
                }

                try
                {
                    config.Validate();
                }
                catch (ArgumentException exception)
                {
                    report.Error($"{path}: {exception.Message}", config);
                }

                if (config.BackdropMaterial == null)
                {
                    report.Error($"{path}: no backdrop material (the ground's gaps would show the sky). Run WrongLine/Road/Rebuild Pieces.", config);
                }
            }
        }

        internal static void Check(SceneryCatalog catalog, string location, ValidationReport report)
        {
            if (catalog.Biomes.Count == 0)
            {
                report.Error($"{location}: there is no biome.", catalog);
            }

            var names = new HashSet<string>();
            foreach (PropEntry entry in catalog.Shared)
            {
                CheckEntry(entry, SceneryCatalog.SHARED_NAME, names, location, catalog, report);
            }

            foreach (BiomeDef biome in catalog.Biomes)
            {
                if (string.IsNullOrWhiteSpace(biome.Name) || biome.Name == SceneryCatalog.SHARED_NAME)
                {
                    report.Error($"{location}: a biome needs a name other than '{SceneryCatalog.SHARED_NAME}' (it is the art folder's name).", catalog);
                }

                foreach (PropEntry entry in biome.Props)
                {
                    CheckEntry(entry, biome.Name, names, location, catalog, report);
                }
            }
        }

        private static void CheckEntry(PropEntry entry, string biome, HashSet<string> names, string location, SceneryCatalog catalog, ValidationReport report)
        {
            string at = $"{location}: {biome}/{entry.Name}";
            if (string.IsNullOrWhiteSpace(entry.Name) || !names.Add(entry.Name))
            {
                report.Error($"{at}: every prop needs a name of its own (it names the prefab and the artist's model).", catalog);
            }

            if (entry.Prefab == null)
            {
                report.Error($"{at}: no prefab. Run WrongLine/Scenery/Rebuild Props.", catalog);
            }
            else if (EditorUtility.IsPersistent(entry.Prefab)
                && AssetDatabase.LoadAssetAtPath<GameObject>(SceneryAssets.ArtModelPath(biome, entry.Name)) != null
                && entry.Prefab.GetComponent<SceneryPropView>().GetComponent<MeshFilter>() != null)
            {
                // A stand-in keeps its mesh on the root; a model's meshes are children. The art came after the last rebuild.
                report.Warning($"{at}: the artist's model is in Art/ but the prefab is still the stand-in. Run WrongLine/Scenery/Rebuild Props.", entry.Prefab);
            }

            if (entry.Mode == PropMode.Row && !(entry.Spacing > 0f))
            {
                report.Error($"{at}: a row needs a positive spacing.", catalog);
            }

            if (entry.Mode == PropMode.Scatter && !(entry.Density >= 0f))
            {
                report.Error($"{at}: the density must not be negative.", catalog);
            }

            float clearance = SceneryPlanner.Clearance(entry.Footprint);
            if (entry.LateralMin < clearance)
            {
                report.Error($"{at}: it stands {entry.LateralMin} m from the centre line, but a footprint of {entry.Footprint} m needs at least {clearance} m; it would never be placed.", catalog);
            }

            if (entry.Mode == PropMode.Scatter && entry.LateralMax < entry.LateralMin)
            {
                report.Error($"{at}: LateralMax is below LateralMin.", catalog);
            }

            if (!(entry.ScaleRange.x > 0f) || entry.ScaleRange.y < entry.ScaleRange.x)
            {
                report.Error($"{at}: the scale range must be positive and in order.", catalog);
            }
        }
    }
}
