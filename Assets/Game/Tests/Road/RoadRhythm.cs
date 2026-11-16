using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WrongLine.Road.Tests
{
    /// <summary>
    /// How a generated road reads: how long the straights run, how often the same kind of piece repeats, whether curves keep
    /// turning the same way, how varied the sequence is. Used to compare generator settings before and after a change; a
    /// road that reads as "the algorithm's" shows in these numbers (long straights, same-kind runs, a small repeating alphabet).
    /// </summary>
    internal static class RoadRhythm
    {
        internal readonly struct Report
        {
            public Report(int pieces, double meanStraightRun, double maxStraightRun, double familyRepeatRatio, double curveSameDirectionRatio, double distinctTrigramRatio, double meanLength)
            {
                Pieces = pieces;
                MeanStraightRun = meanStraightRun;
                MaxStraightRun = maxStraightRun;
                FamilyRepeatRatio = familyRepeatRatio;
                CurveSameDirectionRatio = curveSameDirectionRatio;
                DistinctTrigramRatio = distinctTrigramRatio;
                MeanLength = meanLength;
            }

            public int Pieces { get; }

            /// <summary>Mean length of an unbroken run of plain straight pieces, in metres.</summary>
            public double MeanStraightRun { get; }

            public double MaxStraightRun { get; }

            /// <summary>Share of neighbouring pieces with the same tags (a straight after a straight, a soft curve after a soft curve).</summary>
            public double FamilyRepeatRatio { get; }

            /// <summary>Of the neighbouring pairs that both turn, the share that turn the same way (1 = a spiral, 0 = a slalom).</summary>
            public double CurveSameDirectionRatio { get; }

            /// <summary>Distinct three-piece sequences over all of them: low means a short vocabulary repeating.</summary>
            public double DistinctTrigramRatio { get; }

            public double MeanLength { get; }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append($"pieces {Pieces}, straight run mean {MeanStraightRun:F1} m max {MaxStraightRun:F0} m, ");
                sb.Append($"same-family neighbours {FamilyRepeatRatio:P0}, curves turning the same way {CurveSameDirectionRatio:P0}, ");
                sb.Append($"distinct trigrams {DistinctTrigramRatio:P0}, mean length {MeanLength:F1} m");
                return sb.ToString();
            }
        }

        /// <summary>The pieces after the start piece, in order, of a road run for at least this many pieces.</summary>
        public static List<RoadPieceInfo> Sequence(RoadLayout layout, int pieces)
        {
            layout.EnsureCovered(pieces * 150.0); // More than any piece is long, so there are always enough pieces.
            var sequence = new List<RoadPieceInfo>();
            for (int i = 1; i < layout.EndIndex && sequence.Count < pieces; i++)
            {
                sequence.Add(layout[i].Piece);
            }

            return sequence;
        }

        public static Report Measure(IReadOnlyList<RoadPieceInfo> sequence)
        {
            var straightRuns = new List<double>();
            double run = 0.0;
            foreach (RoadPieceInfo piece in sequence)
            {
                if (piece.Tags == RoadTags.Straight)
                {
                    run += piece.Length;
                }
                else if (run > 0.0)
                {
                    straightRuns.Add(run);
                    run = 0.0;
                }
            }

            if (run > 0.0)
            {
                straightRuns.Add(run);
            }

            int sameFamily = 0;
            for (int i = 1; i < sequence.Count; i++)
            {
                if (sequence[i - 1].Tags == sequence[i].Tags)
                {
                    sameFamily++;
                }
            }

            // Turning pieces in order, with the straights and S-curves between them skipped: does the road keep turning the same way?
            int turningPairs = 0;
            int sameDirection = 0;
            int lastTurn = 0;
            foreach (RoadPieceInfo piece in sequence)
            {
                if (Math.Abs(piece.ExitYaw) <= 1e-6)
                {
                    continue;
                }

                int turn = Math.Sign(piece.ExitYaw);
                if (lastTurn != 0)
                {
                    turningPairs++;
                    if (turn == lastTurn)
                    {
                        sameDirection++;
                    }
                }

                lastTurn = turn;
            }

            var trigrams = new HashSet<string>();
            int trigramCount = 0;
            for (int i = 2; i < sequence.Count; i++)
            {
                trigrams.Add($"{sequence[i - 2].Id},{sequence[i - 1].Id},{sequence[i].Id}");
                trigramCount++;
            }

            return new Report(
                sequence.Count,
                straightRuns.Count == 0 ? 0.0 : straightRuns.Average(),
                straightRuns.Count == 0 ? 0.0 : straightRuns.Max(),
                sequence.Count < 2 ? 0.0 : (double)sameFamily / (sequence.Count - 1),
                turningPairs == 0 ? 0.0 : (double)sameDirection / turningPairs,
                trigramCount == 0 ? 0.0 : (double)trigrams.Count / trigramCount,
                sequence.Count == 0 ? 0.0 : sequence.Average(piece => (double)piece.Length));
        }

        /// <summary>The mean of a measurement over several seeds, so one lucky road does not decide.</summary>
        public static Report MeasureSeeds(Func<ulong, RoadLayout> layoutFor, int seeds, int piecesPerSeed)
        {
            var reports = new List<Report>();
            for (ulong seed = 1; seed <= (ulong)seeds; seed++)
            {
                reports.Add(Measure(Sequence(layoutFor(seed), piecesPerSeed)));
            }

            return new Report(
                (int)reports.Average(r => r.Pieces),
                reports.Average(r => r.MeanStraightRun),
                reports.Max(r => r.MaxStraightRun),
                reports.Average(r => r.FamilyRepeatRatio),
                reports.Average(r => r.CurveSameDirectionRatio),
                reports.Average(r => r.DistinctTrigramRatio),
                reports.Average(r => r.MeanLength));
        }
    }
}
