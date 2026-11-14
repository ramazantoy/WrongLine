# Platform (F4)

Bu modülün kararları, doğrulananları ve inceleme bulguları; çapraz kararlar ve "Açık" listesi `README.md`'de.
Karar değişirse satır silinmez; eskisinin üstü çizilir, yenisi tarihle eklenir.

> F4'ün build tarafı: `Build.md`. Mağaza kapısı (F8'in SDK'sız kısmı, 2026-11-02) ve Steam/Epic backend'leri (F8'in kalanı,
> 2026-11-03, SDK paketleri kurulu değil: uyur) bu dosyada ve `Build.md`'de. Kalan F8 notları `README.md` → "Açık".

## F4 kararları (2026-10-31)
| Konu | Karar | Gerekçe |
|---|---|---|
| Modüller | Build yalnız Editor: `Assets/Framework/Build/{Editor,Tests/Editor}`, `AF.Build.Editor` (→ AF.Core, AF.Core.Editor, `Mono.Cecil.dll`), `AF.Build.Tests`. Platform: `Assets/Framework/Platform/{Runtime,Tests/Editor}`, `AF.Platform` (→ Core, VContainer, UniTask, MessagePipe), `AF.Platform.Tests`; Mock aynı asmdef'te. `com.unity.nuget.mono-cecil` 1.11.6 doğrudan bağımlılık | Build'in dış bağımlılığı (Cecil) var, runtime'a girmiyor. Mock varsayılan backend, ayrı asmdef gerekçesi yok. Steam F8'de ayrı asmdef |
| Platform | `IAchievements {Unlock, IsUnlocked}`, `IPlatformOverlay {IsOpen}` + `PlatformOverlayChanged`. `IPlatformBackend.Install`; `builder.UsePlatform(messagePipe, options, backend)` null → `MockPlatform`, sözleşme eksikse adını veren hata. `OverlayPause` açıkken `platform-overlay` pause handle'ı. Bilinmeyen achievement id `Log.Error`. Mock'lar kendi tipleriyle de kayıtlı (`MockPlatformOverlay.SetOpen`) | Bilinen tuzaklar: eksik case sessizce Mock'a düşer, unlock Steam hazır değilken düşer. Kimlik, DLC, presence, stat, bulut sonra (tüketicisi yok) |

## Template doğrulaması (2026-11-01)
| Bulgu | Düzeltme |
|---|---|
| Bilinmeyen achievement id kontrolü yalnız `MockAchievements`'ta; mağaza backend'i yazmazsa yazım hatası sessiz kalır | ~~Kod yok (backend yok); Platform.md "Mock yapar, mağaza backend'i de yapmalı"; "Açık"ta F8 notu~~ (2026-11-02: kontrol `AchievementGate`'te, her backend'in önünde → "Mağaza kapısı kararları") |

