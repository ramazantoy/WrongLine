using System.Collections.Generic;
using NUnit.Framework;

namespace WrongLine.Traffic.Tests
{
    public class TrafficPassabilityTests
    {
        private static TrafficCarPlan Block(int lane, double from, double to)
        {
            double sigma = (from + to) * 0.5;
            return new TrafficCarPlan(0, 0, 0, lane, sigma, (to - from) * 0.5, 10.0, 0.0, 4f);
        }

        private static TrafficPassability.Result Check(List<TrafficCarPlan> cars, int change = 10)
        {
            return TrafficPassability.Check(cars, 2, 0, 500, change);
        }

        [Test]
        public void AnEmptyRoad_CanBeDrivenThrough()
        {
            Assert.IsTrue(Check(new List<TrafficCarPlan>()).Passable);
        }

        [Test]
        public void AWallAcrossEveryLane_StopsThePlayerThere()
        {
            var cars = new List<TrafficCarPlan>();
            for (int lane = 0; lane < 4; lane++)
            {
                cars.Add(Block(lane, 200.0, 210.0));
            }

            TrafficPassability.Result result = Check(cars);

            Assert.IsFalse(result.Passable);
            Assert.LessOrEqual(result.ReachedMeters, 200);
        }

        [Test]
        public void ALaneChangeNeedsBothLanesFreeTheWholeChange()
        {
            // Lane 2 blocked at 100-110; lane 3 is free, but lane 1 is blocked at 80-120: the way left is shut, the way right opens.
            var cars = new List<TrafficCarPlan> { Block(2, 100.0, 110.0), Block(1, 80.0, 120.0), Block(0, 0.0, 500.0) };
            Assert.IsTrue(Check(cars).Passable);

            // Now lane 3 is blocked just where the change to it would have to happen (90-99 in lane 3).
            cars.Add(Block(3, 85.0, 99.0));
            Assert.IsFalse(Check(cars, 10).Passable);
        }

        [Test]
        public void AChangeThatIsTooLongForTheGap_IsNotPossible()
        {
            // Lane 2 ends at 100 (blocked 100-110); lane 3 is blocked 0-90 and 120-130: a 10 m gap at lane 3 is too short for a 15 m change.
            var cars = new List<TrafficCarPlan> { Block(2, 100.0, 110.0), Block(1, 0.0, 500.0), Block(3, 0.0, 90.0), Block(3, 105.0, 130.0) };

            Assert.IsFalse(Check(cars, 15).Passable);
        }
    }
}
