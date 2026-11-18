using NUnit.Framework;
using UnityEngine;
using WrongLine.Road;
using WrongLine.Road.Tests;

namespace WrongLine.Crash.Tests
{
    public class RoadProjectionTests
    {
        private static readonly WorldPoint Origin = new WorldPoint(0.0, 0.0, 0.0);

        private static IRoadPath Path(string pieceName)
        {
            RoadPieceInfo info = RoadTestPieces.Named(RoadTestPieces.All(), pieceName);
            return new SinglePiecePath(new PlacedPiece(0, info, RoadPose.Origin, 0.0));
        }

        private static Vector3 Point(IRoadPath path, double distance, float lateral, float height)
        {
            return RoadSampler.At(path, distance, lateral, height).Position.RelativeTo(Origin);
        }

        [Test]
        public void APointAboveAStraightRoadProjectsToItsDistance()
        {
            IRoadPath path = Path("Straight_XL");

            // Between the coarse samples (a metre apart): only the fine search gets it this close.
            Assert.AreEqual(70.37, RoadProjection.DistanceOf(path, Origin, Point(path, 70.37, 3f, 1.5f), 50.0), 0.05);
        }

        [Test]
        public void APointOnABendProjectsToItsDistance()
        {
            IRoadPath path = Path("CurveEasy_R");
            double length = path[0].Piece.Length;
            double wanted = length * 0.6 + 0.37;

            double found = RoadProjection.DistanceOf(path, Origin, Point(path, wanted, 4f, 1f), length * 0.5, 30.0);

            Assert.AreEqual(wanted, found, 0.1);
        }

        [Test]
        public void TheSearchFindsPointsBehindTheHintAsWellAsAheadOfIt()
        {
            IRoadPath path = Path("Straight_XL");

            Assert.AreEqual(30.0, RoadProjection.DistanceOf(path, Origin, Point(path, 30.0, -2f, 0.5f), 60.0, 40.0), 0.05);
            Assert.AreEqual(90.0, RoadProjection.DistanceOf(path, Origin, Point(path, 90.0, -2f, 0.5f), 60.0, 40.0), 0.05);
        }

        [Test]
        public void APointBeyondTheWindowIsNotFoundThere()
        {
            // The search only looks around the hint; what it returns is the nearest edge of its window.
            IRoadPath path = Path("Straight_XL");

            double found = RoadProjection.DistanceOf(path, Origin, Point(path, 110.0, 0f, 0.5f), 40.0, 20.0);

            // The window ends at 60; the fine search may step one more metre.
            Assert.LessOrEqual(found, 61.5);
            Assert.Less(found, 100.0, "it did not find the point at 110");
        }
    }
}
