using System.Globalization;
using NUnit.Framework;

namespace WrongLine.Driving.Tests
{
    public class RoadDebugInfoTests
    {
        private static readonly RoadDebugInfo Info = new RoadDebugInfo(1234.56, 17, "CurveSoft_R", 34.25, 60f, 987654321UL, 30f, 9, 3, -1.25f, 0, "right");

        [Test]
        public void ShowsDistancePieceSeedSpeedAndCounts()
        {
            string text = Info.Format(59.6f);

            StringAssert.Contains("s 1234.6 m", text);
            StringAssert.Contains("piece #17 CurveSoft_R (34.3/60 m)", text);
            StringAssert.Contains("seed 987654321", text);
            StringAssert.Contains("speed 30.0 m/s", text);
            StringAssert.Contains("active 9", text);
            StringAssert.Contains("origin shifts 3", text);
            StringAssert.Contains("x -1.25 m   lane 0   signal right", text);
            StringAssert.Contains("60 fps", text);
        }

        [Test]
        public void IsTheSameOnADeviceWhereTheDecimalSeparatorIsAComma()
        {
            CultureInfo before = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                StringAssert.Contains("s 1234.6 m", Info.Format(60f));
            }
            finally
            {
                CultureInfo.CurrentCulture = before;
            }
        }
    }
}
