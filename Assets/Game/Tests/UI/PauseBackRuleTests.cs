using NUnit.Framework;
using WrongLine.UI;

namespace WrongLine.Tests.UI
{
    public sealed class PauseBackRuleTests
    {
        [Test]
        public void Driving_OpensThePause()
        {
            Assert.AreEqual(PauseBack.OpenPause, PauseBackRule.Next(false, false, false));
        }

        [Test]
        public void Paused_Resumes()
        {
            Assert.AreEqual(PauseBack.Resume, PauseBackRule.Next(false, true, false));
        }

        [Test]
        public void Settings_CloseBeforeThePause()
        {
            Assert.AreEqual(PauseBack.CloseSettings, PauseBackRule.Next(false, true, true));
        }

        [Test]
        public void AfterACrash_NothingHappens()
        {
            Assert.AreEqual(PauseBack.None, PauseBackRule.Next(true, false, false));
            Assert.AreEqual(PauseBack.None, PauseBackRule.Next(true, true, true));
        }
    }
}
