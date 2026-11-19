namespace WrongLine.UI
{
    /// <summary>What Back (the Android back key, Escape) does while driving.</summary>
    public enum PauseBack
    {
        None,
        OpenPause,
        CloseSettings,
        Resume,
    }

    public static class PauseBackRule
    {
        /// <summary>After a crash Back does nothing (the game over screen is up or coming); otherwise the topmost open layer closes first.</summary>
        public static PauseBack Next(bool crashed, bool paused, bool settingsOpen)
        {
            if (crashed)
            {
                return PauseBack.None;
            }

            if (settingsOpen)
            {
                return PauseBack.CloseSettings;
            }

            return paused ? PauseBack.Resume : PauseBack.OpenPause;
        }
    }
}
