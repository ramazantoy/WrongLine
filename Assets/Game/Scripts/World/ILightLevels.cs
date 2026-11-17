namespace WrongLine.World
{
    /// <summary>How lit the night lights are right now (0 off, 1 fully on), as the views read them each frame.</summary>
    internal interface ILightLevels
    {
        float StreetLamps { get; }

        float Headlights { get; }
    }
}
