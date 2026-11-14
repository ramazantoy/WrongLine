# Build: build menüsü ve kontroller (F4), Addressables içerik adımı (F7)

Bu modülün kararları, doğrulananları ve inceleme bulguları; çapraz kararlar ve "Açık" listesi `README.md`'de.
Karar değişirse satır silinmez; eskisinin üstü çizilir, yenisi tarihle eklenir.

## F4 kararları (2026-10-31)
| Konu | Karar | Gerekçe |
|---|---|---|
| Modüller | Build yalnız Editor: `Assets/Framework/Build/{Editor,Tests/Editor}`, `AF.Build.Editor` (→ AF.Core, AF.Core.Editor, `Mono.Cecil.dll`), `AF.Build.Tests`. Platform: `Assets/Framework/Platform/{Runtime,Tests/Editor}`, `AF.Platform` (→ Core, VContainer, UniTask, MessagePipe), `AF.Platform.Tests`; Mock aynı asmdef'te. `com.unity.nuget.mono-cecil` 1.11.6 doğrudan bağımlılık | Build'in dış bağımlılığı (Cecil) var, runtime'a girmiyor. Mock varsayılan backend, ayrı asmdef gerekçesi yok. Steam F8'de ayrı asmdef |
| Build türleri | `Development` = development player + `SHOW_LOGS` (+ Pipeline runtime, toggle'dan); `Release` = define yok. Aktif hedef build edilir, menü platform değiştirmez | Dev build yalnız yerel test (loopback HTTP + kod çalıştırma); test edenler Release alır |
| Çıktı | `Builds/<Platform>/<Kind>/` (gönderilecek klasör), `<Kind>-symbols/` (IL2CPP BackUp ve Burst DoNotShip klasörleri taşınır), `<Kind>-report.txt`. Sabit yol; rapor her build başında, klasör build'den hemen önce silinir; yalnız `Builds/` içi silinir; reddedilen build öncekini bırakır | Bilinen tuzaklar: eski "OK" raporu doğrulanmamış build'in yanında kalır; debug klasörü depoya girer |
| Build öncesi kontrol | Ortam görüntüsü (`BuildEnvironment`) üzerinde saf kurallar + ValidateAll + serialize layout. Hata: Play/derleme, ValidateAll hatası, kaydedilmemiş sahne/prefab, PlayerSettings/compiler arg/`csc.rsp` define'ı, özel Build Profile aktif, layout farkı, desteklenmeyen/kurulu olmayan hedef, enabled sahne yok, Release'te placeholder ad. Uyarı: kirli asset, Release'te Mono, stripping > Minimal, Release'te commit'lenmemiş değişiklik, Development'ta placeholder | Testler proje ayarına dokunmadan kötü kurulum verir. Kirli asset uyarısı: Play'de değişen materyal keyword'ü build'e girebilir |
| Serialize layout kontrolü | `PlayerBuildInterface.CompilePlayerScripts` build'in hedefi/seçeneği/`extraScriptingDefines`'ıyla `Temp/`'e; editor tarafı `Library/ScriptAssemblies`; ikisi Cecil ile okunur, `Assets/` assembly'lerinde UnityEngine.Object türevi ya da `[Serializable]` tiplerin serialize alanları (ad + tip) karşılaştırılır. ~30 sn | Unity bunu build sonunda alan adı vermeden yakalıyor. `#if UNITY_EDITOR` dahil |
| Build sonrası | Symbols taşınır; çıktıda Pipeline runtime aranır (IL2CPP `global-metadata.dat`'ta `RuntimePipelineServer`, Mono `Unity.Pipeline.dll`), Release'te bulunursa Failed; build öncesi/sonrası `git status` farkı rapora | Pipeline-release kararı artefakt düzeyinde korunur; build'in projeye bıraktığı çöp görünür |
| Tek kapı | `BuildGuard` (`IPreprocessBuildWithContext`, order -10000) `Builder`'ın SessionState bayrağı yoksa build'i durdurur; `IncludeTestAssemblies` geçer | Build Profiles penceresi ve Pipeline `build run` kontrolleri ve define'ları atlar |
| Unity 6 Build Profiles | Kullanılmıyor; classic platform profili. Özel profil aktifse build hatası. Flavor fazında yeniden bakılır | Profilin kendi define'ı, sahne listesi ve PlayerSettings override'ı menüyü atlar |
| Proje ayarları | Standalone IL2CPP; stripping Minimal (Standalone, Android, iOS); artık define'lar (`SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY`) silindi; identifier override kapalı, identifier'lar `com.DefaultCompany.TemplateProject` | IL2CPP doğrulaması Minimal'de; identifier Company/Product'tan türesin |
| IL2CPP kod üretimi / link.xml | VContainer source generator ve `link.xml` eklenmedi. Stripping yükselten oyun `[Preserve]`/`link.xml` ekler; build uyarır | Minimal'de kullanıcı ve paket assembly'leri strip edilmez; generator yalnız performans |

## Doğrulananlar (F4, Editor'da ve Windows IL2CPP build'lerinde)
| Konu | Sonuç → kural |
|---|---|
| Unity 6.3 API | `ScriptCompilationSettings.extraScriptingDefines` var. `IPreprocessBuildWithContext` (`BuildCallbackContext.Report/IsPlayerBuild`). `BuildProfile.GetActiveBuildProfile()` platform profilinde null. `PlayerSettings` değişikliği `SaveAssetIfDirty` ile yazılmaz. `SetApplicationIdentifier` override bayrağını açar. `AssemblyBuilder` obsolete (CS0618) ama çalışıyor; testte pragma ile |
| Pipeline 0.9.1 | Toggle'lar PlayerSettings define'ı (Standalone + Server). Build işlemcisi yalnız PlayerSettings'e bakar. Build sonunda boş `Assets/Settings/Pipeline/Resources` klasörü kalır → klasör gitignored. IL2CPP player'da `script eval` derleyemez; player komutları console, runtime status/quit/simulate_key/simulate_pointer/set_time_scale |
| Dev build (IL2CPP, ~12 dk, 259 MB + 3.2 GB symbols) | Player.log: `Root scope built` bir kez, global/device yüklendi, `Game state: BootState -> MenuState`, `Input mode: none -> menu` (struct `GameStateChanged` aboneye ulaştı), `App boot ready`, `Platform: Mock`; exception/AOT/MissingMethod yok, audit hatası yok (`RegistrationAudit` reflection IL2CPP'de çalışıyor) |
| Save + binding round-trip (IL2CPP) | Elle yazılan `global.json` (Gameplay/Pause override'ı, bayat `action` etiketi) → Newtonsoft okudu, `InputBindings` (internal, `[Inject]`siz ctor, VContainer reflection) override'ı id ile uyguladı (yetim uyarısı yok) → quit'te `AppSuspending` → capture → Newtonsoft → kuyruk/thread pool → `File.Replace` ile düzeltilmiş etiketle yazdı, eski dosya `.bak` oldu |
| Rendering Debugger | Dev build'de Ctrl+Backspace (odaklı pencerede simüle) ile açılıyor, kendi action'ları mod servisinin dışında. Release'de `DEVELOPMENT_BUILD`/`UNITY_EDITOR` guard'ı arkasında, yok |
| Release build (IL2CPP, ~5 dk, 132 MB) | Pipeline runtime çıktıda yok (artefakt kontrolü). Bozuk `device.json` → Player.log'da Save `Log.Error` var; `Root scope built`, `App boot ready`, `Game state:`, `Platform:` yok (Info derlenmedi) |
| Doğrulanamayan (IL2CPP) | `PauseChanged`/`PlatformOverlayChanged` yayın yolu player'da tetiklenemedi (UI yok, eval yok); abonelikleri boot'ta kuruldu, hata yok. Yayın EditMode testlerinde. Slot new/save/load/delete player'da çağrılamadı; aynı kuyruk ve format global'de doğrulandı |

## F4 incelemesi (bağımsız, git diff üzerinden, 2026-10-31)
| Bulgu | Düzeltme |
|---|---|
| Android'de çıktı klasör içinde paket; "tek dosya" dalı hiç çalışmıyor, Pipeline taraması zip içini görmüyor, sonuç "not included" | Pipeline kontrolü build öncesine de alındı: `SerializedLayoutCheck` derlediği player assembly listesini döndürür, `BuildChecks.EvaluatePlayerAssemblies` Release'te `Unity.Pipeline.dll` varsa reddeder (her platformda). Paket çıktısı `BuildPaths.IsPackage(target)` (Android) ile ayrılır, raporda "aranmadı" notu. İlk düzeltme `File.Exists(location)` idi, Windows'ta `.exe`'yi paket sandı (symbols taşınmadı); doğrulama build'inde yakalandı |
| Build ortasındaki exception (çalışan player'ın kilitlediği dosya, symbols taşıma hatası) raporsuz ve yarım klasörle biter | Silme → build → sonrası tek try/catch; Failed + not + `Log.Exception`, rapor her durumda yazılır |
| Başarısız Release gönderilecek klasörde kalıyor | Başarısız Release'in klasörü silinir (Development incelemek için kalır) |
| Dosya adı temizliği host'a bağlı (`GetInvalidFileNameChars` Mac/Linux'ta farklı) | Sabit Windows karakter kümesi + kontrol karakterleri |
| Layout kontrolünün "Unity serialize eder mi" süzgeci testsiz; assembly bulunamazsa her şeyi sessizce atlıyor | `IsUnitySerialized(Assembly)` test edildi (Serializable, MonoBehaviour, düz sınıf, enum, olmayan tip); editor kopyası yüklü değilse hata |
| "Build changed project files" önceden değişmiş dosyanın yeniden yazılmasını ve untracked klasör içini kaçırıyor | `--untracked-files=all` + build öncesi/sonrası içerik hash'i |
| `Git`: yönlendirilip okunmayan stderr kilitleyebilir; status başarısızken rapor temiz görünür | stderr yönlendirilmez, stdout async okunur, zaman aşımında süreç öldürülür; status okunamazsa commit "(working tree state unknown)", Release'te uyarı |
| `DefinesSymbol` yalnız satır başındaki define'ı görüyor (`-warnaserror -define:X` geçiyor) | Satır option'lara bölünür |
| Standalone Server subtarget'ı sessizce Player build ediliyor | Build.md'de yazılı (menü yalnız player build eder) |
| Platform.md mağaza backend'inin build menüsü define'ıyla seçildiğini söylüyor (öyle bir define yok); Build.md Pipeline satırı toggle'a bağlı olduğunu söylemiyor | İkisi düzeltildi |
| Commit hijyeni: URP asset'leri build yan etkisi; ProjectSettings'te `m_BuildTargetBatching` Standalone girdisi Unity'nin yazdığı varsayılan değerler | URP kararı kullanıcıda; batching girdisi zararsız, kalır. Build.md'deki `unity command` örnekleri Unity'nin genel CLI'ı, AI akışına özel değil, kalır |

## F7 kararları (2026-11-01)
Oyuncu ayarları ve dil. Kapsam: yeni `AF.Settings` ve `AF.Localization` modülleri (+Editor, Tests), Audio (`AudioVolumeChanged`,
`UseAudio` imzası), Input (`ConfineCursor`), Build (Addressables içerik adımı), Game (`GameRootScope`, `Game.asmdef`,
`Assets/Game/Localization/`), `Assets/AddressableAssetsData/`, paket, PlayerSettings, `.gitignore`.
| Konu | Karar | Gerekçe |
|---|---|---|
| Preloaded Assets | Localization ayar asset'i kalıcı olarak listede | Paket her build'de ekleyip bellekte çıkarıyor, disk değişik kalıyordu (build'de görüldü); listede olunca dokunmuyor |
| Addressables build | `AF.Build.Editor` (`versionDefines` → `AF_ADDRESSABLES`): checks'ten sonra, eski çıktı silinmeden önce `BuildPlayerContent`; hata → Failed. Ayar "build with player" Do not build değilse build hatası; ayar yoksa hata. Build sonrası `StreamingAssets/aa/settings.json` yoksa Failed (paket çıktısında aranmaz) | İçerik Addressables'ın işi; raporda görünür, başarısız içerik eski build'i silmez. Addressables'ın kendi kancası `BuildGuard`'dan önce çalışıyor |
| Content state | `addressables_content_state.bin` varsayılan yerinde (gitignored); platform klasörlerinin `.meta`'ları da `.gitignore`'da (Windows, OSX, Linux, Android, iOS, WebGL) (inceleme) | Temiz klonda yetim meta olmasın; uzak içerik yok |

## Doğrulananlar (F7, paket kaynağından, Editor'da ve Windows IL2CPP dev build'inde)
| Konu | Sonuç → kural |
|---|---|
| Addressables 2.10.3 | `AddressablesPlayerBuildProcessor.PrepareForBuild` "Do not build"da da `Library/com.unity.addressables/aa/<platform>`'u (varsa) `StreamingAssets/aa`'ya ekler, `link.xml`'i config klasörüne kopyalar (InitializeOnLoad siler, gitignored). Varsayılan ayar `DoNotBuildWithPlayer`. İlk içerik build'i `ProfileDataSourceSettings.asset` ve `ProjectSettings/ScriptableBuildPipeline.json` yaratır |
| Dev build (IL2CPP, 11:36) | Rapor: içerik 23 sn, 10 konum, `aa` çıktıda. Dosyasız açılış: Unity'nin hatırladığı 960x540 pencereden kenarlıksız 1920x1080'e geçti (stil popup, başlıksız), `tr (system)`, hiç dosya yazılmadı. Elle yazılan Windowed 1280x720 + `en`: istemci 1280x720 başlıklı pencere, `en (saved)`, müzik 0.5; quit'te eksik alanlar normalize edilip yazıldı, sonraki açılışta yazılmadı. Exception/AOT yok, struct broker'lar hatasız. Build inceleme düzeltmelerinden önce alındı; düzeltmeler Editor'da doğrulandı |

## F7 incelemesi (bağımsız, git diff üzerinden, 2026-11-01)
| Bulgu | Düzeltme |
|---|---|
| `Windows.meta` yetim kalacaktı | `.gitignore`'a platform klasör metaları |

## Template doğrulaması (2026-11-01)
| Bulgu | Düzeltme |
|---|---|
| Rapor silme, değişen dosya hash'i ve `NoteChangedFiles` try/catch dışındaydı: oradaki IO istisnası raporsuz bitirirdi (F4 "rapor her durumda") | Gövde `Run`'a alındı; `Build` try/catch/finally ile `Finish`'i her yolda çağırır. Değişen dosya hash'i IO hatasında fırlatmaz (inceleme: başarılı build sonrası "Failed" + silinmemiş Release klasörü). Windows Release build'iyle doğrulandı (hash düzeltmesinden önce; aşağıda) |
| Build.md: Pipeline dev define'ı yalnız Standalone/Server → Android/iOS Development build'inde CLI bağlantısı yok; Android paketinin yanında symbols taşıma yok; iOS çıktısı Xcode projesi; layout kontrolü `Temp/BuildLayoutCheck`'i siler | Doküman. Android'de IL2CPP BackUp klasörünün oluşup oluşmadığı ilk mobil build'de görülür |

### Doğrulananlar (template doğrulaması, Windows IL2CPP Release, 2026-11-01)
| Konu | Sonuç |
|---|---|
| Release build (`AllowPlaceholderIdentity`, 5:19, 138.7 MB) | Succeeded; Addressables içeriği 8 sn, çıktıda; BackUp ve Burst klasörleri `Release-symbols/`'a; Pipeline runtime yok. Rapor build sırasında elle düzenlenen dosyaları da "build changed project files" altında listeledi (önce/sonra karşılaştırması, beklenen). URP asset'leri bu build'de yeniden yazılmadı; `Assets/Resources/` build ortasında oluşup sonunda silindi |

## Mağaza kapısı kararları (2026-11-02)
Runtime tarafı ve backend notları `Platform.md` → "Mağaza kapısı kararları".
| Konu | Karar | Gerekçe |
|---|---|---|
| Mağaza ekseni | `Store { None, Steam, Epic }` + `BuildStores` (`Store.cs`): tabloda yalnız hedefler (Steam: Windows 64/32, macOS, Linux; Epic: Windows 64, macOS); define `STORE_<AD>` ve backend assembly `AF.Platform.<Ad>` addan türer. `BuildRequest(kind, store)`; `BuildKinds.Defines(request)` = tür + mağaza define'ı (layout kontrolü, build, rapor) | Bilinen tuzak: menü profili mağazanın define setini seçerse define yalnız build'de olur. Yeni mağaza tek satır + menü |
| Menü ve çıktı | `AF/Build/<Mağaza>/{Development, Release, Check Release}`. Çıktı `Builds/<Platform>/<Mağaza>-<Kind>/` (+ `-symbols`, `-report.txt`); mağazasız yollar aynen. Raporun ilk satırı mağazayı söyler | Mağaza build'i mağazasız build'i ezmez; sabit yol |
| Kontroller | Mağaza aktif hedefi desteklemiyorsa hata. Mağaza build'inin player assembly'lerinde backend assembly'si yoksa hata (derleme başarısızsa da). PlayerSettings'te `STORE_*` mevcut define kuralıyla hata (Editor'da backend denemek için geçici eklenebilir; o sürede Editor o mağazanın build'i gibi davranır, Mock kuran Platform testleri düşer) | Backend'siz mağaza build'i Mock ship ederdi |
| `steam_appid` | Çıktıda adında `steam_appid` geçen dosya: Release Failed (klasör `DiscardFailedRelease` ile silinir), Development not. Paket (Android) aranmaz | Steam'in restart kontrolünü atlatır; build sonrası hata sayılır. Yerel test için exe yanına elle konur |
| Dışarıda | ~~Öbür mağazanın SDK dosyalarını sökme, mağaza SDK kanıt dosyası,~~ yükleme adımı, flavor | SDK'sız doğrulanamaz; backend'le gelir (2026-11-03: dosya kuralları → "Mağaza dosyaları kararları") |

## Mağaza dosyaları kararları (2026-11-03)
Steam/Epic backend işinin (F8 kalanı, `Platform.md` → "Steam/Epic backend kararları") build tarafı. Kaynak: daha önce denenmiş bir build
menüsünün mağaza adımları (profil define'ları, build sonrası silme, yasak ad taraması, kanıt dosyaları, VC++ kopyası).
| Konu | Karar | Gerekçe |
|---|---|---|
| SDK kapatma define'ı | `BuildStores`'ta mağaza başına (`DISABLESTEAMWORKS`, `EOS_DISABLE`); `BuildKinds.Defines(request)` öbür mağazalarınkini ekler, mağazasız build hepsini | İki mağazada satılan oyun iki paketi birden taşır; Steamworks.NET'in kodu define'la boşalır, EOS asmdef'leri `!EOS_DISABLE` kısıtlı. Paket yokken etkisiz |
| Build sonrası (`StoreFiles.Apply`, klasör build'lerinde) | 1) öbür mağazaların dosyaları silinir (ad deseni her yerde; klasör yol sonu: `steam_api.bundle`, `StreamingAssets/EOS`), `EOSBootstrapper*` her build'den; 2) Epic + Win64: `BuildRedist/Epic/` VC++ üçlüsü exe yanına; 3) yasak ad taraması (öbür mağazaların parçaları + `eosbootstrapper`); 4) kanıt: Steam Win64 `steam_api64.dll` (Win32 `steam_api.dll`), Epic Win64 `EOSSDK-Win64-Shipping.dll` + dolu `StreamingAssets/EOS`. 2–4'te bulgu: Release Failed, Development not | Paketlerin native dosyaları define'a bakmadan her build'e girer; `Steamworks.NET.txt`'yi paketin editor kancası yazar. Silme listesi + bağımsız tarama: listede olmayan kalıntı da Release'i düşürür |
| macOS/Linux | Silme listesinde SDK'ların belgelenmiş adları (`libsteam_api.so`, `steam_api.bundle`, `libEOSSDK-*`); kanıt yalnız Windows'ta, başka hedefte rapora "kontrol edilmedi" notu | Yalnız Windows build'i alındı; tarama öbür hedeflerde de Release'i korur |
| VC++ (Epic) | Kaynak proje kökünde `BuildRedist/Epic/` (oyun reposu commit'ler, template'te yok). Epic build öncesi kontrol: dosya eksik (Win64) ya da `Assets/StreamingAssets/EOS` boş → Release hatası, Development uyarısı (`BuildEnvironment.MissingEpicRuntimeFiles`, `HasEosConfig`) | EGS redist kurmaz, EOSSDK dinamik bağlı; Steam'de exe yanındaki kopya enjekte overlay'lerin runtime'ını gölgeler. 12 dk'lık build'den önce |
| EOSBootstrapper | Plugin dosyayı `BuildPlayer` döndükten sonra async yazıyor (editor döngüsü gerekir; `Builder` senkron, bekleyemez). `StoreFiles.RemoveLateBootstrapper`: EOS paketi kuruluyken Windows build'inden sonra `EditorApplication.update`'te en çok 120 sn bekler, dosya kilidi kalkınca siler, rapora satır ekler | Build akışını beklemeye bölmek de mümkün; template'te build sonucu senkron döner (CLI). Arada kopyalanan klasörde kalabilir: dokümanda |
| SDK paketi kontrolü (2026-11-03, Steam aşaması) | `BuildStores.SdkPackage` (Steam `com.rlabrecque.steamworks.net`, Epic `com.playeveryware.eos`), `BuildEnvironment.InstalledStoreSdks`; paket yoksa mağaza build'i hata alır ve layout kontrolünün player derlemesi atlanır | `GameRootScope` mağaza dallarını taşıyınca paketsiz mağaza kontrolü console'a derleme hatası basıyordu |
| Doğrulama | `StoreFilesTests` (geçici klasör: silme, VC++ kopyası, eksik kanıt, yasak ad, Development notu), `BuildKindsTests` (define setleri), `BuildChecksTests` (Epic ön kontrolü, SDK paketi). Gerçek build: Steam Development (Windows IL2CPP): rapor "Steam SDK in, no other store's files"; Epic build alınmadı (kullanıcı kararı), EOSBootstrapper izleyicisi ve öbür SDK'nın gerçek build'den sökülmesi denenmedi | |

### Mağaza dosyaları incelemesi (bağımsız, git diff üzerinden, 2026-11-04)
| Bulgu | Düzeltme |
|---|---|
| Geç gelen `EOSBootstrapper` kilitli kalırsa (Defender) ya da silinemezse yalnız log; Release raporu "Succeeded" kalıyordu. Domain reload izleyiciyi düşürür; `bootstrapperNameOverride` adı değiştirir | `StoreFiles.ReportLate`: rapora satır, Release'te ilk satır Failed (test). Reload ve ad değişikliği dokümanda |
| Mono build'de `Managed/com.rlabrecque.steamworks.net.dll` (`DISABLESTEAMWORKS` ile boş derlenir, player yükler) "steamworks" yasak parçasına takılıp Epic/mağazasız Release'i düşürüyordu; mesaj "StoreFiles'a ekle" diyerek yüklenen assembly'yi sildirebilirdi | SDK başına `Allowed` (Steamworks managed dll'i, test); mesaj "SDK dosyasıysa ekle, oyunun dosyasıysa kuralı ayarla" |
| Yeni mağaza `_sdkPackages`'a eklenmezse `SdkPackage` `KeyNotFoundException` fırlatıyordu; doküman adımı yoktu | `All` enum'dan, `SdkPackage` `TryGetValue` (null), kontrol "BuildStores lists no SDK package"; dokümanlar |
| Doküman: macOS/Linux'ta "mağazanın dosyaları kontrol edilmez" dar; Epic'in `StreamingAssets/EOS` kontrolü her hedefte | Build.md düzeltildi |
