using System.Collections.Generic;
using NUnit.Framework;
using WrongLine.Road;
using WrongLine.Traffic;

namespace WrongLine.Crash.Tests
{
    public class CollisionRuleTests
    {
        private const double PlayerSpeed = 30.0;

        private static readonly TrafficModelSpec[] Models = { new TrafficModelSpec(4f, 1f) };

        private CrashSettings _settings;
        private CollisionRule _rule;

        [SetUp]
        public void SetUp()
        {
            _settings = new CrashSettings();
            _rule = new CollisionRule(_settings);
        }

        private static TrafficCarPlan Car(int id, int lane, double speed, double startDistance)
        {
            return new TrafficCarPlan(id, 0, 0, lane, 0.0, 10.0, speed, startDistance, 4f);
        }

        /// <summary>Drives the player straight along a lane's middle plus <paramref name="offsetX"/> and returns every non-empty result.</summary>
        private List<CollisionResult> Drive(TrafficCarPlan[] cars, int lane, double offsetX, double seconds, double frame, double ratio = 1.0)
        {
            var results = new List<CollisionResult>();
            float x = (float)(RoadSection.LaneCenterX(lane) + offsetX);
            var last = new PlayerBox(0.0, 0.0, x);
            for (double t = frame; t <= seconds + 1e-9; t += frame)
            {
                var now = new PlayerBox(t, PlayerSpeed * t, x);
                CollisionResult result = _rule.Update(last, now, cars, Models, ratio);
                if (result.Kind != CollisionKind.None)
                {
                    results.Add(result);
                    if (result.Kind == CollisionKind.Crash)
                    {
                        break; // The game stops checking at the first crash.
                    }
                }

                last = now;
            }

            return results;
        }

        [Test]
        public void CatchingUpWithACarInTheSameLaneIsACrash()
        {
            TrafficCarPlan[] cars = { Car(7, 2, 15.0, 50.0) };

            List<CollisionResult> results = Drive(cars, 2, 0.0, 6.0, 1.0 / 60.0);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(CollisionKind.Crash, results[0].Kind);
            Assert.AreEqual(7, results[0].CarId);
            Assert.AreEqual(1f, results[0].Ahead);
            Assert.AreEqual(15f, results[0].ClosingSpeed, 0.01f);
        }

        [Test]
        public void MeetingACarComingTheOtherWayIsACrash()
        {
            TrafficCarPlan[] cars = { Car(3, 2, -20.0, 300.0) };

            List<CollisionResult> results = Drive(cars, 2, 0.0, 8.0, 1.0 / 60.0);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(CollisionKind.Crash, results[0].Kind);
            Assert.AreEqual(50f, results[0].ClosingSpeed, 0.01f);
        }

        [Test]
        public void PassingACarInTheNextLaneWithoutTouchingIsNothing()
        {
            TrafficCarPlan[] cars = { Car(1, 1, 15.0, 50.0) };

            Assert.IsEmpty(Drive(cars, 2, 0.0, 6.0, 1.0 / 60.0));
        }

        [Test]
        public void ABoxesWidthOfOverlapAlongTheSideIsASideswipeOnce()
        {
            // The player's middle is shallow of the car's: the boxes overlap by 0.2 m across the road.
            double touching = _settings.Hitbox.PlayerHalfWidth + Models[0].HalfWidth * _settings.Hitbox.TrafficScale;
            double lanes = RoadSection.LaneCenterX(2) - RoadSection.LaneCenterX(1);
            TrafficCarPlan[] cars = { Car(4, 1, 15.0, 50.0) };

            List<CollisionResult> results = Drive(cars, 2, touching - 0.2 - lanes, 6.0, 1.0 / 60.0);

            Assert.AreEqual(1, results.Count, "a touch that goes on is reported once");
            Assert.AreEqual(CollisionKind.Sideswipe, results[0].Kind);
            Assert.AreEqual(-1, results[0].Side);
            Assert.AreEqual(0.2f, results[0].Depth, 0.05f);
        }

