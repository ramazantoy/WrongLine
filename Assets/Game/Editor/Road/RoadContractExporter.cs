using System.Collections.Generic;
using System.IO;
using System.Text;
using AF.Core;
using UnityEditor;
using UnityEngine;

namespace WrongLine.Road.Editor
{
    /// <summary>
    /// Writes the contract the real road pieces are made from: <c>section.json</c> and one JSON per piece, into
    /// <c>Assets/Game/Road/Contract</c>. Running it twice changes no file.
    /// </summary>
    public static class RoadContractExporter
    {
        private const string FOLDER = "Assets/Game/Road/Contract";
        private const string RECIPES = "Assets/Game/Road/Recipes";

        public static RoadPieceContract Build(string name, in TrackRecipe recipe, bool isStart)
        {
            TrackShape shape = TrackIntegrator.Bake(recipe);
            var contract = new RoadPieceContract
            {
                name = name,
                isStart = isStart,
                tags = RoadTagger.From(recipe).ToString(),
                length = recipe.Length,
                yawDegrees = recipe.YawDegrees,
                yawProfile = recipe.YawProfile.ToString(),
                pitchDegrees = recipe.PitchDegrees,
                pitchProfile = recipe.PitchProfile.ToString(),
                bankDegrees = recipe.BankDegrees,
                frameStep = shape.Step,
                frameCount = shape.Count,
                positions = new float[shape.Count * 3],
                forwards = new float[shape.Count * 3],
                ups = new float[shape.Count * 3],
            };

            for (int i = 0; i < shape.Count; i++)
            {
                Write(contract.positions, i, shape[i].Position);
                Write(contract.forwards, i, shape[i].Forward);
                Write(contract.ups, i, shape[i].Up);
            }

            Vector3 exit = shape.Exit.Position;
            contract.exitPosition = new[] { exit.x, exit.y, exit.z };
            contract.exitYawDegrees = (float)(new RoadPieceInfo(0, name, recipe, isStart).ExitYaw * 180.0 / System.Math.PI);
            return contract;
        }

        public static RoadSectionContract BuildSection()
        {
            return new RoadSectionContract
            {
                notes = new[]
                {
                    "Unity space: left-handed, +Y up, +Z forward, +X right. Metres. To Blender (right-handed, +Z up): blender = (x, z, y), so Unity forward (+Z) is Blender +Y and right (+X) is +X; swapping two axes flips handedness, so flip the triangle winding too.",
                    "A piece's pivot is its entry frame: the first position (0,0,0), facing +Z, up +Y. The next piece's pivot sits on this piece's exit.",
                    "Every piece starts and ends level, upright and straight, so any piece joins any other.",
                    "The road surface is at y = 0 on the centre line (before banking). The asphalt spans -halfWidth..halfWidth across: laneCount lanes of laneWidth, with a lane line (lineWidth wide) at each of lineOffsets.",
                    "UV: u runs 0-1 across the asphalt (curbs continue past 0 and 1); v is the distance along the piece divided by tileLength.",
                    "The road is two lanes each way. The line between the directions (lineOffsets[centerLineIndex]) is drawn solid yellow, the other lines dashed white; the colour comes from the line texture read by the line's UV u, so every vertex of the line submesh needs the same u (across the asphalt) and v (along the piece) as the asphalt around it.",
                    "Frames are every frameStep metres along the piece; positions, forwards and ups are x y z triples per frame.",
                    "A piece's mesh has four submeshes in submeshOrder, one material slot each.",
                },
                halfWidth = RoadSection.HalfWidth,
                curbWidth = RoadSection.CurbWidth,
                curbHeight = RoadSection.CurbHeight,
                thickness = RoadSection.Thickness,
                lineWidth = RoadSection.LineWidth,
                laneCount = RoadSection.LaneCount,
                laneWidth = RoadSection.LaneWidth,
                oncomingLanes = RoadSection.OncomingLaneCount,
                centerLineIndex = RoadSection.CenterLineIndex,
                lineOffsets = LineOffsets(),
                lineHeight = RoadSection.LineHeight,
                tileLength = RoadSection.TileLength,
                submeshOrder = new[] { "asphalt", "curb", "line", "body" },
            };
        }

        private static float[] LineOffsets()
        {
            var offsets = new float[RoadSection.LineCount];
            for (int i = 0; i < offsets.Length; i++)
            {
                offsets[i] = RoadSection.LineX(i);
            }

            return offsets;
        }

        public static string ToJson(RoadPieceContract contract) => JsonUtility.ToJson(contract, true) + "\n";

        public static string ToJson(RoadSectionContract contract) => JsonUtility.ToJson(contract, true) + "\n";

        [MenuItem("WrongLine/Road/Export Recipes")]
        public static void ExportAll()
        {
            Directory.CreateDirectory(FOLDER);
            int written = 0;
            int unchanged = 0;
            var wanted = new HashSet<string>();

            Count(WriteIfChanged("section.json", ToJson(BuildSection()), wanted), ref written, ref unchanged);
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(RoadPieceRecipe)}", new[] { RECIPES }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<RoadPieceRecipe>(AssetDatabase.GUIDToAssetPath(guid));
                string json = ToJson(Build(asset.name, asset.Recipe, asset.IsStart));
                Count(WriteIfChanged(asset.name + ".json", json, wanted), ref written, ref unchanged);
            }

            // A piece that no longer exists leaves its file behind; say so rather than delete it.
            foreach (string file in Directory.GetFiles(FOLDER, "*.json"))
            {
                if (!wanted.Contains(Path.GetFileName(file)))
                {
                    Log.Warning($"[Road] {file} belongs to no recipe any more; delete it if the piece is gone.");
                }
            }

            AssetDatabase.Refresh();
            Log.Info($"[Road] Contract exported to {FOLDER}: {written} written, {unchanged} unchanged.");
        }

        private static bool WriteIfChanged(string fileName, string json, HashSet<string> wanted)
        {
            wanted.Add(fileName);
            string path = $"{FOLDER}/{fileName}";
            if (File.Exists(path) && File.ReadAllText(path) == json)
            {
                return false;
            }

            File.WriteAllText(path, json, new UTF8Encoding(false));
            return true;
        }

        private static void Count(bool wasWritten, ref int written, ref int unchanged)
        {
            if (wasWritten)
            {
                written++;
            }
            else
            {
                unchanged++;
            }
        }

        private static void Write(float[] target, int index, Vector3 value)
        {
            target[index * 3] = value.x;
            target[index * 3 + 1] = value.y;
            target[index * 3 + 2] = value.z;
        }
    }
}
