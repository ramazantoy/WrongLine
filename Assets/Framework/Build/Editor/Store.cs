using System;
using System.Collections.Generic;
using System.Linq;
using AF.Core;
using UnityEditor;

namespace AF.Build.Editor
{
    /// <summary>
    /// Whose services a build talks to. A store build defines <c>STORE_&lt;NAME&gt;</c> and must carry that store's platform
    /// backend, the <c>AF.Platform.&lt;Name&gt;</c> assembly; at runtime <c>UsePlatform</c> refuses any other backend.
    /// </summary>
    public enum Store
    {
        /// <summary>No store: the Mock platform backend. The build menu's plain Development and Release.</summary>
        None,
        Steam,
        Epic,
    }

    /// <summary>
    /// What each store builds with. Adding a store: a <see cref="Store"/> value, its targets, SDK define and SDK package here,
    /// its files in <see cref="StoreFiles"/>, its menu items, and its branch in the Platform module's store list; then the
    /// backend itself (Platform.md, "Adding a store backend").
    /// </summary>
    public static class BuildStores
    {
        private static readonly Dictionary<Store, BuildTarget[]> _targets = new Dictionary<Store, BuildTarget[]>
        {
            [Store.Steam] = new[] { BuildTarget.StandaloneWindows64, BuildTarget.StandaloneWindows, BuildTarget.StandaloneOSX, BuildTarget.StandaloneLinux64 },
            [Store.Epic] = new[] { BuildTarget.StandaloneWindows64, BuildTarget.StandaloneOSX },
        };

        // The SDK packages' own switches: Steamworks.NET compiles to nothing with DISABLESTEAMWORKS, the EOS plugin's
        // assemblies require !EOS_DISABLE. Harmless while the package is not installed.
        private static readonly Dictionary<Store, string> _sdkDisableDefines = new Dictionary<Store, string>
        {
            [Store.Steam] = "DISABLESTEAMWORKS",
            [Store.Epic] = "EOS_DISABLE",
        };

        // The package each store's backend compiles against; the template installs none of them.
        private static readonly Dictionary<Store, string> _sdkPackages = new Dictionary<Store, string>
        {
            [Store.Steam] = "com.rlabrecque.steamworks.net",
            [Store.Epic] = "com.playeveryware.eos",
        };

        /// <summary>Every store; <see cref="Store.None"/> is not one.</summary>
        public static IEnumerable<Store> All => ((Store[])Enum.GetValues(typeof(Store))).Where(store => store != Store.None);

        /// <summary>The SDK package the store's backend needs; null for <see cref="Store.None"/> and a store not listed yet.</summary>
        public static string SdkPackage(Store store) => _sdkPackages.TryGetValue(store, out var package) ? package : null;

        /// <summary>The scripting define of a store build; null for <see cref="Store.None"/>.</summary>
        public static string Define(Store store) => store == Store.None ? null : "STORE_" + store.ToString().ToUpperInvariant();

        /// <summary>
        /// The defines that compile every other store's SDK out of a build for <paramref name="store"/>; a store-less build
        /// gets all of them. A game sold on two stores has both SDKs installed, and each build carries only its own.
        /// </summary>
        public static IEnumerable<string> SdkDisableDefines(Store store) =>
            _sdkDisableDefines.Where(entry => entry.Key != store).Select(entry => entry.Value);

        /// <summary>The assembly that holds the store's platform backend; null for <see cref="Store.None"/>.</summary>
        public static string BackendAssembly(Store store) => store == Store.None ? null : FrameworkConstants.Prefix + ".Platform." + store;

        /// <summary>The targets the store sells on; empty for <see cref="Store.None"/>, which builds on every supported target.</summary>
        public static IReadOnlyList<BuildTarget> Targets(Store store) =>
            store == Store.None ? Array.Empty<BuildTarget>() : _targets[store];

        public static bool Supports(Store store, BuildTarget target) => store == Store.None || Array.IndexOf(_targets[store], target) >= 0;
    }
}
