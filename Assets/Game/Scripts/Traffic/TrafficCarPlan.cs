using System;
using WrongLine.Road;

namespace WrongLine.Traffic
{
    /// <summary>
    /// One planned car: which model, in which lane, and how it moves. It meets the player at <see cref="Sigma"/> in lane
    /// <see cref="Lane"/>, driving at <see cref="Speed"/> along the road (<see cref="StartDistance"/> is where that straight run
    /// would have it when the run's clock is 0). A car that changes lane did so earlier, in <see cref="ChangeSeconds"/> starting at
    /// <see cref="ChangeStart"/>: until then it drove at <see cref="FromSpeed"/> in <see cref="FromLane"/>, over the change its speed
    /// moves from one to the other, and the change is over before the player reaches the car (the planner sees to it).
    /// </summary>
    public readonly struct TrafficCarPlan
    {
        public readonly int Id;
        public readonly int Model;
        public readonly int Paint;

        /// <summary>The lane the car is in when the player passes it.</summary>
        public readonly int Lane;

        /// <summary>Where on the road the player is when it passes this car, in metres.</summary>
        public readonly double Sigma;

        /// <summary>Half the stretch of σ this car blocks in its lane, in metres.</summary>
        public readonly double HalfSigma;

        /// <summary>Metres per second along the road after any lane change; negative for a car coming the other way.</summary>
        public readonly double Speed;

        /// <summary>Distance along the road at clock 0 of the run after the lane change, extended backwards, in metres.</summary>
        public readonly double StartDistance;

        public readonly float Length;

        public readonly int FromLane;
        public readonly double FromSpeed;
        public readonly double ChangeStart;
        public readonly double ChangeSeconds;

        public TrafficCarPlan(int id, int model, int paint, int lane, double sigma, double halfSigma, double speed, double startDistance, float length)
            : this(id, model, paint, lane, sigma, halfSigma, speed, startDistance, length, lane, speed, 0.0, 0.0)
        {
        }

        public TrafficCarPlan(int id, int model, int paint, int lane, double sigma, double halfSigma, double speed, double startDistance, float length, int fromLane, double fromSpeed, double changeStart, double changeSeconds)
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
            FromLane = fromLane;
            FromSpeed = fromSpeed;
            ChangeStart = changeStart;
            ChangeSeconds = changeSeconds;
        }

        public double SigmaStart => Sigma - HalfSigma;

        public double SigmaEnd => Sigma + HalfSigma;

        public bool HasChange => FromLane != Lane;

        public double ChangeEnd => ChangeStart + ChangeSeconds;

        /// <summary>Distance along the road after <paramref name="seconds"/> of the run.</summary>
        public double DistanceAt(double seconds)
        {
            if (!HasChange || seconds >= ChangeEnd)
            {
                return StartDistance + Speed * seconds;
            }

            double end = StartDistance + Speed * ChangeEnd;
            if (seconds <= ChangeStart)
            {
                return end - (FromSpeed + Speed) * 0.5 * ChangeSeconds - FromSpeed * (ChangeStart - seconds);
            }

            // The speed moves in a straight line from FromSpeed to Speed: what is left to cover from `seconds` to the end.
            double tau = seconds - ChangeStart;
            double dv = Speed - FromSpeed;
            double wholeChange = FromSpeed * ChangeSeconds + dv * ChangeSeconds * 0.5;
            double sofar = FromSpeed * tau + dv * tau * tau / (2.0 * ChangeSeconds);
            return end - (wholeChange - sofar);
        }

        /// <summary>The speed along the road at <paramref name="seconds"/>.</summary>
        public double SpeedAt(double seconds)
        {
            if (!HasChange || seconds >= ChangeEnd)
            {
                return Speed;
            }

            return seconds <= ChangeStart ? FromSpeed : FromSpeed + (Speed - FromSpeed) * (seconds - ChangeStart) / ChangeSeconds;
        }

        /// <summary>Metres right of the road's centre line at <paramref name="seconds"/> (the lane's middle, moving over during a change).</summary>
        public double LaneXAt(double seconds)
        {
            double to = RoadSection.LaneCenterX(Lane);
            if (!HasChange)
            {
                return to;
            }

            double from = RoadSection.LaneCenterX(FromLane);
            return from + (to - from) * Smooth(Fraction(seconds));
        }

        /// <summary>Sideways speed at <paramref name="seconds"/>, metres per second (positive to the right of the road).</summary>
        public double LateralSpeedAt(double seconds)
        {
            if (!HasChange || seconds <= ChangeStart || seconds >= ChangeEnd)
            {
                return 0.0;
            }

            double f = Fraction(seconds);
            return (RoadSection.LaneCenterX(Lane) - RoadSection.LaneCenterX(FromLane)) * 6.0 * f * (1.0 - f) / ChangeSeconds;
        }

        /// <summary>Whether the car is in <paramref name="lane"/> (while it changes, in both lanes).</summary>
        public bool OccupiesLane(int lane, double seconds)
        {
            if (!HasChange)
            {
                return lane == Lane;
            }

            if (seconds < ChangeStart)
            {
                return lane == FromLane;
            }

            return seconds < ChangeEnd ? lane == FromLane || lane == Lane : lane == Lane;
        }

        /// <summary>
        /// Which way the turn signal points at <paramref name="seconds"/>: +1 towards the right of the road, −1 towards the left, 0 off.
        /// It starts <paramref name="lead"/> seconds before the car moves and ends <paramref name="tail"/> seconds after.
        /// </summary>
        public int SignalSideAt(double seconds, double lead, double tail)
        {
            if (!HasChange || seconds < ChangeStart - lead || seconds > ChangeEnd + tail)
            {
                return 0;
            }

            return Lane > FromLane ? 1 : -1;
        }

        /// <summary>Whether the signal lamp is lit at <paramref name="seconds"/> (lit for the first half of each period, from its start).</summary>
        public bool SignalLampAt(double seconds, double lead, double period)
        {
            double since = seconds - (ChangeStart - lead);
            return since >= 0.0 && (long)Math.Floor(since / (period * 0.5)) % 2 == 0;
        }

        private double Fraction(double seconds)
        {
            return Math.Min(1.0, Math.Max(0.0, (seconds - ChangeStart) / ChangeSeconds));
        }

        private static double Smooth(double f)
        {
            return f * f * (3.0 - 2.0 * f);
        }
    }
}
