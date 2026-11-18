namespace WrongLine.Crash
{
    /// <summary>The player's car hit a traffic car for real: the run is over.</summary>
    public readonly struct CarCrashed
    {
        public readonly int CarId;

        public readonly int Side;

        /// <summary>How fast the player closed in on the car along the road, in metres per second.</summary>
        public readonly float ClosingSpeed;

        /// <summary>+1 when the car hit was ahead of the player, −1 when behind.</summary>
        public readonly float Ahead;

        /// <summary>Where on the road the player was, in metres.</summary>
        public readonly double Distance;

        public CarCrashed(int carId, int side, float closingSpeed, float ahead, double distance)
        {
            CarId = carId;
            Side = side;
            ClosingSpeed = closingSpeed;
            Ahead = ahead;
            Distance = distance;
        }
    }
}
