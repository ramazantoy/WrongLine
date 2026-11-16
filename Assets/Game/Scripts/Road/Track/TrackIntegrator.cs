using System;
using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// Bakes a <see cref="TrackRecipe"/> into a <see cref="TrackShape"/>. Heading, slope and roll are closed-form in the
    /// distance along the piece; only the position is integrated (midpoint rule with sub-steps), so the result is exact to
    /// well under a millimetre and identical on every platform run.
    /// </summary>
    public static class TrackIntegrator
    {
        /// <summary>Distance between baked frames, in metres.</summary>
        public const float Step = 0.5f;

        private const int SubSteps = 16;
        private const float LengthTolerance = 1e-3f;

        public static TrackShape Bake(in TrackRecipe recipe)
        {
            if (recipe.Length <= 0f)
            {
                throw new ArgumentException("A piece needs a positive length.", nameof(recipe));
            }

            int intervals = Mathf.RoundToInt(recipe.Length / Step);
            if (intervals < 1 || Mathf.Abs(recipe.Length - intervals * Step) > LengthTolerance)
            {
                throw new ArgumentException($"Length {recipe.Length} is not a whole multiple of the {Step} m step.", nameof(recipe));
            }

            var frames = new TrackFrame[intervals + 1];
            double x = 0.0;
            double y = 0.0;
            double z = 0.0;
            double subStep = Step / SubSteps;

            for (int i = 0; i <= intervals; i++)
            {
                float u = (float)i / intervals;
                frames[i] = new TrackFrame(new Vector3((float)x, (float)y, (float)z), Orientation(recipe, u));

                if (i == intervals)
                {
                    break;
                }

                for (int k = 0; k < SubSteps; k++)
                {
                    float mid = (i + (k + 0.5f) / SubSteps) / intervals;
                    Vector3 forward = Heading(recipe, mid);
                    x += forward.x * subStep;
                    y += forward.y * subStep;
                    z += forward.z * subStep;
                }
            }

            return new TrackShape(frames, Step);
        }

        private static Vector3 Heading(in TrackRecipe recipe, float u)
        {
            float yaw = recipe.YawDegrees * Mathf.Deg2Rad * TrackProfiles.Yaw(recipe.YawProfile, u);
            float pitch = recipe.PitchDegrees * Mathf.Deg2Rad * TrackProfiles.Pitch(recipe.PitchProfile, u);
            float cosPitch = Mathf.Cos(pitch);
            return new Vector3(cosPitch * Mathf.Sin(yaw), Mathf.Sin(pitch), cosPitch * Mathf.Cos(yaw));
        }

        private static Quaternion Orientation(in TrackRecipe recipe, float u)
        {
            float yaw = recipe.YawDegrees * Mathf.Deg2Rad * TrackProfiles.Yaw(recipe.YawProfile, u);
            Vector3 forward = Heading(recipe, u);
            Vector3 right = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw));
            Vector3 up = Vector3.Cross(forward, right);
            float bank = recipe.BankDegrees * TrackProfiles.Bank(recipe.YawProfile, u);
            // Unity rolls a positive angle about +Z towards the right edge going up; positive bank here lowers it.
            return Quaternion.LookRotation(forward, up) * Quaternion.AngleAxis(-bank, Vector3.forward);
        }
    }
}
