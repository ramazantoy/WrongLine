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
