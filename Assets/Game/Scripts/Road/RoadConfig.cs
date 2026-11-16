using System;
using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>Every tunable number of the endless road. Read at the start of a run; edit it in the Inspector between runs.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Road Config")]
    public sealed class RoadConfig : ScriptableObject
    {
        [Tooltip("0 gives a new random road every run; any other number gives the same road every run.")]
        [SerializeField] private int _seed;

        [Tooltip("Road built ahead of the follower, in metres.")]
        [SerializeField] private float _aheadMeters = 300f;

        [Tooltip("Road kept behind the follower, in metres.")]
        [SerializeField] private float _behindMeters = 60f;

        [Tooltip("The scene origin jumps to the follower once it is this far from it, in metres. Keeps float positions precise.")]
        [SerializeField] private float _originShiftMeters = 500f;

        [Tooltip("Instances of each piece made while the menu is up, so the run does not stall creating them.")]
        [SerializeField, Range(0, 16)] private int _prewarmPerPiece = 3;

        [SerializeField] private RoadGeneratorSettings _generator = new RoadGeneratorSettings();

        public int Seed => _seed;

        public float AheadMeters => _aheadMeters;

        public float BehindMeters => _behindMeters;

        public float OriginShiftMeters => _originShiftMeters;

        public int PrewarmPerPiece => _prewarmPerPiece;

        public RoadGeneratorSettings Generator => _generator;

        /// <exception cref="ArgumentException">A number makes no sense.</exception>
        public void Validate()
        {
            if (!(_aheadMeters > 0f) || !(_behindMeters >= 0f))
            {
                throw new ArgumentException("AheadMeters must be positive and BehindMeters not negative.");
            }

            if (!(_originShiftMeters > 0f))
            {
                throw new ArgumentException("OriginShiftMeters must be positive.");
            }

            if (_prewarmPerPiece < 0)
            {
                throw new ArgumentException("PrewarmPerPiece must not be negative.");
            }

            _generator.Validate();
        }
    }
}
