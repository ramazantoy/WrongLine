using System.Collections.Generic;
using NUnit.Framework;
using WrongLine.Road;

namespace WrongLine.Car.Tests
{
    public class TurnSignalRuleTests
    {
        private const float Dt = 1f / 60f;

        // Four 3 m lanes: lane 0 is -6..-3, lane 1 -3..0, lane 2 0..3, lane 3 3..6.
        private static readonly float LaneOne = RoadSection.LaneCenterX(1);
        private static readonly float LaneTwo = RoadSection.LaneCenterX(2);
        private static readonly float LaneThree = RoadSection.LaneCenterX(3);

        [Test]
        public void StayingInTheLane_NeverSignals()
        {
            var rule = new TurnSignalRule(new CarSettings());

            for (int i = 0; i < 300; i++)
            {
                rule.Update(0.5f, 2.2f, Dt);
                Assert.AreEqual(SignalDirection.None, rule.Direction);
                Assert.IsFalse(rule.LampOn);
            }
        }

        [Test]
        public void ATargetInTheNextLane_StartsTheSignalAtOnce_LitFirst()
        {
            var rule = new TurnSignalRule(new CarSettings());

            rule.Update(LaneTwo, LaneThree, Dt);

            Assert.AreEqual(SignalDirection.Right, rule.Direction);
            Assert.IsTrue(rule.LampOn);
        }

        [Test]
        public void ATargetOnTheLeft_SignalsLeft()
        {
            var rule = new TurnSignalRule(new CarSettings());

            rule.Update(LaneTwo, LaneOne, Dt);

            Assert.AreEqual(SignalDirection.Left, rule.Direction);
        }

        [Test]
        public void TheLamp_BlinksAtThePeriod_HalfLitHalfDark()
        {
            var settings = new CarSettings();
            var rule = new TurnSignalRule(settings);
            var changes = new List<float>();
            bool lit = false;
            float time = 0f;

            for (int i = 0; i < 120; i++)
            {
                rule.Update(LaneTwo, LaneThree, Dt); // The car never gets there: the signal keeps going.
                time += Dt;
                if (rule.LampOn != lit)
                {
                    lit = rule.LampOn;
                    changes.Add(time);
                }
            }

            float half = settings.Signal.Period * 0.5f;
            Assert.GreaterOrEqual(changes.Count, 5);
            for (int i = 1; i < changes.Count; i++)
            {
                Assert.AreEqual(half, changes[i] - changes[i - 1], Dt * 1.5f, $"change {i}");
            }
        }

        [Test]
        public void ACarThatSettlesAtOnce_StillBlinksTheMinimumNumberOfTimes()
        {
            var settings = new CarSettings();
            var rule = new TurnSignalRule(settings);
            rule.Update(LaneTwo, LaneThree, Dt);

            float time = 0f;
            while (rule.Direction != SignalDirection.None && time < 10f)
            {
                rule.Update(LaneThree, LaneThree, Dt); // Already there.
                time += Dt;
            }

            float minimum = settings.Signal.MinBlinks * settings.Signal.Period;
            Assert.AreEqual(minimum, time, Dt * 2f);
            Assert.IsFalse(rule.LampOn);
        }

        [Test]
        public void ACarStillOnTheWay_KeepsSignallingPastTheMinimum()
        {
            var rule = new TurnSignalRule(new CarSettings());

            for (int i = 0; i < 600; i++)
            {
                rule.Update(LaneTwo, LaneThree, Dt);
            }

            Assert.AreEqual(SignalDirection.Right, rule.Direction);
        }

        [Test]
        public void TurningTheChangeRound_FlipsTheSignalAtOnce_AndRestartsTheBlink()
        {
            var rule = new TurnSignalRule(new CarSettings());
            for (int i = 0; i < 30; i++)
            {
                rule.Update(LaneTwo, LaneThree, Dt); // 0.5 s in: the lamp is dark.
            }

            Assert.IsFalse(rule.LampOn);

            rule.Update(LaneTwo, LaneOne, Dt);

            Assert.AreEqual(SignalDirection.Left, rule.Direction);
            Assert.IsTrue(rule.LampOn);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void HoveringOnALaneLine_DoesNotChangeTheLane_UntilItCrossesByTheHysteresis(int lineIndex)
        {
            var settings = new CarSettings();
            var rule = new TurnSignalRule(settings);
            float line = RoadSection.LineX(lineIndex);
            float margin = settings.Signal.Hysteresis;
            int left = lineIndex;
            int right = lineIndex + 1;

            rule.Update(line - margin * 2f, 0f, Dt); // The first update just places the car, in the lane left of the line.
            Assert.AreEqual(left, rule.CarLane);

            rule.Update(line + margin * 0.5f, 0f, Dt);
            Assert.AreEqual(left, rule.CarLane, "Just past the line is not enough.");

            rule.Update(line + margin * 1.5f, 0f, Dt);
            Assert.AreEqual(right, rule.CarLane);

            rule.Update(line - margin * 0.5f, 0f, Dt);
            Assert.AreEqual(right, rule.CarLane, "Just back over the line is not enough either.");

            rule.Update(line - margin * 1.5f, 0f, Dt);
            Assert.AreEqual(left, rule.CarLane);
        }

        [Test]
        public void ATargetTwoLanesAway_SignalsTheWholeWay_AndStopsOnlyOnceTheCarIsThere()
        {
            var settings = new CarSettings();
            var rule = new TurnSignalRule(settings);
            float outer = RoadSection.LaneCenterX(0);

            rule.Update(LaneThree, outer, Dt);
            Assert.AreEqual(SignalDirection.Left, rule.Direction);

            for (int i = 0; i < 400; i++)
            {
                rule.Update(LaneTwo, outer, Dt); // Crossed one lane, not both.
            }

            Assert.AreEqual(SignalDirection.Left, rule.Direction);
            Assert.AreEqual(2, rule.CarLane);

            for (int i = 0; i < 400; i++)
            {
                rule.Update(outer, outer, Dt);
            }

            Assert.AreEqual(0, rule.CarLane);
            Assert.AreEqual(SignalDirection.None, rule.Direction);
        }

        [Test]
        public void TheFirstUpdate_PlacesTheCarInItsLaneWithoutASignal_AnywhereOnTheRoad()
        {
            for (int lane = 0; lane < RoadSection.LaneCount; lane++)
            {
                var rule = new TurnSignalRule(new CarSettings());
                float x = RoadSection.LaneCenterX(lane);

                rule.Update(x, x, Dt);

                Assert.AreEqual(lane, rule.CarLane, $"lane {lane}");
                Assert.AreEqual(SignalDirection.None, rule.Direction, $"lane {lane}");
            }
        }

        [Test]
        public void APause_FreezesTheBlink()
        {
            var rule = new TurnSignalRule(new CarSettings());
            rule.Update(LaneTwo, LaneThree, Dt);
            for (int i = 0; i < 25; i++)
            {
                rule.Update(LaneTwo, LaneThree, Dt);
            }

            bool lamp = rule.LampOn;

            for (int i = 0; i < 1000; i++)
            {
                rule.Update(LaneTwo, LaneThree, 0f);
            }

            Assert.AreEqual(lamp, rule.LampOn);
            Assert.AreEqual(SignalDirection.Right, rule.Direction);
        }
    }
}
