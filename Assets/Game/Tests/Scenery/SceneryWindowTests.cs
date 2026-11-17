using System.Collections.Generic;
using NUnit.Framework;

namespace WrongLine.Scenery.Tests
{
    public class SceneryWindowTests
    {
        private readonly List<int> _added = new List<int>();
        private readonly List<int> _removed = new List<int>();

        [Test]
        public void TheFirstUpdate_AddsEveryPiece()
        {
            var window = new SceneryWindow();

            window.Update(3, 7, _added, _removed);

            CollectionAssert.AreEqual(new[] { 3, 4, 5, 6 }, _added);
            CollectionAssert.IsEmpty(_removed);
            Assert.AreEqual(4, window.Count);
        }

        [Test]
        public void MovingForward_AddsTheNewPieces_AndRemovesTheOldOnes_InOrder()
        {
            var window = new SceneryWindow();
            window.Update(3, 7, _added, _removed);

            window.Update(5, 10, _added, _removed);

            CollectionAssert.AreEqual(new[] { 7, 8, 9 }, _added);
            CollectionAssert.AreEqual(new[] { 3, 4 }, _removed);
            Assert.AreEqual(5, window.First);
            Assert.AreEqual(10, window.End);
        }

        [Test]
        public void TheSameRange_ChangesNothing()
        {
            var window = new SceneryWindow();
            window.Update(0, 4, _added, _removed);

            window.Update(0, 4, _added, _removed);

            CollectionAssert.IsEmpty(_added);
            CollectionAssert.IsEmpty(_removed);
        }

        [Test]
        public void AJumpPastTheOldRange_ReplacesEverything()
        {
            var window = new SceneryWindow();
            window.Update(0, 3, _added, _removed);

            window.Update(10, 12, _added, _removed);

            CollectionAssert.AreEqual(new[] { 10, 11 }, _added);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, _removed);
        }

        [Test]
        public void AnEndBeforeTheFirst_IsAnEmptyWindow()
        {
            var window = new SceneryWindow();
            window.Update(2, 5, _added, _removed);

            window.Update(6, 4, _added, _removed);

            CollectionAssert.IsEmpty(_added);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, _removed);
            Assert.AreEqual(0, window.Count);
        }
    }
}