## Mağaza kapısı kararları (2026-11-02)
F8'in SDK'sız kısmı. Kapsam (kullanıcı kararı): yalnız kapı; SDK paketi, SDK çağrısı ve backend yok, yeni test yazılmadı (mevcut
Platform testleri uyarlandı), build alınmadı. Build tarafı `Build.md` → "Mağaza kapısı kararları".
| Konu | Karar | Gerekçe |
|---|---|---|
| Başarım kapısı | Oyun `IAchievements` kullanır (API aynı). Backend'ler `IStoreAchievements` kaydeder; `UsePlatform` önüne `AchievementGate` (internal) koyar: `AchievementIds`'te olmayan id (null dahil) `Log.Error` + yok sayılır, `IsUnlocked` false. `IAchievements` `UsePlatform`'dan önce ya da backend'de kaydedilirse `UsePlatform` fırlatır. `MockAchievements` artık id kontrol etmez, ctor'u parametresiz | Bilinmeyen id kontrolü yalnız Mock'taydı; mağaza backend'i unutursa yazım hatası sessiz kalırdı (template doğrulaması R6). Ortak başarım mantığının tek yeri; backend yalnız listelenmiş id görür |
| Mağaza seçimi | Build ekseni `Store { None, Steam, Epic }`, define `STORE_<AD>` yalnız build menüsünden. Runtime'da `StoreBuild.Name` (internal, `#if`; Editor ve mağazasız build'de null). Backend'i oyun root scope'ta `#if STORE_<AD>` ile `UsePlatform`'a verir; mağaza kimliği (AppId vb.) backend'in kendi options'ında | Tek karar noktası define'dan; asset'ten okunan platform yanlış commit'le Mock ship edebilir. Kimlik oyunun verisi, seçim composition root'ta görünür |
| Mock reddi | İki katman: build öncesi mağaza build'inin player assembly'lerinde `AF.Platform.<Mağaza>` yoksa hata (backend yok ya da SDK paketi yok); runtime'da `UsePlatform` `backend.Name != StoreBuild.Name` ise fırlatır (Mock ve başka mağaza dahil). Backend'in `Name`'i build menüsündeki mağaza adı | Bilinen tuzak: switch'te eksik case sessizce Mock'a düşer, Mock başarım ship edilir. Build kontrolü backend'in yokluğunu, runtime kontrolü oyunun onu geçmediğini yakalar |
| Backend asmdef'i | (Tarif, kod yok) `Assets/Framework/Platform/<Mağaza>/Runtime/AF.Platform.<Mağaza>.asmdef`; `versionDefines` SDK paketi → `AF_<SDK>`; `defineConstraints` `["AF_<SDK>", "STORE_<AD> \|\| UNITY_EDITOR"]` | Paket yoksa hiç derlenmez (template ve mobil oyun SDK taşımaz); Editor'da derlenir, hata hemen görünür; player'a yalnız kendi mağazasında girer |
| Yeni mağaza | `Store` değeri + `BuildStores` hedefleri + 3 menü maddesi + `StoreBuild` dalı + backend asmdef'i + root scope dalı. Define ve backend assembly adı mağaza adından türer | İleride Google Play Games, Game Center vb. aynı yoldan |
| Doğrulanmayan | ~~Define kısıtı karşılanmayan asmdef'e `Game.asmdef` referansının derlemeyi bozmaması (Unity'nin bilinen davranışı, editor-only asmdef referansıyla aynı yol)~~ | ~~Template'te backend yok; ilk backend'de görülür~~ (2026-11-03: doğrulandı → "Steam/Epic backend kararları") |

### Doğrulananlar (mağaza kapısı, Editor'da, 2026-11-02)
| Konu | Sonuç |
|---|---|
| Derleme, test, validation | Console 0; yeni dosyalar `.rsp`'de; EditMode 393/393, sonrası 16 error + 8 warning (Platform'unki aynı mesaj, artık kapıdan); ValidateAll 0; Check Release yalnız placeholder hatası |
| Mağaza kontrolü | `ClaudeValidate.Store` Steam ve Epic (Windows 64): player script'leri `STORE_<AD>` ile derlendi (layout farkı yok), "This Steam/Epic build has no … platform backend" hatasıyla reddedildi |

### Mağaza kapısı incelemesi (bağımsız, git diff üzerinden, 2026-11-02)
| Bulgu | Düzeltme |
|---|---|
| Doküman "Editor'da `STORE_*` hiç yok" derken Play Mode için PlayerSettings'e eklemeyi öneriyordu; eklenince `StoreBuild.Name` Editor'da dolu, Mock kuran Platform testleri düşer | Platform.md, `StoreBuild` XML ve Build.md kaydı: o sürede Editor o mağazanın build'i gibi davranır, testler düşer, build reddedilir |
| `IAchievements`'ı oyun `UsePlatform`'dan önce kaydederse hata backend'i suçluyordu; sonra kaydederse kapıyı sessizce değiştirir (VContainer'da son kayıt kazanır) | Önce kayıt için ayrı hata; sonradan kayıt dokümanda ("nothing else registers it"). Build callback'te resolve ile kontrol eklenmedi: backend'in SDK'ya bağlı ctor'unu boot'tan önce çalıştırırdı |
| `MockAchievements` `AsSelf` kayıtlı; araç `Resolve<MockAchievements>().Unlock` ile kapıyı atlayıp yazım hatasını açabiliyordu (önceden hata logluyordu) | `Unlock` açık arayüz uygulaması (`IStoreAchievements.Unlock`); Mock'un kendi tipi yalnız listeler ve sıfırlar |
| `BuildStores.Targets(None)` boş dönüyor, özeti "her hedef" diyordu | Özet düzeltildi |

