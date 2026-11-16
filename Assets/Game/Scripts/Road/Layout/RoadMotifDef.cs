using System;
using System.Collections.Generic;

namespace WrongLine.Road
{
    /// <summary>
    /// A motif: a fixed run of road pieces (by name) that reads as one deliberate figure, such as a slalom or a long sweep,
    /// instead of pieces picked one at a time. Plain data; the generator resolves the names against the catalog.
    /// </summary>
    [Serializable]
    public sealed class RoadMotifDef
    {
        public string Name = "";

        /// <summary>How much the motif is liked among the motifs that fit.</summary>
        public float Weight = 1f;

        public string[] Pieces = Array.Empty<string>();

        public void Validate()
        {
            if (string.IsNullOrEmpty(Name))
            {
                throw new ArgumentException("A motif needs a name.");
            }

            if (!(Weight > 0f))
            {
                throw new ArgumentException($"Motif '{Name}': the weight must be positive.");
            }

            if (Pieces == null || Pieces.Length < 2)
            {
                throw new ArgumentException($"Motif '{Name}': a motif needs at least two pieces.");
            }
        }

        /// <summary>The motifs a new road config starts with.</summary>
        public static List<RoadMotifDef> Defaults()
        {
            return new List<RoadMotifDef>
            {
                Make("SlalomRight", 1f, "CurveSoft_R", "CurveSoft_L", "CurveSoft_R"),
                Make("SlalomLeft", 1f, "CurveSoft_L", "CurveSoft_R", "CurveSoft_L"),
                Make("SweepRight", 1.2f, "BankSoft_R", "BankSoft_R"),
                Make("SweepLeft", 1.2f, "BankSoft_L", "BankSoft_L"),
                Make("Roller", 0.8f, "RampUp_L", "Straight_S", "SteepDown_S"),
                Make("Rise", 0.8f, "RampUp_S", "Straight_S", "RampUp_L", "Straight_M"),
            };
        }

        private static RoadMotifDef Make(string name, float weight, params string[] pieces)
        {
            return new RoadMotifDef { Name = name, Weight = weight, Pieces = pieces };
        }
    }
}
