namespace WrongLine.Crash
{
    /// <summary>The player's car touched a traffic car along its side; the player is pushed away and nothing else is lost.</summary>
    public readonly struct CarSideswiped
    {
        public readonly int CarId;

        /// <summary>+1 when the traffic car is to the player's right, −1 to its left.</summary>
        public readonly int Side;

        /// <summary>How far the boxes overlapped across the road, in metres.</summary>
        public readonly float Depth;

        /// <summary>Sideswipes so far in this run, this one included.</summary>
        public readonly int Count;

        public CarSideswiped(int carId, int side, float depth, int count)
        {
            CarId = carId;
            Side = side;
            Depth = depth;
            Count = count;
        }
    }
}
