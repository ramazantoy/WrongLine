using UnityEditor;
using UnityEngine;

namespace WrongLine.Scenery.Editor
{
    /// <summary>Where the roadside's assets live, and the config made on first use.</summary>
    public static class SceneryAssets
    {
        public const string ROOT = "Assets/Game/Scenery";
        public const string ART = ROOT + "/Art";
        public const string PREFABS = ROOT + "/Prefabs";
        public const string MESHES = ROOT + "/Meshes";
        public const string MATERIALS = ROOT + "/Materials";
        public const string CATALOG_PATH = ROOT + "/SceneryCatalog.asset";
        public const string CONFIG_PATH = ROOT + "/SceneryConfig.asset";

        /// <summary>The ground's detail texture (grey, tileable), tinted per biome; white while it is not delivered.</summary>
        public const string GROUND_TEXTURE_PATH = ART + "/Ground_Detail.png";

        /// <summary>Metres one repeat of the ground texture covers. Must divide BiomeBlendDriver.GroundUvPeriod (1024 m).</summary>
        public const float GroundTileMeters = 16f;

        public static string ArtFolder(string biome) => $"{ART}/{biome}";

        public static string ArtModelPath(string biome, string prop) => $"{ArtFolder(biome)}/{prop}.fbx";

        /// <summary>The biome's palette texture, which every model of the biome shares.</summary>
        public static string ArtAtlasPath(string biome) => $"{ArtFolder(biome)}/{biome}_Atlas.png";

        /// <summary>The biome's emission texture (the parts that light up at night, black elsewhere), when it has one.</summary>
        public static string ArtEmissionPath(string biome) => $"{ArtFolder(biome)}/{biome}_Emission.png";

        public static SceneryConfig EnsureConfig()
        {
            EnsureFolder(ROOT);
            var config = AssetDatabase.LoadAssetAtPath<SceneryConfig>(CONFIG_PATH);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<SceneryConfig>();
                AssetDatabase.CreateAsset(config, CONFIG_PATH);
                AssetDatabase.SaveAssetIfDirty(config);
            }

            return config;
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            int split = folder.LastIndexOf('/');
            EnsureFolder(folder.Substring(0, split));
            AssetDatabase.CreateFolder(folder.Substring(0, split), folder.Substring(split + 1));
        }
    }
}
