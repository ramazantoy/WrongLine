using NUnit.Framework;
using WrongLine.UI;

namespace WrongLine.Tests.UI
{
    public sealed class LanguageLabelTests
    {
        [Test]
        public void TheRegion_IsLeftOut()
        {
            Assert.AreEqual("Türkçe", LanguageLabel.Format("Türkçe (Türkiye)"));
            Assert.AreEqual("English", LanguageLabel.Format("English (United States)"));
        }

        [Test]
        public void ANameWithoutARegion_IsKept()
        {
            Assert.AreEqual("Deutsch", LanguageLabel.Format("Deutsch"));
        }

        [Test]
        public void NothingToShow_GivesAnEmptyString()
        {
            Assert.AreEqual(string.Empty, LanguageLabel.Format(null));
            Assert.AreEqual(string.Empty, LanguageLabel.Format("  "));
        }
    }
}
