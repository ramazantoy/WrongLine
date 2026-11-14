using System;
using System.Collections.Generic;
using System.Linq;

namespace AF.Platform.Epic
{
    /// <summary>
    /// Whether the Epic Games launcher started the game. Any one of its arguments is enough: the offline launcher passes an
    /// empty <c>-AUTH_PASSWORD=</c>, and third-party launchers pass the same arguments, so requiring more would lock out
    /// players who own the game. There is no restart through the launcher: <c>EOS_Platform_CheckForLauncherAndRestart</c>
    /// needs the sandbox id, which differs between the Dev and Live sandboxes of the same build.
    /// </summary>
    internal static class EpicLaunchArguments
    {
        private const string EPIC_PORTAL = "-EpicPortal";
        private const string EPIC_APP = "-epicapp=";
        private const string AUTH_PASSWORD = "-AUTH_PASSWORD=";

        public static bool FromLauncher(IEnumerable<string> arguments) =>
            arguments.Any(argument =>
                string.Equals(argument, EPIC_PORTAL, StringComparison.OrdinalIgnoreCase) ||
                argument.StartsWith(EPIC_APP, StringComparison.OrdinalIgnoreCase) ||
                argument.StartsWith(AUTH_PASSWORD, StringComparison.OrdinalIgnoreCase));
    }
}
