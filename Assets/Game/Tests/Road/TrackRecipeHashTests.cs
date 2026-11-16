using NUnit.Framework;

namespace WrongLine.Road.Tests
{
    public class TrackRecipeHashTests
    {
        private static readonly TrackRecipe Reference = new TrackRecipe(60f, 45f, YawProfile.Single, 8f, PitchProfile.Bump, 12f);

        [Test]
        public void SameParameters_GiveTheSameHash()
        {
            var same = new TrackRecipe(60f, 45f, YawProfile.Single, 8f, PitchProfile.Bump, 12f);

            Assert.AreEqual(Reference.StableHash(), same.StableHash());
        }

        [Test]
        public void HashIsAStableConstant_NotProcessRandomised()
        {
            // System.HashCode is seeded per process; this value must not move between runs or between machines.
            Assert.AreEqual(Reference.StableHash(), Reference.StableHash());
            Assert.AreEqual(TrackRecipe.Straight(20f).StableHash(), TrackRecipe.Straight(20f).StableHash());
            Assert.AreEqual(-1534434353, new TrackRecipe(1f, 2f, YawProfile.S, 3f, PitchProfile.Wave, 4f).StableHash());
        }

        [Test]
        public void NegativeZero_IsTheSameAngleAsZero()
        {
            var positive = new TrackRecipe(40f, -35f, YawProfile.Single, 0f, PitchProfile.Bump, 0f);
            var negative = new TrackRecipe(40f, -35f, YawProfile.Single, -0f, PitchProfile.Bump, -0f);

            Assert.AreEqual(positive.StableHash(), negative.StableHash());
        }

        [TestCaseSource(nameof(ChangedRecipes))]
        public void AnyChangedParameter_ChangesTheHash(TrackRecipe changed)
        {
            Assert.AreNotEqual(Reference.StableHash(), changed.StableHash());
        }

        private static TrackRecipe[] ChangedRecipes()
        {
            return new[]
            {
                new TrackRecipe(61f, 45f, YawProfile.Single, 8f, PitchProfile.Bump, 12f),
                new TrackRecipe(60f, 46f, YawProfile.Single, 8f, PitchProfile.Bump, 12f),
                new TrackRecipe(60f, 45f, YawProfile.S, 8f, PitchProfile.Bump, 12f),
                new TrackRecipe(60f, 45f, YawProfile.Single, 9f, PitchProfile.Bump, 12f),
                new TrackRecipe(60f, 45f, YawProfile.Single, 8f, PitchProfile.Wave, 12f),
                new TrackRecipe(60f, 45f, YawProfile.Single, 8f, PitchProfile.Bump, 13f),
            };
        }
    }
}
