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
        private const double TurnEpsilon = 1e-6;

        private readonly List<RoadPieceInfo> _pieces = new List<RoadPieceInfo>();
        private readonly RoadGeneratorSettings _settings;
        private readonly RoadRandom _random;
        private readonly Queue<int> _recent = new Queue<int>();
        private readonly List<RoadPieceInfo> _candidates = new List<RoadPieceInfo>();
        private readonly List<double> _weights = new List<double>();
        private readonly List<Motif> _motifs = new List<Motif>();
        private readonly List<string> _skippedMotifs = new List<string>();
        private readonly Queue<RoadPieceInfo> _queued = new Queue<RoadPieceInfo>();
        private readonly List<Motif> _fitting = new List<Motif>();
        private int _lastTurn;

        private sealed class Motif
        {
            public Motif(RoadMotifDef def, List<RoadPieceInfo> pieces)
            {
                Def = def;
                Pieces = pieces;
            }

            public RoadMotifDef Def { get; }

            public List<RoadPieceInfo> Pieces { get; }
        }

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
            ResolveMotifs();
        }

        public ulong Seed { get; }

        /// <summary>The motifs left out because they name a piece the piece list does not have (the validator reports these as errors).</summary>
        public IReadOnlyList<string> SkippedMotifs => _skippedMotifs;

        /// <param name="entry">Where the next piece's entry will sit.</param>
        /// <param name="distance">How far along the road that is, in metres (drives the difficulty).</param>
        /// <exception cref="InvalidOperationException">No piece fits (the catalog lacks a straight, or the settings are too tight).</exception>
        public RoadPieceInfo Next(in RoadPose entry, double distance)
        {
            if (_queued.Count > 0)
            {
                // The rest of a motif already placed: its fit was checked as a whole when it was chosen.
                RoadPieceInfo queued = _queued.Dequeue();
                Record(queued);
                return queued;
            }

            RoadPieceInfo motifStart = TryStartMotif(entry);
            if (motifStart != null)
            {
                return motifStart;
            }

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

                if (_lastTurn != 0 && Math.Abs(piece.ExitYaw) > TurnEpsilon && Math.Sign(piece.ExitYaw) == _lastTurn)
                {
                    weight *= _settings.TurnMomentum; // A curve that keeps turning the same way: sweeping bends.
                }

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

            Record(chosen);
            return chosen;
        }

        private void Record(RoadPieceInfo piece)
        {
            _recent.Enqueue(piece.Id);
            while (_recent.Count > _settings.RepeatWindow)
            {
                _recent.Dequeue();
            }

            if (Math.Abs(piece.ExitYaw) > TurnEpsilon)
            {
                _lastTurn = Math.Sign(piece.ExitYaw);
            }
        }

        private void ResolveMotifs()
        {
            var byName = new Dictionary<string, RoadPieceInfo>();
            foreach (RoadPieceInfo piece in _pieces)
            {
                byName[piece.Name] = piece;
            }

            foreach (RoadMotifDef def in _settings.Motifs)
            {
                var resolved = new List<RoadPieceInfo>();
                foreach (string name in def.Pieces)
                {
                    if (!byName.TryGetValue(name, out RoadPieceInfo piece))
                    {
                        resolved = null;
                        break;
                    }

                    resolved.Add(piece);
                }

                if (resolved == null)
                {
                    _skippedMotifs.Add(def.Name);
                }
                else
                {
                    _motifs.Add(new Motif(def, resolved));
                }
            }
        }

        /// <summary>Maybe starts a motif at this entry: returns its first piece and queues the rest, or null for an ordinary pick.</summary>
        private RoadPieceInfo TryStartMotif(in RoadPose entry)
        {
            if (_motifs.Count == 0 || _random.NextDouble() >= _settings.MotifChance)
            {
                return null;
            }

            _fitting.Clear();
            double total = 0.0;
            foreach (Motif motif in _motifs)
            {
                if (Fits(motif, entry))
                {
                    _fitting.Add(motif);
                    total += motif.Def.Weight;
                }
            }

            if (_fitting.Count == 0)
            {
                return null;
            }

            double roll = _random.NextDouble() * total;
            Motif chosen = _fitting[_fitting.Count - 1];
            double running = 0.0;
            foreach (Motif motif in _fitting)
            {
                running += motif.Def.Weight;
                if (roll < running)
                {
                    chosen = motif;
                    break;
                }
            }

            for (int i = 1; i < chosen.Pieces.Count; i++)
            {
                _queued.Enqueue(chosen.Pieces[i]);
            }

            Record(chosen.Pieces[0]);
            return chosen.Pieces[0];
        }

        // Every piece of the motif, one after the other from this entry, must satisfy the same hard rules as a single pick.
        private bool Fits(Motif motif, in RoadPose entry)
        {
            double yaw = entry.YawDegrees;
            double y = entry.Y;
            foreach (RoadPieceInfo piece in motif.Pieces)
            {
                if (yaw + piece.MaxYawDegrees > _settings.MaxYawDegrees || yaw + piece.MinYawDegrees < -_settings.MaxYawDegrees)
                {
                    return false;
                }

                if (y + piece.MinY < _settings.MinHeight || y + piece.MaxY > _settings.MaxHeight)
                {
                    return false;
                }

                yaw += piece.ExitYaw * (180.0 / Math.PI);
                y += piece.ExitOffset.y;
            }

            return true;
        }
    }
}
