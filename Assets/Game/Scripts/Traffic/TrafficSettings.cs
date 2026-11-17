using System;
using WrongLine.Road;

namespace WrongLine.Traffic
{
    /// <summary>
    /// The numbers behind the traffic: how it is laid out along the player's path (σ, see <see cref="TrafficPlanner"/>), how far
    /// it lives around the player and when a car changes level of detail. Read at the start of a run.
    /// </summary>
    [Serializable]
    public sealed class TrafficSettings
    {
        [Serializable]
        public sealed class LayoutGroup
        {
            /// <summary>Nothing is placed in the first stretch of a run, in metres of the player's path.</summary>
            public float ClearStartMeters = 150f;

            /// <summary>The corridor changes lane at most once per slice; a slice is at least this long (it grows when a lane change needs more).</summary>
            public float SliceMeters = 30f;

            /// <summary>Seconds the player's car needs to change one lane (<see cref="LaneChangeMeasure"/> measures it; this must not be less).</summary>
            public float LaneChangeSeconds = 0.6f;

            /// <summary>The player's car length, in metres.</summary>
            public float PlayerLengthMeters = 4.4f;

            /// <summary>Safety room around a car, in metres, and in seconds of the closing speed.</summary>
            public float PadMeters = 2f;

            public float PadSeconds = 0.25f;

            /// <summary>Least gap between two cars of a lane (on the player's path), and the extra gap drawn at random up to this much.</summary>
            public float MinGapMeters = 14f;

            public float GapRangeMeters = 60f;

            /// <summary>Chance the corridor stays in its lane for the next slice; otherwise it moves one lane.</summary>
            public float CorridorStayChance = 0.4f;

            /// <summary>Each lane drives at one speed, drawn from this band as a fraction of the player's cruise speed (oncoming lanes the other way).</summary>
            public float LaneSpeedMin = 0.4f;

            public float LaneSpeedMax = 0.7f;
        }

        [Serializable]
        public sealed class StreamGroup
        {
            /// <summary>A car comes out this far ahead of the player along the road (inside the fog's end), in metres.</summary>
            public float SpawnAheadMeters = 380f;

            /// <summary>A car goes this far behind the player (less than the road kept behind).</summary>
            public float DespawnBehindMeters = 20f;

            public int MaxActive = 24;
        }

        [Serializable]
        public sealed class LodGroup
        {
            /// <summary>Distance from the player where a car drops to LOD1 and to LOD2, in metres.</summary>
            public float NearMeters = 55f;

            public float FarMeters = 140f;

            /// <summary>A car must come this much back inside a limit to go up a level again.</summary>
            public float HysteresisMeters = 4f;
        }

        public LayoutGroup Layout = new LayoutGroup();
        public StreamGroup Stream = new StreamGroup();
        public LodGroup Lod = new LodGroup();

        /// <summary>How many times the player's speed can a car's distance grow while it is out: the planned stretch is this much longer than <see cref="StreamGroup.SpawnAheadMeters"/>.</summary>
        public double HorizonRatio => 1.0 / (1.0 - Layout.LaneSpeedMax);

        /// <exception cref="ArgumentException">A number makes no sense.</exception>
        public void Validate(float roadBehindMeters, float roadAheadMeters)
        {
            Require(Layout.ClearStartMeters >= 0f && Layout.SliceMeters > 0f, "Layout: ClearStartMeters not negative, SliceMeters positive.");
            Require(Layout.LaneChangeSeconds > 0f && Layout.PlayerLengthMeters > 0f, "Layout: LaneChangeSeconds and PlayerLengthMeters must be positive.");
            Require(Layout.PadMeters >= 0f && Layout.PadSeconds >= 0f && Layout.MinGapMeters >= 0f && Layout.GapRangeMeters >= 0f, "Layout: pads and gaps must not be negative.");
            Require(Layout.CorridorStayChance >= 0f && Layout.CorridorStayChance <= 1f, "Layout: CorridorStayChance must be in [0, 1].");
            Require(Layout.LaneSpeedMin > 0f && Layout.LaneSpeedMin <= Layout.LaneSpeedMax && Layout.LaneSpeedMax < 0.85f, "Layout: lane speeds need 0 < min <= max < 0.85 (a car as fast as the player would block the road for ever).");
            Require(Stream.SpawnAheadMeters > 0f && Stream.SpawnAheadMeters <= roadAheadMeters, "Stream: SpawnAheadMeters must be positive and inside the road ahead.");
            Require(Stream.DespawnBehindMeters >= 0f && Stream.DespawnBehindMeters < roadBehindMeters, "Stream: DespawnBehindMeters must be less than the road kept behind, so a car leaves before its piece does.");
            Require(Stream.MaxActive > 0, "Stream: MaxActive must be positive.");
            Require(Lod.NearMeters > 0f && Lod.NearMeters < Lod.FarMeters && Lod.HysteresisMeters >= 0f && Lod.HysteresisMeters < Lod.NearMeters, "Lod: 0 < Near < Far, Hysteresis in [0, Near).");
            Require(RoadSection.LaneCount >= 3, "Traffic needs at least three lanes (one stays free).");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new ArgumentException(message);
            }
        }
    }
}
