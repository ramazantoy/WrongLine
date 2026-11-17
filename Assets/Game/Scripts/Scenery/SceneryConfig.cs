using System;
using UnityEngine;

namespace WrongLine.Scenery
{
    /// <summary>Every tunable number of the roadside: biome lengths, the ground strip and how many props wait in the pool.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Scenery Config")]
    public sealed class SceneryConfig : ScriptableObject
    {
        [SerializeField] private BiomeSettings _biomes = new BiomeSettings();

        [SerializeField] private GroundSettings _ground = new GroundSettings();

        [Tooltip("Instances of each prop made while the menu is up. Pools grow on demand past it.")]
        [SerializeField, Range(0, 32)] private int _prewarmPerProp = 4;

        [Header("Backdrop ground")]
        [Tooltip("The ground strips' material (Road/Materials/Ground.mat): the wide disc under the camera that fills the gaps uses it too, so it has the same texture and biome colour.")]
        [SerializeField] private Material _backdropMaterial;

        [Tooltip("How far below the lowest point of the road pieces in the scene the disc sits, in metres. A banked edge sits up to ~2.3 m below the centre line and the road body hangs 1 m below that.")]
        [SerializeField, Min(0f)] private float _backdropDepth = 5f;

        [Tooltip("Radius of the disc, in metres: past the fog.")]
        [SerializeField, Min(1f)] private float _backdropRadius = 600f;

        [Tooltip("How fast the disc rises when the low pieces leave, in metres a second (it drops at once).")]
        [SerializeField, Min(0f)] private float _backdropRiseSpeed = 4f;

        public Material BackdropMaterial => _backdropMaterial;

        public float BackdropDepth => _backdropDepth;

        public float BackdropRadius => _backdropRadius;

        public float BackdropRiseSpeed => _backdropRiseSpeed;

        public BiomeSettings Biomes => _biomes;

        public GroundSettings Ground => _ground;

        public int PrewarmPerProp => _prewarmPerProp;

        /// <exception cref="ArgumentException">A number makes no sense.</exception>
        public void Validate()
        {
            _biomes.Validate();
            _ground.Validate();
            if (_prewarmPerProp < 0)
            {
                throw new ArgumentException("PrewarmPerProp must not be negative.");
            }
        }
    }
}
