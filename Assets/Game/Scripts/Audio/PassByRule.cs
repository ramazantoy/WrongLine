using System;
using System.Collections.Generic;

namespace WrongLine.Audio
{
    /// <summary>A traffic car went by: how far to the player's right it was and how loud the whoosh should be.</summary>
    public readonly struct PassBy
    {
        public readonly int CarId;
        public readonly float SideMeters;
        public readonly float Volume;

        public PassBy(int carId, float sideMeters, float volume)
        {
            CarId = carId;
            SideMeters = sideMeters;
            Volume = volume;
        }
    }

    /// <summary>
    /// Notices when a traffic car passes the player: the car's place along the road, relative to the player's, changes sign while
    /// the car is in a lane beside the player. Call <see cref="Begin"/>, <see cref="Observe"/> for every car, then <see cref="End"/>
    /// each frame; cars not seen this frame are forgotten. Pure.
    /// </summary>
    public sealed class PassByRule
    {
        private struct Entry
        {
            public double Ahead;
            public int Stamp;
        }

        private readonly PassBySettings _settings;
        private readonly Dictionary<int, Entry> _cars = new Dictionary<int, Entry>();
        private readonly List<int> _stale = new List<int>();
        private int _stamp;

        public PassByRule(PassBySettings settings)
        {
            _settings = settings;
        }

        public void Begin() => _stamp++;

        /// <param name="carId">The traffic car.</param>
        /// <param name="aheadMeters">The car's distance along the road minus the player's: positive while the car is ahead.</param>
        /// <param name="sideMeters">The car's place across the road minus the player's: positive to the player's right.</param>
        /// <param name="deltaTime">Game seconds since the last frame; a pass is not counted while it is 0 (paused).</param>
        public bool Observe(int carId, double aheadMeters, double sideMeters, float deltaTime, out PassBy pass)
        {
            pass = default;
            bool known = _cars.TryGetValue(carId, out Entry before);
            _cars[carId] = new Entry { Ahead = aheadMeters, Stamp = _stamp };
            if (!known)
            {
                return false;
            }

            double step = Math.Abs(aheadMeters - before.Ahead);
            bool crossed = (before.Ahead > 0.0 && aheadMeters <= 0.0) || (before.Ahead < 0.0 && aheadMeters >= 0.0);
            if (!crossed || deltaTime <= 0f || step > _settings.MaxStepMeters || Math.Abs(sideMeters) > _settings.MaxSideMeters)
            {
                return false;
            }

            float closing = (float)(step / deltaTime);
            float volume = _settings.MinVolume + (1f - _settings.MinVolume) * Math.Min(1f, closing / _settings.FullClosingSpeed);
            pass = new PassBy(carId, (float)sideMeters, volume);
            return true;
        }

        public void End()
        {
            _stale.Clear();
            foreach (KeyValuePair<int, Entry> pair in _cars)
            {
                if (pair.Value.Stamp != _stamp)
                {
                    _stale.Add(pair.Key);
                }
            }

            foreach (int id in _stale)
            {
                _cars.Remove(id);
            }
        }
    }
}
