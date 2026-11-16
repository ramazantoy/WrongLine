using NUnit.Framework;

namespace WrongLine.Road.Tests
{
    public class RoadTaggerTests
    {
        [Test]
        public void PlainPiece_IsStraight()
        {
            Assert.AreEqual(RoadTags.Straight, RoadTagger.From(TrackRecipe.Straight(20f)));
        }

        [TestCase(40f, YawProfile.Single, RoadTags.Curve)]
        [TestCase(-40f, YawProfile.Single, RoadTags.Curve)]
        [TestCase(40f, YawProfile.S, RoadTags.SCurve)]
        public void Bend_IsACurveOrAnSCurve_RegardlessOfSide(float yaw, YawProfile profile, RoadTags expected)
        {
            Assert.AreEqual(expected, RoadTagger.From(new TrackRecipe(60f, yaw, profile, 0f, PitchProfile.Bump, 0f)));
        }

        [TestCase(8f, PitchProfile.Bump, RoadTags.Climb)]
        [TestCase(-8f, PitchProfile.Bump, RoadTags.Drop)]
        [TestCase(8f, PitchProfile.Wave, RoadTags.Hill)]
        [TestCase(-8f, PitchProfile.Wave, RoadTags.Dip)]
        public void Slope_IsClimbDropHillOrDip(float pitch, PitchProfile profile, RoadTags expected)
        {
            Assert.AreEqual(expected, RoadTagger.From(new TrackRecipe(60f, 0f, YawProfile.Single, pitch, profile, 0f)));
        }

        [Test]
        public void BankedBend_CarriesBothTags_AndIsNotStraight()
        {
            RoadTags tags = RoadTagger.From(new TrackRecipe(60f, 45f, YawProfile.Single, 0f, PitchProfile.Bump, 12f));

            Assert.AreEqual(RoadTags.Curve | RoadTags.Banked, tags);
            Assert.IsFalse(tags.HasFlag(RoadTags.Straight));
        }
    }
}
