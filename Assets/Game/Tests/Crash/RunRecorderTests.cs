using NUnit.Framework;
using WrongLine.Car;
using WrongLine.Tests.UI;
using WrongLine.UI;

namespace WrongLine.Crash.Tests
{
    public sealed class RunRecorderTests
    {
        private sealed class Motion : ICarMotion
        {
            public double Distance { get; set; }
            public float Speed => 0f;
            public float X => 0f;
            public float TargetX => 0f;
            public float Velocity => 0f;
            public int Lane => 0;
            public SignalDirection Signal => default;
            public bool LampOn => false;
            public bool Crashed => true;
        }

        private static CarCrashed Crash(double placeOnRoad) => new CarCrashed(1, 1, 10f, 20f, 1f, placeOnRoad);

        [Test]
        public void BeforeACrash_NothingIsRecorded()
        {
            var best = new BestDistance(new FakePrefsStore());
            var recorder = new RunRecorder(null, new Motion { Distance = 500 }, best);

            Assert.AreEqual(0, recorder.Meters);
            Assert.AreEqual(0, best.Meters);
            Assert.IsFalse(recorder.IsRecord);
        }

        [Test]
        public void ACrash_WritesTheDistanceTheCarCovered_NotThePlaceOnTheRoad()
        {
            var best = new BestDistance(new FakePrefsStore());
            var recorder = new RunRecorder(null, new Motion { Distance = 812.9 }, best);

            recorder.OnCrashed(Crash(99999));

            Assert.AreEqual(812, recorder.Meters);
            Assert.AreEqual(812, best.Meters);
            Assert.IsTrue(recorder.IsRecord);
        }

        [Test]
        public void ShorterRun_KeepsTheBest_AndIsNotARecord()
        {
            var best = new BestDistance(new FakePrefsStore());
            best.Submit(1000);
            var recorder = new RunRecorder(null, new Motion { Distance = 400 }, best);

            recorder.OnCrashed(Crash(400));

            Assert.AreEqual(400, recorder.Meters);
            Assert.AreEqual(1000, recorder.Best);
            Assert.IsFalse(recorder.IsRecord);
        }
    }
}
