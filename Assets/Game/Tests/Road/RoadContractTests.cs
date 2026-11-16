using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WrongLine.Road.Editor;

namespace WrongLine.Road.Tests
{
    public class RoadContractTests
    {
        private static readonly TrackRecipe Banked = new TrackRecipe(60f, 35f, YawProfile.Single, 8f, PitchProfile.Bump, 12f);

        [Test]
        public void ThePieceContract_CarriesTheRecipeAndTheBakedCentreLine()
        {
            RoadPieceContract contract = RoadContractExporter.Build("Demo", Banked, false);
            TrackShape shape = TrackIntegrator.Bake(Banked);

            Assert.AreEqual("Demo", contract.name);
            Assert.AreEqual(60f, contract.length);
            Assert.AreEqual(35f, contract.yawDegrees);
            Assert.AreEqual("Single", contract.yawProfile);
            Assert.AreEqual(8f, contract.pitchDegrees);
            Assert.AreEqual("Bump", contract.pitchProfile);
            Assert.AreEqual(12f, contract.bankDegrees);
            Assert.AreEqual("Curve, Climb, Banked", contract.tags);
            Assert.AreEqual(shape.Count, contract.frameCount);
            Assert.AreEqual(TrackIntegrator.Step, contract.frameStep);
            Assert.AreEqual(shape.Count * 3, contract.positions.Length);
            Assert.AreEqual(shape.Count * 3, contract.forwards.Length);
            Assert.AreEqual(shape.Count * 3, contract.ups.Length);
        }

        [Test]
        public void TheFirstFrameIsThePivot_AndTheLastIsTheExit()
        {
            RoadPieceContract contract = RoadContractExporter.Build("Demo", Banked, false);
            TrackShape shape = TrackIntegrator.Bake(Banked);
            int last = (contract.frameCount - 1) * 3;

            Assert.AreEqual(Vector3.zero, new Vector3(contract.positions[0], contract.positions[1], contract.positions[2]));
            Assert.AreEqual(0f, Vector3.Distance(shape.Exit.Position, new Vector3(contract.positions[last], contract.positions[last + 1], contract.positions[last + 2])), 1e-6f);
            Assert.AreEqual(0f, Vector3.Distance(shape.Exit.Position, new Vector3(contract.exitPosition[0], contract.exitPosition[1], contract.exitPosition[2])), 1e-6f);
            Assert.AreEqual(35f, contract.exitYawDegrees, 1e-3f);
        }

        [Test]
        public void EveryFrame_HasAUnitForwardAndAnUpAtRightAnglesToIt()
        {
            RoadPieceContract contract = RoadContractExporter.Build("Demo", Banked, false);

            for (int i = 0; i < contract.frameCount; i++)
            {
                var forward = new Vector3(contract.forwards[i * 3], contract.forwards[i * 3 + 1], contract.forwards[i * 3 + 2]);
                var up = new Vector3(contract.ups[i * 3], contract.ups[i * 3 + 1], contract.ups[i * 3 + 2]);

                Assert.AreEqual(1f, forward.magnitude, 1e-4f, $"Frame {i}.");
                Assert.AreEqual(1f, up.magnitude, 1e-4f, $"Frame {i}.");
                Assert.AreEqual(0f, Vector3.Dot(forward, up), 1e-4f, $"Frame {i}.");
            }
        }

        [Test]
        public void TheJson_RoundTripsAndIsTheSameEveryTime()
        {
            RoadPieceContract contract = RoadContractExporter.Build("Demo", Banked, false);

            string json = RoadContractExporter.ToJson(contract);

            Assert.AreEqual(json, RoadContractExporter.ToJson(RoadContractExporter.Build("Demo", Banked, false)));
            RoadPieceContract read = JsonUtility.FromJson<RoadPieceContract>(json);
            Assert.AreEqual(contract.frameCount, read.frameCount);
            CollectionAssert.AreEqual(contract.positions, read.positions);
            CollectionAssert.AreEqual(contract.ups, read.ups);
            StringAssert.EndsWith("\n", json);
        }

        [Test]
        public void TheSectionContract_IsTheCodeConstants_NotACopy()
        {
            RoadSectionContract section = RoadContractExporter.BuildSection();

            Assert.AreEqual(RoadSection.HalfWidth, section.halfWidth);
            Assert.AreEqual(RoadSection.CurbWidth, section.curbWidth);
            Assert.AreEqual(RoadSection.CurbHeight, section.curbHeight);
            Assert.AreEqual(RoadSection.Thickness, section.thickness);
            Assert.AreEqual(RoadSection.LineWidth, section.lineWidth);
            Assert.AreEqual(RoadSection.LineOffset, section.lineOffset);
            Assert.AreEqual(RoadSection.LineHeight, section.lineHeight);
            Assert.AreEqual(RoadSection.TileLength, section.tileLength);
            CollectionAssert.AreEqual(new[] { "asphalt", "curb", "line", "body" }, section.submeshOrder);
            Assert.AreEqual(RoadMeshBuilder.SubmeshCount, section.submeshOrder.Length);
            Assert.AreEqual(RoadMeshBuilder.SubmeshAsphalt, System.Array.IndexOf(section.submeshOrder, "asphalt"));
            Assert.AreEqual(RoadMeshBuilder.SubmeshBody, System.Array.IndexOf(section.submeshOrder, "body"));
        }

        [Test]
        public void EveryDefaultPiece_Exports()
        {
            foreach (RoadPieceSpecs.Spec spec in RoadPieceSpecs.Defaults)
            {
                RoadPieceContract contract = RoadContractExporter.Build(spec.Name, spec.Recipe, spec.IsStart);

                Assert.AreEqual(spec.IsStart, contract.isStart, spec.Name);
                Assert.IsTrue(contract.positions.All(value => !float.IsNaN(value) && !float.IsInfinity(value)), spec.Name);
                Assert.AreEqual(contract.frameCount * 3, contract.positions.Length, spec.Name);
            }
        }
    }
}
