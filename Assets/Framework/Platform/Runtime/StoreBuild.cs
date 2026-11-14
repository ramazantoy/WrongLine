namespace AF.Platform
{
    /// <summary>
    /// The store this player was built for. The build menu's store builds (AF/Build/Steam, AF/Build/Epic) define
    /// <c>STORE_&lt;NAME&gt;</c>; store-less builds define none, and so does the Editor unless one is added to Player
    /// Settings to try a store backend. The name matches the store backend's <see cref="IPlatformBackend.Name"/>. A new
    /// store adds its branch here and its entry in the build menu's store list.
    /// </summary>
    internal static class StoreBuild
    {
        /// <summary>Null in builds without a store and in the Editor (without a store define in Player Settings).</summary>
        public static string Name =>
#if STORE_STEAM
            "Steam";
#elif STORE_EPIC
            "Epic";
#else
            null;
#endif
    }
}
