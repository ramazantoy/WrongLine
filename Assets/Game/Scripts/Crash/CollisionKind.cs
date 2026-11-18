namespace WrongLine.Crash
{
    public enum CollisionKind
    {
        None,

        /// <summary>A light touch along the side: the player is pushed away, nothing else is lost.</summary>
        Sideswipe,

        /// <summary>The cars hit each other for real.</summary>
        Crash,
    }
}
