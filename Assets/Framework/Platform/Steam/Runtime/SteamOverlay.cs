using System;
using AF.Core;
using MessagePipe;
using Steamworks;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace AF.Platform.Steam
{
    /// <summary>
    /// The Steam overlay, from Steam's <c>GameOverlayActivated_t</c>. On a Steam Deck the "closed" callback can get lost, which
    /// would keep the game paused: there, the first gamepad button or left click that reaches the game while the overlay is
    /// "open" closes it, since input only reaches the game once the overlay is gone. A late real "closed" then changes nothing.
    /// </summary>
    internal sealed class SteamOverlay : IPlatformOverlay, IDisposable
    {
        private readonly IPublisher<PlatformOverlayChanged> _changed;
        private Callback<GameOverlayActivated_t> _callback;
        private IDisposable _inputWatch;
        private bool _closedByInput;

        public SteamOverlay(IPublisher<PlatformOverlayChanged> changed) => _changed = changed;

        public bool IsOpen { get; private set; }

        /// <summary>Treat game input that arrives while the overlay is open as the overlay closing (Steam Deck).</summary>
        internal bool TreatGameInputAsClosed { get; set; }

        /// <summary>Called by the session once the Steam API runs.</summary>
        internal void Start(bool treatGameInputAsClosed)
        {
            TreatGameInputAsClosed = treatGameInputAsClosed;
            _callback = Callback<GameOverlayActivated_t>.Create(data => OnActivated(data.m_bActive != 0));
        }

        /// <summary>Called by the session before the Steam API shuts down.</summary>
        internal void Stop()
        {
            _callback?.Dispose();
            _callback = null;
            Set(false);
        }

        /// <summary>Called by the session every frame, outside the input system's update.</summary>
        internal void Tick()
        {
            if (!_closedByInput)
                return;
            _closedByInput = false;
            if (IsOpen)
            {
                Log.Info("Steam: game input arrived while the overlay was open; treating it as closed.");
                Set(false);
            }
        }

        internal void OnActivated(bool active) => Set(active);

        public void Dispose()
        {
            // Managed only: a callback's registration lives in Steamworks.NET's dispatcher, not in Steam.
            _callback?.Dispose();
            _callback = null;
            StopInputWatch();
        }

        private void Set(bool open)
        {
            if (open == IsOpen)
                return;
            IsOpen = open;
            if (open && TreatGameInputAsClosed)
                StartInputWatch();
            else
                StopInputWatch();
            _changed.Publish(new PlatformOverlayChanged(open));
        }

        private void StartInputWatch()
        {
            StopInputWatch();
            // Applied on the next Tick: closing pauses down and switches input modes, which must not happen inside the
            // input system's own event processing.
            _inputWatch = InputSystem.onAnyButtonPress.Call(control =>
            {
                if (control.device is Gamepad || (control.device is Mouse mouse && control == mouse.leftButton))
                    _closedByInput = true;
            });
        }

        private void StopInputWatch()
        {
            _inputWatch?.Dispose();
            _inputWatch = null;
            _closedByInput = false;
        }
    }
}
