namespace WrongLine.Car
{
    /// <summary>What a collision does to the car's movement: a sideswipe pushes it sideways, a crash stops it.</summary>
    internal interface ICarImpact
    {
        /// <summary>Pushes the car sideways, <paramref name="direction"/> +1 to the right, −1 to the left. The speed along the road is not touched.</summary>
        void Sideswipe(int direction, float meters, float speed);

        /// <summary>Stops the car for good: no more input, no more speed, no more sideways movement.</summary>
        void Crash();
    }
}
