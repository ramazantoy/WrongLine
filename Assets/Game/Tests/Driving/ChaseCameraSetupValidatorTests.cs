using System.Linq;
using AF.Core.Editor;
using NUnit.Framework;
using WrongLine.Driving.Editor;

namespace WrongLine.Driving.Tests
{
    public class ChaseCameraSetupValidatorTests
    {
        [Test]
        public void AFieldOfViewTooNarrowForTheLookLimitIsAnError()
        {
            var settings = new ChaseCameraSettings(); // the car may be 24° off the look: half the field of view must be well over that
            var report = new ValidationReport();

            ChaseCameraSetupValidator.CheckFov(settings, 55f, "crash", null, report);

            Assert.AreEqual(1, report.ErrorCount);
            StringAssert.Contains("crash", report.Issues.Single().Message);
        }

        [Test]
        public void TheDefaultFieldsOfViewAreWideEnough()
        {
            var settings = new ChaseCameraSettings();
            var report = new ValidationReport();

            ChaseCameraSetupValidator.CheckFov(settings, settings.BaseFov, "base", null, report);
            ChaseCameraSetupValidator.CheckFov(settings, settings.MaxFov, "max", null, report);
            ChaseCameraSetupValidator.CheckFov(settings, new WrongLine.Crash.CrashSettings().Camera.CrashFov, "crash", null, report);

            Assert.AreEqual(0, report.ErrorCount);
        }
    }
}
