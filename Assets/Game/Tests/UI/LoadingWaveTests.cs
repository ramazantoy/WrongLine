using NUnit.Framework;
using WrongLine.UI;

namespace WrongLine.Tests.UI
{
    public sealed class LoadingWaveTests
    {
        [Test]
        public void StartsEmpty_ReachesFullAfterOneWay_AndIsEmptyAgainAfterTwo()
        {
            Assert.AreEqual(0f, LoadingWave.Fill(0f, 1f), 1e-5f);
            Assert.AreEqual(1f, LoadingWave.Fill(1f, 1f), 1e-5f);
            Assert.AreEqual(0f, LoadingWave.Fill(2f, 1f), 1e-5f);
        }

        [Test]
        public void HalfWay_IsHalfFull()
        {
            Assert.AreEqual(0.5f, LoadingWave.Fill(0.5f, 1f), 1e-5f);
        }

        [Test]
        public void FillingAndEmptying_MirrorEachOther()
        {
            Assert.AreEqual(LoadingWave.Fill(0.3f, 1f), LoadingWave.Fill(1.7f, 1f), 1e-5f);
        }

        [Test]
        public void ItKeepsGoing_PastTheFirstCycle()
        {
            Assert.AreEqual(LoadingWave.Fill(0.3f, 1f), LoadingWave.Fill(4.3f, 1f), 1e-5f);
            Assert.AreEqual(1f, LoadingWave.Fill(5f, 1f), 1e-5f);
        }

        [Test]
        public void Filling_NeverGoesBackwards()
        {
            float last = -1f;
            for (int i = 0; i <= 20; i++)
            {
                float fill = LoadingWave.Fill(i / 20f, 1f);
                Assert.GreaterOrEqual(fill, last);
                last = fill;
            }
        }

        [Test]
        public void ANoWayDuration_ShowsNothing()
        {
            Assert.AreEqual(0f, LoadingWave.Fill(3f, 0f));
        }
    }
}
