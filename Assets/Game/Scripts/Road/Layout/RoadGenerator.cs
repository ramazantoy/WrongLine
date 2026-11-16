using System;
using System.Collections.Generic;

namespace WrongLine.Road
{
    /// <summary>
    /// Chooses the next road piece. Seeded: the same seed, pieces and settings give the same road. Hard rules (the heading
    /// stays inside the yaw band and the height inside the height band, anywhere along the piece) decide what may come next;
    /// soft rules (tag weights, difficulty over distance, pulling back to the middle of the bands, no quick repeats) decide
    /// what is likely. All arithmetic is plain double add and multiply, so the result does not depend on the platform.
    /// </summary>
    public sealed class RoadGenerator
    {
        private const double MinPullFactor = 0.1;

        private readonly List<RoadPieceInfo> _pieces = new List<RoadPieceInfo>();
        private readonly RoadGeneratorSettings _settings;
        private readonly RoadRandom _random;
        private readonly Queue<int> _recent = new Queue<int>();
        private readonly List<RoadPieceInfo> _candidates = new List<RoadPieceInfo>();
        private readonly List<double> _weights = new List<double>();

        public RoadGenerator(IEnumerable<RoadPieceInfo> pieces, RoadGeneratorSettings settings, ulong seed)
        {
            settings.Validate();
            foreach (RoadPieceInfo piece in pieces)
            {
                if (!piece.IsStart)
                {
                    _pieces.Add(piece);
                }
            }

            if (_pieces.Count == 0)
            {
                throw new ArgumentException("The generator needs at least one selectable piece.", nameof(pieces));
            }

            _settings = settings;
            Seed = seed;
            _random = new RoadRandom(seed);
        }

        public ulong Seed { get; }

        /// <param name="entry">Where the next piece's entry will sit.</param>
        /// <param name="distance">How far along the road that is, in metres (drives the difficulty).</param>
        /// <exception cref="InvalidOperationException">No piece fits (the catalog lacks a straight, or the settings are too tight).</exception>
        public RoadPieceInfo Next(in RoadPose entry, double distance)
        {
            double yaw = entry.YawDegrees;
            double difficulty = Math.Min(1.0, Math.Max(0.0, distance / _settings.DifficultyRampMeters));
            double center = (_settings.MaxHeight + _settings.MinHeight) * 0.5;
            double halfRange = (_settings.MaxHeight - _settings.MinHeight) * 0.5;

            _candidates.Clear();
            _weights.Clear();
            double total = 0.0;
            bool anyFresh = false;

            foreach (RoadPieceInfo piece in _pieces)
            {
                if (yaw + piece.MaxYawDegrees > _settings.MaxYawDegrees || yaw + piece.MinYawDegrees < -_settings.MaxYawDegrees)
                {
                    continue;
                }

                if (entry.Y + piece.MinY < _settings.MinHeight || entry.Y + piece.MaxY > _settings.MaxHeight)
                {
                    continue;
                }

                double intensity = piece.Intensity;
                double easy = 1.0 - _settings.EasyIntensityPenalty * intensity;
                double hard = _settings.HardBase + _settings.HardGain * intensity;
                double weight = _settings.WeightOf(piece.Tags) * (easy + (hard - easy) * difficulty);

                double yawPull = _settings.YawPull * (Math.Abs(yaw) - Math.Abs(yaw + piece.ExitYaw * (180.0 / Math.PI))) / _settings.MaxYawDegrees;
                double heightPull = _settings.HeightPull * (Math.Abs(entry.Y - center) - Math.Abs(entry.Y + piece.ExitOffset.y - center)) / halfRange;
                weight *= Math.Max(MinPullFactor, 1.0 + yawPull + heightPull);

                bool fresh = !_recent.Contains(piece.Id);
                anyFresh |= fresh;
                _candidates.Add(piece);
                _weights.Add(fresh ? weight : -weight); // A negative weight marks a recent piece; see below.
            }

            if (_candidates.Count == 0)
            {
                throw new InvalidOperationException($"No road piece fits at yaw {yaw:F1} degrees, height {entry.Y:F1} m.");
            }

            for (int i = 0; i < _weights.Count; i++)
            {
                // Recent pieces are left out unless nothing else fits.
                double weight = _weights[i];
                if (weight < 0.0)
                {
                    weight = anyFresh ? 0.0 : -weight;
                    _weights[i] = weight;
                }

                total += weight;
            }

            double roll = _random.NextDouble() * total;
            RoadPieceInfo chosen = null;
            double running = 0.0;
            for (int i = 0; i < _candidates.Count; i++)
            {
                if (_weights[i] <= 0.0)
                {
                    continue;
                }

                chosen = _candidates[i]; // The last one with weight stands in if rounding leaves the roll past the end.
                running += _weights[i];
                if (roll < running)
                {
                    break;
                }
            }

            _recent.Enqueue(chosen.Id);
            while (_recent.Count > _settings.RepeatWindow)
            {
                _recent.Dequeue();
            }

            return chosen;
        }
    }
}
