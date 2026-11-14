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