## Steam/Epic backend kararları (2026-11-03)
F8'in kalanı. Kullanıcı kararı: Claude erişimi bitebileceği için backend'ler şimdi yazılır, SDK paketleri kurulu olmadan
commit'lenir (uyur); Steam gerçek Steam'le (Spacewar), Epic yalnız derleme + testle doğrulanır. Kaynak: daha önce denenmiş bir
entegrasyonu (aşağıdaki notlar). Build tarafı `Build.md` → "Mağaza dosyaları kararları".
| Konu | Karar | Gerekçe |
|---|---|---|
| Uyku | Backend ve test asmdef'leri SDK paketine `versionDefines` (`AF_STEAMWORKS`, `AF_EOS`) ile bağlı; backend `defineConstraints` `["AF_<SDK>", "STORE_<AD> \|\| UNITY_EDITOR"]`, test `["UNITY_INCLUDE_TESTS", "AF_<SDK>"]`. Template manifest'inde SDK yok; uyandırma = paket satırı + AppId (Epic: plugin config'i) | Mevcut karar ("Backend asmdef'i"); mobil ve mağazasız oyun SDK taşımaz. Testler backend'le uyur, paketle uyanır |
| Kablolama | `GameRootScope` mağaza dallarını şimdiden taşır (`#if STORE_STEAM` → `SteamPlatform(new SteamOptions { AppId = 0 })`, `#elif STORE_EPIC` → `EpicPlatform()`, değilse Mock); `Game.asmdef` backend asmdef'lerine GUID ile referans verir | Uyandırmada root scope'a dokunulmaz, yalnız AppId. Kısıtı karşılanmayan asmdef'e referans derlemeyi bozmuyor (doğrulandı: paket yokken console 0, `Game` derleniyor) |
| `UsePlatform` sırası | `ConfigureGame`'in ilk çağrısı (önceden Save/Input/Audio/Localization'dan sonraydı) | Boot adımları kayıt sırasıyla koşar: mağaza kapısı save ve dil yüklenmeden çalışır (player log'da doğrulandı) |
| Ortak kapı | `AF.Platform.StoreLaunch.Block(message, ct)`: player'da `Log.Error` + Windows'ta `MessageBoxW` (başlık `productName`, hata/ön plan/en üst) + `Application.Quit` + `UniTask.Never(ct)`; mesaj null → sessiz çıkış; Editor'da yalnız log, boot sürer (servisler pasif) | `NativeMessageBox` + `Block` deseni. Editor'da `Quit` çalışmaz, bitmeyen boot kimseye yaramaz. Root scope kapanınca `Never` iptal olur, `AppBoot` OCE'yi sessiz geçer (player'da doğrulandı) |
| Steam backend | `SteamPlatform` (`Name` "Steam", ctor AppId'yi doğrular: 0 → fırlatır; 480 Spacewar debug olmayan player'da fırlatır). `SteamSession` (`RegisterEntryPoint`: `IBootStep`, `ITickable`, `IDisposable`): Editor dışında `RestartAppIfNecessary` (true → sessiz çıkış; `DllNotFoundException` → "dosyalar bozuk"), `SteamAPI.InitEx` (başarısız → "Steam'den başlat"), `BIsSubscribed` (false → "bu hesapta yok"); her kare `RunCallbacks`; `AppSuspending(Quit)`'te `Shutdown`, `Dispose`'da SDK çağrısı yok. `SteamAchievements`: çalışmıyorsa `Log.Warning` + yok say; `GetAchievement` → `SetAchievement` + `StoreStats`; tanımsız API adı `Log.Error`. `SteamOverlay`: `GameOverlayActivated_t`; Deck'te (`IsRunningOnSteamHardware() == SteamDeck`; SDK 1.65'te `IsSteamRunningOnSteamDeck` yok) açıkken gelen ilk gamepad tuşu/sol tık bir sonraki `Tick`'te kapandı sayılır | `SteamLaunchGate`, `SteamAchievementService`, `SteamOverlaySource` + pause handler'ı desenleri. SDK 1.61+ istatistikleri süreç başlamadan senkronlar (`RequestCurrentStats` yok). Kapanma input update'inin içinde uygulanmaz: pause/mod geçişi action'ları değiştirir. Geç gelen gerçek "kapandı" idempotent `Set` ile etkisiz (ayrı mandal gereksiz) |
| SDK sürümü | Steamworks.NET 2025.165.0 (git, tag sabit; Steamworks SDK 1.65). `ProjectSettings/SteamworksNETSettings.json` `canManageDefineSymbols: false` commit'li; proje kökü `steam_appid.txt` gitignored | Paket kurulunca `STEAMWORKS_NET`'i PlayerSettings'e yazar, build kuralı bunu hata sayar; ayar dosyası paket yokken zararsız |
| Steam SDK sürümü (Epic aşamasında, 2026-11-03) | Önerilen pin Steamworks.NET 2025.162.1 (denenmiş sürüm); yalnız Steam'de satılan oyun 2025.165.0+ alabilir. Backend iki API ile derlenir: asmdef'te `AF_STEAMWORKS_HARDWARE_API` (paket ≥ 2025.165.0) → `IsRunningOnSteamHardware`, değilse `IsSteamRunningOnSteamDeck` | İki paket birlikteyken EOS 6.2.0'ın editor Steam yardımcısı (`com.playeveryware.eos-Editor.steam.utility`, Steamworks.NET'e version define'la bağlı, `#if DISABLESTEAMWORKS` editor'de etkisiz) `STEAMGAMESEARCH_`/`STEAMMUSICREMOTE_INTERFACE_VERSION` ister; 2025.163.0'dan beri yoklar. 2025.165.0 `IsSteamRunningOnSteamDeck`'i kaldırdı. Tablo: 162.1 sabitler var/Deck eski API; 163.0–164.1 sabit yok/eski API; 165.0 sabit yok/yeni API |
| Build kontrolü | Mağaza build'inde SDK paketi kurulu değilse (`BuildStores.SdkPackage`, `PackageInfo.FindForPackageName`) hata ve player script derlemesi atlanır | Kablolamadan sonra paket yokken `GameRootScope`'un mağaza dalı derlenmez: console'a "GameRootScope.cs ... AF.Platform.Steam yok" hatası düşüyor, template bozuk görünüyordu |
| Epic backend | `EpicPlatform` (`Name` "Epic", seçenek yok: kimlik plugin config'inde `StreamingAssets/EOS`). `EpicSession` (`IBootStep`, `IDisposable`): Editor dışında `EpicLaunchArguments.FromLauncher` (`-EpicPortal`, `-epicapp=`, `-AUTH_PASSWORD=`, büyük/küçük harf duyarsız) yoksa `StoreLaunch.Block`; player'da `SetStackTraceLogType(Log, None)`; Windows specifics elle `Register` (try), `EOSManager` GameObject'i (kendisi DontDestroyOnLoad), log seviyeleri adıyla (try). Login arka planda (`UniTaskVoid`, scope token'ı): exchange code → Auth → Connect (`InvalidUser` → create + yeniden), ara sonuçlar (`IsOperationComplete` false) atlanır, 600 sn realtime. `EpicAchievements`: her unlock önce `EpicUnlockQueue`'ya (`persistentDataPath/epic-achievements/<deployment>_<hesap>.json`, temp + `File.Replace`, boşalınca dosya silinir), login sonrası `QueryPlayerAchievements` ile açıklar okunur, kuyruk tek tek gönderilir (kalıcı hata düşer, oturum geneli durur, diğeri o oturum atlanır); kuyruk hesabı login hesabından farklıysa gönderilmez. `EpicOverlay`: `AddNotifyDisplaySettingsUpdated`, açık = `IsVisible && IsExclusiveInput`; notification kaldırılmaz (plugin quit'te platformu bırakır, sonrasında SDK çağrısı yok) | Desenler: `EpicLaunchGate`, `PlatformInstaller.InstallEpic`, `EpicAchievementService`, `EpicPendingAchievementQueue`, `EpicOverlaySource`'u; stat'lar, unlock bildirimi (yalnız stat eşikli başarımlar için) ve hile profili çıkarıldı. Kuyruk dosyası Save modülünün kuralı dışında (oyuncu verisi değil; Epic onayladıkça kendi dosyası) |
| Kapsam dışı | Steam oyun dili seçicisi, Deck'te Exclusive'i gizleme, Steam Input, yüzen klavye, stat, bulut API, flavor (demo AppId), macOS/Linux doğrulaması, yükleme adımı | Modüller arası bağ ya da SDK/hedef olmadan doğrulanamaz; Platform.md (doküman) "Not included" |

