# Platform

Store and platform services behind small interfaces: achievements and the store overlay. A backend implements all of
them for one store; the Mock backend is used in the Editor and in builds without a store. The build menu's store
builds (Steam, Epic) pick the store; game code never knows which one it runs on. Built on VContainer and MessagePipe;
needs Core.

## Setup

```csharp
protected override void ConfigureGame(IContainerBuilder builder, MessagePipeOptions messagePipe)
{
    // First: a store backend's launch check is then the first boot step.
    builder.UsePlatform(messagePipe, new PlatformOptions { AchievementIds = Achievements.All });
    builder.UseSave();
    builder.UseInput(_inputActions, messagePipe, InputModes.CreateOptions());
}
```

| Option | Default | Meaning |
|---|---|---|
| `AchievementIds` | empty | Every achievement id the game unlocks, exactly as defined in the store's back office. |
| `PauseOnOverlay` | `true` | Pause the game while the store overlay is open. |

`UsePlatform(messagePipe, options, backend)` installs `backend`; without one it installs `MockPlatform`. A store build
passes its store's backend (see [Store backends](#store-backends)); the template installs no store SDK, so every build
it can make runs the Mock until a game turns a backend on.

## Contracts

| Game code uses | Backends register | Use |
|---|---|---|
| `IAchievements` | `IStoreAchievements` | `Unlock(id)`, `IsUnlocked(id)` |
| `IPlatformOverlay` | `IPlatformOverlay` | `IsOpen`, and the `PlatformOverlayChanged { IsOpen }` message when it changes |

`IAchievements` is a gate `UsePlatform` registers in front of the backend's `IStoreAchievements`; nothing else
registers it. `UsePlatform` refuses an `IAchievements` registered before it or by the backend; one registered after it
would replace the gate and let unknown ids through. Every backend registers every contract. A store without a feature registers a no-op implementation; `UsePlatform`
throws, naming the missing interface, when a backend leaves one out. A store that starts asynchronously registers an
`IBootStep` from its backend, so `IAppBoot.WaitReady` covers it.

### Achievements

- The game's save data is the truth; the store only mirrors it. After loading, unlock again everything the save says
  was earned: a store that was not ready yet may have missed it. `Unlock` is idempotent.
- An id missing from `AchievementIds` is logged as an error and ignored (`IsUnlocked` reads it as locked). Stores ignore
  unknown ids silently; the gate checks them for every backend, so a typo shows up in the Editor on the Mock already.
  A backend only ever receives listed ids.
- Stats (progress counters) are not part of the template: their semantics differ per store and they cannot be undone.

### Overlay

With `PauseOnOverlay`, the game holds a pause request (`platform-overlay`) while the overlay is open. Input follows the
pause through the Input module's paused mode. Closing the overlay releases only that request: a pause menu the game
opened meanwhile stays open. A game that opens its pause menu when the overlay opens listens to
`PlatformOverlayChanged`.

## Mock

| Mock | Behaviour |
|---|---|
| `MockAchievements` | The Mock's `IStoreAchievements`. Keeps unlocks in memory for the session and logs them. `Unlocked` lists them; `ResetAll()` locks everything again. Unlocking goes through `IAchievements`, so the id check applies. |
| `MockPlatformOverlay` | Never opens on its own. `SetOpen(bool)` opens and closes it, publishing `PlatformOverlayChanged` once per change. |

Both are also registered as their own type, so tests and Play Mode tools can resolve and drive them:

```csharp
container.Resolve<MockPlatformOverlay>().SetOpen(true); // the game pauses
```

## Store builds

The build menu makes store-less builds and store builds (**AF > Build > Steam**, **AF > Build > Epic**; see Build.md,
"Stores"). A store build defines `STORE_STEAM` or `STORE_EPIC` in the player only; the Editor does not have them, so
Play Mode runs the Mock.

| Build | Backend | If it is not |
|---|---|---|
| Editor, store-less build | the Mock (`UsePlatform` without a backend) | — |
| Store build | the store's own, whose `Name` is the store's name | refused before building when the backend assembly is not in the player; `UsePlatform` throws at startup when another backend (the Mock included) is passed |

