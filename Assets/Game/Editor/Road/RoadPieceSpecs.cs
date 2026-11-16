using System.Collections.Generic;

namespace WrongLine.Road.Editor
{
    /// <summary>
    /// The pieces created on the first "Rebuild Pieces". After that the recipe assets are the source of truth: tweak them in
    /// the Inspector, this list only fills in the ones that are missing.
    /// </summary>
    internal static class RoadPieceSpecs
    {
        public readonly struct Spec
        {
            public readonly string Name;
            public readonly TrackRecipe Recipe;
            public readonly bool IsStart;

            public Spec(string name, TrackRecipe recipe, bool isStart)
            {
                Name = name;
                Recipe = recipe;
                IsStart = isStart;
            }
        }

        public static IReadOnlyList<Spec> Defaults { get; } = Create();

        private static List<Spec> Create()
        {
            var specs = new List<Spec>
            {
                new Spec("Start", TrackRecipe.Straight(60f), true),
                new Spec("Straight_S", TrackRecipe.Straight(20f), false),
                new Spec("Straight_M", TrackRecipe.Straight(40f), false),
                new Spec("Straight_L", TrackRecipe.Straight(80f), false),
            };

            AddPair(specs, "CurveSoft", 60f, 35f, 0f);
            AddPair(specs, "CurveSharp", 40f, 90f, 0f);
            AddPair(specs, "BankSoft", 60f, 45f, 12f);
            AddPair(specs, "BankSharp", 40f, 90f, 20f);

            specs.Add(new Spec("SCurve_R", new TrackRecipe(80f, 35f, YawProfile.S, 0f, PitchProfile.Bump, 0f), false));
            specs.Add(new Spec("SCurve_L", new TrackRecipe(80f, -35f, YawProfile.S, 0f, PitchProfile.Bump, 0f), false));

            specs.Add(Pitched("RampUp_S", 40f, 8f, PitchProfile.Bump));
            specs.Add(Pitched("RampUp_L", 60f, 14f, PitchProfile.Bump));
            specs.Add(Pitched("RampDown_S", 40f, -8f, PitchProfile.Bump));
            specs.Add(Pitched("RampDown_L", 60f, -14f, PitchProfile.Bump));
            specs.Add(Pitched("Hill_S", 60f, 10f, PitchProfile.Wave));
            specs.Add(Pitched("Hill_L", 100f, 14f, PitchProfile.Wave));
            specs.Add(Pitched("Dip_S", 60f, -10f, PitchProfile.Wave));
            specs.Add(Pitched("Dip_L", 100f, -14f, PitchProfile.Wave));
            return specs;
        }

        // Positive yaw turns right, and a right turn banks to the right (positive bank).
        private static void AddPair(List<Spec> specs, string name, float length, float yaw, float bank)
        {
            specs.Add(new Spec(name + "_R", new TrackRecipe(length, yaw, YawProfile.Single, 0f, PitchProfile.Bump, bank), false));
            specs.Add(new Spec(name + "_L", new TrackRecipe(length, -yaw, YawProfile.Single, 0f, PitchProfile.Bump, -bank), false));
        }

        private static Spec Pitched(string name, float length, float pitch, PitchProfile profile)
        {
            return new Spec(name, new TrackRecipe(length, 0f, YawProfile.Single, pitch, profile, 0f), false);
        }
    }
}
