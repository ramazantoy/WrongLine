using NUnit.Framework;
using UnityEngine;
using WrongLine.Road.Editor;

namespace WrongLine.Road.Tests
{
    public class RoadLineTextureTests
    {
        private const int W = RoadLineTexture.Width;
        private const int H = RoadLineTexture.Height;

        private static Color32 Sample(Color32[] pixels, float u, int row) => pixels[row * W + Mathf.Clamp((int)(u * W), 0, W - 1)];

        [Test]
        public void TheLineBetweenTheTwoDirections_IsSolidYellow_AlongTheWholeRepeat()
        {
            Color32[] pixels = RoadLineTexture.Build(W, H);
            float u = RoadLineTexture.LineU(RoadSection.CenterLineIndex);

            for (int row = 0; row < H; row++)
            {
                Color32 c = Sample(pixels, u, row);
                Assert.AreEqual(255, c.a, $"row {row}: no gap in the yellow line");
                Assert.AreEqual(RoadLineTexture.Yellow.r, c.r);
                Assert.AreEqual(RoadLineTexture.Yellow.g, c.g);
                Assert.AreEqual(RoadLineTexture.Yellow.b, c.b);
            }
        }

        [Test]
        public void EveryOtherLine_IsWhiteAndDashed_WithTheDashFirst()
        {
            Color32[] pixels = RoadLineTexture.Build(W, H);
            int dashRows = RoadLineTexture.DashRows(H);

            for (int line = 0; line < RoadSection.LineCount; line++)
            {
                if (line == RoadSection.CenterLineIndex)
                {
                    continue;
                }

                float u = RoadLineTexture.LineU(line);
                for (int row = 0; row < H; row++)
                {
                    Color32 c = Sample(pixels, u, row);
                    Assert.AreEqual(row < dashRows ? 255 : 0, c.a, $"line {line} row {row}");
                    Assert.AreEqual(255, c.r);
                    Assert.AreEqual(255, c.g);
                    Assert.AreEqual(255, c.b);
                }
            }
        }

        [Test]
        public void TheDashIsAsLongAsTheRoadSectionSays_OutOfItsPeriod()
        {
            Assert.AreEqual(RoadSection.LineDashLength / RoadSection.LineDashPeriod, RoadLineTexture.DashRows(H) / (float)H, 0.5f / H);
        }

        [Test]
        public void TheMeshLines_LandOnTheirOwnColumns_ThroughTheContractUvs()
        {
            // Not the texture alone: the UVs the mesh builder gives each line must reach the column painted for that line.
            TrackShape shape = TrackIntegrator.Bake(TrackRecipe.Straight(40f));
            Mesh mesh = RoadMeshBuilder.Build(shape, shape.Count - 1);
            try
            {
                Color32[] pixels = RoadLineTexture.Build(W, H);
                var vertices = mesh.vertices;
                var uvs = mesh.uv;
                int[] line = mesh.GetIndices(RoadMeshBuilder.SubmeshLine);
                int yellow = 0;
                int white = 0;
                foreach (int i in line)
                {
                    Color32 c = Sample(pixels, uvs[i].x, 0); // Row 0 is inside every dash.
                    bool atCentre = Mathf.Abs(vertices[i].x) < 0.5f;
                    Assert.AreEqual(atCentre ? RoadLineTexture.Yellow.g : RoadLineTexture.White.g, c.g, $"vertex at x {vertices[i].x}");
                    yellow += atCentre ? 1 : 0;
                    white += atCentre ? 0 : 1;
                }

                Assert.Greater(yellow, 0);
                Assert.Greater(white, 0);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
