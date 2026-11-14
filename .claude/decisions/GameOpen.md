# WrongLine (açık işler)

Biten madde silinmez, üstü çizilir; kalıcı sonucu `Game.md`'ye taşınır.

## Açık
- ~~**Ekran yönü:** dikey mi yatay mı, M0'da karar verilir.~~ Dikey (2026-11-14, `Game.md` M0 kararları).
- ~~**Lisans ve görünürlük:** public'e ne zaman geçileceği ve lisans (MIT önerisi) M0'da kararlaştırılır.~~ MIT eklendi
  (2026-11-14, `Game.md`); görünürlük aşağıda.
- **Public'e geçiş:** ne zaman açılacağı; ilk oynanabilir milestone'dan sonra kararlaştırılır.
- **İlk Android build:** `AF/Build` Development, cihazda açılış, dikey ekran, Menu → Gameplay; `adb logcat -s Unity`. Template'in
  "İlk mobil oyunda doğrulanacaklar" listesi (`decisions/README.md`) bununla birlikte. İlk build'in yeniden yazdığı URP
  asset'leri ayrı commit.

## Template'e gidenler
- **`Application.runInBackground` Editor'da ayarı da değiştirir (2026-11-14):** CLAUDE.md "yalnız o oturum, ayar değişmez"
  diyor; oysa atama `ProjectSettings.asset`'i bellekte kirletiyor ve sonraki `AssetDatabase.SaveAssets()` `runInBackground: 1`
  yazıyor (Check Release de "unsaved ProjectSettings" uyarısı verdi). Öneri: kural "Play'den sonra
  `PlayerSettings.runInBackground = false` geri al; `SaveAssets`'ten önce diff'e bak" olsun ya da `PlayMode.cs` girişte eski
  değeri saklayıp `Exit`'te geri koysun.
