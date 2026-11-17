using System;
using System.Collections.Generic;
using UnityEngine;

namespace WrongLine.Scenery
{
    /// <summary>A biome: the colour of its ground and the props that grow in it.</summary>
    [Serializable]
    public sealed class BiomeDef
    {
        [SerializeField] private string _name;

        [Tooltip("Multiplies the ground texture while the road runs through this biome.")]
        [SerializeField] private Color _groundColor = Color.white;

        [SerializeField] private List<PropEntry> _props = new List<PropEntry>();

        public string Name => _name;

        public Color GroundColor => _groundColor;

        public IReadOnlyList<PropEntry> Props => _props;
    }

    /// <summary>
    /// Every roadside prop: the ones every biome shares (street lamps, signs) and each biome's own. Its entries are edited here;
    /// WrongLine/Scenery/Rebuild Props makes their prefabs (the artist's model, or a stand-in).
    /// </summary>
    [CreateAssetMenu(menuName = "WrongLine/Scenery Catalog")]
    public sealed class SceneryCatalog : ScriptableObject
    {
        /// <summary>Folder name the shared props' art lives in.</summary>
        public const string SHARED_NAME = "Shared";

        [SerializeField] private List<PropEntry> _shared = new List<PropEntry>();
        [SerializeField] private List<BiomeDef> _biomes = new List<BiomeDef>();

        public IReadOnlyList<PropEntry> Shared => _shared;

        public IReadOnlyList<BiomeDef> Biomes => _biomes;
    }
}
