using NUnit.Framework;
using UnityEngine;
using WrongLine.Road;
using WrongLine.Road.Tests;

namespace WrongLine.Scenery.Tests
{
    public class GroundBackdropRuleTests
    {
        [Test]
        public void TheTarget_IsBelowEveryPartOfEveryPieceInTheRange_ByTheDepth()
        {
            for (ulong seed = 1; seed <= 5; seed++)
            {
                RoadLayout layout = RoadTestPieces.Layout(seed);
                layout.EnsureCovered(4000.0);
                for (int first = layout.FirstIndex; first + 6 < layout.EndIndex; first += 3)
                {
                    int last = first + 6;
                    double target = GroundBackdropRule.Target(layout, first, last, 3.0);
                    double lowest = double.MaxValue;
                    for (double d = layout[first].EntryDistance; d <= layout[last].ExitDistance; d += 1.0)
                    {
                        lowest = System.Math.Min(lowest, RoadSampler.At(layout, d).Position.Y);
                    }

                    Assert.LessOrEqual(target, lowest - 3.0 + 1e-3, $"seed {seed}, pieces {first}-{last}: the disc would reach the road.");
                    Assert.Greater(target, lowest - 3.0 - 0.5, "Not much lower than it needs to be.");
                }
            }
        }

        [Test]
        public void Next_DropsAtOnce_ButRisesNoFasterThanItsSpeed()
        {
            Assert.AreEqual(-40.0, GroundBackdropRule.Next(-3.0, -40.0, 0.016f, 4f), 1e-9, "A lower piece came in: down at once.");
            Assert.AreEqual(-39.0, GroundBackdropRule.Next(-40.0, -3.0, 0.25f, 4f), 1e-9, "Up by speed × time.");
            Assert.AreEqual(-3.0, GroundBackdropRule.Next(-3.5, -3.0, 1f, 4f), 1e-9, "Never past the target.");
            Assert.AreEqual(-40.0, GroundBackdropRule.Next(-40.0, -3.0, 0f, 4f), 1e-9, "No time, no rise.");
        }

        [Test]
        public void TheDisc_FacesUp_AndReachesItsRadius()
        {
            Mesh mesh = DiscMesh.Build(600f, 10, 32);
            try
            {
                Vector3[] v = mesh.vertices;
                int[] t = mesh.triangles;
                for (int i = 0; i < t.Length; i += 3)
                {
                    Assert.Greater(Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).y, 0f, $"triangle {i / 3}");
                }

                Assert.AreEqual(600f, mesh.bounds.extents.x, 1f);
                Assert.AreEqual(0f, mesh.bounds.extents.y, 1e-5f, "Flat.");
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