So a store build cannot quietly fall back to the Mock and ship achievements that reach no store. To try a store
backend in Play Mode, add its define to Player Settings for the session and remove it afterwards. While it is there the
Editor behaves like that store's build: `UsePlatform` refuses the Mock, so the Platform module's tests that install
the Mock fail, and the build menu refuses to build.

## Store backends

The template carries a Steam and an Epic backend, each in its own assembly next to its store's SDK. The template
installs neither SDK package, so neither backend compiles and a store build is refused until a game installs the
package: a mobile game, or a PC game sold elsewhere, carries no store SDK. The root scope already passes each backend
under its store's define:

```csharp
var platform = new PlatformOptions { AchievementIds = Achievements.All };
#if STORE_STEAM
builder.UsePlatform(messagePipe, platform, new SteamPlatform(new SteamOptions { AppId = 0 })); // The game's Steam app id.
#elif STORE_EPIC
builder.UsePlatform(messagePipe, platform, new EpicPlatform());
#else
builder.UsePlatform(messagePipe, platform);
#endif
```

`UsePlatform` is the root scope's first call: the backend's launch check is then the first boot step, and a refused
launch quits before the save or the language loads.

| Store | SDK package | Backend assembly | Verified |
|---|---|---|---|
| Steam | Steamworks.NET 2025.162.1 (Steamworks SDK 1.62), or 2025.165.0 for a game not also on Epic | `AF.Platform.Steam` (`Assets/Framework/Platform/Steam/`) | Windows IL2CPP Development build on 2025.165.0 with Spacewar (app 480, started next to a `steam_appid.txt`): the API starts before the save loads, an achievement unlocks, Steam shuts down on quit; without `steam_api64.dll` the message box shows and the game quits. On 2025.162.1: compiles, tests pass. The overlay and the Steam Deck rule are covered by tests only: Steam injects its overlay only into a game it started. |
| Epic | PlayEveryWare's EOS plugin 6.2.0 (EOS SDK 1.19) | `AF.Platform.Epic` (`Assets/Framework/Platform/Epic/`) | Compiles, alone and next to Steamworks.NET 2025.162.1; tests pass (launch arguments, the unlock queue, unlocking before login). Not run against EOS: no Epic product was set up, and no Epic build was made. |

A backend assembly compiles only while its SDK package is installed (a version define on the package) and, outside the
Editor, only into its own store's builds (a define constraint on `STORE_<NAME> || UNITY_EDITOR`). Its tests sleep with
it and wake with the package.

### Steam

Turning it on in a game:

1. Install the package: in `Packages/manifest.json`, add
   `"com.rlabrecque.steamworks.net": "https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.162.1"`.
   A game also sold on Epic stays on 2025.162.1: from 2025.163.0 on, Steamworks.NET lacks two constants the EOS plugin's
   (6.2.0) editor tools use, and the project stops compiling. A Steam-only game may take 2025.165.0 or newer; the
   backend uses SDK 1.65's `IsRunningOnSteamHardware` from 2025.165.0 on (a version define) and
   `IsSteamRunningOnSteamDeck` before. `ProjectSettings/SteamworksNETSettings.json` keeps the package from adding
   `STEAMWORKS_NET` to Player Settings, where the build menu would refuse it; leave it in place.
2. Write the game's app id in the root scope (`AppId = 0` above). The launch check refuses 0, and 480 (Spacewar,
   Valve's test app) outside a development player, with a message box naming the problem.
