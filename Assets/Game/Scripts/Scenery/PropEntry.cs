using System;
using UnityEngine;

namespace WrongLine.Scenery
{
    /// <summary>The stand-in shape WrongLine/Scenery/Rebuild Props makes while the artist's model is not there.</summary>
    public enum PlaceholderShape
    {
        TreeCone,
        TreeRound,
        TreeBare,
        Bush,
        Rock,
        Cactus,
        StreetLamp,
        Sign,
        UtilityPole,
        Bale,
        Fence,
        Stake,
    }

    /// <summary>One kind of prop in the scenery catalog: its prefab and where it may stand.</summary>
    [Serializable]
    public sealed class PropEntry
    {
        [Tooltip("Name of the prop; the artist's model is Scenery/Art/<Biome>/<Name>.fbx.")]
        [SerializeField] private string _name;

        [Tooltip("Built by WrongLine/Scenery/Rebuild Props.")]
        [SerializeField] private SceneryPropView _prefab;

        [SerializeField] private PlaceholderShape _placeholder;

        [Tooltip("Stand-in colours: the main part (crown, rock, board) and the frame (trunk, pole).")]
        [SerializeField] private Color _mainColor = Color.white;

        [SerializeField] private Color _frameColor = Color.gray;

        [SerializeField] private PropMode _mode;

        [Tooltip("Scatter: props per 100 m of road, both sides together.")]
        [SerializeField, Min(0f)] private float _density = 1f;

        [Tooltip("Row: metres of road between two props.")]
        [SerializeField, Min(0f)] private float _spacing = 40f;

        [SerializeField] private RowSide _side;

        [Tooltip("Distance from the road's centre line, in metres. A row stands at the minimum.")]
        [SerializeField] private float _lateralMin = 12f;

        [SerializeField] private float _lateralMax = 40f;

        [Tooltip("Radius the prop needs on the ground, in metres.")]
        [SerializeField, Min(0f)] private float _footprint = 1f;

        [SerializeField] private Vector2 _scaleRange = Vector2.one;

        [SerializeField] private bool _castShadows;

        [Tooltip("A street lamp's head in the prop's own space (where its pool of light is centred); zero for a prop that is no lamp.")]
        [SerializeField] private Vector3 _lightHead;

        public string Name => _name;

        public Vector3 LightHead => _lightHead;

        public SceneryPropView Prefab => _prefab;

        public PlaceholderShape Placeholder => _placeholder;

        public Color MainColor => _mainColor;

        public Color FrameColor => _frameColor;

        public PropMode Mode => _mode;

        public float Density => _density;

        public float Spacing => _spacing;

        public RowSide Side => _side;

        public float LateralMin => _lateralMin;

        public float LateralMax => _lateralMax;

        public float Footprint => _footprint;

        public Vector2 ScaleRange => _scaleRange;

        public bool CastShadows => _castShadows;

        public PropRule ToRule(int id, int biome)
        {
            return new PropRule(id, biome, _mode, _density, _spacing, _side, _lateralMin, Math.Max(_lateralMin, _lateralMax), _footprint, _scaleRange.x, _scaleRange.y);
        }
    }
}
