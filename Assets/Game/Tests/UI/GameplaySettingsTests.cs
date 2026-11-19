using System.Collections.Generic;
using MessagePipe;
using NUnit.Framework;
using WrongLine.UI;

namespace WrongLine.Tests.UI
{
    internal sealed class CountingPublisher<T> : IPublisher<T>
    {
        public int Count;

        public void Publish(T message) => Count++;
    }

    /// <summary>A dictionary standing in for PlayerPrefs; counts the writes.</summary>
    internal sealed class FakePrefsStore : IPrefsStore
    {
        public readonly Dictionary<string, int> Values = new Dictionary<string, int>();
        public int Writes;

        public int GetInt(string key, int defaultValue) => Values.TryGetValue(key, out var v) ? v : defaultValue;

        public void SetInt(string key, int value)
        {
            Values[key] = value;
            Writes++;
        }
    }

    public sealed class GameplaySettingsTests
    {
        [Test]
        public void Haptics_AreOnByDefault()
        {
            Assert.IsTrue(new GameplaySettings(new CountingPublisher<GameplaySettingsChanged>(), new FakePrefsStore()).Haptics);
        }

        [Test]
        public void SetHaptics_PublishesAndWrites_OnlyWhenItChanges()
        {
            var publisher = new CountingPublisher<GameplaySettingsChanged>();
            var store = new FakePrefsStore();
            var settings = new GameplaySettings(publisher, store);

            settings.SetHaptics(true);
            Assert.AreEqual(0, publisher.Count);
            Assert.AreEqual(0, store.Writes);

            settings.SetHaptics(false);
            Assert.IsFalse(settings.Haptics);
            Assert.AreEqual(1, publisher.Count);
            Assert.AreEqual(1, store.Writes);
        }

        [Test]
        public void ASavedChoice_IsReadBackByTheNextInstance()
        {
            var store = new FakePrefsStore();
            new GameplaySettings(new CountingPublisher<GameplaySettingsChanged>(), store).SetHaptics(false);

            var next = new GameplaySettings(new CountingPublisher<GameplaySettingsChanged>(), store);

            Assert.IsFalse(next.Haptics);
        }
    }
}
