using System;
using AF.Core.Editor;
using UnityEditor;
using WrongLine.Road;

namespace WrongLine.World.Editor
{
    /// <summary>
    /// A curve world config with numbers that make no sense, or a fog that cannot fit in the road built ahead, would only show as
    /// a wrong picture or fail at the start of a run; this fails the validation instead.
    /// </summary>
    public sealed class CurveWorldConfigValidator : IValidator
    {
        private const string GAME_FOLDER = "Assets/Game";

        public void ValidateAssets(ValidationReport report)
        {
            var roads = new System.Collections.Generic.List<(string Path, RoadConfig Config)>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(RoadConfig)}", new[] { GAME_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                roads.Add((path, AssetDatabase.LoadAssetAtPath<RoadConfig>(path)));
            }

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(CurveWorldConfig)}", new[] { GAME_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<CurveWorldConfig>(path);
                if (config == null)
                {
                    continue;
                }

                Check(config, path, report);
                foreach ((string roadPath, RoadConfig road) in roads)
                {
                    if (road != null)
                    {
                        CheckFog(config, road.AheadMeters, $"{path} with {roadPath}", report);
                    }
                }
            }
        }

        internal static void Check(CurveWorldConfig config, string location, ValidationReport report)
        {
            try
            {
                config.Settings.Validate();
            }
            catch (ArgumentException exception)
            {
                report.Error($"{location}: {exception.Message}", config);
            }
        }

        internal static void CheckFog(CurveWorldConfig config, float aheadMeters, string location, ValidationReport report)
        {
            try
            {
                FogRange.Compute(config.Settings, aheadMeters);
            }
            catch (ArgumentException exception)
            {
                report.Error($"{location}: {exception.Message}", config);
            }
        }
    }
}
