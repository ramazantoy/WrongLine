using System;
using System.Collections.Generic;
using AF.Core.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WrongLine.Road.Editor
{
    /// <summary>
    /// Keeps a wrong road piece out of the game: a piece whose mesh is stale or does not sit on its centre line, whose ends
    /// are not flat, level and straight (so it would not join every other piece), whose bends are too tight for the road
    /// width, or a catalog the generator cannot build a road from.
    /// </summary>
    public sealed class RoadCatalogValidator : IValidator
    {
        private const string GAME_FOLDER = "Assets/Game";
        private const string REBUILD_HINT = "Run WrongLine/Road/Rebuild Pieces.";

        private const float AngleToleranceDegrees = 0.1f;
        private const float LevelTolerance = 1e-3f;
        private const float PositionTolerance = 1e-3f;

        /// <summary>The tightest bend allowed. The road is 10.2 m wide with its curbs, so the inside edge must not fold over.</summary>
        private const float MinTurnRadius = 8f;

        /// <summary>The most the road's up may turn between two baked frames (banking changing too abruptly).</summary>
        private const float MaxUpStepDegrees = 3f;

        private static readonly RoadTags[] _requiredTags =
        {
            RoadTags.Straight, RoadTags.Curve, RoadTags.Climb, RoadTags.Drop, RoadTags.Banked,
        };

        public void ValidateAssets(ValidationReport report)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(RoadCatalog)}", new[] { GAME_FOLDER });
            if (guids.Length == 0)
            {
                report.Error($"No {nameof(RoadCatalog)} asset in {GAME_FOLDER}. {REBUILD_HINT}");
            }

            var referencedRecipes = new HashSet<RoadPieceRecipe>();
            var checkedViews = new HashSet<RoadPieceView>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var catalog = AssetDatabase.LoadAssetAtPath<RoadCatalog>(path);
                if (catalog == null)
                {
                    continue;
                }

                CheckCatalog(catalog.Start, catalog.Pieces, path, report);
                foreach (RoadPieceView view in Views(catalog))
                {
                    if (view == null || !checkedViews.Add(view))
                    {
                        continue;
                    }

                    CheckPiece(view, AssetDatabase.GetAssetPath(view), report);
                    if (view.Recipe != null)
                    {
                        referencedRecipes.Add(view.Recipe);
                    }
                }
            }

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(RoadPieceRecipe)}", new[] { GAME_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var recipe = AssetDatabase.LoadAssetAtPath<RoadPieceRecipe>(path);
                if (recipe != null && guids.Length > 0 && !referencedRecipes.Contains(recipe))
                {
                    report.Error($"{path}: the recipe has no piece in a catalog. {REBUILD_HINT}", recipe);
                }
            }
        }

        /// <summary>Checks the pieces as a set: the start piece, the selectable pieces, and what the generator needs from them.</summary>
        internal static void CheckCatalog(RoadPieceView start, IReadOnlyList<RoadPieceView> pieces, string location, ValidationReport report)
        {
            if (start == null)
            {
                report.Error($"{location}: the catalog has no start piece.");
            }
            else if (start.Recipe == null)
            {
                report.Error($"{location}: the start piece '{start.name}' has no recipe.", start);
            }
            else
            {
                if (!start.Recipe.IsStart)
                {
                    report.Error($"{location}: '{start.name}' is the start piece but its recipe is not marked as a start.", start);
                }

                if (start.Recipe.Tags != RoadTags.Straight)
                {
                    report.Error($"{location}: the start piece '{start.name}' must be a plain straight (tags: {start.Recipe.Tags}), so a run begins level.", start);
                }
            }

            if (pieces == null || pieces.Count == 0)
            {
                report.Error($"{location}: the catalog has no selectable pieces.");
                return;
            }

            var seen = new HashSet<RoadPieceView>();
            RoadTags present = RoadTags.None;
            bool left = false;
            bool right = false;
            for (int i = 0; i < pieces.Count; i++)
            {
                RoadPieceView piece = pieces[i];
                if (piece == null)
                {
                    report.Error($"{location}: selectable piece slot {i} is empty.");
                    continue;
                }

                if (!seen.Add(piece))
                {
                    report.Error($"{location}: '{piece.name}' is listed twice.", piece);
                    continue;
                }

                if (piece.Recipe == null)
                {
                    report.Error($"{location}: selectable piece '{piece.name}' has no recipe.", piece);
                    continue;
                }

                if (piece.Recipe.IsStart)
                {
                    report.Error($"{location}: '{piece.name}' is marked as a start but is in the selectable pieces; the generator would pick it mid-road.", piece);
                    continue;
                }

                RoadTags tags = piece.Recipe.Tags;
                present |= tags;
                if (tags.HasFlag(RoadTags.Curve))
                {
                    left |= piece.Recipe.Recipe.YawDegrees < 0f;
                    right |= piece.Recipe.Recipe.YawDegrees > 0f;
                }
            }

            foreach (RoadTags required in _requiredTags)
            {
                if (!present.HasFlag(required))
                {
                    report.Error($"{location}: no selectable piece is tagged {required}; the generator needs at least one.");
                }
            }

            if (present.HasFlag(RoadTags.Curve) && !(left && right))
            {
                report.Error($"{location}: the curves only turn {(left ? "left" : "right")}; the generator needs both directions to steer back.");
            }
        }

        /// <summary>Checks one piece prefab: its recipe, its mesh and materials, and its centre line.</summary>
        internal static void CheckPiece(RoadPieceView view, string location, ValidationReport report)
        {
            if (view.Recipe == null)
            {
                report.Error($"{location}: the piece has no recipe.", view);
                return;
            }

            TrackShape shape;
            try
            {
                shape = view.Recipe.Shape;
            }
            catch (ArgumentException exception)
            {
                report.Error($"{location}: the recipe cannot be baked: {exception.Message}", view);
                return;
            }

            if (view.BakedHash != view.Recipe.Recipe.StableHash())
            {
                report.Error($"{location}: the mesh was built from an older version of the recipe. {REBUILD_HINT}", view);
            }

            CheckDashAlignment(view.Recipe.Recipe.Length, location, report, view);
            CheckMesh(view, shape, location, report);
            CheckShape(shape, location, report, view);
        }

        /// <summary>A piece must be a whole number of dash periods long, or the lane dashes break at the joint.</summary>
        internal static void CheckDashAlignment(float length, string location, ValidationReport report, Object context)
        {
            float periods = length / RoadSection.LineDashPeriod;
            if (Mathf.Abs(periods - Mathf.Round(periods)) > 1e-3f)
            {
                report.Error($"{location}: the length {length} m is not a multiple of the lane dash period {RoadSection.LineDashPeriod} m, so the dashes would break at the joint.", context);
            }
        }

        /// <summary>Checks a baked centre line: pivot, flat ends, continuity, tightest bend.</summary>
        internal static void CheckShape(TrackShape shape, string location, ValidationReport report, Object context)
        {
            TrackFrame entry = shape.Entry;
            if (Vector3.Distance(entry.Position, Vector3.zero) > PositionTolerance || Quaternion.Angle(entry.Rotation, Quaternion.identity) > AngleToleranceDegrees)
            {
                report.Error($"{location}: the entry frame is not the piece pivot (origin, no rotation).", context);
            }

            CheckEnd("entry", shape.Entry, shape[1], location, report, context);
            CheckEnd("exit", shape.Exit, shape[shape.Count - 2], location, report, context);

            bool turnReported = false;
            bool upReported = false;
            for (int i = 1; i < shape.Count; i++)
            {
                TrackFrame previous = shape[i - 1];
                TrackFrame frame = shape[i];
                float s = i * shape.Step;

                float turn = Vector3.Angle(previous.Forward, frame.Forward);
                if (!turnReported && turn > 0f && shape.Step / (turn * Mathf.Deg2Rad) < MinTurnRadius)
                {
                    report.Error($"{location}: the bend at {s:F1} m is tighter than the {MinTurnRadius} m minimum radius, so the inside of the road would fold over.", context);
                    turnReported = true;
                }

                if (!upReported && Vector3.Angle(previous.Up, frame.Up) > MaxUpStepDegrees)
                {
                    report.Error($"{location}: the road's up turns more than {MaxUpStepDegrees} degrees between two frames at {s:F1} m (banking changes too abruptly).", context);
                    upReported = true;
                }
            }
        }

        private static void CheckMesh(RoadPieceView view, TrackShape shape, string location, ValidationReport report)
        {
            var filter = view.GetComponent<MeshFilter>();
            var renderer = view.GetComponent<MeshRenderer>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
            {
                report.Error($"{location}: the piece has no mesh. {REBUILD_HINT}", view);
                return;
            }

            if (mesh.subMeshCount != RoadMeshBuilder.SubmeshCount)
            {
                report.Error($"{location}: the mesh has {mesh.subMeshCount} submeshes, the road section has {RoadMeshBuilder.SubmeshCount}. {REBUILD_HINT}", view);
                return;
            }

            if (renderer == null)
            {
                report.Error($"{location}: the piece has no MeshRenderer.", view);
            }
            else
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials.Length != RoadMeshBuilder.SubmeshCount)
                {
                    report.Error($"{location}: the renderer has {materials.Length} materials for {RoadMeshBuilder.SubmeshCount} submeshes.", view);
                }

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        report.Error($"{location}: material slot {i} is empty.", view);
                    }
                }
            }

            // By where the vertices are, not how the mesh is built: a mesh from another tool passes if its shape is right.
            foreach (string problem in RoadMeshContractCheck.Check(mesh, shape))
            {
                report.Error($"{location}: the mesh breaks the road piece contract: {problem}.", view);
            }

            // The artist's model is in the project but the piece does not use it: Rebuild Pieces found it breaking the contract
            // (made for another cross-section) and kept the generated mesh. Visible until the artist redelivers.
            if (EditorUtility.IsPersistent(view)
                && AssetDatabase.LoadAssetAtPath<GameObject>(RoadPieceBuilder.ArtModelPath(view.name)) != null
                && !AssetDatabase.GetAssetPath(mesh).EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                report.Warning($"{location}: Art/{view.name}.fbx is not used, the generated mesh stands in (the art does not fit the current road section).", view);
            }
        }

        private static void CheckEnd(string name, TrackFrame frame, TrackFrame neighbour, string location, ValidationReport report, Object context)
        {
            if (Vector3.Angle(Vector3.up, frame.Up) > AngleToleranceDegrees)
            {
                report.Error($"{location}: the {name} is banked, so it would not join a level piece.", context);
            }

            if (Mathf.Abs(frame.Forward.y) > LevelTolerance)
            {
                report.Error($"{location}: the {name} is on a slope, so it would not join a level piece.", context);
            }

            if (Vector3.Angle(frame.Forward, neighbour.Forward) > AngleToleranceDegrees)
            {
                report.Error($"{location}: the road is still curving at the {name}, so the next piece would meet it at a kink.", context);
            }

            if (Vector3.Angle(frame.Up, neighbour.Up) > AngleToleranceDegrees)
            {
                report.Error($"{location}: the banking is still changing at the {name}.", context);
            }
        }

        private static IEnumerable<RoadPieceView> Views(RoadCatalog catalog)
        {
            yield return catalog.Start;
            foreach (RoadPieceView piece in catalog.Pieces)
            {
                yield return piece;
            }
        }
    }
}
