using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AF.Core;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AF.Build.Editor
{
    /// <summary>
    /// The files each store's SDK puts into a build. An SDK package adds its native files to every build of the platform, so
    /// after the player build the other stores' files are removed, the remaining names are checked, and a store build must
    /// show its own SDK. An Epic build on Windows also gets the Visual C++ runtime next to the executable. A finding fails a
    /// Release and is a note in a Development build.
    /// </summary>
    internal static class StoreFiles
    {
        /// <summary>Where a game keeps the Visual C++ runtime files its Epic build copies next to the executable.</summary>
        public const string EpicRuntimeFolder = "BuildRedist/Epic";

        /// <summary>The EOS plugin's configuration folder under StreamingAssets. It carries the client secret.</summary>
        public const string EosConfigFolder = "StreamingAssets/EOS";

        // The EOS SDK links them dynamically and the Epic Games Store installs no runtime: without them EOSSDK does not load
        // and the store services fail silently. Only for Epic: next to a Steam game they would shadow the runtime of injected
        // overlays.
        public static readonly IReadOnlyList<string> EpicRuntimeFiles = new[] { "msvcp140.dll", "vcruntime140.dll", "vcruntime140_1.dll" };

        // The EOS plugin's post-build step writes EOSBootstrapper.exe (and an .ini) into every Windows build while the overlay
        // is enabled. The Epic Games Store installs the overlay itself and starts the game's own executable: dead files.
        private const string EOS_BOOTSTRAPPER_FILES = "EOSBootstrapper*";
        private const string EOS_BOOTSTRAPPER_PART = "eosbootstrapper";
        private const string EOS_PACKAGE = "com.playeveryware.eos";

        // The plugin writes the bootstrapper after the player build has returned (an async step that needs the editor loop);
        // its tool finishes in seconds, the limit only guards against a hang.
        private const double BootstrapperWaitSeconds = 120;

        private sealed class Sdk
        {
            /// <summary>File name patterns, anywhere in the build.</summary>
            public string[] Files = Array.Empty<string>();

            /// <summary>Folder paths, matched against the end of a folder's path in the build.</summary>
            public string[] Folders = Array.Empty<string>();

            /// <summary>Name fragments that must not remain in another store's build.</summary>
            public string[] ForbiddenParts = Array.Empty<string>();

            /// <summary>
            /// Files another store's build may keep although their name has a forbidden part: the SDK's managed assembly, which
            /// its disable define compiles empty and which a Mono player still loads.
            /// </summary>
            public string[] Allowed = Array.Empty<string>();

            /// <summary>Files this store's build must contain, per target; other targets are not checked.</summary>
            public Dictionary<BuildTarget, string[]> RequiredFiles = new Dictionary<BuildTarget, string[]>();

            /// <summary>Folders this store's build must contain with at least one file, on every target.</summary>
            public string[] RequiredFolders = Array.Empty<string>();
        }

        // Windows names come from shipped builds; the macOS and Linux names are the SDKs' documented ones, not yet built.
        private static readonly Dictionary<Store, Sdk> _sdks = new Dictionary<Store, Sdk>
        {
            [Store.Steam] = new Sdk
            {
                // Steamworks.NET.txt: the package's post-build hook writes it into every build; editor code does not see the
                // player's DISABLESTEAMWORKS.
                Files = new[] { "steam_api.dll", "steam_api64.dll", "libsteam_api.so", "Steamworks.NET.txt" },
                Folders = new[] { "steam_api.bundle" },
                ForbiddenParts = new[] { "steam_api", "steamworks" },
                Allowed = new[] { "com.rlabrecque.steamworks.net.dll" },
                RequiredFiles = new Dictionary<BuildTarget, string[]>
                {
                    [BuildTarget.StandaloneWindows64] = new[] { "steam_api64.dll" },
                    [BuildTarget.StandaloneWindows] = new[] { "steam_api.dll" },
                },
            },
            [Store.Epic] = new Sdk
            {
                Files = new[] { "EOSSDK-*", "libEOSSDK-*", "DynamicLibraryLoaderHelper-*", "GfxPluginNativeRender-*", "xaudio2_9redist.dll" },
                Folders = new[] { EosConfigFolder },
                ForbiddenParts = new[]
                {
                    "eossdk", "epic.onlineservices", "playeveryware", "gfxpluginnativerender", "dynamiclibraryloaderhelper", "vcruntime", "msvcp",
                },
                RequiredFiles = new Dictionary<BuildTarget, string[]>
                {
                    [BuildTarget.StandaloneWindows64] = new[] { "EOSSDK-Win64-Shipping.dll" },
                },
                RequiredFolders = new[] { EosConfigFolder },
            },
        };

        /// <summary>After a successful player build of a folder (not a package): the steps in the class summary.</summary>
        public static void Apply(BuildOutcome outcome, string outputFolder, string projectRoot)
        {
            var removed = new List<string>();
            foreach (var entry in _sdks.Where(entry => entry.Key != outcome.Store))
                Remove(outputFolder, entry.Value, removed);
            foreach (var file in Directory.GetFiles(outputFolder, EOS_BOOTSTRAPPER_FILES))
            {
                File.Delete(file);
                removed.Add(Relative(outputFolder, file));
            }
            if (removed.Count > 0)
                outcome.AddNote($"Removed files another store's SDK added: {string.Join(", ", removed)}.");

            var findings = new List<string>();
            if (outcome.Store == Store.Epic && outcome.Target == BuildTarget.StandaloneWindows64)
                CopyEpicRuntime(outputFolder, projectRoot, outcome, findings);
            findings.AddRange(Leftovers(outputFolder, outcome.Store));
            findings.AddRange(MissingRequired(outputFolder, outcome));

            var checkedTarget = outcome.Store == Store.None || _sdks[outcome.Store].RequiredFiles.ContainsKey(outcome.Target);
            if (!checkedTarget)
                outcome.AddNote($"The {outcome.Store} SDK's files are not checked on {outcome.Target}; only Windows is verified.");
            if (findings.Count == 0)
            {
                if (outcome.Store != Store.None && checkedTarget)
                    outcome.AddNote($"Store files: the {outcome.Store} SDK is in the build, no other store's files are.");
                return;
            }
            if (outcome.Kind == BuildKind.Release)
                outcome.Status = BuildStatus.Failed;
            foreach (var finding in findings)
                outcome.AddNote(outcome.Kind == BuildKind.Release ? finding : finding + " A Release build fails on this.");
        }

        /// <summary>
        /// When the EOS package is installed, removes the bootstrapper its post-build step writes after the build has
        /// returned, and appends a line to the report. When it cannot (still locked when the wait ends, or the delete fails),
        /// the report says so and a Release report turns Failed. Nothing to do for other targets or without the package. The
        /// wait does not survive a domain reload, and a bootstrapper renamed in the plugin's tools configuration is not found.
        /// </summary>
        public static void RemoveLateBootstrapper(BuildTarget target, BuildKind kind, string outputFolder, string reportFile)
        {
            if (target != BuildTarget.StandaloneWindows64 && target != BuildTarget.StandaloneWindows)
                return;
            if (PackageInfo.FindForPackageName(EOS_PACKAGE) == null)
                return;

            var deadline = EditorApplication.timeSinceStartup + BootstrapperWaitSeconds;

            void Poll()
            {
                var files = Directory.Exists(outputFolder) ? Directory.GetFiles(outputFolder, EOS_BOOTSTRAPPER_FILES) : Array.Empty<string>();
                var ready = files.Length > 0 && files.All(IsUnlocked);
                if (!ready && EditorApplication.timeSinceStartup < deadline)
                    return;

                EditorApplication.update -= Poll;
                if (files.Length == 0)
                    return; // The overlay is off, or the tool did not run: nothing was written.
                var names = string.Join(", ", files.Select(file => Relative(outputFolder, file)));
                if (!ready)
                {
                    ReportLate(reportFile, kind, $"After the build: {names}, which the EOS plugin wrote once the build had returned, was still " +
                                                 $"locked after {BootstrapperWaitSeconds:0} s and is still in the build. Delete it before shipping.");
                    return;
                }
                try
                {
                    foreach (var file in files)
                        File.Delete(file);
                    ReportLate(reportFile, null, $"After the build: removed {names}, which the EOS plugin wrote once the build had returned.");
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    ReportLate(reportFile, kind, $"After the build: {names} could not be removed ({exception.Message}). Delete it before shipping.");
                }
            }

            EditorApplication.update += Poll;
        }

        /// <summary>
        /// Appends a note to a report already written. With <paramref name="failedKind"/> Release, the report's first line turns
        /// Failed: the build folder holds something that must not ship.
        /// </summary>
        internal static void ReportLate(string reportFile, BuildKind? failedKind, string note)
        {
            if (failedKind == null)
                Log.Info(note);
            else
                Log.Error(note);
            if (!File.Exists(reportFile))
                return;
            var lines = File.ReadAllLines(reportFile).ToList();
            if (failedKind == BuildKind.Release && lines.Count > 0)
                lines[0] = lines[0].Replace(": " + BuildStatus.Succeeded, ": " + BuildStatus.Failed);
            lines.Add("- " + note);
            File.WriteAllLines(reportFile, lines);
        }

        /// <summary>Visual C++ runtime files missing from <see cref="EpicRuntimeFolder"/> under the project root.</summary>
        public static IReadOnlyList<string> MissingEpicRuntimeFiles(string projectRoot) =>
            EpicRuntimeFiles.Where(file => !File.Exists(Path.Combine(projectRoot, EpicRuntimeFolder, file))).ToList();

        /// <summary>True when the project's EOS configuration folder holds at least one file.</summary>
        public static bool HasEosConfig(string assetsFolder)
        {
            var folder = Path.Combine(assetsFolder, EosConfigFolder);
            return Directory.Exists(folder) && Directory.EnumerateFiles(folder).Any(file => !file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));
        }

        private static void Remove(string outputFolder, Sdk sdk, List<string> removed)
        {
            foreach (var folder in Directory.GetDirectories(outputFolder, "*", SearchOption.AllDirectories)
                         .Where(folder => sdk.Folders.Any(suffix => EndsWithPath(folder, suffix)))
                         .ToList())
            {
                if (!Directory.Exists(folder))
                    continue;
                Directory.Delete(folder, recursive: true);
                removed.Add(Relative(outputFolder, folder) + "/");
            }
            foreach (var file in sdk.Files.SelectMany(pattern => Directory.GetFiles(outputFolder, pattern, SearchOption.AllDirectories)).Distinct().ToList())
            {
                File.Delete(file);
                removed.Add(Relative(outputFolder, file));
            }
        }

        private static void CopyEpicRuntime(string outputFolder, string projectRoot, BuildOutcome outcome, List<string> findings)
        {
            var missing = MissingEpicRuntimeFiles(projectRoot);
            if (missing.Count > 0)
            {
                findings.Add($"{EpicRuntimeFolder}/ lacks {string.Join(", ", missing)}: without the Visual C++ runtime next to the executable " +
                             "EOSSDK does not load on a PC that has no runtime installed. See Build.md, \"Store files\".");
                return;
            }
            foreach (var file in EpicRuntimeFiles)
                File.Copy(Path.Combine(projectRoot, EpicRuntimeFolder, file), Path.Combine(outputFolder, file), overwrite: true);
            outcome.AddNote($"Copied the Visual C++ runtime from {EpicRuntimeFolder}/ next to the executable.");
        }

        private static IEnumerable<string> Leftovers(string outputFolder, Store store)
        {
            var others = _sdks.Where(entry => entry.Key != store).Select(entry => entry.Value).ToList();
            var parts = others.SelectMany(sdk => sdk.ForbiddenParts).Append(EOS_BOOTSTRAPPER_PART).ToList();
            var allowed = others.SelectMany(sdk => sdk.Allowed).ToList();
            return Directory.GetFileSystemEntries(outputFolder, "*", SearchOption.AllDirectories)
                .Where(entry => !allowed.Contains(Path.GetFileName(entry), StringComparer.OrdinalIgnoreCase))
                .Where(entry => parts.Any(part => Path.GetFileName(entry).IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(entry => $"{Relative(outputFolder, entry)} looks like another store's SDK file and is still in the build. If it " +
                                 "is, add it to that store's files in StoreFiles; if the game needs it, adjust the rules there.");
        }

        private static IEnumerable<string> MissingRequired(string outputFolder, BuildOutcome outcome)
        {
            if (outcome.Store == Store.None)
                yield break;
            var sdk = _sdks[outcome.Store];
            if (sdk.RequiredFiles.TryGetValue(outcome.Target, out var files))
            {
                foreach (var file in files.Where(file => Directory.GetFiles(outputFolder, file, SearchOption.AllDirectories).Length == 0))
                    yield return $"{file} is missing: this {outcome.Store} build does not carry the {outcome.Store} SDK.";
            }
            foreach (var folder in sdk.RequiredFolders)
            {
                var found = Directory.GetDirectories(outputFolder, "*", SearchOption.AllDirectories)
                    .Any(candidate => EndsWithPath(candidate, folder) && Directory.EnumerateFiles(candidate).Any());
                if (!found)
                    yield return $"{folder} is missing or empty: the {outcome.Store} SDK cannot start without its configuration.";
            }
        }

        private static bool EndsWithPath(string path, string suffix)
        {
            var normalized = path.Replace('\\', '/');
            return normalized.EndsWith("/" + suffix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUnlocked(string path)
        {
            try
            {
                using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
    }
}