3. In Steamworks, give each achievement the API name the game uses in `AchievementIds`.
4. Steam Cloud, if wanted: Auto-Cloud needs no code. Root `WinAppDataLocalLow` (the game's `persistentDataPath`),
   subdirectory `<Company>/<Product>/saves`, pattern `*.json`. Keep `local/` out: it holds this machine's settings.
5. Build with **AF > Build > Steam > Development**, then Release.

What it does:

- **Launch.** Outside the Editor, `SteamAPI.RestartAppIfNecessary` first: when the game was not started through Steam,
  Steam starts it again and this process quits silently. Then the API starts (`SteamAPI.InitEx`) and the license is
  checked (`SteamApps.BIsSubscribed`). Steam not running, Steam out of date, the game not on the account, or
  `steam_api64.dll` missing or broken each get their own message and quit (`StoreLaunch.Block`); the boot never
  finishes. The message is a native message box on Windows; macOS and Linux only log it for now. In the Editor a failed
  start is logged and the game goes on with Steam's services inactive.
- **Callbacks** are pumped every frame; on quit the API shuts down and no Steam call runs after it, nor from `Dispose`.
- **Achievements**: `GetAchievement`, then `SetAchievement` and `StoreStats` when still locked. The Steam client keeps
  them while offline. An id the app does not have is logged as an error. Ignored (with a warning) while Steam is not
  running; the game unlocks again after its next load.
- **Overlay**: `GameOverlayActivated_t`. On a Steam Deck the "closed" callback can get lost and leave the game paused:
  there the first gamepad button or left click that reaches the game while the overlay is "open" closes it. From
  Steamworks.NET 2025.165.0 on, a Steam Machine (the same SteamOS overlay) gets the same rule.

Running a local build without starting it from Steam: put a `steam_appid.txt` with the app id next to the executable
(the build menu never ships one), with Steam running and logged in. The overlay does not open in a game started that
way; try it from the Steam client. In the Editor, Steamworks.NET writes one with 480
into the project root (ignored by git); trying the backend in Play Mode also needs the `STORE_STEAM` define (see
[Store builds](#store-builds)).

### Epic

Turning it on in a game:

1. Install the package. Download `com.playeveryware.eos-6.2.0.tgz` from PlayEveryWare's releases
   (github.com/PlayEveryWare/eos_plugin_for_unity), put it in `Packages/` (git LFS tracks `*.tgz`) and add
   `"com.playeveryware.eos": "file:com.playeveryware.eos-6.2.0.tgz"` to `Packages/manifest.json`. Unity asks once to
   accept the unsigned package ("Missing Signature").
2. In **EOS Plugin > EOS Configuration**, enter the product, sandbox, deployment and client from the Epic Developer
   Portal. The plugin writes them to `Assets/StreamingAssets/EOS/`; that folder carries the client secret and ships only
   in Epic builds (Build.md, "Store files"). **EOS Plugin > Log Level Configuration** sets EOS's own logging.
3. In the Developer Portal, give each achievement the id the game uses in `AchievementIds` (the same ids as Steam's API
   names keep one list).
4. Copy the Visual C++ runtime into `BuildRedist/Epic/` (Build.md, "Store files").
5. Build with **AF > Build > Epic > Development**. Start a local build with `-EpicPortal` (no login then), or from the
   Epic Games launcher for a real login.

What it does:

- **Launch.** Outside the Editor, the game quits with a message box unless the Epic Games launcher started it: any one
  of `-EpicPortal`, `-epicapp=` or `-AUTH_PASSWORD=` is enough (the offline launcher passes an empty password, and
  third-party launchers pass the same arguments). There is no restart through the launcher: that needs the sandbox id,
  which differs between the Dev and Live sandboxes of one build. Then EOS starts (the plugin's `EOSManager`), with the
  workarounds the plugin needs on Windows (it sometimes has no Windows specifics yet; it applies log levels by list
  position, so they are applied again by name). EOS failing to start never stops the game: its services stay inactive.
- **Login** runs in the background and never holds the boot: Auth with the launcher's exchange code, then Connect
  (creating the Connect user on the first login). It has no time limit, because nothing waits for it and the first login
  waits for the player to accept the app's permissions. The exchange code works once, so there is no retry.
- **Achievements.** EOS keeps nothing while offline, so every unlock goes into a queue file first, even when EOS did
  not start
  (`persistentDataPath/epic-achievements/<deployment>_<account>.json`, outside `saves/` so that cloud saves never sync
  it) and leaves it only when Epic confirms. The queue is sent after login and after each unlock, one at a time. A
  result retrying cannot fix (an unknown id) drops the unlock with an error; a lost connection or login stops sending
  until the next trigger; anything else keeps the unlock for the next session. Without the launcher's account and
  deployment ids nothing is unlocked. `IsUnlocked` reads what Epic reported after login and what it confirmed since.
- **Overlay**: EOS's display settings notification; open means visible with exclusive input, so achievement toasts do
  not pause the game.

### Adding a store backend

A store backend lives in its own assembly next to the store's SDK; nothing else references the SDK. The Steam backend
is the example to copy.

1. Install the store's SDK package.
2. Create `Assets/Framework/Platform/<Store>/Runtime/AF.Platform.<Store>.asmdef` referencing `AF.Platform`, `AF.Core`,
   VContainer (and UniTask, MessagePipe as needed) and the SDK's assembly, with:
   - a version define on the SDK package (for example `AF_STEAMWORKS`), so the assembly compiles only when the SDK is
     installed;
   - define constraints `AF_STEAMWORKS` and `STORE_STEAM || UNITY_EDITOR`: it compiles in the Editor, where its errors
     show at once, and into the players of its own store only.
3. Implement `IPlatformBackend` with `Name` equal to the store's name in the build menu, registering every contract
   and, for its launch check, an `IBootStep`. The store's identity (app id, product ids) goes in the backend's own
   options, written once in the root scope.
4. Pass it from the root scope under the store's define, and reference its assembly from `Game.asmdef`.
5. A store the build menu does not know yet also needs its `Store` entry, targets, SDK define and SDK package in
   `BuildStores`, its files in `StoreFiles`, menu items and `StoreBuild` branch (Build.md, "Stores").

What a store backend has to handle:

- **Launch gate.** A store usually requires the game to be started through its client. Steam:
  `SteamAPI.RestartAppIfNecessary` runs before `SteamAPI.Init` (it cannot run after), then the license is checked
  (Steam not running, or the game not on the account). Epic: the launcher passes `-EpicPortal`, `-epicapp=` or
  `-AUTH_PASSWORD=`; accept any one of them, because the offline launcher passes an empty `-AUTH_PASSWORD=` and
  third-party launchers pass the same arguments. Errors go to a native message box (`StoreLaunch.Block`): localization
  is not running yet.
- **A blocked launch quits, but the scope still builds.** `Application.Quit` takes effect at the end of the frame, so
  `Install` still registers every contract; the boot step does not finish, so the game never leaves its boot.
- **The app id is written once.** A wrong Steam app id makes `RestartAppIfNecessary` start another game.
- **No SDK calls after shutdown.** The order of `OnDestroy` and `Dispose` against the SDK's shutdown is undefined.
- **SDK calls that throw.** Some SDK getters throw instead of returning null when the native library failed to load
  (a missing runtime); an exception in `Install` stops the game, so guard them.
- **Achievements before the store is ready.** The game re-unlocks from its save after loading. A store without an
  offline cache (Epic) queues unlocks in a file of its own, keyed by deployment and account, outside the cloud-saved
  folder, and removes them only once the store confirms.
- **The overlay.** Steam reports open and closed through a callback that can get lost on Steam Deck; treat game input
  that arrives while it is "open" as closed there.
- **Build files.** A store build ships its own SDK's native files and none of another store's (Build.md, "Store
  files").
- **Two SDKs in one project.** A game sold on two stores installs both packages, so their versions must compile
  together, editor tools included (Steamworks.NET and the EOS plugin above).

## Not included

User identity, DLC and ownership, rich presence, stats, cloud storage APIs (Steam Auto-Cloud needs no code; a store API
plugs in as an `ISaveStorage`), virtual keyboard, Steam Input, demo and playtest app ids (flavors). On Steam Deck: the
game language Steam reports (`SteamApps.GetCurrentGameLanguage`) as the first startup language, and hiding exclusive
fullscreen from the display settings (Deck's compositor; Proton reports a Windows player). They are added with the same
pattern when a game needs them.