### Doğrulananlar (Steam, 2026-11-03)
| Konu | Sonuç |
|---|---|
| Paket kuruluyken | Steamworks.NET 2025.165.0 kuruldu, PlayerSettings'e define eklemedi; console 0; EditMode 420/420 (11 Steam testi), ValidateAll 0 |
| Steam Development build (Windows IL2CPP, 10:42) | Defines `SHOW_LOGS;STORE_STEAM;EOS_DISABLE`; rapor "the Steam SDK is in the build, no other store's files are" |
| Çalışma zamanı (Spacewar 480, exe yanında `steam_appid.txt`, Steam açık) | `Platform: Steam`; Steam boot adımı save yüklemesinden önce; `Steam: started for app 480`; `IsUnlocked` false → `Unlock` → true; Menu'ye geçiş; pipeline `quit` ile temiz kapanış |
| Overlay | Shift+Tab açılmadı: Steam overlay'i yalnız client'ın başlattığı oyuna enjekte ediyor (bilinen davranış). Overlay ve Deck kuralı yalnız testte (`SteamOverlayTests`) |
| `steam_api64.dll` yokken | `DllNotFoundException` loglandı, "files are damaged" kutusu açıldı, boot ilerlemedi (save yüklenmedi), kutu kapanınca süreç kendiliğinden çıktı, OCE logu yok |
| Paket kaldırılınca | Steam assembly'leri derlenmiyor, console 0; EditMode 409/409; ValidateAll 0; `ClaudeValidate.Store Steam` → placeholder + "Steam SDK package … not installed" (derleme hatası yok) |

