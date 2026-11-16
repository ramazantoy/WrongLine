using System;
using System.Collections.Generic;
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
            var catalogs = new List<(string Path, HashSet<string> Names)>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(RoadCatalog)}", new[] { GAME_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var catalog = AssetDatabase.LoadAssetAtPath<RoadCatalog>(path);
                if (catalog != null)
                {
                    var names = new HashSet<string>();
                    foreach (RoadPieceView piece in catalog.Pieces)
                    {
                        if (piece != null)
                        {
                            names.Add(piece.name);
                        }
                    }

                    catalogs.Add((path, names));
                }
            }

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(RoadConfig)}", new[] { GAME_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<RoadConfig>(path);
                if (config == null)
                {
                    continue;
                }

                Check(config, path, report);
                foreach ((string catalogPath, HashSet<string> names) in catalogs)
                {
                    CheckMotifs(config, names, $"{path} with {catalogPath}", report);
                }
            }
        }

        /// <summary>A motif that names a piece the catalog does not have would be skipped silently; this makes it an error.</summary>
        internal static void CheckMotifs(RoadConfig config, ISet<string> catalogPieceNames, string location, ValidationReport report)
        {
            foreach (RoadMotifDef motif in config.Generator.Motifs)
            {
                foreach (string name in motif.Pieces)
                {
                    if (!catalogPieceNames.Contains(name))
                    {
                        report.Error($"{location}: the motif '{motif.Name}' names the piece '{name}', which the catalog does not have, so the motif would never be placed.", config);
                    }
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
