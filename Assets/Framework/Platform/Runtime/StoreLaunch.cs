using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
#endif

namespace AF.Platform
{
    /// <summary>
    /// Ends a launch a store refuses: not started through the store's client, not owned, the SDK's files missing. A store
    /// backend returns it from its boot step. The message goes to a native message box (Windows), because localization and
    /// the game's UI are not running yet. <c>Application.Quit</c> takes effect at the end of the frame and the root scope is
    /// already built, so every contract stays registered; the returned task never completes, so the boot never finishes
    /// and nothing that waits for it starts.
    /// </summary>
    public static class StoreLaunch
    {
        /// <param name="message">Shown to the player, in English; null quits silently (the store starts the game again itself).</param>
        /// <param name="cancellation">The boot step's token: the task ends with it when the root scope goes away.</param>
        public static UniTask Block(string message, CancellationToken cancellation)
        {
#if UNITY_EDITOR
            // The Editor cannot quit Play Mode from here, and a boot that never finishes helps nobody: log and go on without
            // the store. Its services stay inactive.
            Log.Error($"{message ?? "The store restarts the game."} In the Editor the game goes on without the store.");
            return UniTask.CompletedTask;
#else
            if (message != null)
            {
                Log.Error(message);
                ShowMessageBox(message);
            }
            Application.Quit();
            return UniTask.Never(cancellation);
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // MB_ICONERROR | MB_SETFOREGROUND | MB_TOPMOST: the game window may not be up yet.
        private const uint MessageBoxFlags = 0x10 | 0x10000 | 0x40000;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr window, string text, string caption, uint type);

        private static void ShowMessageBox(string message) => MessageBoxW(IntPtr.Zero, message, Application.productName, MessageBoxFlags);
#elif !UNITY_EDITOR
        // macOS and Linux players only log it for now.
        private static void ShowMessageBox(string message)
        {
        }
#endif
    }
}