        [Test]
        public void ADeeperOverlapAcrossTheRoadIsACrash()
        {
            double touching = _settings.Hitbox.PlayerHalfWidth + Models[0].HalfWidth * _settings.Hitbox.TrafficScale;
            double lanes = RoadSection.LaneCenterX(2) - RoadSection.LaneCenterX(1);
            TrafficCarPlan[] cars = { Car(4, 1, 15.0, 50.0) };

            List<CollisionResult> results = Drive(cars, 2, touching - 0.7 - lanes, 6.0, 1.0 / 60.0);

            Assert.AreEqual(CollisionKind.Crash, results[0].Kind);
        }

        [Test]
        public void ATouchThatDeepensPastASideswipeBecomesACrash()
        {
            // The player drifts into a car alongside: first a graze, then a hit.
            TrafficCarPlan[] cars = { Car(4, 1, 30.0, 0.0) };
            double touching = _settings.Hitbox.PlayerHalfWidth + Models[0].HalfWidth * _settings.Hitbox.TrafficScale;
            var results = new List<CollisionResult>();
            var last = new PlayerBox(0.0, 0.0, (float)(RoadSection.LaneCenterX(1) + touching + 1.0));
            for (int i = 1; i <= 120; i++)
            {
                double t = i / 60.0;
                float x = (float)(RoadSection.LaneCenterX(1) + touching + 1.0 - t * 1.0);
                var now = new PlayerBox(t, PlayerSpeed * t, x);
                CollisionResult result = _rule.Update(last, now, cars, Models, 1.0);
                if (result.Kind != CollisionKind.None)
                {
                    results.Add(result);
                    if (result.Kind == CollisionKind.Crash)
                    {
                        break;
                    }
                }

                last = now;
            }

            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(CollisionKind.Sideswipe, results[0].Kind);
            Assert.AreEqual(CollisionKind.Crash, results[1].Kind);
        }

        [Test]
        public void TheFrameRateDoesNotChangeTheResult()
        {
            // The gap closes at 50 m/s; at 306.25 m the quarter-second frames see it at +6.25 m and then −6.25 m, never inside the 3.94 m reach.
            TrafficCarPlan[] cars = { Car(7, 2, -20.0, 306.25) };

            List<CollisionResult> fast =Drive(cars, 2, 0.0, 8.0, 1.0 / 120.0);
            _rule = new CollisionRule(_settings);
            List<CollisionResult> slow = Drive(cars, 2, 0.0, 8.0, 0.25);

            Assert.AreEqual(1, fast.Count);
            Assert.AreEqual(1, slow.Count, "a quarter-second frame covers 14 m: it must not step over a 4 m car");
            Assert.AreEqual(fast[0].Kind, slow[0].Kind);
        }

        [Test]
        public void ACarChangingLaneIntoThePlayerIsFound()
        {
            // The car starts in lane 1 and moves to lane 2 while the player comes up behind it.
            var car = new TrafficCarPlan(9, 0, 0, 2, 0.0, 10.0, 15.0, 80.0, 4f, 1, 15.0, 1.0, 1.5);

            List<CollisionResult> results = Drive(new[] { car }, 2, 0.0, 6.0, 1.0 / 60.0);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(CollisionKind.Crash, results[0].Kind);
        }

        [Test]
        public void ABendMakesTheBoxesShorterAlongTheDistance()
        {
            // On the outside of a bend a metre of box is less distance along the road: a gap that touches at ratio 1 does not at 2.
            // The gap closes at 15 m/s from 50 m: 3.94 m apart (the boxes touch at ratio 1) at 3.07 s, 1.97 m apart (ratio 2) at 3.20 s.
            TrafficCarPlan[] cars = { Car(7, 2, 15.0, 50.0) };

            Assert.IsNotEmpty(Drive(cars, 2, 0.0, 3.12, 1.0 / 60.0, 1.0));
            _rule = new CollisionRule(_settings);
            Assert.IsEmpty(Drive(cars, 2, 0.0, 3.12, 1.0 / 60.0, 2.0));
        }
    }
}
