using NUnit.Framework;
using WrongLine.Audio;

namespace WrongLine.Tests.Audio
{
    public sealed class PassByRuleTests
    {
        private const float Frame = 1f / 60f;

        private static bool Step(PassByRule rule, int id, double ahead, double side, out PassBy pass)
        {
            rule.Begin();
            bool result = rule.Observe(id, ahead, side, Frame, out pass);
            rule.End();
            return result;
        }

        [Test]
        public void ACarOvertakenInTheNextLane_WhooshesOnce()
        {
            var rule = new PassByRule(new PassBySettings());
            int passes = 0;

            for (double ahead = 1.0; ahead > -1.0; ahead -= 0.2)
            {
                if (Step(rule, 7, ahead, 3.0, out PassBy pass))
                {
                    passes++;
                    Assert.AreEqual(3f, pass.SideMeters);
                }
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void ACarThatOvertakesThePlayerCountsToo()
        {
            var rule = new PassByRule(new PassBySettings());
            int passes = 0;

            for (double ahead = -1.0; ahead < 1.0; ahead += 0.2)
            {
                passes += Step(rule, 7, ahead, -3.0, out _) ? 1 : 0;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void ACarFarAcrossTheRoad_IsNotAPass()
        {
            var rule = new PassByRule(new PassBySettings());
            bool any = false;

            for (double ahead = 1.0; ahead > -1.0; ahead -= 0.2)
            {
                any |= Step(rule, 7, ahead, 20.0, out _);
            }

            Assert.IsFalse(any);
        }

        [Test]
        public void AJumpAlongTheRoad_IsNotAPass()
        {
            var rule = new PassByRule(new PassBySettings());
            Step(rule, 7, 40.0, 0.0, out _);

            Assert.IsFalse(Step(rule, 7, -40.0, 0.0, out _));
        }

        [Test]
        public void NothingIsHeardWhileTheGameStandsStill()
        {
            var rule = new PassByRule(new PassBySettings());
            rule.Begin();
            rule.Observe(7, 1.0, 0.0, Frame, out _);
            rule.End();

            rule.Begin();
            bool heard = rule.Observe(7, -1.0, 0.0, 0f, out _);
            rule.End();

            Assert.IsFalse(heard);
        }

        [Test]
        public void FasterPassesAreLouder()
        {
            var slow = new PassByRule(new PassBySettings());
            var fast = new PassByRule(new PassBySettings());
            Step(slow, 1, 0.1, 0.0, out _);
            Step(fast, 1, 1.0, 0.0, out _);

            Step(slow, 1, -0.1, 0.0, out PassBy slowPass);
            Step(fast, 1, -1.0, 0.0, out PassBy fastPass);

            Assert.Greater(fastPass.Volume, slowPass.Volume);
            Assert.LessOrEqual(fastPass.Volume, 1f);
        }

        [Test]
        public void ACarThatLeavesIsForgotten_SoANewOneWithTheSameIdStartsFresh()
        {
            var rule = new PassByRule(new PassBySettings());
            Step(rule, 7, 0.1, 0.0, out _);

            rule.Begin();
            rule.End(); // Car 7 was not seen this frame.

            // It comes back behind the player: no earlier frame to compare with, so no pass.
            Assert.IsFalse(Step(rule, 7, -0.1, 0.0, out _));
        }
    }
}
