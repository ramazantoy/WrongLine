# WrongLine (oyun kararları)

Oyunun kararları ve doğrulananları burada; açık işler ve template'e gidenler `GameOpen.md`'de. Framework kararları diğer
dosyalarda; bu dosya template'te yok, bu yüzden cherry-pick'lerle çakışmaz. Karar değişirse satır silinmez; eskisinin üstü
çizilir, yenisi eklenir. Dosya büyürse özellik başına bölünür (`Game.<Özellik>.md`).

## Kimlik ve kaynaklar
| Konu | Karar | Gerekçe |
|---|---|---|
| Amaç | Mağazaya çıkmayan Android oyunu. Repo public GitHub portföyü olur | Portföy: bitmiş bir oyun ve okunabilir kod |
| Platform | Yalnız Android | Kullanıcı kararı. Template Android'de denendi; Web build ekseni template'te yok |
| Oyun fikri | Lunapark rayı gibi spline yol (yokuş, iniş, sağ/sol viraj), ufku büken curve shader, takip kamerası. Ayrıntı oynadıkça netleşir | Kullanıcının fikri; `Design/`'daki GDD ve prototip yalnız gevşek referans |
| Template'in sahada denenmesi | Framework burada yamanmaz. Sorun `GameOpen.md` → "Template'e gidenler"e yazılır, template'te düzeltilir, `git cherry-pick -x` ile alınır. Oyuna özgü kurallar CLAUDE.md'nin başında durur | İki kopya ayrışırsa düzeltme geri taşınamaz; cherry-pick yalnız CLAUDE.md'nin baş bloğuyla çakışır |
| Başlangıç noktası | Template `4cd74e6` ile tree aynı ("Use this template", geçmiş ayrı); `template` remote'unda | Cherry-pick'in temeli |
| Tasarım kaynakları | `Design/` gitignored | Taslak tasarım public repoda koddan farklı şey söyler |

## M0 kararları (2026-11-14)
| Konu | Karar | Gerekçe |
|---|---|---|
| Kimlik | Company `RamazanToy`, Product `WrongLine`, sürüm 0.1.0; identifier `com.ramazantoy.wrongline` (override açık, küçük harf) | persistentDataPath bu adlardan türer, sonradan değişmemeli. Varsayılan `com.RamazanToy.WrongLine` büyük harf taşırdı |
| Ekran yönü | Yalnız dikey (Portrait), autorotate kapalı | Tek elle sürükle-yönlendir; yol ileri doğru uzun görünür |
| Lisans | MIT, kökte `LICENSE`. Public'e geçiş zamanı ayrıca kararlaştırılır | Portföy reposu; en serbest yaygın lisans |
| Aktif hedef | Editor Android hedefinde çalışır | Tek platform; kalite seviyesi ve ayarlar oyuncununkiyle aynı |
| Namespace | `WrongLine`; özellik başına `WrongLine.<Özellik>` (`Intro`, `Menu`, `Driving`). Asmdef adı `Game` kalır | CLAUDE.md yapısı; asmdef adı araçlarca sabit adla bulunur |
| Sahneler ve scope'lar | Intro → `IntroScope` (Manual), Menu → `MenuScope` (Menu), Gameplay → `DrivingScope` (Playing). Sahne adları template'inki | Ad değişimi build listesi + `Scenes.cs` eşlemesi ister, kazancı yok |
| Menü | `MenuScreen` prefab'ı (uGUI + TMP, 1080×1920, match 0.5), Play butonu `UI/menu.play` ile lokalize, `Load<PlayingState>(Gameplay)` | Cihazda sürüş sahnesine ulaşmak için en küçük yol |
| Font | TMP Essential Resources (LiberationSans, OFL; `OFL.txt` yanında) | Lisans kuralına uyar; özel font sonra |
| Android build | M0'da yapılmadı; ilk build `GameOpen.md`'de açık madde | Kullanıcı kararı: Editor doğrulaması M0 için yeterli |

## Doğrulananlar (M0, 2026-11-14)
- Console 0 error, Validate All 0/0. Play: Intro → Menu → (Play) → Gameplay; Menu'den ve Gameplay'den doğrudan Play; console
  temiz. Play etiketi sistem diline göre ("Oyna").
- Check Release: placeholder hatası yok (yalnız kirli ağaç uyarısı).

