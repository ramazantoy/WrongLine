using System;
using AF.Core;
using Epic.OnlineServices.UI;
using MessagePipe;
using PlayEveryWare.EpicOnlineServices;

namespace AF.Platform.Epic
{
    /// <summary>
    /// The Epic overlay, from EOS's display settings notification. Open means visible with exclusive input, so achievement
    /// toasts do not pause the game. The overlay shares the gamepad with the game, so Steam Deck's lost-callback rule does
    /// not apply here.
    /// </summary>
    internal sealed class EpicOverlay : IPlatformOverlay
    {
        private readonly IPublisher<PlatformOverlayChanged> _changed;

        public EpicOverlay(IPublisher<PlatformOverlayChanged> changed) => _changed = changed;

        public bool IsOpen { get; private set; }

        /// <summary>
        /// Called by the session once EOS runs. The notification is not removed on the way out: the plugin releases the
        /// platform when the app quits, and no EOS call runs after that.
        /// </summary>
        internal void Start()
        {
            try
            {
                // Throws instead of returning null when the native platform failed to load (no Visual C++ runtime).
                var ui = EOSManager.Instance.GetEOSUIInterface();
                if (ui == null)
                {
                    Log.Error("Epic: no UI interface; the overlay does not pause the game.");
                    return;
                }
                var options = new AddNotifyDisplaySettingsUpdatedOptions();
                ui.AddNotifyDisplaySettingsUpdated(ref options, null, OnDisplaySettingsUpdated);
            }
            catch (Exception exception)
            {
                Log.Error($"Epic: the overlay notification could not be registered ({exception.Message}).");
            }
        }

        internal void Set(bool open)
        {
            if (open == IsOpen)
                return;
            IsOpen = open;
            _changed.Publish(new PlatformOverlayChanged(open));
        }

        private void OnDisplaySettingsUpdated(ref OnDisplaySettingsUpdatedCallbackInfo data) => Set(data.IsVisible && data.IsExclusiveInput);
    }
}
