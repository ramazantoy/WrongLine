namespace WrongLine.Traffic
{
    /// <summary>
    /// One planned car: which model, in which lane, and how it moves. It drives at <see cref="Speed"/> along the road, starting at
    /// <see cref="StartDistance"/> when the run's clock is 0, and meets the player at <see cref="Sigma"/>.
    /// </summary>
    public readonly struct TrafficCarPlan
    {
        public readonly int Id;
        public readonly int Model;
        public readonly int Paint;
        public readonly int Lane;

        /// <summary>Where on the road the player is when it passes this car, in metres.</summary>
        public readonly double Sigma;

        /// <summary>Half the stretch of σ this car blocks in its lane, in metres.</summary>
        public readonly double HalfSigma;

        /// <summary>Metres per second along the road; negative for a car coming the other way.</summary>
        public readonly double Speed;

        /// <summary>Distance along the road at clock 0, in metres.</summary>
        public readonly double StartDistance;

        public readonly float Length;

        public TrafficCarPlan(int id, int model, int paint, int lane, double sigma, double halfSigma, double speed, double startDistance, float length)
        {
            Id = id;
            Model = model;
            Paint = paint;
            Lane = lane;
            Sigma = sigma;
            HalfSigma = halfSigma;
            Speed = speed;
            StartDistance = startDistance;
            Length = length;
        }

        public double SigmaStart => Sigma - HalfSigma;

        public double SigmaEnd => Sigma + HalfSigma;

        /// <summary>Distance along the road after <paramref name="seconds"/> of the run.</summary>
        public double DistanceAt(double seconds)
        {
            return StartDistance + Speed * seconds;
        }
    }
}
