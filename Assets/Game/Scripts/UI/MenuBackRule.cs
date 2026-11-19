namespace WrongLine.UI
{
    /// <summary>What the menu's Back (the Android back key, Escape) does.</summary>
    public enum MenuBack
    {
        CloseSettings,
        CloseQuitPrompt,
        AskToQuit,
    }

    public static class MenuBackRule
    {
        /// <summary>The topmost open layer closes first; with nothing open, Back asks whether to quit.</summary>
        public static MenuBack Next(bool settingsOpen, bool quitPromptOpen)
        {
            if (settingsOpen)
            {
                return MenuBack.CloseSettings;
            }

            return quitPromptOpen ? MenuBack.CloseQuitPrompt : MenuBack.AskToQuit;
        }
    }
}
