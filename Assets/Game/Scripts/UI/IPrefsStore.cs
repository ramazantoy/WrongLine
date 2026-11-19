namespace WrongLine.UI
{
    /// <summary>Where the game's own small settings and records live: PlayerPrefs on the device, a dictionary in tests.</summary>
    public interface IPrefsStore
    {
        int GetInt(string key, int defaultValue);

        void SetInt(string key, int value);
    }
}
