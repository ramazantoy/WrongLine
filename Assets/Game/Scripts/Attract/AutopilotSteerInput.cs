using WrongLine.Car;

namespace WrongLine.Attract
{
    /// <summary>Steering the autopilot writes and the car reads, in place of the player's pointer and keys.</summary>
    internal sealed class AutopilotSteerInput : ISteerInput
    {
        public float DragFraction => 0f;

        public float Axis { get; set; }
    }
}
