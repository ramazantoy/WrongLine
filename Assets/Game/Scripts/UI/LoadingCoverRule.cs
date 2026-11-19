namespace WrongLine.UI
{
    /// <summary>When the loading screen covers a scene change.</summary>
    public static class LoadingCoverRule
    {
        /// <summary>
        /// The loading screen covers every change except the one that leaves the intro: the splash goes straight to the menu, and a
        /// cover in between would only flash.
        /// </summary>
        public static bool ShouldCover(string fromScene) => fromScene != Scenes.Intro;
    }
}
