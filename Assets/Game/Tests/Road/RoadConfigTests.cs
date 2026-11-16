using System;
using AF.Core.Editor;
using NUnit.Framework;
using UnityEditor;
using WrongLine.Road.Editor;

namespace WrongLine.Road.Tests
{
    public class RoadConfigTests
    {
        private RoadTestKit _kit;

        [SetUp]
        public void SetUpKit()
        {
            _kit = new RoadTestKit();
        }

        [TearDown]
        public void TearDownKit()
        {
            _kit.Dispose();
        }

        [Test]
        public void TheDefaults_AreValid_AndMatchTheGeneratorDefaults()
        {
            RoadConfig config = _kit.MakeConfig();

            Assert.DoesNotThrow(() => config.Validate());
            Assert.AreEqual(new RoadGeneratorSettings().MaxYawDegrees, config.Generator.MaxYawDegrees);
            Assert.AreEqual(0, config.Seed, "0 means a new random road every run.");
        }

        [TestCase("_aheadMeters", 0f)]
        [TestCase("_aheadMeters", -5f)]
        [TestCase("_behindMeters", -1f)]
        [TestCase("_originShiftMeters", 0f)]
        public void NonsenseNumbers_AreRejected(string field, float value)
        {
            RoadConfig config = _kit.MakeConfig(serialized => serialized.FindProperty(field).floatValue = value);

            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [Test]
        public void NonsenseGeneratorRules_AreRejectedToo()
        {
            RoadConfig config = _kit.MakeConfig(serialized => serialized.FindProperty("_generator").FindPropertyRelative("MaxYawDegrees").floatValue = 95f);

            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [Test]
        public void TheValidator_ReportsABadConfig_AndNotAGoodOne()
        {
            var report = new ValidationReport();

            RoadConfigValidator.Check(_kit.MakeConfig(), "good", report);
            Assert.AreEqual(0, report.ErrorCount);

            RoadConfigValidator.Check(_kit.MakeConfig(serialized => serialized.FindProperty("_originShiftMeters").floatValue = 0f), "bad", report);
            Assert.AreEqual(1, report.ErrorCount);
            StringAssert.Contains("bad", report.Issues[0].Message);
        }

        [Test]
        public void TheDefaultMotifs_OnlyNamePiecesTheDefaultCatalogHas()
        {
            var report = new ValidationReport();
            var names = new System.Collections.Generic.HashSet<string>(System.Linq.Enumerable.Select(RoadPieceSpecs.Defaults, spec => spec.Name));

            RoadConfigValidator.CheckMotifs(_kit.MakeConfig(), names, "defaults", report);

            Assert.AreEqual(0, report.ErrorCount, string.Join("; ", System.Linq.Enumerable.Select(report.Issues, issue => issue.Message)));
        }

        [Test]
        public void AMotifNamingAPieceTheCatalogLacks_IsAnError()
        {
            var report = new ValidationReport();
            var names = new System.Collections.Generic.HashSet<string> { "CurveSoft_R", "CurveSoft_L" }; // No banks, no ramps.

            RoadConfigValidator.CheckMotifs(_kit.MakeConfig(), names, "short catalog", report);

            Assert.Greater(report.ErrorCount, 0);
            StringAssert.Contains("which the catalog does not have", report.Issues[0].Message);
        }

        [TestCase("")]
        public void AMotifWithoutAName_IsRejected(string name)
        {
            var settings = new RoadGeneratorSettings();
            settings.Motifs.Add(new RoadMotifDef { Name = name, Weight = 1f, Pieces = new[] { "Straight_S", "Straight_M" } });

            Assert.Throws<ArgumentException>(() => settings.Validate());
        }

        [TestCase(0f, 2)]
        [TestCase(-1f, 2)]
        [TestCase(1f, 1)]
        [TestCase(1f, 0)]
        public void AMotifWithABadWeightOrTooFewPieces_IsRejected(float weight, int pieces)
        {
            var settings = new RoadGeneratorSettings();
            settings.Motifs.Add(new RoadMotifDef { Name = "Bad", Weight = weight, Pieces = new string[pieces] });

            Assert.Throws<ArgumentException>(() => settings.Validate());
        }

        [TestCase(-0.1f)]
        [TestCase(1f)]
        public void ANonsenseMotifChance_IsRejected(float chance)
        {
            Assert.Throws<ArgumentException>(() => new RoadGeneratorSettings { MotifChance = chance }.Validate());
        }
    }
}
