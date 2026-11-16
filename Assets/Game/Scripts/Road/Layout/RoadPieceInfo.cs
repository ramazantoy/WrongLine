using System;
using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// A road piece as the generator sees it: its baked centre line and the numbers the placement rules need (how far it
    /// turns, how high it reaches, how demanding it is). Pure data, built from a <see cref="TrackRecipe"/>.
    /// </summary>
    public sealed class RoadPieceInfo
    {
        private const float IntensityYaw = 90f;
        private const float IntensityPitch = 14f;
        private const float IntensityBank = 20f;

        public RoadPieceInfo(int id, string name, in TrackRecipe recipe, bool isStart)
        {
            Id = id;
            Name = name;
            IsStart = isStart;
            Shape = TrackIntegrator.Bake(recipe);
            Tags = RoadTagger.From(recipe);
            ExitOffset = Shape.Exit.Position;
            ExitYaw = recipe.YawProfile == YawProfile.Single ? recipe.YawDegrees * Math.PI / 180.0 : 0.0;

            float minYaw = 0f;
            float maxYaw = 0f;
            float minY = 0f;
            float maxY = 0f;
            for (int i = 0; i < Shape.Count; i++)
            {
                Vector3 forward = Shape[i].Forward;
                float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                minYaw = Mathf.Min(minYaw, yaw);
                maxYaw = Mathf.Max(maxYaw, yaw);
                minY = Mathf.Min(minY, Shape[i].Position.y);
                maxY = Mathf.Max(maxY, Shape[i].Position.y);
            }

            MinYawDegrees = minYaw;
            MaxYawDegrees = maxYaw;
            MinY = minY;
            MaxY = maxY;
            Intensity = Mathf.Clamp01(Mathf.Max(
                Mathf.Abs(recipe.YawDegrees) / IntensityYaw,
                Mathf.Abs(recipe.PitchDegrees) / IntensityPitch,
                Mathf.Abs(recipe.BankDegrees) / IntensityBank));
        }

        /// <summary>Identifies the piece to whoever built the list (the catalog index).</summary>
        public int Id { get; }

        public string Name { get; }

        public bool IsStart { get; }

        public TrackShape Shape { get; }

        public RoadTags Tags { get; }

        public float Length => Shape.Length;

        /// <summary>Where the exit is, in the piece's local space.</summary>
        public Vector3 ExitOffset { get; }

        /// <summary>How far the heading has turned at the exit, in radians (exact, from the recipe).</summary>
        public double ExitYaw { get; }

        /// <summary>The lowest and highest heading anywhere along the piece, relative to its entry (the entry itself counts: 0).</summary>
        public float MinYawDegrees { get; }

        public float MaxYawDegrees { get; }

        /// <summary>The lowest and highest the centre line gets, relative to the entry height.</summary>
        public float MinY { get; }

        public float MaxY { get; }

        /// <summary>0 for a plain straight to 1 for the most demanding bend, slope or bank; used by the difficulty curve.</summary>
        public float Intensity { get; }

        public override string ToString() => Name;
    }
}
