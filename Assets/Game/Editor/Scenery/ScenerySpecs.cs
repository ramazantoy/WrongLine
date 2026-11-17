using UnityEngine;

namespace WrongLine.Scenery.Editor
{
    /// <summary>
    /// The catalog Rebuild Props writes when there is none yet: the shared props and four biomes, with stand-in shapes and
    /// colours until the artist's models arrive. Once the catalog exists it is edited in the Inspector, never overwritten.
    /// </summary>
    internal static class ScenerySpecs
    {
        internal readonly struct Biome
        {
            public readonly string Name;
            public readonly Color Ground;

            public Biome(string name, string groundHex)
            {
                Name = name;
                Ground = Hex(groundHex);
            }
        }

        internal readonly struct Prop
        {
            public readonly string Biome;
            public readonly string Name;
            public readonly PlaceholderShape Shape;
            public readonly Color Main;
            public readonly Color Frame;
            public readonly PropMode Mode;
            public readonly float DensityOrSpacing;
            public readonly RowSide Side;
            public readonly float LateralMin;
            public readonly float LateralMax;
            public readonly float Footprint;
            public readonly Vector2 Scale;
            public readonly bool Shadows;

            public Prop(string biome, string name, PlaceholderShape shape, string main, string frame, PropMode mode, float densityOrSpacing, RowSide side,
                float lateralMin, float lateralMax, float footprint, float scaleMin, float scaleMax, bool shadows)
            {
                Biome = biome;
                Name = name;
                Shape = shape;
                Main = Hex(main);
                Frame = Hex(frame);
                Mode = mode;
                DensityOrSpacing = densityOrSpacing;
                Side = side;
                LateralMin = lateralMin;
                LateralMax = lateralMax;
                Footprint = footprint;
                Scale = new Vector2(scaleMin, scaleMax);
                Shadows = shadows;
            }
        }

        public static readonly Biome[] Biomes =
        {
            new Biome("Meadow", "94CD68"),
            new Biome("Autumn", "DBA35C"),
            new Biome("Desert", "FFF1AF"),
            new Biome("Snow", "FFFFFF"),
        };

        private const PropMode S = PropMode.Scatter;
        private const PropMode R = PropMode.Row;
        private const RowSide Alt = RowSide.Alternate;

        // Lateral minimums keep SceneryPlanner.Clearance(footprint) = 6.6 + footprint + 1.5 m, plus room for a banked road.
        public static readonly Prop[] Props =
        {
            new Prop(SceneryCatalog.SHARED_NAME, "Shared_StreetLamp", PlaceholderShape.StreetLamp, "FFF3C4", "9AA0AD", R, 40f, Alt, 9f, 9f, 0.4f, 1f, 1f, true),
            new Prop(SceneryCatalog.SHARED_NAME, "Shared_RoadSign", PlaceholderShape.Sign, "3A7FC8", "9AA0AD", S, 0.4f, Alt, 9.5f, 11f, 0.6f, 1f, 1f, false),
            new Prop(SceneryCatalog.SHARED_NAME, "Shared_UtilityPole", PlaceholderShape.UtilityPole, "6B5440", "6B5440", R, 60f, RowSide.Right, 15f, 15f, 0.4f, 1f, 1f, false),

            new Prop("Meadow", "Meadow_TreeRound_01", PlaceholderShape.TreeRound, "5E8F4E", "7A5A3C", S, 4f, Alt, 11f, 45f, 2f, 0.8f, 1.3f, true),
            new Prop("Meadow", "Meadow_TreeRound_02", PlaceholderShape.TreeRound, "6FA35A", "7A5A3C", S, 3f, Alt, 14f, 60f, 2f, 0.9f, 1.4f, true),
            new Prop("Meadow", "Meadow_Pine_01", PlaceholderShape.TreeCone, "4F7F45", "6B4F3A", S, 4f, Alt, 12f, 70f, 1.8f, 0.8f, 1.5f, true),
            new Prop("Meadow", "Meadow_Bush_01", PlaceholderShape.Bush, "5B8A47", "5B8A47", S, 6f, Alt, 9.5f, 30f, 1f, 0.7f, 1.3f, false),
            new Prop("Meadow", "Meadow_Rock_01", PlaceholderShape.Rock, "8D8A84", "8D8A84", S, 2f, Alt, 10f, 50f, 1.2f, 0.7f, 1.6f, false),

            new Prop("Autumn", "Autumn_Tree_01", PlaceholderShape.TreeRound, "C8642E", "6B4F3A", S, 4f, Alt, 11f, 45f, 2f, 0.8f, 1.3f, true),
            new Prop("Autumn", "Autumn_Tree_02", PlaceholderShape.TreeRound, "D9A23A", "6B4F3A", S, 3f, Alt, 14f, 60f, 2f, 0.9f, 1.4f, true),
            new Prop("Autumn", "Autumn_TreeBare_01", PlaceholderShape.TreeBare, "6B4F3A", "6B4F3A", S, 3f, Alt, 11f, 50f, 1.5f, 0.8f, 1.3f, true),
            new Prop("Autumn", "Autumn_HayBale_01", PlaceholderShape.Bale, "D8B860", "C8A850", S, 3f, Alt, 10f, 40f, 1f, 0.9f, 1.1f, false),
            new Prop("Autumn", "Autumn_Fence_01", PlaceholderShape.Fence, "8A6A4A", "6B4F3A", S, 1.5f, Alt, 11f, 20f, 2f, 1f, 1f, false),

            new Prop("Desert", "Desert_Cactus_01", PlaceholderShape.Cactus, "4E8A4A", "4E8A4A", S, 4f, Alt, 9.5f, 45f, 0.8f, 0.8f, 1.3f, true),
            new Prop("Desert", "Desert_Cactus_02", PlaceholderShape.Cactus, "5F9A52", "5F9A52", S, 3f, Alt, 9.5f, 50f, 0.8f, 0.6f, 1f, true),
            new Prop("Desert", "Desert_Rocks_01", PlaceholderShape.Rock, "B5704A", "B5704A", S, 3f, Alt, 11f, 40f, 2f, 0.8f, 1.3f, false),
            new Prop("Desert", "Desert_Rocks_02", PlaceholderShape.Rock, "C99060", "C99060", S, 1.5f, Alt, 25f, 70f, 5f, 0.8f, 1.2f, false),
            new Prop("Desert", "Desert_DryBush_01", PlaceholderShape.Bush, "9C8A55", "9C8A55", S, 5f, Alt, 9.5f, 35f, 1f, 0.6f, 1f, false),

            new Prop("Snow", "Snow_Pine_01", PlaceholderShape.TreeCone, "5C7F6A", "6B4F3A", S, 6f, Alt, 12f, 60f, 1.8f, 0.8f, 1.5f, true),
            new Prop("Snow", "Snow_Pine_02", PlaceholderShape.TreeCone, "4A6E58", "6B4F3A", S, 4f, Alt, 14f, 70f, 1.8f, 1f, 1.7f, true),
            new Prop("Snow", "Snow_Rock_01", PlaceholderShape.Rock, "9AA3AD", "9AA3AD", S, 3f, Alt, 10f, 50f, 1.2f, 0.8f, 1.8f, false),
            new Prop("Snow", "Snow_Stake_01", PlaceholderShape.Stake, "E05A3A", "F2F2F2", R, 25f, Alt, 8.5f, 8.5f, 0.2f, 1f, 1f, false),
        };

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;
        }
    }
}
