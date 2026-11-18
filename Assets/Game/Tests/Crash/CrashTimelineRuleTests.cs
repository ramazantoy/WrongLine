using NUnit.Framework;

namespace WrongLine.Crash.Tests
{
    public class CrashTimelineRuleTests
    {
        [Test]
        public void TheGameEasesIntoSlowMotionThenCallsForTheGameOverScreen()
        {
            var settings = new CrashSettings();
            var timeline = new CrashTimelineRule(settings.SlowMotion);

            timeline.Update(settings.SlowMotion.EaseSeconds * 0.5f);
            Assert.AreEqual(1f + (settings.SlowMotion.Scale - 1f) * 0.5f, timeline.Scale, 1e-4f);
            Assert.IsFalse(timeline.GameOverDue);

            timeline.Update(settings.SlowMotion.EaseSeconds);
            Assert.AreEqual(settings.SlowMotion.Scale, timeline.Scale, 1e-4f);

            timeline.Update(settings.SlowMotion.GameOverSeconds);
            Assert.IsTrue(timeline.GameOverDue);
        }

        [Test]
        public void APausedFrameDoesNotAdvanceTheTimeline()
        {
            var settings = new CrashSettings();
            var timeline = new CrashTimelineRule(settings.SlowMotion);

            for (int i = 0; i < 1000; i++)
            {
                timeline.Update(0f);
            }

            Assert.AreEqual(0f, timeline.Elapsed);
            Assert.AreEqual(1f, timeline.Scale);
            Assert.IsFalse(timeline.GameOverDue);
        }

        [Test]
        public void NoEaseTimeGoesStraightToTheSlowScale()
        {
            var settings = new CrashSettings();
            settings.SlowMotion.EaseSeconds = 0f;
            var timeline = new CrashTimelineRule(settings.SlowMotion);

            timeline.Update(0.001f);

            Assert.AreEqual(settings.SlowMotion.Scale, timeline.Scale, 1e-4f);
        }

        [Test]
        public void ARealTimeCountIsNotStretchedBySlowMotion()
        {
            // The caller passes real seconds: 2.5 of them end it, however many game seconds that is.
            var settings = new CrashSettings();
            var timeline = new CrashTimelineRule(settings.SlowMotion);

            for (int i = 0; i < 149; i++)
            {
                timeline.Update(1f / 60f);
            }

            Assert.IsFalse(timeline.GameOverDue);
            timeline.Update(1f / 60f);
            timeline.Update(1f / 60f);
            Assert.IsTrue(timeline.GameOverDue);
        }
    }
}
