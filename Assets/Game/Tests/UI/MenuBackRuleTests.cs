using NUnit.Framework;
using WrongLine.UI;

namespace WrongLine.Tests.UI
{
    public sealed class MenuBackRuleTests
    {
        [Test]
        public void NothingOpen_AsksToQuit()
        {
            Assert.AreEqual(MenuBack.AskToQuit, MenuBackRule.Next(false, false));
        }

        [Test]
        public void OpenSettings_CloseFirst()
        {
            Assert.AreEqual(MenuBack.CloseSettings, MenuBackRule.Next(true, false));
        }

        [Test]
        public void OpenQuitPrompt_ClosesNotQuits()
        {
            Assert.AreEqual(MenuBack.CloseQuitPrompt, MenuBackRule.Next(false, true));
        }

        [Test]
        public void Settings_AreAboveTheQuitPrompt()
        {
            Assert.AreEqual(MenuBack.CloseSettings, MenuBackRule.Next(true, true));
        }
    }
}
