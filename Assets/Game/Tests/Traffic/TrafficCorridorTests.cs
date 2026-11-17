using NUnit.Framework;

namespace WrongLine.Traffic.Tests
{
    public class TrafficCorridorTests
    {
        [Test]
        public void ItStartsInTheStartLane()
        {
            var corridor = new TrafficCorridor(1UL, 2, 150.0, 30.0, 0.4f);

            Assert.AreEqual(2, corridor.LaneOf(0));
        }

        [Test]
        public void ItMovesAtMostOneLaneBetweenSlices_AndStaysOnTheRoad()
        {
            for (ulong seed = 1; seed <= 50; seed++)
            {
                var corridor = new TrafficCorridor(seed, 2, 150.0, 30.0, 0.4f);
                int previous = corridor.LaneOf(0);
                for (int slice = 1; slice < 300; slice++)
                {
                    int lane = corridor.LaneOf(slice);
                    Assert.LessOrEqual(System.Math.Abs(lane - previous), 1, $"seed {seed} slice {slice}");
                    Assert.That(lane, Is.InRange(0, 3));
                    previous = lane;
                }
            }
        }

        [Test]
        public void AskingInADifferentOrder_GivesTheSameLanes()
        {
            var forward = new TrafficCorridor(9UL, 2, 150.0, 30.0, 0.4f);
            var jumping = new TrafficCorridor(9UL, 2, 150.0, 30.0, 0.4f);

            int far = jumping.LaneOf(80);
            for (int slice = 0; slice <= 80; slice++)
            {
                forward.LaneOf(slice);
            }

            Assert.AreEqual(forward.LaneOf(80), far);
            Assert.AreEqual(forward.LaneOf(40), jumping.LaneOf(40));
        }

        [Test]
        public void ASlicesClearZoneReachesHalfASliceIntoItsNeighbours()
        {
            var corridor = new TrafficCorridor(3UL, 2, 150.0, 30.0, 0.4f);
            int first = corridor.LaneOf(0);

            Assert.IsTrue(corridor.Reserves(first, 160.0, 170.0));
            Assert.IsTrue(corridor.Reserves(first, 190.0, 190.0), "half a slice past the first slice's end (180 + 15 = 195)");
            Assert.IsFalse(corridor.Reserves(first, 100.0, 120.0), "before the clear start's zone (150 - 15)");
        }
    }
}
