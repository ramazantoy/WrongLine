using WrongLine.Road;

namespace WrongLine.Scenery
{
    /// <summary>One prop to put down: which, where in world space, which way it faces and how big.</summary>
    public readonly struct SceneryItem
    {
        public readonly int PropId;
        public readonly WorldPoint Position;

        /// <summary>Heading around the world up, in degrees (props always stand upright).</summary>
        public readonly float YawDegrees;

        public readonly float Scale;

        public SceneryItem(int propId, in WorldPoint position, float yawDegrees, float scale)
        {
            PropId = propId;
            Position = position;
            YawDegrees = yawDegrees;
            Scale = scale;
        }
    }
}
