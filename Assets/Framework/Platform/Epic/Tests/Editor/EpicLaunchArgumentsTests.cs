using NUnit.Framework;

namespace AF.Platform.Epic.Tests
{
    public class EpicLaunchArgumentsTests
    {
        [TestCase("-EpicPortal")]
        [TestCase("-epicportal")]
        [TestCase("-epicapp=Sugar")]
        [TestCase("-AUTH_PASSWORD=abc123")]
        [TestCase("-AUTH_PASSWORD=")]
        public void AnyLauncherArgument_IsEnough(string argument) =>
            Assert.IsTrue(EpicLaunchArguments.FromLauncher(new[] { "Game.exe", "-logFile", "log.txt", argument }));

        [Test]
        public void NoLauncherArgument_IsNotFromTheLauncher() =>
            Assert.IsFalse(EpicLaunchArguments.FromLauncher(new[] { "Game.exe", "-screen-fullscreen", "0", "-EpicPortalX" }));
    }
}
