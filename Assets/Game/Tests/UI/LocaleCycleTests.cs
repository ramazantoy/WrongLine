using NUnit.Framework;
using WrongLine.UI;

namespace WrongLine.Tests.UI
{
    public sealed class LocaleCycleTests
    {
        [Test]
        public void Next_MovesOn_AndWrapsAtTheEnd()
        {
            Assert.AreEqual(1, LocaleCycle.Step(0, 3, 1));
            Assert.AreEqual(0, LocaleCycle.Step(2, 3, 1));
        }

        [Test]
        public void Previous_MovesBack_AndWrapsAtTheStart()
        {
            Assert.AreEqual(0, LocaleCycle.Step(1, 3, -1));
            Assert.AreEqual(2, LocaleCycle.Step(0, 3, -1));
        }

        [Test]
        public void UnknownCurrent_StartsFromTheFirst()
        {
            Assert.AreEqual(0, LocaleCycle.Step(-1, 2, 1));
            Assert.AreEqual(0, LocaleCycle.Step(7, 2, -1));
        }

        [Test]
        public void NoLanguages_GiveNoIndex()
        {
            Assert.AreEqual(-1, LocaleCycle.Step(0, 0, 1));
        }

        [Test]
        public void SingleLanguage_StaysPut()
        {
            Assert.AreEqual(0, LocaleCycle.Step(0, 1, 1));
            Assert.AreEqual(0, LocaleCycle.Step(0, 1, -1));
        }
    }
}
