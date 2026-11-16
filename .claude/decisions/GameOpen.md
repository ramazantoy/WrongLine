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
  M1a sözleşmesi bitince Blender tabanlı asset akışıyla. Yol tarifi Unity'den çıkar, Blender okur. Referans paketler yerelde
  kalır.
- **Uç yol parçaları:** dik düşüş, loop; up vektörü ve kamera zorlanırsa M1'de çözülmez, M2'ye ertelenir.
- **Sonraki milestone adayları:** curve shader, yol kenarı dekoru ve engeller, araç ve oyuncu kontrolü (`x` sürükleme,
  arcade, bankta görsel yatma), kamera cilası. Sırası M1 oynanınca yazılır.
- **Kalıcı havuz:** parça havuzu önce oyunun kendi root singleton'ı olur; `IPools`'a root-lifetime modu gerekiyorsa
  "Template'e gidenler"e taşınır.

## Template'e gidenler
- ~~**`Application.runInBackground` Editor'da ayarı da değiştirir (2026-11-14):** CLAUDE.md "yalnız o oturum, ayar değişmez"
  diyor; oysa atama `ProjectSettings.asset`'i bellekte kirletiyor ve sonraki `AssetDatabase.SaveAssets()` `runInBackground: 1`
  yazıyor (Check Release de "unsaved ProjectSettings" uyarısı verdi). Öneri: kural "Play'den sonra
  `PlayerSettings.runInBackground = false` geri al; `SaveAssets`'ten önce diff'e bak" olsun ya da `PlayMode.cs` girişte eski
  değeri saklayıp `Exit`'te geri koysun.~~ Template'te düzeltildi (`c24e775`), buraya alındı (`4dfcb8d`, 2026-11-16).
