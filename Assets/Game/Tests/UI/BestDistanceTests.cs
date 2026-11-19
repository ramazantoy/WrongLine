using NUnit.Framework;
using WrongLine.UI;

namespace WrongLine.Tests.UI
{
    public sealed class BestDistanceTests
    {
        [Test]
        public void FirstRun_IsARecord()
        {
            var best = new BestDistance(new FakePrefsStore());

            Assert.IsTrue(best.Submit(1284.7));
            Assert.AreEqual(1284, best.Meters);
        }

        [Test]
        public void ShorterOrEqualRun_IsNotARecord()
        {
            var best = new BestDistance(new FakePrefsStore());
            best.Submit(500);

            Assert.IsFalse(best.Submit(499.9));
            Assert.IsFalse(best.Submit(500.4));
            Assert.AreEqual(500, best.Meters);
        }

        [Test]
        public void LongerRun_ReplacesTheRecord()
        {
            var best = new BestDistance(new FakePrefsStore());
            best.Submit(500);

            Assert.IsTrue(best.Submit(501));
            Assert.AreEqual(501, best.Meters);
        }

        [Test]
        public void ARunThatWentNowhere_IsNeverARecord()
        {
            var store = new FakePrefsStore();
            var best = new BestDistance(store);

            Assert.IsFalse(best.Submit(0));
            Assert.IsFalse(best.Submit(-20));
            Assert.IsFalse(best.Submit(double.NaN));
            Assert.AreEqual(0, best.Meters);
            Assert.AreEqual(0, store.Writes);
        }

        [Test]
        public void TheRecord_IsReadBackByTheNextInstance()
        {
            var store = new FakePrefsStore();
            new BestDistance(store).Submit(900);

            Assert.AreEqual(900, new BestDistance(store).Meters);
        }

        [Test]
        public void ANegativeStoredValue_IsClampedToZero()
        {
            var store = new FakePrefsStore();
            store.SetInt("records.best_meters", -5);

            Assert.AreEqual(0, new BestDistance(store).Meters);
        }
    }
}
