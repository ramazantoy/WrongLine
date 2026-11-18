namespace WrongLine.Crash
{
    /// <summary>What <see cref="CollisionRule"/> found in one update.</summary>
    public readonly struct CollisionResult
    {
        public static readonly CollisionResult None = new CollisionResult(CollisionKind.None, -1, 0, 0f, 0f, 0f);

        public readonly CollisionKind Kind;

        /// <summary>The traffic car's id.</summary>
        public readonly int CarId;

        /// <summary>+1 when the car is to the player's right, −1 to its left.</summary>
        public readonly int Side;

        /// <summary>How far the boxes overlap across the road, in metres.</summary>
        public readonly float Depth;

        /// <summary>How fast the player closes in on the car along the road, in metres per second (negative: pulling away).</summary>
        public readonly float ClosingSpeed;

        /// <summary>+1 when the car is ahead of the player, −1 behind it.</summary>
        public readonly float Ahead;

        public CollisionResult(CollisionKind kind, int carId, int side, float depth, float closingSpeed, float ahead)
        {
            Kind = kind;
            CarId = carId;
            Side = side;
            Depth = depth;
            ClosingSpeed = closingSpeed;
            Ahead = ahead;
        }
    }
}
