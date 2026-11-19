using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using WrongLine.UI;

namespace WrongLine.Tests.UI
{
    public sealed class UiScreenTests
    {
        private const float WaitLimit = 3f;

        private GameObject _go;
        private CanvasGroup _group;

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        private UiScreen Make(float hiddenScale, bool startActive = false, bool startOpen = false)
        {
            _go = new GameObject("screen", typeof(RectTransform), typeof(CanvasGroup));
            _group = _go.GetComponent<CanvasGroup>();
            var screen = _go.AddComponent<UiScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("_hiddenScale").floatValue = hiddenScale;
            so.FindProperty("_startOpen").boolValue = startOpen;
            so.FindProperty("_seconds").floatValue = 0.05f;
            so.ApplyModifiedPropertiesWithoutUndo();
            _go.SetActive(startActive);
            return screen;
        }

        /// <summary>Lets the editor tick (PrimeTween runs from the editor update) until the condition holds or time runs out.</summary>
        private static IEnumerator Until(System.Func<bool> condition)
        {
            float end = Time.realtimeSinceStartup + WaitLimit;
            while (!condition() && Time.realtimeSinceStartup < end)
            {
                yield return null;
            }
        }

        /// <summary>Waits real time; a grow eases past its end size and back, so "reached it once" is not "finished".</summary>
        private static IEnumerator Wait(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Show_ActivatesTheScreen_AndFadesItInFully()
        {
            var screen = Make(1f);

            screen.Show();
            Assert.IsTrue(screen.IsOpen);
            Assert.IsTrue(_go.activeSelf);
            Assert.IsTrue(_group.interactable);
            Assert.IsTrue(_group.blocksRaycasts);

            yield return Until(() => _group.alpha > 0.999f);
            Assert.AreEqual(1f, _group.alpha, 1e-3f);
        }

        [UnityTest]
        public IEnumerator Hide_StopsInputAtOnce_AndSwitchesTheScreenOffWhenFaded()
        {
            var screen = Make(1f);
            screen.Show();
            yield return Until(() => _group.alpha > 0.999f);

            screen.Hide();
            Assert.IsFalse(screen.IsOpen);
            Assert.IsFalse(_group.interactable);
            Assert.IsFalse(_group.blocksRaycasts);

            yield return Until(() => !_go.activeSelf);
            Assert.IsFalse(_go.activeSelf);
        }

        [UnityTest]
        public IEnumerator ShowAgainWhileHiding_KeepsTheScreenOn()
        {
            var screen = Make(1f);
            screen.Show();
            yield return Until(() => _group.alpha > 0.999f);

            screen.Hide();
            screen.Show();
            yield return Until(() => _group.alpha > 0.999f);
            // Give the cancelled hide's end every chance to switch the screen off.
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            Assert.IsTrue(_go.activeSelf);
            Assert.IsTrue(screen.IsOpen);
            Assert.AreEqual(1f, _group.alpha, 1e-3f);
        }

        [UnityTest]
        public IEnumerator AScreenThatGrows_StartsSmall_AndEndsAtFullSize()
        {
            var screen = Make(0f);

            screen.Show();
            Assert.Less(_go.transform.localScale.x, 0.1f);

            yield return Wait(0.5f);
            Assert.AreEqual(1f, _go.transform.localScale.x, 1e-3f);
        }

        [UnityTest]
        public IEnumerator AScreenThatOnlyFades_KeepsItsSize()
        {
            var screen = Make(1f);

            screen.Show();
            yield return Until(() => _group.alpha > 0.999f);

            Assert.AreEqual(1f, _go.transform.localScale.x, 1e-5f);
        }

        [UnityTest]
        public IEnumerator ShowOnAnOpenScreen_DoesNotRestartTheAnimation()
        {
            var screen = Make(1f);
            screen.Show();
            yield return Until(() => _group.alpha > 0.999f);

            _group.alpha = 0.5f; // a marker: a restarted fade would pull it back to 1
            screen.Show();
            yield return Wait(0.2f);

            Assert.AreEqual(0.5f, _group.alpha, 1e-3f);
        }

        [Test]
        public void AScreenThatStartsOpen_IsOpenAtOnce_AndCanBeHidden()
        {
            var screen = Make(1f, startActive: true, startOpen: true);
            Assert.IsTrue(screen.IsOpen);

            screen.Hide();

            Assert.IsFalse(screen.IsOpen);
        }

        [Test]
        public void AScreenThatStartsClosed_IsShownByTheFirstShow_AndStaysOpenAfterUnityWakesIt()
        {
            var screen = Make(0f);
            Assert.IsFalse(screen.IsOpen);

            screen.Show();
            // What Unity does on the first activation: it wakes the component in the middle of Show.
            typeof(UiScreen).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(screen, null);

            Assert.IsTrue(screen.IsOpen);
        }

        [Test]
        public void ShowAndHide_WhenAlreadyThere_DoNothing()
        {
            var screen = Make(1f);

            screen.Hide();
            Assert.IsFalse(screen.IsOpen);
            Assert.IsFalse(_go.activeSelf);

            screen.Show();
            screen.Show();
            Assert.IsTrue(screen.IsOpen);
        }
    }
}
