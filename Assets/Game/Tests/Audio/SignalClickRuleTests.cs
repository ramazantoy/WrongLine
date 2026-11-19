using NUnit.Framework;
using WrongLine.Audio;
using WrongLine.Haptics;

namespace WrongLine.Tests.Audio
{
    public sealed class SignalClickRuleTests
    {
        [Test]
        public void TicksWhenTheLampComesOn_AndTocksWhenItGoesOff_OnlyOnTheEdges()
        {
            var rule = new SignalClickRule();

            Assert.AreEqual(SignalClick.None, rule.Observe(false));
            Assert.AreEqual(SignalClick.Tick, rule.Observe(true));
            Assert.AreEqual(SignalClick.None, rule.Observe(true));
            Assert.AreEqual(SignalClick.Tock, rule.Observe(false));
            Assert.AreEqual(SignalClick.None, rule.Observe(false));
            Assert.AreEqual(SignalClick.Tick, rule.Observe(true));
        }

        [Test]
        public void OnlyASliderTickVibratesLightly()
        {
            Assert.AreEqual(HapticKind.Tick, UiFeedbackMap.HapticOf(UiFeedbackKind.Tick));
            Assert.AreEqual(HapticKind.Tap, UiFeedbackMap.HapticOf(UiFeedbackKind.Tap));
            Assert.AreEqual(HapticKind.Tap, UiFeedbackMap.HapticOf(UiFeedbackKind.Back));
            Assert.AreEqual(HapticKind.Tap, UiFeedbackMap.HapticOf(UiFeedbackKind.Toggle));
        }
    }
}
