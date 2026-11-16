using AF.Audio;
using AF.Core;
using AF.Input;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.Settings;
using MessagePipe;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using VContainer;
using WrongLine.Road;
#if STORE_STEAM
using AF.Platform.Steam;
#elif STORE_EPIC
using AF.Platform.Epic;
#endif

namespace WrongLine
{
    /// <summary>
    /// The game's project-wide composition root. It sits on the root scope prefab that VContainerSettings points to.
    /// Register game-wide services and the message types they own here.
    /// </summary>
    public sealed class GameRootScope : RootScope
    {
        [SerializeField, FormerlySerializedAs("inputActions"), Required, Tooltip("The game's actions asset. The UI input module of the EventSystem below uses the same one.")]
        private InputActionAsset _inputActions;

        [SerializeField, Required, Tooltip("The game's mixer. Its groups and volume parameters are listed in AudioChannels.")]
        private AudioMixer _audioMixer;

        [SerializeField, Required, Tooltip("The road pieces: the start piece and the ones the generator picks from. Rebuilt by WrongLine/Road/Rebuild Pieces.")]
        private RoadCatalog _roadCatalog;

        [SerializeField, Required, Tooltip("The road's tunable numbers: seed, window, floating origin, generator rules.")]
        private RoadConfig _roadConfig;

        protected override void ConfigureGame(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            // First: a store's launch check is the first boot step, so a refused launch quits before anything loads. The Mock
            // in the Editor and in builds without a store; a store build (AF/Build/Steam, AF/Build/Epic) passes its store's
            // backend under its STORE_ define and refuses any other. The backends compile only with their store's SDK
            // package installed (Platform.md, "Store backends").
            var platform = new PlatformOptions();
#if STORE_STEAM
            builder.UsePlatform(messagePipe, platform, new SteamPlatform(new SteamOptions { AppId = 0 })); // The game's Steam app id.
#elif STORE_EPIC
            // Its identity is in the EOS plugin's configuration (Tools > EOS Plugin > EOS Configuration).
            builder.UsePlatform(messagePipe, platform, new EpicPlatform());
#else
            builder.UsePlatform(messagePipe, platform);
#endif
            // Its boot step loads the global and device data; the settings are applied as they load.
            builder.UseSave();
            builder.UseInput(_inputActions, messagePipe, InputModes.CreateOptions());
            builder.UseAudio(_audioMixer, messagePipe, AudioChannels.CreateOptions());
            // After UseSave: its boot step applies the saved language and waits for its tables.
            builder.UseLocalization(messagePipe);
            // After UseSave, UseAudio and UseInput: display settings, and saving the channel volumes.
            builder.UseSettings(messagePipe);

            // The road pieces live in the root's pools, so they are made once and outlast every scene.
            builder.RegisterInstance(_roadCatalog);
            builder.RegisterInstance(_roadConfig);
            builder.Register<RoadPiecePool>(Lifetime.Singleton).As<IRoadPiecePool>();
        }
    }
}
