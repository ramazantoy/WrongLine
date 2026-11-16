using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Road.Tests
{
    public class RoadRhythmTests
    {
        private const int Seeds = 20;
        private const int PiecesPerSeed = 300;

        [TestCase(1.0f, 1.0f, 0f)]
        [TestCase(1.8f, 1.0f, 0.12f)]
        [TestCase(3f, 1.0f, 0.12f)]
        [TestCase(4f, 0.6f, 0.12f)]
        [TestCase(6f, 0.5f, 0.12f)]
        public void Report_TheRhythmWithTheseSettings(float momentum, float yawPull, float motifChance)
        {
            RoadRhythm.Report report = RoadRhythm.MeasureSeeds(seed => RoadTestPieces.Layout(seed, new RoadGeneratorSettings { TurnMomentum = momentum, YawPull = yawPull, MotifChance = motifChance }), Seeds, PiecesPerSeed);

            Debug.Log($"[RoadRhythm] momentum {momentum} yawPull {yawPull} motifChance {motifChance}: {report}");
            Assert.Greater(report.Pieces, 0);
        }

        [Test]
        public void Report_TheRhythmOfTheDefaultSettings()
        {
            RoadRhythm.Report report = RoadRhythm.MeasureSeeds(seed => RoadTestPieces.Layout(seed), Seeds, PiecesPerSeed);

            Debug.Log($"[RoadRhythm] defaults, {Seeds} seeds: {report}");
            Assert.Greater(report.Pieces, 0);
        }

        [Test]
        public void TheShippedRoadConfigAsset_GivesTheTunedRhythm_NotJustTheCodeDefaults()
        {
            // The game runs on the asset's numbers; a default changed in code does not reach an asset saved earlier.
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<RoadConfig>("Assets/Game/Road/RoadConfig.asset");
            Assert.IsNotNull(config, "Assets/Game/Road/RoadConfig.asset");

            RoadRhythm.Report report = RoadRhythm.MeasureSeeds(seed => RoadTestPieces.Layout(seed, config.Generator), Seeds, PiecesPerSeed);

            Assert.Greater(report.CurveSameDirectionRatio, 0.38, report.ToString());
            Assert.Less(report.MeanStraightRun, 60.0, report.ToString());
        }

        [Test]
        public void TurnMomentum_MakesTheRoadKeepTurningTheSameWay()
        {
            var off = new RoadGeneratorSettings { TurnMomentum = 1f, MotifChance = 0f };
            var on = new RoadGeneratorSettings { TurnMomentum = 4f, MotifChance = 0f };

            RoadRhythm.Report without = RoadRhythm.MeasureSeeds(seed => RoadTestPieces.Layout(seed, off), Seeds, PiecesPerSeed);
            RoadRhythm.Report with = RoadRhythm.MeasureSeeds(seed => RoadTestPieces.Layout(seed, on), Seeds, PiecesPerSeed);

            Assert.Greater(with.CurveSameDirectionRatio, without.CurveSameDirectionRatio + 0.05, $"without {without}, with {with}");
        }

        [Test]
        public void TheDefaults_KeepTheRoadFromAlternatingAndFromLongStraights()
        {
            RoadRhythm.Report report = RoadRhythm.MeasureSeeds(seed => RoadTestPieces.Layout(seed), Seeds, PiecesPerSeed);

            Assert.Greater(report.CurveSameDirectionRatio, 0.38, report.ToString());
            Assert.Less(report.MeanStraightRun, 60.0, report.ToString());
        }

        [Test]
        public void Motifs_AppearInTheRoad_AsTheirWholeRun()
        {
            var settings = new RoadGeneratorSettings { MotifChance = 0.5f };
            RoadLayout layout = RoadTestPieces.Layout(3, settings);
            System.Collections.Generic.List<RoadPieceInfo> sequence = RoadRhythm.Sequence(layout, 400);
            string names = string.Join(",", System.Linq.Enumerable.Select(sequence, piece => piece.Name));

            // The sweep is the one motif whose pair never comes up by chance as often: two soft banks the same way.
            Assert.IsTrue(names.Contains("BankSoft_R,BankSoft_R") || names.Contains("BankSoft_L,BankSoft_L"), "no sweep motif in 400 pieces");
            Assert.IsTrue(names.Contains("RampUp_L,Straight_S,SteepDown_S") || names.Contains("RampUp_S,Straight_S,RampUp_L,Straight_M"), "no climbing motif in 400 pieces");
        }

        [Test]
        public void AMotifThatDoesNotFitTheBands_IsNeverPlaced()
        {
            // Three right turns of 35 degrees is 105 degrees: past the 75 degree band, so a motif of them can never be placed whole.
            var settings = new RoadGeneratorSettings
            {
                MotifChance = 0.9f,
                Motifs = new System.Collections.Generic.List<RoadMotifDef> { new RoadMotifDef { Name = "Spiral", Weight = 1f, Pieces = new[] { "CurveSoft_R", "CurveSoft_R", "CurveSoft_R" } } },
            };
            RoadLayout layout = RoadTestPieces.Layout(9, settings);
            layout.EnsureCovered(3000 * 30.0);

            for (int i = 1; i < layout.EndIndex; i++)
            {
                double yaw = layout[i].Entry.YawDegrees + layout[i].Piece.MaxYawDegrees;
                Assert.LessOrEqual(yaw, settings.MaxYawDegrees + 1e-3, $"piece {i} {layout[i].Piece}");
            }
        }

        [Test]
        public void AMotifNamingAMissingPiece_IsSkipped_AndReported()
        {
            var settings = new RoadGeneratorSettings
            {
                Motifs = new System.Collections.Generic.List<RoadMotifDef> { new RoadMotifDef { Name = "Ghost", Weight = 1f, Pieces = new[] { "Straight_S", "NoSuchPiece" } } },
            };

            var generator = new RoadGenerator(RoadTestPieces.All(), settings, 1);

            CollectionAssert.AreEqual(new[] { "Ghost" }, generator.SkippedMotifs);
        }

        [Test]
        public void TheSameSeed_GivesTheSameRoad_WithMotifs()
        {
            var settings = new RoadGeneratorSettings { MotifChance = 0.4f };

            string a = string.Join(",", System.Linq.Enumerable.Select(RoadRhythm.Sequence(RoadTestPieces.Layout(21, settings), 300), p => p.Id));
            string b = string.Join(",", System.Linq.Enumerable.Select(RoadRhythm.Sequence(RoadTestPieces.Layout(21, settings), 300), p => p.Id));

            Assert.AreEqual(a, b);
        }
    }
}
