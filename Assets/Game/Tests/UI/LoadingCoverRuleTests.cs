using NUnit.Framework;
using WrongLine.UI;

namespace WrongLine.Tests.UI
{
    public sealed class LoadingCoverRuleTests
    {
        [Test]
        public void LeavingTheIntro_IsNotCovered()
        {
            Assert.IsFalse(LoadingCoverRule.ShouldCover(Scenes.Intro));
        }

        [Test]
        public void EveryOtherChange_IsCovered()
        {
            Assert.IsTrue(LoadingCoverRule.ShouldCover(Scenes.Menu));
            Assert.IsTrue(LoadingCoverRule.ShouldCover(Scenes.Gameplay));
            Assert.IsTrue(LoadingCoverRule.ShouldCover(null));
        }
    }
}
