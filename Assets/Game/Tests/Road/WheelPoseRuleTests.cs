using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace WrongLine.Road.Tests
{
    public class WheelPoseRuleTests
    {
        // The truck's axles and the van's (pivot not in the middle of its axles).
        private const float TruckFront = 2.6f;
        private const float TruckRear = -2.2f;
        private const float TruckTrack = 0.86f;

        private static readonly WorldPoint Origin = new WorldPoint(0.0, 0.0, 0.0);

        /// <summary>A road of one piece, laid at the origin (the generator cannot place every piece from the start).</summary>
        private static IRoadPath With(string pieceName, out PlacedPiece piece)
        {
            RoadPieceInfo info = RoadTestPieces.Named(RoadTestPieces.All(), pieceName);
            piece = new PlacedPiece(0, info, RoadPose.Origin, 0.0);
            return new SinglePiecePath(piece);
        }

        /// <summary>
        /// How far above or below the road each wheel of the posed car is, the worst of the four, in metres. The wheel's foot on the
        /// road is the surface point (searched on a 10 cm grid around where it should be) that is nearest in the road's own plane; the
        /// error is the wheel's height over the surface there, so it does not depend on how the car was placed.
        /// </summary>
        private static float WorstWheel(IRoadPath path, double distance, float lateral, int direction, Vector3 position, Quaternion rotation, float front, float rear, float track)
        {
            float worst = 0f;
            foreach (float z in new[] { front, rear })
            {
                foreach (int side in new[] { -1, 1 })
                {
                    Vector3 wheel = position + rotation * new Vector3(side * track, 0f, z);
                    double aroundS = distance + direction * z;
                    float aroundX = lateral + direction * side * track;
                    float bestPlane = float.MaxValue;
                    float height = 0f;
                    for (double s = aroundS - 3.0; s <= aroundS + 3.0; s += 0.1)
                    {
                        for (float x = aroundX - 1.5f; x <= aroundX + 1.5f; x += 0.1f)
                        {
                            RoadSample sample = RoadSampler.At(path, s, x);
                            Vector3 offset = wheel - sample.Position.RelativeTo(Origin);
                            Vector3 up = sample.Rotation * Vector3.up;
                            float vertical = Vector3.Dot(offset, up);
                            float plane = (offset - up * vertical).magnitude;
                            if (plane < bestPlane)
                            {
                                bestPlane = plane;
                                height = Mathf.Abs(vertical);
                            }
                        }
                    }

                    worst = Mathf.Max(worst, height);
                }
            }

            return worst;
        }

        private static float WorstOver(string pieceName, int direction, bool wheelPose)
        {
            IRoadPath layout = With(pieceName, out PlacedPiece piece);
            float worst = 0f;
            for (double d = piece.EntryDistance + 6.0; d < piece.EntryDistance + piece.Piece.Length - 6.0; d += 4.0)
            {
                foreach (float lateral in new[] { -4.5f, 1.5f, 4.5f })
                {
                    Vector3 position;
                    Quaternion rotation;
                    if (wheelPose)
                    {
                        WheelPoseRule.At(layout, d, lateral, direction, TruckFront, TruckRear, TruckTrack, Origin, out position, out rotation);
                    }
                    else
                    {
                        // The old way: the road's frame at the car's middle.
                        RoadSample sample = RoadSampler.At(layout, d, lateral);
                        position = sample.Position.RelativeTo(Origin);
                        rotation = direction > 0 ? sample.Rotation : sample.Rotation * Quaternion.Euler(0f, 180f, 0f);
                    }

                    worst = Mathf.Max(worst, WorstWheel(layout, d, lateral, direction, position, rotation, TruckFront, TruckRear, TruckTrack));
                }
            }

            return worst;
        }

        [Test]
        public void TheWheelsSitOnTheRoad_OnBankedAndSteepPieces_WhereTheMiddleFrameLeavesThemFloating()
        {
            foreach (string name in new[] { "BankSharp_R", "BankSharp_L", "SteepUp_S", "SteepDown_S", "SteepDown_L", "BankSoft_R", "Hill_S", "Dip_S" })
            {
                float wheels = WorstOver(name, 1, true);
                Assert.Less(wheels, 0.10f, $"{name}: a wheel is {wheels * 100f:F0} cm off the road");
            }

            Assert.Greater(WorstOver("BankSharp_R", 1, false), 0.25f, "The old placement was off by tens of centimetres on a sharp bank: this test must see the difference.");
            Assert.Greater(WorstOver("SteepUp_S", 1, false), 0.2f);
        }

        [Test]
        public void ACarComingTheOtherWay_SitsOnTheRoadToo_FacingBackAlongIt()
        {
            foreach (string name in new[] { "BankSharp_R", "SteepDown_S" })
            {
                Assert.Less(WorstOver(name, -1, true), 0.10f, name);
            }

            IRoadPath layout = With("Straight_M", out PlacedPiece piece);
            double d = piece.EntryDistance + 20.0;
            WheelPoseRule.At(layout, d, -4.5f, -1, TruckFront, TruckRear, TruckTrack, Origin, out Vector3 position, out Quaternion rotation);
            RoadSample road = RoadSampler.At(layout, d, -4.5f);

            Assert.Less(Vector3.Dot(rotation * Vector3.forward, road.Rotation * Vector3.forward), -0.999f);
            Assert.Greater(Vector3.Dot(rotation * Vector3.up, road.Rotation * Vector3.up), 0.999f);
        }

        [Test]
        public void OnAFlatStraight_TheCarStandsWhereTheRoadSaysAndFollowsItsOrientation()
        {
            IRoadPath layout = With("Straight_M", out PlacedPiece piece);
            double d = piece.EntryDistance + 20.0;

            WheelPoseRule.At(layout, d, 1.5f, 1, TruckFront, TruckRear, TruckTrack, Origin, out Vector3 position, out Quaternion rotation);
            RoadSample road = RoadSampler.At(layout, d, 1.5f);

            // The truck's pivot is 0.2 m ahead of the middle of its axles: the pivot must still end up at the lane's middle.
            Assert.AreEqual(0f, Vector3.Distance(position, road.Position.RelativeTo(Origin)), 1e-3f);
            Assert.Less(Quaternion.Angle(rotation, road.Rotation), 0.05f);
        }

        [Test]
        public void ThePivotIsOffsetByTheAxlesNotCentredBetweenThem_OnASlope()
        {
            IRoadPath layout = With("SteepUp_S", out PlacedPiece piece);
            double d = piece.EntryDistance + piece.Piece.Length * 0.5;

            // A van-like car: axles at +1.65 and −1.5, so its pivot is 0.075 m behind the middle of the axles.
            WheelPoseRule.At(layout, d, 0f, 1, 1.65f, -1.5f, 0.84f, Origin, out Vector3 position, out Quaternion rotation);
            Vector3 front = RoadSampler.At(layout, d + 1.65, 0f).Position.RelativeTo(Origin);
            Vector3 rear = RoadSampler.At(layout, d - 1.5, 0f).Position.RelativeTo(Origin);
            Vector3 axleMiddle = (front + rear) * 0.5f;

            Assert.AreEqual(0.075f, Vector3.Dot(axleMiddle - position, rotation * Vector3.forward), 0.01f);
        }
    }

    /// <summary>A road path of a single piece.</summary>
    internal sealed class SinglePiecePath : IRoadPath
    {
        private readonly PlacedPiece _piece;

        public SinglePiecePath(PlacedPiece piece)
        {
            _piece = piece;
        }

        public int FirstIndex => 0;

        public int EndIndex => 1;

        public double EndDistance => _piece.ExitDistance;

        public PlacedPiece this[int index] => _piece;

        public void EnsureCovered(double distance)
        {
        }

        public void DiscardBefore(int index)
        {
        }
    }
}
