using System.Collections.Generic;
using NUnit.Framework;

namespace WrongLine.Car.Tests
{
    public class TurnSignalRuleTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void StayingInTheLane_NeverSignals()
        {
            var rule = new TurnSignalRule(new CarSettings());

            for (int i = 0; i < 300; i++)
            {
                rule.Update(0.5f, 1.2f, Dt);
                Assert.AreEqual(SignalDirection.None, rule.Direction);
                Assert.IsFalse(rule.LampOn);
            }
        }

        [Test]
        public void ATargetInTheNextLane_StartsTheSignalAtOnce_LitFirst()
        {
            var rule = new TurnSignalRule(new CarSettings());

            rule.Update(0f, 3f, Dt);

            Assert.AreEqual(SignalDirection.Right, rule.Direction);
            Assert.IsTrue(rule.LampOn);
        }

        [Test]
        public void ATargetOnTheLeft_SignalsLeft()
        {
            var rule = new TurnSignalRule(new CarSettings());

            rule.Update(0f, -3f, Dt);

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
                rule.Update(0f, 3f, Dt); // The car never gets there: the signal keeps going.
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
            rule.Update(0f, 3f, Dt);

            float time = 0f;
            while (rule.Direction != SignalDirection.None && time < 10f)
            {
                rule.Update(3f, 3f, Dt); // Already there.
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
                rule.Update(0f, 3f, Dt);
            }

            Assert.AreEqual(SignalDirection.Right, rule.Direction);
        }

        [Test]
        public void TurningTheChangeRound_FlipsTheSignalAtOnce_AndRestartsTheBlink()
        {
            var rule = new TurnSignalRule(new CarSettings());
            for (int i = 0; i < 30; i++)
            {
                rule.Update(0f, 3f, Dt); // 0.5 s in: the lamp is dark.
            }

            Assert.IsFalse(rule.LampOn);

            rule.Update(0f, -3f, Dt);

            Assert.AreEqual(SignalDirection.Left, rule.Direction);
            Assert.IsTrue(rule.LampOn);
        }

        [Test]
        public void HoveringOnALaneLine_DoesNotChangeTheLane_UntilItCrossesByTheHysteresis()
        {
            var settings = new CarSettings();
            var rule = new TurnSignalRule(settings);
            float line = Road.RoadSection.LineOffset;
            float margin = settings.Signal.Hysteresis;

            rule.Update(line + margin * 0.5f, 0f, Dt);
            Assert.AreEqual(0, rule.CarLane, "Just past the line is not enough.");

            rule.Update(line + margin * 1.5f, 0f, Dt);
            Assert.AreEqual(1, rule.CarLane);

            rule.Update(line - margin * 0.5f, 0f, Dt);
            Assert.AreEqual(1, rule.CarLane, "Just back over the line is not enough either.");

            rule.Update(line - margin * 1.5f, 0f, Dt);
            Assert.AreEqual(0, rule.CarLane);
        }

        [Test]
        public void APause_FreezesTheBlink()
        {
            var rule = new TurnSignalRule(new CarSettings());
            rule.Update(0f, 3f, Dt);
            for (int i = 0; i < 25; i++)
            {
                rule.Update(0f, 3f, Dt);
            }

            bool lamp = rule.LampOn;

            for (int i = 0; i < 1000; i++)
            {
                rule.Update(0f, 3f, 0f);
            }

            Assert.AreEqual(lamp, rule.LampOn);
            Assert.AreEqual(SignalDirection.Right, rule.Direction);
        }
    }
}
