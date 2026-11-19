using WrongLine.Driving;

namespace WrongLine.Attract
{
    /// <summary>The road streamer's readout for a scene that has none on screen.</summary>
    internal sealed class NullRoadDebugHud : IRoadDebugHud
    {
        public void Show(in RoadDebugInfo info)
        {
        }
    }
}
