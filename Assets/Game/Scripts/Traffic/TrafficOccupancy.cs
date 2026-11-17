using System;

namespace WrongLine.Traffic
{
    /// <summary>
    /// The stretch of the player's path (σ, metres along the road where the player is when it passes the car) in which a car of a
    /// lane blocks that lane. A car at constant speed meets the player once, at σ, and while the two overlap the player moves
    /// ahead by (closing time) × (its own speed): a slow car on the same side blocks a long stretch, an oncoming one a short one.
    /// The stretch does not change as the cars move, so the whole layout can be planned on σ alone. Pure.
    /// </summary>
    public static class TrafficOccupancy
    {
        /// <param name="carLength">The car's length, in metres.</param>
        /// <param name="playerLength">The player car's length, in metres.</param>
        /// <param name="playerSpeed">The player's speed where they meet, in m/s.</param>
        /// <param name="carSpeed">The car's speed along the road (negative: oncoming), in m/s.</param>
        /// <returns>Half the length of the blocked stretch, in metres of σ.</returns>
        public static double HalfLength(double carLength, double playerLength, double playerSpeed, double carSpeed, double padMeters, double padSeconds)
        {
            double closing = Math.Abs(playerSpeed - carSpeed);
            if (!(closing > 1e-6))
            {
                throw new ArgumentException("The car is as fast as the player: it never meets it.", nameof(carSpeed));
            }

            double room = (carLength + playerLength) * 0.5 + padMeters + padSeconds * closing;
            return room * playerSpeed / closing;
        }
    }
}
