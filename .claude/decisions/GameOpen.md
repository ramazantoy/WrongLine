# WrongLine (açık işler)

Biten madde silinmez, üstü çizilir; kalıcı sonucu `Game.md`'ye taşınır.

## Açık
- ~~**Ekran yönü:** dikey mi yatay mı, M0'da karar verilir.~~ Dikey (2026-11-14, `Game.md` M0 kararları).
- ~~**Lisans ve görünürlük:** public'e ne zaman geçileceği ve lisans (MIT önerisi) M0'da kararlaştırılır.~~ MIT eklendi
  (2026-11-14, `Game.md`); görünürlük aşağıda.
- **Public'e geçiş:** proje hazır olunca (oyun bitince) açılır, o zamana kadar private (2026-11-16, kullanıcı kararı).
- **İlk Android build:** `AF/Build` Development, cihazda açılış, dikey ekran, Menu → Gameplay; `adb logcat -s Unity`. Template'in
  "İlk mobil oyunda doğrulanacaklar" listesi (`decisions/README.md`) bununla birlikte. İlk build'in yeniden yazdığı URP
  asset'leri ayrı commit.
- ~~**M1b gerçek yol parçaları**~~ Yapıldı (2026-11-17, `Game.md` M2 kararları): 33 parça sanatçıdan geldi, hepsi sözleşme
  ölçümünden geçti, oyunda. **Kalan:** araç (aşağıdaki "Araç" maddesi) ve CurveEasy/BankEasy/CurveWide'ın 2 m halka aralığıyla
  yeniden üretimi (3840 üçgen çok; sanatçıdan istendi, araçtan sonra).
