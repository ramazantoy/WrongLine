namespace WrongLine.Car
{
    /// <summary>What the player is asking the car to do sideways. The only way driving code reads input.</summary>
    internal interface ISteerInput
    {
        /// <summary>Pointer movement since the last frame while it is held down, as a fraction of the screen width (right is positive).</summary>
        float DragFraction { get; }

        /// <summary>A held direction in [-1, 1] (keys, stick, d-pad).</summary>
        float Axis { get; }
    }
}
