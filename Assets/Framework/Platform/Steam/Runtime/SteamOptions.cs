namespace AF.Platform.Steam
{
    /// <summary>The game's identity on Steam, passed to <see cref="SteamPlatform"/> in the root scope.</summary>
    public sealed class SteamOptions
    {
        /// <summary>
        /// The game's app id from Steamworks. A wrong id makes Steam start another app when the game is launched outside
        /// Steam. 480 is Spacewar, Valve's test app: only for local Development builds, refused in other players.
        /// </summary>
        public uint AppId { get; set; }
    }
}
