using System;
using AF.Core.Editor;
using UnityEditor;

namespace WrongLine.Road.Editor
{
    /// <summary>A road config with numbers that make no sense would only fail when the first run starts; this fails the validation instead.</summary>
    public sealed class RoadConfigValidator : IValidator
    {
        private const string GAME_FOLDER = "Assets/Game";

        public void ValidateAssets(ValidationReport report)
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(RoadConfig)}", new[] { GAME_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<RoadConfig>(path);
                if (config != null)
                {
                    Check(config, path, report);
                }
            }
        }

        internal static void Check(RoadConfig config, string location, ValidationReport report)
        {
            try
            {
                config.Validate();
            }
            catch (ArgumentException exception)
            {
                report.Error($"{location}: {exception.Message}", config);
            }
        }
    }
}
