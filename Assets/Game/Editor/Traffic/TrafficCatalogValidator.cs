using System;
using AF.Core.Editor;
using UnityEditor;
using UnityEngine;
using WrongLine.Road;

namespace WrongLine.Traffic.Editor
{
    /// <summary>A traffic catalog or config that would only fail when a run starts fails the validation instead.</summary>
    public sealed class TrafficCatalogValidator : IValidator
    {
        private const string GAME_FOLDER = "Assets/Game";

        public void ValidateAssets(ValidationReport report)
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(TrafficCatalog)}", new[] { GAME_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var catalog = AssetDatabase.LoadAssetAtPath<TrafficCatalog>(path);
                if (catalog == null)
                {
                    continue;
                }

                try
                {
                    catalog.Validate();
                }
                catch (ArgumentException exception)
                {
                    report.Error($"{path}: {exception.Message}", catalog);
                }

                foreach (TrafficCatalog.Entry entry in catalog.Models)
                {
                    if (entry.Prefab != null && entry.Prefab.GetComponentsInChildren<MeshRenderer>(true).Length == 0)
                    {
                        report.Error($"{path}: '{entry.Name}' has no mesh.", catalog);
                    }
                }
            }

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(TrafficConfig)}", new[] { GAME_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<TrafficConfig>(path);
                var road = AssetDatabase.LoadAssetAtPath<RoadConfig>("Assets/Game/Road/RoadConfig.asset");
                if (config == null || road == null)
                {
                    continue;
                }

                try
                {
                    config.Settings.Validate(road.BehindMeters, road.AheadMeters);
                }
                catch (ArgumentException exception)
                {
                    report.Error($"{path}: {exception.Message}", config);
                }
            }
        }
    }
}
