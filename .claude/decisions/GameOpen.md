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
- **M1b gerçek yol parçaları:** low-poly hand-paint yol parçaları ve araç (ayrı parçalı: gövde, kapı, tampon, tekerlek),
  Blender tabanlı asset akışıyla. **Sözleşme hazır** (2026-11-16): `Assets/Game/Road/Contract/` (26 parça + `section.json`,
  `WrongLine/Road/Export Recipes`), brif taslağı yerel `.claude/notes/m1b/brief.md`; My3DArtist'e **gönderilmedi**, kullanıcı
  onayı bekliyor. Yol tarifi Unity'den çıkar, Blender okur. Referans paketler yerelde kalır. Asset gelince: import, parça
  prefab'ının mesh'i değişir (tarif/hash aynı), `RoadCatalogValidator` 1 cm uç oturmasını ölçer.
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
- **M1 elle denemesinden (2026-11-16, kullanıcı):** kamera fena değil, M1 için yol gayet iyi. İleri fazlar için: (1) **curve
  shader** gerekli görünüyor (ufuk bükülmesi); (2) **yol "doğal" hissi bir tık eksik**: oluşan yola bakınca dağıtım
  algoritmasının izi görülüyor (ağırlıklar, `RepeatWindow`, bantlar: `RoadGeneratorSettings`; parça tipleri ve boyları da
  tekrarı belli ediyor); (3) bunun bir kısmı **sis (fog)** ile ilgili: yolun uzakta bitişi ve parça ritmi görünüyor. Bir
  sonraki milestone'da önce fog + curve shader, sonra dağıtımın ince ayarı (daha uzun yumuşak virajlar, parça uzunluğu
  çeşitliliği, ardışık parça "motifleri") denenir.
- **Sonraki milestone adayları:** curve shader, yol kenarı dekoru ve engeller, araç ve oyuncu kontrolü (`x` sürükleme,
  arcade, bankta görsel yatma), kamera cilası. Sırası M1 oynanınca yazılır.
- ~~**Kalıcı havuz:** parça havuzu önce oyunun kendi root singleton'ı olur; `IPools`'a root-lifetime modu gerekiyorsa
  "Template'e gidenler"e taşınır.~~ Template'e gidecek bir şey yok: root singleton `IPools`'tan root'un havuzunu alır
  (2026-11-16, `Game.md` M1 kararları "Havuz").

## Template'e gidenler
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
