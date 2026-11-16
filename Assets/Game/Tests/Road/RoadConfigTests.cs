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
    }
}