- ~~**Uç yol parçaları:** dik düşüş, loop; up vektörü ve kamera zorlanırsa M1'de çözülmez, M2'ye ertelenir.~~ Dik düşüş ve
  dik tırmanış yapıldı (`SteepDown_S/L` −60°/−70°, `SteepUp_S/L` +60°/+70°; yarıçap ≥ 12 m, katalogda 26 parça). **Loop
  M2'ye ertelendi** (tasarım çözümlenip uygulanmadı); neden:
  1. *Yön temsili:* pitch 90°'yi geçince `forward.z` negatifleşir; `RoadPieceInfo`'nun yaw bandı `atan2(forward)`'tan
     ölçülüyor, loop parçası bandı hep ihlal eder. Yaw'ın tarif değerinden (ψ) ölçülmesi gerekir.
  2. *Kendini kesmeme:* "z hep artar" kanıtı loop içinde geçersiz (arka bacak z'de geri gider). Giriş/çıkışın kesişmemesi
     için loop'a yanal kayma (vida) gerekir; yol genişliği 10,2 m, yani en az ~12 m yanal kayma ve yeni bir doğrulama.
  3. *Kamera:* `ChaseCameraRule` up'ı `Slerp(dünya up, hedef up)` ile karıştırır; loop tepesinde iki vektör zıt olur ve
     Slerp tanımsızdır. Loop için ayrı up kuralı (ör. loop boyunca hedef up'a kilitlenmek) gerekir.
  4. *Doğrulama:* en dar yarıçap (≈ 8 m için ≥ 100 m'lik loop) ve up dönüş sınırı loop'a özel olmalı; `Intensity` ve
     yükseklik bandı (loop ≈ 2R yüksek) yeniden düşünülmeli.
  Çözüm yolu: yeni `PitchProfile.Loop` (tek yönlü tam devir, `θ(u) = 360° (u − sin 2πu / 2π)`) + vida için yaw profili.
- ~~**M1 elle denemesinden (2026-11-16, kullanıcı):**~~ Curve shader, sis ve dağıtım ince ayarı M2'de yapıldı
  (`Game.md` M2 kararları, 2026-11-17); kamera maddesi ayrı ("Keskin virajlarda ileriyi görmek zor"). Eski not: kamera fena değil, M1 için yol gayet iyi. İleri fazlar için: (1) **curve
  shader** gerekli görünüyor (ufuk bükülmesi); (2) **yol "doğal" hissi bir tık eksik**: oluşan yola bakınca dağıtım
  algoritmasının izi görülüyor (ağırlıklar, `RepeatWindow`, bantlar: `RoadGeneratorSettings`; parça tipleri ve boyları da
  tekrarı belli ediyor); (3) bunun bir kısmı **sis (fog)** ile ilgili: yolun uzakta bitişi ve parça ritmi görünüyor. Bir
  sonraki milestone'da önce fog + curve shader, sonra dağıtımın ince ayarı (daha uzun yumuşak virajlar, parça uzunluğu
  çeşitliliği, ardışık parça "motifleri") denenir.
- **Sonraki milestone adayları (M2 sonrası, 2026-11-17):** araç Unity'de (placeholder küpün yerine, sonra oyuncu kontrolü:
  `x` sürükleme, arcade, bankta görsel yatma), kamera cilası (keskin virajda ileriyi görme, hıza bağlı FOV, Cinemachine
  kararı), takip glitch'i, trafik, yol kenarı dekoru ve engeller, kazada parça fırlatma. Sırası M2
  oynanınca yazılır.
- ~~**Kamera: Cinemachine mi? (2026-11-16, kullanıcı önerisi, karar M3'te):** takip kamerası şimdi saf C# `ChaseCameraRule` +
  `ChaseCameraDriver`; kullanıcı yönetimin basitleşmesi ve hızla FOV'un (hızlanmada açılma) bedavaya gelmesi için Cinemachine
  3'e geçmeyi öneriyor. Değerlendirme: FOV hıza bağlama Cinemachine'siz de tek satır (`ChaseCameraRule`'a `Fov(hız)` +
  config eğrisi, testli). Cinemachine'in asıl kazancı sarsıntı/blend/ek kamera; bedeli yeni paket (onay ister), floating origin
  kaymasında `OnTargetObjectWarped` bağı, `UpBlend` (bank'a yatma) kuralının Cinemachine'e taşınması ve "kurallar saf C#"
  ilkesinden sapma. Karar M3'te (araç + oyuncu kontrolü + hıza bağlı FOV ihtiyacı netleşince): önce `Fov(hız)`'ı kural olarak
  yaz, sonra Cinemachine'in neyi kolaylaştırdığına bak. M2'de kamera değişmez.~~ Hayır (Faz A, 2026-11-17, `Game.md` Faz A kararları): FOV hıza bağlama kural olarak yazıldı, kamera yol uzayında.
- ~~**Keskin virajlarda ileriyi görmek zor (2026-11-16, M2 elle denemesi, kullanıcı):** kamera hep aracın baktığı yöne
  bakıyor, viraj dönmeden önce yolun devamı görünmüyor (özellikle CurveSharp/BankSharp). Çözüm adayı kamera cilasında
  (M3, Cinemachine kararıyla birlikte): bakış noktası aracın önündeki yol üzerinde (`s + LookAhead` noktası, yani yol
  eğrisini izleyen hedef; şimdi düz `LookAhead` metre ileri), kamerayı viraj yönüne doğru yaw kaydırma, viraja girerken hafif
  FOV açma. Ölçü: viraj girişinde viraj çıkışı ekranın içinde kalmalı.~~ Yapıldı (Faz A): bakış yoldaki `s + 30 m` noktasına, araç kadrajda kalacak şekilde kenetli; viraj çıkışı giriş anında kadrajda, testle ölçüldü.
- **Araç cilası (2026-11-17, araç Unity'de placeholder olarak):** (1) FBX import uyarısı: "A polygon of Mesh 'Body' … is
  self-intersecting and has been discarded" (küçük delik olabilir, sanatçıya iletildi); (2) gölge: yakın araç gölgesi pikselli ve
  spoiler'da kendi gölgesinden "diş" deseni (shadow acne): ışığın bias ve normal bias ayarı ya da gölge çözünürlüğü, mobil
  gölge kalitesi kararıyla birlikte; (3) ~~cam opak~~ yarı saydam yapıldı (2026-11-17); iç mekân kabuğu eksik: iç mekânda yalnız gösterge paneli ve koltuklar var, tavan, zemin ve kapı iç panelleri yok (gövdenin iç yüzü çift taraflı çizimle görünüyor, sanatçıdan gerçek iç yüzeyler istenecek); (4)
  far/fren lambası emission maskesi geçici `Car_Emission.png` ile bağlandı (Faz A3), ayrı kanallı `Car_LightMask.png` gelecek; (5) kapının arka yüzü tek katman (kopunca culling'le görünmez: çift taraflı
  materyal ya da ince kalınlık); (6) araç başına boya rengi (`_PaintColor`, `MaterialPropertyBlock`); (7) kazada kopan
  parçaların ayrılması (hiyerarşi hazır: Hinge_* child'ları, kütle merkezli pivotlar).
- **Yollar 4 şeritli olmalı (2026-11-17, kullanıcı):** **Kod yapıldı** (Faz A2, `Game.md`): `RoadSection` 4 × 3 m, `section.json`, ölçüm, `TurnSignalRule`, `LateralLimit`, başlangıç şeridi, testler. **Sanatçı 33 parçayı 4 şeritli teslim etti (2026-11-17)**: Rebuild + Export koşuldu, hepsi sözleşme ölçümünden (çizgi UV'leri dahil) geçti, Validate 0 hata 0 uyarı. Mobil üçgen yükü ve oyunda görünüm henüz ölçülmedi/denenmedi.
- ~~**Gün-gece her oyunda aynı saatte başlıyor (2026-11-17, kullanıcı):**~~ Yapıldı (Faz A2): başlangıç fazı koşu seed'inden, config'te kapatılabilir (`Game.md`).
- **Yolun doğuşu hâlâ görülüyor (2026-11-17, kullanıcı: "ileri bakınca fog tam o etkiyi vermemiş"):** cookie/ışık maskesi sise etki etmez. İki neden ölçüldü (`Game.md` Faz A2): (1) ufuk altı sis rengi uyumsuzluğu: **düzeltildi** (`HorizonHold`), his kullanıcıda; (2) viraj yüzünden bazı parçalar sis tamamlanmadan doğuyor (kadrajdakilerin %6'sında sis < 0,8, en az 0,61). Pencereyi "kameraya düz uzaklık ≥ sis sonu + pay" olacak şekilde uzatmak bunu bitirir (en kötü ~580 m yol, üçgen +~%45, ortalamada çok daha az); **karar kullanıcıda**: (1) sonrası yeterli mi, değilse dinamik pencere (üst sınır config'te, üçgen bütçesi ölçülür) ya da sisi yakınlaştırmak (sis sonu 360 → ~300).
- ~~**Gece far yanmıyor (2026-11-17, kullanıcı; sıradaki faz "A3: ışıklar"):**~~ Yapıldı (Faz A3, `Game.md`): sokak lambaları emission + sahte ışık havuzu, araçta gerçek far (Spot) ve sinyal (Point), `LightSwitchRule`, HDR kapalı + gece bloom'u. **Kalan (araç görseli fazında):** `Car_LightMask.png` (far/stop/sinyal ayrı kanal; şimdi `Car_Emission.png` tek blok: far ve stop birlikte yanıyor, fren lambası ayrı yanmaz), `CurvedLit` maskesi `(0,1,0,0)` yerine kanal başına seviye, stop ışığı (gerçek ya da emission).
- **Işıklar (Faz A3) açıkları:** (1) alacakaranlık geçişinin hissi (eşikler, `FadeSeconds`, `Darkness` eğrisi) ve far huzmesinin uzunluğu/şiddeti (`LightsConfig`; şimdi 60 yoğunluk, 45 m) kullanıcı gözüyle; (2) Android'de ek ışık (cluster yok, per-pixel Forward) ve bloom maliyeti, "Mobil yük" ile birlikte; (3) sinyalin gerçek sürüşte denenmesi; (4) `SceneryCatalogValidator` lamba başı kontrolü yok (başın modelin sınırları içinde olması); (5) `ScenerySpecs`'te lamba başı yok: katalog baştan üretilirse `_lightHead` elle yazılır; (6) hızlanma hissi post-process'i (kullanıcı planı: radial blur, vignette, FOV ile birlikte) ayrı volume olarak, ayrı iş.
- ~~**Araç mesh'leri havada duruyor (2026-11-17, kullanıcı notu):**~~ Hata değil, bilinçli (Faz A2, `Game.md`): ölçüldü, tekerlek tabanı kökte y = 0; yalnız prefab modunda zemin olmadığı için havada görünüyor, Play'de düzgün.
- **Cinemachine (2026-11-17, kullanıcı kararı):** kaza milestone'unda eklenir (Impulse, kaza kamerası blend'i); şartları `Game.md` Faz A2 kararlarında (kural saf C# kalır, Brain ManualUpdate).
- ~~**Araç v2: baştan çizim (2026-11-18, kullanıcı kararı, brif GÖNDERİLDİ `my3dartist-c0`):**~~ (iptal: 2026-11-19'da Meshy ile yapıldı, `Game.md` "Araç v3") Eski brif özeti: trafik araçları oyuncununkinden daha başarılı çıktı; oyuncu aracı sıfırdan, trafik v2 kalitesinde, kompakt sportif hatch silüetinde yeniden modellenir (aşağıdaki ertelenmiş blokaj iptal, "son faza" kararı kalktı). Brif yerelde `.claude/notes/car-v2/brief.md`: FBX sözleşmesi aynı düğüm adları (`CarWreck` sabit adlar), 2048² atlas, `Car_Base`/`Car_PaintMask`/`Car_LightMask` (R far, G stop, B sol, A sağ), tek LOD ~3500–4500 üçgen, önce blokaj turu. Teslimde Unity işi: `Car.prefab` lamba küpleri ve 6 ışığın yeri, `_steeringAxis` → (0,0,1), LightMask'tan emission üretimi (`TrafficBuilder` deseni), ölçü değişirse `CrashSettings`/`CarConfig`/`PlayerLengthMeters`.
- **Araç v3 (Meshy) açıkları (2026-11-19):** (1) 124k üçgenli oyuncu aracının Android maliyeti ve gölge geçişi ölçülmedi (cihaz yok); gerekirse 45k'lık sürüm (Meshy'den yeniden `remesh`, 5 kredi); (2) direksiyon yok, kapı ve kaput kopmaz, cam opak, boya sabit kırmızı, fren lambası ayrı yanmaz (`Game.md` "Açıklar"); (3) çarpışma kutusu (`CrashSettings`), `CarConfig` ve `PlayerLengthMeters` yeni ölçüye (2,01 × 4,20 m) göre doğrulanmadı; (4) parlama gücü (`_EmissionColor` 1,6) ve gece görünümü kullanıcı gözüyle; (5) marka: form Mazda 3 MPS'e yakın, amblemler silindi, public öncesi karar; (6) pişirme script'i (`.claude/scripts/CarMeshyBuild.cs`) Editor menü aracına taşınabilir.
- **Trafik araçları Meshy ile, LOD'lu (2026-11-19, kullanıcı fikri; AYRI CONTEXT):** dört model (Hatchback, Sedan, Van, Truck; `TrafficBuilder`) Meshy ile yeniden yapılır, her biri üç LOD. Taslak ve ilk adımlar yerelde `.claude/notes/traffic-meshy/handoff.md` (tarif: oyuncu aracındaki hat; boya için gövdeyi ayırt edici düz bir anahtar renkle ürettirip dokudan `PaintMask` çıkarmak; LOD'lar `meshy_remesh` ile, her LOD kendi küçük dokusuyla; paylaşılan tekerlek; ışık maskesi `LightMaskKey` ile). Başlamadan: plan modu, bütçe (~65 kredi/model) ve trafik üçgen bütçesi.
- ~~**Araç görseli SON FAZA ERTELENDİ (2026-11-17, kullanıcı kararı):**~~ (iptal, yukarıya bak) sanatçı durduruldu. Eldeki blokaj (düzeltme turu, 4668 üçgen, ekran görüntüleri sanatçının `My3DArtist/assets/props/car_sport/shots/blk4_*.png`) temiz ama boyasız; Unity'deki araç eski haliyle kalır (placeholder lamba küpleri dahil). Dönüldüğünde: üç küçük düzeltme (ön tampon köşesi kütlesi, taşan kapı kolları, stop köşesi kıvrımı), sonra atlası Krita köprüsüyle boyama (kurulum `MyVideoDesigner/.claude/notes/krita-el-cizimi.md`, köprü eklentisi kurulum izni kullanıcıdan istenir), `Car_LightMask.png`, FBX'i Unity'ye alıp direksiyon ekseni ve hiyerarşi kontrolü, lamba küplerinin yenisiyle değişimi. Gece farı (Faz A3) araç maskesine bağlı olduğundan o zamana kadar bekler ya da `Car_Emission.png` ile geçici yapılır.
- ~~**Sanatçı brifi (GÖNDERİLDİ 2026-11-17, oturum `my3dartist-60`):**~~ (araç kısmı iptal, Meshy; yol kısmı teslim edildi ve alındı) Eski özet: yol kısmı TESLİM EDİLDİ ve alındı; araç kısmı sürüyor: sanatçı önce blokaj/ekran görüntüsü turunu gönderecek, onaysız atlas ve final export yok.** Gönderilen metin (özet): **0) YOL:** kesit 4 × 3 m (asfalt 12 m, yarı genişlik 6 m), üç şerit çizgisi −3, 0, +3 m (0,15 m, +2 cm), kaldırım/gövde/UV/submesh sırası aynı; 33 parça yeni `section.json` ile; CurveEasy, BankEasy, CurveWide halka aralığı 2 m; asfalt texture'ı 3 şeride boyanmış iz içeriyorsa 4 şeride göre. **ARAÇ (kullanıcı: "hâlâ dandik, daha detaylı"), taban model kalır, üstüne detay, bütçe ~2400 → ~4500 üçgen:** gerçek lamba geometrisi (far yuvası: reflektör + cam, stop grubu, 4 ayrı sinyal lambası; yerlerindeki placeholder küpler bunların yerine geçer), gövde detayı (ön ızgara, hava girişleri, plaka yuvası, egzoz ucu, kapı kolları, cam çerçevesi trim'i, marşpiyel, derz çizgileri), tekerlek (jant kolları, lastik yanağı pahı, fren kaliper izi atlas'ta), atlas el boyaması (gerekirse 2048), önce blokaj/ekran görüntüsü turu onaya gelir. Eski madde listesi: (1) 4 amber sinyal lambası (ön far panelinin dış köşesi, arka stop grubunun dışı), sol ve sağ ayrı UV adası, ≥ 4 px boşluk, `Car_Base.png`'de sönük amber; (2) `Car_LightMask.png` (atlasla aynı boyut, sRGB kapalı): R far, G stop/fren, B sol sinyal, A sağ sinyal, gerisi siyah (şimdiki `Car_Emission.png` far ve freni tek blokta tutuyor); (3) iç mekân kabuğu: tavan, taban, kapı iç panelleri gerçek iç yüzey (+~200 üçgen), gövdenin çift taraflı çizimi kalksın; (4) direksiyon pivotu göbek merkezinde, yerel +Z kolon ekseninde (şimdi kolon ekseni (0; 0,417; 0,909), kodda); (5) obje adları, hiyerarşi ve atlas boyutu aynı. Asset gelince: placeholder lamba küpleri (`Car.prefab`, `SignalLamp.mat`) silinir, `CarView` lambaları shader'a bağlar.
- **Trafik araçları için LOD (2026-11-17, kullanıcı):** yol ve sis uzaklığı 300 → 400 m'ye çıktı (sis 100–360 m), trafik
  araçları artık daha uzaktan görünür. Araç (≈2400 üçgen, 12+ parça) uzakta pahalı: trafik sistemi gelince araç başına LOD
  ya da uzak mesafe sadeleştirmesi kurulur. Öneri: `LOD0` tam model (kopan parçalarla), `LOD1` (~35 m'den, ~600 üçgen,
  parçalar gövdeye birleşik, tek mesh), `LOD2` (~120 m'den, küp/kapsül silüeti ya da ~150 üçgen), sis bitişinden önce
  `LOD2`'ye düşmüş olmalı. Seçim saf C# kural (`mesafe → LOD`, histerezisle titremesiz) + view; `LODGroup` ya da elle
  geçiş kararı trafik milestone'unda; sanatçıdan LOD brifi o zaman. Kaza anında yalnız yakın araç (`LOD0`) parçalarına ayrılır.
  **Trafik brifi gönderildi (2026-11-17, oturum `my3dartist-ac`), teslim edildi ve Unity'de (`Game.md` M3):** 4 model
  (hatchback, sedan, van, kamyon) × 3 LOD, tek ortak atlas, `Traffic_LightMask` kanal düzeni.
- **Trafik (M3) açıkları (2026-11-17):** (1) **Android ölçümü:** ilk build henüz yok (yukarıdaki "İlk Android build"); trafik
  bütçesi (aktif ~12–15 araç, 3–9 bin üçgen) ve draw call mobilde ölçülür; `FrameStatsLog` planlandı, yazılmadı. (2) Sanatçıdan
  istenebilecekler: ayna, van/kamyon yan camı, LOD1'de van/kamyon yan yüzündeki ince üçgen şeritleri (gölgede çizgi), sinyal
  bölgeleri köşede L biçimli, far/stop kanalları lens gradyanı taşıyor. (3) Gece görünümü (far/stop emission, `Traffic_Emission.png`) ve trafik sinyali (artık var, `Game.md` M3 eki) kullanıcı gözüyle;
  lamba yerleri sedanın önünde, van ve kamyonda köşe varsayılanı (maskede bulunamadı): sanatçıdan far/stop/sinyal bölgelerinin
  gövdeye bağlı olması istenebilir. Shader'a kanal başına emission seviyesi gelirse `Car_LightMask` doğrudan kullanılır. (4) Tekerlek dönmüyor (LOD0 tekerlekleri hazır, kural yok). (5) Yoğunluk/his ayarı (`TrafficConfig`: boşluk, şerit
  hızları) kullanıcı gözüyle; her şerit tek hızda; araç başına hız farkı σ modelini bozar (aralık parça parça hesaplanmalı),
  sonraki iş. Trafiğin şerit değiştirmesi yapıldı (`Game.md` M3 eki), sıklığı (`LaneChange.Chance`) kullanıcı gözüyle. (6) Kaza milestone'u `ITrafficFrame`'i okur; çarpışma, puan, Cinemachine orada.
  (7) Bozma kontrolü yalnız planlayıcı, doluluk, LOD kuralı ve view'da yapıldı; hız profili, koridor, koşu ve ayar testleri için
  henüz yok. (8) Origin kaymasında araç ve draw call sayısı Play'de ölçülmedi.
- **M3 öncesi ayrı context'ler (2026-11-17, kullanıcı planı; her biri kendi planıyla):**
  1. ~~**Araç kontrolü** (`x` sürükleme, arcade, bankta yatma)~~ yapıldı (Faz A) ve **şerit değiştirirken sinyal** (kural ve placeholder lamba yapıldı; atlas ve maske sanatçıda, aşağıdaki "Sanatçı brifi"): `Car_Emission` maskesinde şimdi
     yalnız far ve fren lambası var; sinyal için atlasta sağ ve sol sinyal bölgeleri gerekir (sanatçıdan atlas ve maske
     güncellemesi), sinyal saf C# kuralı (şerit değişiminin başlaması → yanıp sönme zamanlaması) + emission view'ı.
  2. ~~**Araç titremesi (jitter):** kullanıcı gözlemi: araç hafifçe titriyor, "özellikle sol click'lerde" (ifade olduğu gibi
     kaydedildi, ne demek istediği o context'te netleşecek: sol virajlar mı, Editor'da sol tık mı). Aşağıdaki follower glitch
     maddesiyle birlikte ele alınır; kamera `PostLateTick`, follower `Tick` sırası, `ChaseCameraRule` üstel yaklaşma ve kare
     başına örnekleme ve floating origin kayması ilk şüpheliler.~~ Çözüldü (Faz A): kök kamera gecikmesiydi.
- ~~**Takip (spline follower) bazı yollarda glitch atıyor (2026-11-17, kullanıcı, M2 elle denemesi):** M1'de yoktu; M2'de
  eklenenler: gerçek sanatçı mesh'leri, curve shader (köşe bükme), gece/gündüz ve sis, motifler ve yeni parçalar. "Bazı
  yollarda" yani seed/parçaya bağlı. Henüz araştırılmadı; kullanıcı kararı: gerçek araç gelince ele alınır. İlk bakılacaklar:
  (1) follower `RoadCursor`'ın parça çerçeve tablosundan konum alıyor, sanatçı mesh'i ile tarif aynı merkez hattında mı
  (uçlarda 1 cm ölçüldü, ortada değil), (2) curve shader'ın bükmesi yalnız görüntüyü değiştirir, follower'ın konumunu değil:
  araç yol yüzeyinden ayrık görünebilir, (3) floating origin kayması karesinde follower/kamera/parça yerleşimi, (4) yeni
  parçalarla yaw/yükseklik bandında geçişler. Tekrarlamak için seed + kare numarası kaydedilmeli.~~ Kök bulundu (Faz A): kamera gecikmesi kare süresine bağlıydı, yol uzayı kamerayla çözüldü; follower, curve shader ve origin kayması temiz çıktı. Kullanıcı gözüyle son teyit bekliyor.
- **Yol şerit çizgisi (2026-11-16, kullanıcı):** şeritler şimdi düz sürekli çizgi; kesik kesik olmalı. Yaklaşım: çizgi
  texture'ına alfa ile kesik desen (9 m'lik karo: ~3 m çizgi, ~6 m boşluk), `CurvedLit`'e alfa kesme (`_Cutoff`), mesh ve UV
  sözleşmesi değişmez. Sanatçıdan yeni `Road_Line_Base.png` (alfa kanallı) gerekir.
- **Araç (M3 hazırlığı, 2026-11-16):** stil referansı yerelde duran satın alınmış bir araç paketi (~2900 poligon: yuvarlak hatlı kompakt 4 kapılı araba, büyük
  tekerlek ve çamurluk kemeri, açılı far panelleri, spoiler). Doğrudan ona yakın
  istenir, mesh sıfırdan. Ayrı parçalı: gövde, kapı, tampon, 4 tekerlek; tek materyal. İlk teslim (kama coupe) reddedildi. Brif yerel
  `.claude/notes/m1b/vehicle-brief.md`, sanatçıya gönderildi; Unity'ye M3'te bağlanır. Boya: gövdede tek renklendirilebilir
  boya alanı (GDD'deki araç başına renk için), `MaterialPropertyBlock` M3'te.
  **Kazada kopan parçalar (kullanıcı):** 4 kapı, kaput, bagaj, 2 tampon, 4 tekerlek, 2 ayna, spoiler, direksiyon ayrı obje ve
  kendi pivotuyla; Body kabuk, Interior sabit; cam yarı saydam ayrı materyal; kaput/bagaj altında koyu boşluk kapağı.
  Bütçe ~2400 üçgen. Fırlatma mantığı (parçaları ayırıp fizik/tween ile savurma, havuz) kaza milestone'unda; saf C# kural
  + view ayrımı.
- ~~**Kalıcı havuz:** parça havuzu önce oyunun kendi root singleton'ı olur; `IPools`'a root-lifetime modu gerekiyorsa
  "Template'e gidenler"e taşınır.~~ Template'e gidecek bir şey yok: root singleton `IPools`'tan root'un havuzunu alır
  (2026-11-16, `Game.md` M1 kararları "Havuz").
- **Ortam (Faz B, 2026-11-17) açıkları:**
  1. **Zeminin ucu görünüyor:** zemin 80 m'de bitiyor, iç virajda da kesiliyor; arkasında gökyüzünün ufuk altı rengi var ve
     zeminle uyuşmuyor (Play'de açık şerit olarak görüldü). Öneri: `SkyGradient`'in ufuk altı rengini biyomun zemin rengine
     (ışık ve sisle aynı tonda) bağlamak; olmazsa zemini 150 m'ye uzatmak (üçgen +~%30). Karar kullanıcının gözünden sonra.
  2. ~~**Sanatçı teslimi:**~~ tam set geldi ve oyunda (2026-11-17, `Game.md` Faz B eki). Eski not: brif (`.claude/notes/env/brief.md`, `my3dartist-33`): önce `Meadow_TreeRound_01` +
     `Shared_StreetLamp` denemesi, sonra 4 biyom ve `Ground_Detail.png`. Gelince: `Rebuild Props` + `Rebuild Pieces`
     (zemin dokusu), eksen/pivot/ölçek kontrolü.
  3. **Mobil yük:** aktif ~100 prop + zemin ~6000 üçgen; ağaç gölgeleri açık. Android'de ölçülür (ilk build ile).
  4. **Sonraki ortam fikirleri:** biyoma göre gökyüzü/sis tonu, uzak siluet (dağ, kanyon, tepe kartları), biyom başına prop
     yoğunluğu ayarı oynanınca.

- **Kaza milestone'u (K1 + K1b kodu bitti 2026-11-18, `Game.md` K1/K1b kararları; kullanıcı gözü ve commit bekliyor):**
  - ~~**K1** çarpışma, sürtme, kaza, parçalanma, game over~~ ve ~~**K1b** zamanla hızlanma (eski K3)~~ yapıldı.
  - **Kalanlar (K1):** (1) **Android ölçüm build'i yapılamadı** (cihaz bağlı değildi, `adb devices` boş): bağlanınca "İlk Android
    build" + `FrameStatsLog` (planlandı, yazılmadı) + kaza anının (16 rigidbody, zemin collider'ları, yavaş çekim) maliyeti;
    (2) sürtmenin Play'de görülmesi (yalnız testle; hitbox 0,92 ölçek ve 0,35 m eşiği his işi); (3) kaza hissi: menteşeler Play'de
    hemen koptu (`CrashSettings.Wreck.BreakForce` 3000 düşük kalıyor olabilir), parça ve gövde kütleleri, fırlatma gücü, kaza
    kamerasının dönüşü; (4) **puan/kombo** yok (`CarSideswiped.Count` mesajı hazır; sürtme cezası ayrı karar); (5) Menü
    butonu Play'de denenmedi; game over ekranı dummy; (6) `TrafficCatalog.Entry`'ye model genişliği yazılmadı:
    `TrafficModelSpec.HalfWidth` hepsinde 0,9 varsayılanı, `Rebuild Traffic` FBX sınırlarından yazmalı; (7) kaza noktasında
    parçalar yola oturuyor ama curve shader yolu görüntüde büker, fizik gerçek yolda (uzaktaki enkaz için fark ölçülmedi);
    (8) donan araca aynı şeritte arkadan gelen trafik yavaş çekimde enkaza girebilir (nadir; görülürse o şeridin araçları da
    durdurulur); (9) kaza kamerası (enkazı izleyip ileri geri gider) kullanıcı gözüyle; (10) sürüş kamerasında "aracı hep görünür
    tutma" için mesafe kısaltma (dolly) K2 kamera işine madde (bugünkü kenetler aracı zaten kadrajda tutuyor).
  - **K1b kalanları:** 0,07 m/sn² kazanç (30 → 42 m/sn ~3 dk) hissi ve üst hıza Play'de ulaşma; `ChaseCameraConfig` FOV'u (76°) ve
    sonrasında hızlanma hissi post-process'i (Işıklar açıkları 6).
  - ~~**K2** Cinemachine, yolda kayan dolly, Impulse, kaza vcam'i~~ kodu bitti (2026-11-18, `Game.md` K2 kararları, commit ve kullanıcı
    gözü bekliyor). **Kalanlar (K2):** (1) sarsıntı gücü (0,4 / 2,5), blend süresi (0,5 sn) ve kaza FOV'u (66°) his işi, FOV değişince
    enkazın görüntüdeki yeri Play'de yeniden ölçülmeli; (2) kalkışta kameradan ~3 m açılma (dolly filtresi `VelocityTime` 0,4) hoş
    mu; (3) aralık yumuşak sınır (≤ ~0,8 m aşar): sert sınır istenirse ayrı karar; (4) `RotationComposer` kullanılmadı (kenet ve
    `UpBlend` kuralda): ileride composer'ın dead zone'u istenirse bakış hedefi ayrı vcam'e; (5) Cinemachine'in `Noise` (hızda titreşim)
    kullanılmadı; (6) Android'de Brain ve Impulse maliyeti ölçülmedi.

## Template'e gidenler
- **Play açıkken script derlenince root scope iki kez kurulur (2026-11-16):** Editor'ın "Script Changes While Playing"
  tercihi varsayılan "Recompile And Continue Playing"; Play sırasında derleme olunca (ya da `wait-editor.sh --refresh`)
  domain reload sonrası `RootScope(Clone)` ikilenir ve konsola "2 event systems" / "2 audio listeners" uyarıları basılır.
  Temiz Play'de tek (doğrulandı). Öneri: template bir Editor betiğiyle `EditorPrefs` `ScriptCompilationDuringPlay`'i 2
  ("Stop Playing And Recompile") yapsın ya da dokümana "Play'de kodu değiştirme, durdurup derle" kuralı yazsın.
- **Root havuz instance'ının sahne scope'unda geri verilmesi, Play durma anında (2026-11-16):** root `IPools`'tan spawn edilip
  (parent'sız) bir sahne scope'unun `Dispose`'unda `Despawn` edilen instance, Editor'da Play durunca assertion basıyor
  (`'t.GetParent() == nullptr'`, `SetParent(_container)`): o anda instance'ın sahnesi (DDOL) geçersiz (`IsValid() == false`),
  objeler henüz yok edilmemiş. Oyun tarafında `scene.IsValid()` ile atlatıldı (`RoadPiecePool.Release`). Öneri: `Despawn`,
  instance'ın sahnesi geçersizse ya da `PooledObject.IsQuitting` ise taşıma yapmadan dönsün; `IsQuitting` (internal) Play
  durmada henüz kalkmıyor olabilir, denenmedi.
- ~~**`Application.runInBackground` Editor'da ayarı da değiştirir (2026-11-14):** CLAUDE.md "yalnız o oturum, ayar değişmez"
  diyor; oysa atama `ProjectSettings.asset`'i bellekte kirletiyor ve sonraki `AssetDatabase.SaveAssets()` `runInBackground: 1`
  yazıyor (Check Release de "unsaved ProjectSettings" uyarısı verdi). Öneri: kural "Play'den sonra
  `PlayerSettings.runInBackground = false` geri al; `SaveAssets`'ten önce diff'e bak" olsun ya da `PlayMode.cs` girişte eski
  değeri saklayıp `Exit`'te geri koysun.~~ Template'te düzeltildi (`c24e775`), buraya alındı (`4dfcb8d`, 2026-11-16).
