using UnityEngine;

namespace WrongLine.UI
{
    /// <summary>
    /// <see cref="IPrefsStore"/> over PlayerPrefs. Each write is flushed at once: the values change rarely (a toggle, a record), and
    /// a crash or a killed app must not lose a record.
    /// </summary>
    public sealed class PlayerPrefsStore : IPrefsStore
    {
        public int GetInt(string key, int defaultValue) => PlayerPrefs.GetInt(key, defaultValue);

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }
    }
}