### Doğrulananlar (Epic ve iki SDK birlikte, 2026-11-03)
| Konu | Sonuç |
|---|---|
| EOS 6.2.0 kurulumu | tgz `file:` mutlak yolla; "Missing Signature" diyaloğu açıldı (kullanıcı kapattı; açıkken CLI zaman aşımı + Unity'nin "On demand scheduler requested to import while stopped" hataları). `Assets/`'a dosya yazmadı |
| Epic derlemesi | `AF.Platform.Epic` 0 hata; `namespace AF.Platform.Epic` içinde `Epic.OnlineServices.X` tam adı `AF.Platform.Epic`'e çözüldüğü için alias kullanıldı. Epic testleri 16/16 |
| İki paket birlikte | Steamworks.NET 2025.165.0 + EOS 6.2.0 derlenmedi (EOS editor Steam yardımcısı); 2025.162.1 + EOS 6.2.0 derlendi. EditMode 438/438, ValidateAll 0; `Store Steam` yalnız placeholder (Steam player'ı `EOS_DISABLE` ile derleniyor), `Store Epic` placeholder + VC++ + EOS config ön kontrolleri |
| Çalışma zamanı | Yok: Epic ürünü/kimlik bilgisi yok, Epic build alınmadı (kullanıcı kararı). Login, kuyruk gönderimi, overlay bildirimi, EOSBootstrapper izleyicisi ilk Epic oyununda |
| Paketler kaldırılınca | Backend ve test assembly'leri derlenmiyor; EditMode 411/411; ValidateAll 0; `Store Steam`/`Store Epic` → "SDK package … not installed" (console'a derleme hatası yok) |

### Steam/Epic backend incelemesi (bağımsız, `git diff f47a8af..0b00f0b` üzerinden, 2026-11-04)
SDK çağrıları kaynakla karşılaştırıldı (imzalar doğru, `Callback.Dispose` yönetilen, EOS getter'ları shutdown sonrası null).
| Bulgu | Düzeltme |
|---|---|
| AppId 480 (Release) / 0: `SteamPlatform` ctor'u `ConfigureGame`'de fırlatıyordu; player'da root scope kurulmuyor, kutu yok, çıkış yok | Kontrol `SteamPlatform.AppIdProblem` → `SteamSession.Run`'ın ilk adımı, `StoreLaunch.Block` (kutu + çıkış); ctor fırlatmaz (test) |
| Steam açılışında yalnız `DllNotFoundException` yakalanıyordu: `EntryPointNotFoundException` vb. boot'u sessiz asılı bırakırdı; `InitEx`'in her hatası "Steam çalışmıyor" diyordu | Her exception → "dosyalar bozuk"; `VersionMismatch` → "Steam güncel değil" |
| Epic login zaman aşımından sonra başarılı login boşa gidiyordu (`OnLoggedIn` hiç çalışmıyor, kuyruk o oturum gönderilmiyor) | Zaman sınırı kaldırıldı: login'i bekleyen yok, ilk login izin ekranını bekler |
| macOS/Linux Editor'da (Windows hedefli) `AF.Platform.Epic` derlenmezdi: `WindowsPlatformSpecifics` plugin'de `(UNITY_STANDALONE_WIN && !UNITY_EDITOR) \|\| UNITY_EDITOR_WIN` altında | Aynı koşul |
| EOS başlamazsa kuyruk kurulmuyordu (doküman "her unlock önce kuyruğa" diyor) | Kuyruk komut satırından, `StartEos`'tan önce kurulur |
| Deck kuralı yalnız `SteamDeck`; SDK 1.65'te `SteamMachine` aynı SteamOS overlay'i | 2025.165.0+ yolunda Steam Machine de dahil (denenmedi) |
| Kabul: backend içi somut tip inject'i (`SteamSession(SteamOverlay)`, `EpicSession(EpicOverlay, EpicAchievements)`) | Template doğrulamasındaki "modül içi tiplerin somut inject'i" kabulü; değişiklik yok |
| Doküman: mesaj kutusu yalnız Windows'ta; yeni mağaza adımlarında SDK paketi eksikti | Platform.md (doküman) ve Build.md düzeltildi; build tarafı → `Build.md` "Mağaza dosyaları incelemesi" |

Doğrulama: Steam testleri 2025.162.1 ile 12/12 (yeni: ctor fırlatmaz). Epic düzeltmeleri derlendi (EOS kuruluyken
`AF.Platform.Epic.dll` düzeltmeden sonra yeniden üretildi); testleri koşulmadı: kullanıcı kararı, "Epic için detaylı test
yok, sahada deneriz". EOS'un ikinci kurulumunda Editor çöktü, yeniden açılınca "Missing Signature" tekrar geldi; paketler
`manifest.json`/`packages-lock.json` HEAD'e alınıp `Client.Resolve` ile kaldırıldı. Paketsiz: EditMode 413/413 (sonrası
17 error + 8 warning), ValidateAll 0, `Store Steam`/`Store Epic` SDK paketi hatasıyla ret.

## Mağaza backend'i notları (önceki deneyimden; 2026-11-03'te backend'lere uygulandı, yalnız Steam'in Windows yolu çalıştırıldı)
Backend yazılırken bakılır; dokümandaki "What a store backend has to handle" bölümünün kaynağı. Denenen sürümler: Steamworks.NET (git
paketi) ve PlayEveryWare EOS plugin'i (6.x, tgz).
- Steam açılış kapısı: `RestartAppIfNecessary(appId)` `SteamAPI.Init`'ten önce (sonra çağrılamaz); `steam_api64.dll` yüklenemezse
  `DllNotFoundException` → "dosyalar bozuk" mesajı. Lisans: Init başarısız → "Steam'den başlat", `SteamApps.BIsSubscribed` false →
  "bu hesapta yok" (iki ayrı mesaj). Mesaj native message box (Win32 `MessageBoxW`), İngilizce, başlık `productName`: localization
  ayakta değil. Steam'in oyunu yeniden başlattığı yol sessiz çıkar.
- Engellenen açılış: `Application.Quit` kare sonunda etkili; container tam kurulmalı (bilinen hata: engellenen dalda Mock kuruldu, erken
  return'e "optimize" edilmemeli). Template'te: sözleşmeler yine kaydedilir, boot adımı bitmez; boot'u bekleyen akış durur.
- AppId tek yerde, flavor define'ıyla seçilir; yanlış AppId oyuncuyu başka uygulamaya yollar (demo exe'si tam oyunu açar).
  Steamworks.NET'in `SteamManager`'ına AppId gömülmez (orada `AppId_t.Invalid` kalır).
- `steam_appid.txt` restart kontrolünü atlatır: build çıktısı kontrolü bu işte. Editor'da Steam için proje kökünde bir kopya
  gerekir; kapı Editor'da çalışmaz, yalnız gerçek build'de doğrulanır.
- `OnDestroy`/`Dispose`'da Steamworks çağrısı yok (`SteamAPI.Shutdown`'a göre sıra tanımsız).
- Overlay (`GameOverlayActivated_t`): Deck'te kapandı callback'i kaybolabiliyor → "açık"ken oyuna ulaşan ilk gamepad tuşu ya da sol
  tık kapandı sayılır. Oyunun kendi yüzen klavyesinin tetiklediği overlay aktivasyonu yok sayılır. EOS overlay'i kolu oyunla
  paylaştığı için Epic'te bu kural yok.
