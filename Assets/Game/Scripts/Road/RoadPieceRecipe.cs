using System;
using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// The authoring asset for one road piece: the numbers its mesh and its centre line are baked from. Editing it and running
    /// "WrongLine/Road/Rebuild Pieces" regenerates the piece's mesh and prefab.
    /// </summary>
    [CreateAssetMenu(menuName = "WrongLine/Road Piece Recipe")]
    public sealed class RoadPieceRecipe : ScriptableObject
    {
        private const float MaxYaw = 170f;
        private const float MaxPitch = 80f;
        private const float MaxBank = 60f;

        [SerializeField] private float _length = 40f;
        [SerializeField] private float _yawDegrees;
        [SerializeField] private YawProfile _yawProfile;
        [SerializeField] private float _pitchDegrees;
        [SerializeField] private PitchProfile _pitchProfile;
        [SerializeField] private float _bankDegrees;
        [Tooltip("The fixed straight piece a run begins on. The generator never picks it.")]
        [SerializeField] private bool _isStart;

        [NonSerialized] private TrackShape _shape;

        public bool IsStart => _isStart;

        public TrackRecipe Recipe => new TrackRecipe(_length, _yawDegrees, _yawProfile, _pitchDegrees, _pitchProfile, _bankDegrees);

        public RoadTags Tags => RoadTagger.From(Recipe);

        /// <summary>The baked centre line, built on first use and rebuilt after an edit.</summary>
        public TrackShape Shape => _shape ??= TrackIntegrator.Bake(Recipe);

        private void OnValidate()
        {
            _length = Mathf.Max(2f * TrackIntegrator.Step, Mathf.Round(_length / TrackIntegrator.Step) * TrackIntegrator.Step);
            _yawDegrees = Mathf.Clamp(_yawDegrees, -MaxYaw, MaxYaw);
            _pitchDegrees = Mathf.Clamp(_pitchDegrees, -MaxPitch, MaxPitch);
            _bankDegrees = Mathf.Clamp(_bankDegrees, -MaxBank, MaxBank);
            _shape = null;
        }
    }
}
