using NUnit.Framework;
using WrongLine.UI;

namespace WrongLine.Tests.UI
{
    public sealed class LoadingWaveTests
    {
        [Test]
        public void StartsEmpty_AndFillsAtASteadySpeed()
        {
            Assert.AreEqual(0f, LoadingWave.Fill(0f, 2f), 1e-5f);
            Assert.AreEqual(0.25f, LoadingWave.Fill(0.5f, 2f), 1e-5f);
            Assert.AreEqual(0.5f, LoadingWave.Fill(1f, 2f), 1e-5f);
            Assert.AreEqual(0.75f, LoadingWave.Fill(1.5f, 2f), 1e-5f);
        }

        [Test]
        public void WhenFull_ItStartsOverFromEmpty_NotBackwards()
        {
            Assert.Greater(LoadingWave.Fill(1.99f, 2f), 0.99f);
            Assert.AreEqual(0f, LoadingWave.Fill(2f, 2f), 1e-5f);
            Assert.Less(LoadingWave.Fill(2.1f, 2f), 0.1f);
        }

        [Test]
        public void EveryCycle_IsTheSame()
        {
            Assert.AreEqual(LoadingWave.Fill(0.3f, 1f), LoadingWave.Fill(4.3f, 1f), 1e-4f);
        }

        [Test]
        public void WithinACycle_ItNeverGoesBackwards()
        {
            float last = -1f;
            for (int i = 0; i < 20; i++)
            {
                float fill = LoadingWave.Fill(i / 20f, 1f);
                Assert.GreaterOrEqual(fill, last);
                last = fill;
            }
        }

        [Test]
        public void ANoDuration_ShowsNothing()
        {
            Assert.AreEqual(0f, LoadingWave.Fill(3f, 0f));
        }
    }
}
