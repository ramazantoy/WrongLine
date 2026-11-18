using NUnit.Framework;
using WrongLine.Tests;

namespace WrongLine.Crash.Tests
{
    public class GameOverActionsTests
    {
        private FakeSceneLoader _loader;
        private GameOverActions _actions;

        [SetUp]
        public void SetUp()
        {
            _loader = new FakeSceneLoader();
            _actions = new GameOverActions(_loader);
        }

        [Test]
        public void RetryLoadsTheDrivingSceneAgain()
        {
            _actions.Retry();

            CollectionAssert.AreEqual(new[] { Scenes.Gameplay }, _loader.Loads);
            Assert.IsTrue(_actions.Used);
        }

        [Test]
        public void MenuGoesBackToTheMenuInTheMenuState()
        {
            _actions.Menu();

            CollectionAssert.AreEqual(new[] { "MenuState " + Scenes.Menu }, _loader.Loads);
        }

        [Test]
        public void ASecondTapLoadsNothingMore()
        {
            _actions.Retry();
            _actions.Retry();
            _actions.Menu();

            Assert.AreEqual(1, _loader.Loads.Count);
        }

        [Test]
        public void NothingLoadsUntilAButtonIsUsed()
        {
            Assert.IsEmpty(_loader.Loads);
            Assert.IsFalse(_actions.Used);
        }
    }
}
