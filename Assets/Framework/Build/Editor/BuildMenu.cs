using AF.Core;
using UnityEditor;

namespace AF.Build.Editor
{
    /// <summary>AF/Build: the build menu. Results go to the console and to the report next to the build.</summary>
    internal static class BuildMenu
    {
        private const string MENU = FrameworkConstants.MenuRoot + "Build/";
        private const string STEAM_MENU = MENU + "Steam/";
        private const string EPIC_MENU = MENU + "Epic/";

        [MenuItem(MENU + "Development")]
        private static void Development() => Builder.Build(BuildKind.Development);

        [MenuItem(MENU + "Release")]
        private static void Release() => Builder.Build(BuildKind.Release);

        [MenuItem(MENU + "Check Release")]
        private static void CheckRelease() => Check(Store.None);

        [MenuItem(STEAM_MENU + "Development")]
        private static void SteamDevelopment() => Builder.Build(new BuildRequest(BuildKind.Development, Store.Steam));

        [MenuItem(STEAM_MENU + "Release")]
        private static void SteamRelease() => Builder.Build(new BuildRequest(BuildKind.Release, Store.Steam));

        [MenuItem(STEAM_MENU + "Check Release")]
        private static void SteamCheckRelease() => Check(Store.Steam);

        [MenuItem(EPIC_MENU + "Development")]
        private static void EpicDevelopment() => Builder.Build(new BuildRequest(BuildKind.Development, Store.Epic));

        [MenuItem(EPIC_MENU + "Release")]
        private static void EpicRelease() => Builder.Build(new BuildRequest(BuildKind.Release, Store.Epic));

        [MenuItem(EPIC_MENU + "Check Release")]
        private static void EpicCheckRelease() => Check(Store.Epic);

        private static void Check(Store store)
        {
            var title = store == Store.None ? "Build check (Release)" : $"Build check ({store} Release)";
            Builder.Check(new BuildRequest(BuildKind.Release, store)).LogToConsole(title);
        }
    }
}