## Intro (2026-11-14, M1 öncesi)
| Konu | Karar | Gerekçe |
|---|---|---|
| Splash | `Assets/Game/Video/Splash.mp4` (kullanıcının kendi videosu: 1080×1920, 3,4 sn, ses yok). `VideoPlayer` kameranın arka düzleminde, FitInside, kamera arka planı siyah, unscaled zaman, Play On Awake kapalı. `IntroFlow` video **ve** boot bitince Menu'yü yükler; boot adımı hata verirse intro'da kalır. `IntroVideoView` önce `Prepare` eder (6 sn'de hazır olmazsa uyarı ve geçilir), sonra klibin süresi kadar unscaled bekler; player event'i dinlenmez | Kullanıcı isteği, kullanıcının önceki çalışmasından birebir. Decoder açılışı ilk kareleri takıltabildiği için süre `Play()`'den sayılır. Ses kanalı yok, mixer/AudioSource kuralına girmiyor |
| Testler | `Game.Tests` asmdef'i bu işle açıldı (`WrongLine.Tests`, Editor, `UNITY_INCLUDE_TESTS`); `Game` internal'ları `AssemblyInfo.cs` ile açar. Ortak test sahteleri `TestDoubles.cs`'te (`FakeSceneLoader`) | `IntroFlow` internal |

Doğrulananlar: `IntroFlowTests` 3/3; `IntroFlow` boot'u beklemeyecek şekilde bozulunca 2'si kırıldı. Validate All 0/0. Play
Intro'dan: video süresince Intro, sonra Menu; console temiz.

## M1 kararları (2026-11-16, plan yazılmadan önce konuşuldu)
Plan onayında kesinleşir; plan çelişirse buraya yeni satır eklenir, eskinin üstü çizilir.

| Konu | Karar | Gerekçe |
|---|---|---|
| Kapsam | M1 yalnız yol: sonsuz, prosedürel, devir daimli. Araç ve oyuncu kontrolü yok; görünür bir dummy follower (küp/kapsül) yol üzerinde `s` boyunca config'teki sabit hızla ilerler, basit arkadan takip kamerası izler, debug bilgisi gösterilir | Önce yol sistemi tamamlanır; araç ve kontrol bağlamı şişirmesin |
| "Oynanabilir biter" kuralı | M1 için "dummy follower sonsuz yolda sorunsuz ilerler" olarak okunur; kural M1'de esnetildi. Milestone sıralaması M serisine bağlı kalmak zorunda değil | Kullanıcı kararı |
| Yol üretimi | Runtime'da mesh üretilmez. Parçalar Editor'da parametrik (eğim, viraj açısı ve yarıçapı, uzunluk, bank) üretilip prefab olur; runtime'da kuralla yapboz gibi birleştirilir | Mesh üretimi ve GC mobilde pahalı; parça sanatsal olarak kontrol edilir, hand-paint atlas doğrudan oturur |
| Spline | Unity Splines paketi. Her parçanın kendi spline'ı prefab'ının içindedir; tek sonsuz uzayan spline yok. Parça yerel koordinatta yaşar, giriş ve çıkış bağlantısı (konum, teğet, up) vardır | Sonsuz `s` hassasiyeti bozar; havuz ve floating origin ile doğal uyuşur |
| Birleştirici | Saf C#, seed'li, `IRoadPath` arayüzünün arkasında; Splines yalnız adaptörde. Parça seçip zincirler (sıralama kuralları, çeşitlilik eğrisi). Aynı seed aynı yolu üretir | Kurallar saf C#; EditMode'da test edilir |
| Başlangıç parçası | Birleştiricinin dışında, sabit düz parça; ilk bağlantı durumunu o verir, birleştirici çıkışından devam eder. Eğim ve viraj kuralları ona uygulanmaz | Oyun başı eğimli olmamalı; yeniden başlatma aynı yapıyla çalışır |
| Float hassasiyeti | Floating origin (ya da parça yerel koordinatı) planın baştan kararıdır; öneri floating origin | Sonradan eklemek zor |
| Parça doğrulama | Editor validator (`IValidator`): bağlantı uyumu, etiketler, up sürekliliği; yanlış parça oyuna giremez | Zincir hatalarını erken yakalar |
| Yol görünümü | "Normal" yol (asfalt, şerit, kaldırım), kalınlığı vardır, altı sis ya da boşluk. Lunapark hissi geometri ve kameradan gelir. Arcade, gerçek fizik yok | Kullanıcı kararı |
| Yol mesh'i UV | `s` boyunca akar, genişlik 0–1 | M1b'de tekrar eden yol texture'ı mesh koduna dokunmadan oturur |
| Havuz | Parçalar root scope'ta kalıcı havuzda yaşar (gameplay'ler arası yok edilmez), ön ısıtma Menu'de, kapalı kök objenin altında beklerler. `IPools.For` root'ta Scoped olduğundan yetmez: önce oyunun kendi root singleton'ı, genelleşirse template'e taşınır | Kullanıcı kararı; her oyunda destroy/yeniden yaratma olmasın |
| Havuz kuralları | Havuza dönen parça `Release` içinde kapatılır ve sıfırlanır (çağıran hatırlamaz, EditMode testi); sahneye bağlı referans tutmaz; paylaşılan materyale runtime'da yazılmaz; yaratma `IObjectResolver.Instantiate`; ön ısıtma adedi config'de | Havuzdaki parçanın yanlışlıkla aktif ya da kirli kalması tek gerçek risk |
| Alt aşamalar | M1a: sözleşme (bağlantılar, ölçüler, pivotlar, UV), Editor parça üretici, gri kutu parçalar, validator, birleştirici, havuz, dummy follower, kamera, testler; placeholder'la tek başına biter. M1b: sözleşmeye uyan gerçek low-poly hand-paint parçalar (Blender tabanlı ayrı asset akışı); yol tarifi tek kaynaktır, Unity üretir, Blender o tarifi okur | Asset bekleyen milestone açılmaz kuralı; kullanıcı gerçek görünümü görmek istiyor |
| Kapsam dışı | Araç, oyuncu kontrolü, curve shader, yol kenarı dekoru, engeller sonraki milestone'larda | Kullanıcı kararı |
| Parça tipleri | Düz, viraj, yokuş yukarı/aşağı, bank, up sürekliliği; uç parçalar (dik düşüş, loop) en sona, çözülemezse M2'ye ertelenir. Başlangıç önerisi ~10 tip, tip başına 2–3 varyant | Up vektörü ve kamera uçlarda zorlanır; milestone bitmez hale gelmesin |
| Referans asset | Satın alınmış referans paketler yalnız yerelde (repo dışı, gitignore'lu klasör), repoya girmez. Kendi tasarımımız olacak; mesh, UV ve texture kopyalanmaz | Repo public olacak; yalnız CC0 ya da kendi işimiz |