- Başarım (Steam): `GetAchievement` → açık değilse `SetAchievement` + `StoreStats`; Steam istemcisi offline önbellek tutar. Stat'lar
  idempotent değil ve stat başarımını oyun açmaz (template'te stat yok).
- Epic açılış kapısı: `-EpicPortal`, `-epicapp=`, `-AUTH_PASSWORD=`'den biri yeter, daraltılmaz (offline launcher boş
  `-AUTH_PASSWORD=` verir; üçüncü parti launcher'lar aynı argümanları üretir). Launcher üzerinden yeniden başlatma yok
  (`CheckForLauncherAndRestart` sandbox id ister, Dev ve Live'da farklı). Yerel deneme exe'ye `-EpicPortal`.
- EOS plugin: Windows platform-specifics kaydı `EOSManager` Awake'inde null görülebildi → elle `Register` (ikinci kayıt fırlatır,
  try); log seviyesi config'i kategori index'iyle uygulanıyor, Http kaçıyor → adıyla yeniden uygulanır; native render plugin'iyle
  `GetEOSPlatformInterface`/`GetEOSUIInterface` native platform yokken (VC++ eksik) null yerine fırlatır → try; kurulumda ya da
  `Initialize`'da exception oyunu/diğer initializable'ları durdurur. Plugin init'te Log tipi mesajların yığın izi kapatılır
  (`SetStackTraceLogType(LogType.Log, None)`, Editor dışı).
- Epic başarım: Connect login (launcher'ın exchange code'u) gerekir; SDK'nın offline önbelleği yok → hesap başına kalıcı kuyruk
  (`<deployment>_<hesap>.json`), cloud save klasörü dışında (senkronlanan kuyruk aynı deltayı iki PC'den gönderirdi), Epic
  onaylayınca silinir. Stat'lar delta. Epic Dev Portal id'leri Steam API adlarıyla aynı tutulur, ayrı alan yok.
- Build dosyaları: Epic build'inden `steam_api64.dll` ve `Steamworks.NET.txt` sökülür (paketin redist kancası editor assembly'sinde,
  define'ı görmez); Steam build'inden EOS dll'leri ve `StreamingAssets/EOS` (ClientSecret taşır). Epic: `msvcp140.dll`,
  `vcruntime140.dll`, `vcruntime140_1.dll` exe yanına (EGS redist kurmaz; yoksa EOSSDK yüklenmez, başarımlar sessizce ölür).
  Kanıt: Steam build'inde `steam_api64.dll`, Epic'te `EOSSDK-Win64-Shipping.dll` + dolu `StreamingAssets/EOS`. `eos_steam_config.json`
  Epic'te kalır (silmek hata satırı ekliyor).
