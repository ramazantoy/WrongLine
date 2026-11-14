using System;
using System.IO;
using System.Linq;
using AF.Build.Editor;
using NUnit.Framework;
using UnityEditor;

namespace AF.Build.Tests
{
    public class StoreFilesTests
    {
        private const string STEAM_DLL = "Game_Data/Plugins/x86_64/steam_api64.dll";
        private const string STEAM_NOTE = "Game_Data/Plugins/Steamworks.NET.txt";
        private const string EOS_DLL = "Game_Data/Plugins/x86_64/EOSSDK-Win64-Shipping.dll";
        private const string EOS_RENDER = "Game_Data/Plugins/x86_64/GfxPluginNativeRender-x64.dll";
        private const string EOS_CONFIG = "Game_Data/StreamingAssets/EOS/eos_product_config.json";

        private string _root;
        private string _output;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "StoreFilesTests-" + Guid.NewGuid().ToString("N"));
            _output = Path.Combine(_root, "Builds", "Windows", "Release");
            Directory.CreateDirectory(_output);
            Touch("Game.exe");
            Touch("Game_Data/globalgamemanagers");
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_root, recursive: true);

        [Test]
        public void SteamBuild_RemovesEpicFiles_AndKeepsSteam()
        {
            Touch(STEAM_DLL, STEAM_NOTE, EOS_DLL, EOS_RENDER, EOS_CONFIG, "EOSBootstrapper.exe", "EOSBootstrapper.ini");

            var outcome = Apply(BuildKind.Release, Store.Steam);

            Assert.AreEqual(BuildStatus.Succeeded, outcome.Status, string.Join("\n", outcome.Notes));
            AssertExists(STEAM_DLL, STEAM_NOTE);
            AssertGone(EOS_DLL, EOS_RENDER, "Game_Data/StreamingAssets/EOS", "EOSBootstrapper.exe", "EOSBootstrapper.ini");
        }

        [Test]
        public void EpicBuild_RemovesSteamFiles_AndCopiesTheRuntime()
        {
            Touch(STEAM_DLL, STEAM_NOTE, EOS_DLL, EOS_CONFIG);
            AddRuntimeFiles();

            var outcome = Apply(BuildKind.Release, Store.Epic);

            Assert.AreEqual(BuildStatus.Succeeded, outcome.Status, string.Join("\n", outcome.Notes));
            AssertGone(STEAM_DLL, STEAM_NOTE);
            AssertExists(EOS_DLL, EOS_CONFIG, "msvcp140.dll", "vcruntime140.dll", "vcruntime140_1.dll");
        }

        [Test]
        public void StorelessBuild_RemovesEveryStoresFiles()
        {
            Touch(STEAM_DLL, STEAM_NOTE, EOS_DLL, EOS_CONFIG);

            var outcome = Apply(BuildKind.Release, Store.None);

            Assert.AreEqual(BuildStatus.Succeeded, outcome.Status, string.Join("\n", outcome.Notes));
            AssertGone(STEAM_DLL, STEAM_NOTE, EOS_DLL, "Game_Data/StreamingAssets/EOS");
        }

        [Test]
        public void MissingSdk_FailsRelease_AndIsANoteInDevelopment()
        {
            var release = Apply(BuildKind.Release, Store.Steam);
            var development = Apply(BuildKind.Development, Store.Steam);

            Assert.AreEqual(BuildStatus.Failed, release.Status);
            Assert.IsTrue(release.Notes.Any(note => note.Contains("steam_api64.dll is missing")), string.Join("\n", release.Notes));
            Assert.AreEqual(BuildStatus.Succeeded, development.Status);
            Assert.IsTrue(development.Notes.Any(note => note.Contains("A Release build fails")), string.Join("\n", development.Notes));
        }

        [Test]
        public void EpicBuild_WithoutTheRuntime_FailsRelease()
        {
            Touch(EOS_DLL, EOS_CONFIG);

            var outcome = Apply(BuildKind.Release, Store.Epic);

            Assert.AreEqual(BuildStatus.Failed, outcome.Status);
            Assert.IsTrue(outcome.Notes.Any(note => note.Contains("vcruntime140_1.dll")), string.Join("\n", outcome.Notes));
        }

        [Test]
        public void EpicBuild_WithoutItsConfiguration_FailsRelease()
        {
            Touch(EOS_DLL);
            Directory.CreateDirectory(Path.Combine(_output, "Game_Data/StreamingAssets/EOS"));
            AddRuntimeFiles();

            var outcome = Apply(BuildKind.Release, Store.Epic);

            Assert.AreEqual(BuildStatus.Failed, outcome.Status);
            Assert.IsTrue(outcome.Notes.Any(note => note.Contains("StreamingAssets/EOS is missing or empty")), string.Join("\n", outcome.Notes));
        }

        [Test]
        public void UnknownFileOfAnotherStore_FailsRelease()
        {
            Touch(STEAM_DLL, "Game_Data/Managed/PlayEveryWare.EpicOnlineServices.dll");

            var outcome = Apply(BuildKind.Release, Store.Steam);

            Assert.AreEqual(BuildStatus.Failed, outcome.Status);
            Assert.IsTrue(outcome.Notes.Any(note => note.Contains("PlayEveryWare.EpicOnlineServices.dll looks like another store")),
                string.Join("\n", outcome.Notes));
        }

        [Test]
        public void EmptySteamworksAssembly_InAMonoBuild_IsAllowed()
        {
            Touch(EOS_DLL, EOS_CONFIG, "Game_Data/Managed/com.rlabrecque.steamworks.net.dll");
            AddRuntimeFiles();

            var outcome = Apply(BuildKind.Release, Store.Epic);

            Assert.AreEqual(BuildStatus.Succeeded, outcome.Status, string.Join("\n", outcome.Notes));
        }

        [Test]
        public void LateNote_FailsAReleaseReport()
        {
            var report = Path.Combine(_root, "report.txt");
            File.WriteAllLines(report, new[] { "Steam Release build for StandaloneWindows64: Succeeded in 5:00", "Notes:" });
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "EOSBootstrapper.exe is still in the build.");

            StoreFiles.ReportLate(report, BuildKind.Release, "EOSBootstrapper.exe is still in the build.");

            var lines = File.ReadAllLines(report);
            StringAssert.Contains(": Failed in 5:00", lines[0]);
            Assert.AreEqual("- EOSBootstrapper.exe is still in the build.", lines[lines.Length - 1]);
        }

        [Test]
        public void TargetWithoutRules_IsNoted()
        {
            var outcome = Apply(BuildKind.Release, Store.Steam, BuildTarget.StandaloneOSX);

            Assert.AreEqual(BuildStatus.Succeeded, outcome.Status);
            Assert.IsTrue(outcome.Notes.Any(note => note.Contains("not checked on StandaloneOSX")), string.Join("\n", outcome.Notes));
        }

        [Test]
        public void MissingRuntimeFiles_AreListed()
        {
            Assert.AreEqual(3, StoreFiles.MissingEpicRuntimeFiles(_root).Count);

            AddRuntimeFiles();

            Assert.AreEqual(0, StoreFiles.MissingEpicRuntimeFiles(_root).Count);
        }

        private BuildOutcome Apply(BuildKind kind, Store store, BuildTarget target = BuildTarget.StandaloneWindows64)
        {
            var outcome = new BuildOutcome(new BuildRequest(kind, store), target) { Status = BuildStatus.Succeeded };
            StoreFiles.Apply(outcome, _output, _root);
            return outcome;
        }

        private void AddRuntimeFiles()
        {
            foreach (var file in StoreFiles.EpicRuntimeFiles)
            {
                var path = Path.Combine(_root, StoreFiles.EpicRuntimeFolder, file);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, file);
            }
        }

        private void Touch(params string[] files)
        {
            foreach (var file in files)
            {
                var path = Path.Combine(_output, file);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, file);
            }
        }

        private void AssertExists(params string[] paths)
        {
            foreach (var path in paths)
                Assert.IsTrue(File.Exists(Path.Combine(_output, path)), path + " should be in the build");
        }

        private void AssertGone(params string[] paths)
        {
            foreach (var path in paths)
            {
                var full = Path.Combine(_output, path);
                Assert.IsFalse(File.Exists(full) || Directory.Exists(full), path + " should be removed");
            }
        }
    }
}
