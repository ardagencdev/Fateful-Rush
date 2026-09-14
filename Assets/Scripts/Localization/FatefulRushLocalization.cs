using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class FatefulRushLocalization
{
    private const string TableName = "UI";

    private static readonly Dictionary<string, string> Cache =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private static readonly string[] BriefingTipKeys =
    {
        "briefing.tip.0c148bdf", "briefing.tip.4204a777", "briefing.tip.083542a0", "briefing.tip.e4869cf4",
        "briefing.tip.057cf834", "briefing.tip.a5ae9dea", "briefing.tip.b5a52989", "briefing.tip.a4d5f554",
        "briefing.tip.e35801b9", "briefing.tip.6873f782", "briefing.tip.c1a289b4", "briefing.tip.9dfd489a",
        "briefing.tip.afc4480c", "briefing.tip.59c45885", "briefing.tip.b936ba74", "briefing.tip.1fa93703",
        "briefing.tip.d77bf53b", "briefing.tip.f0779c3f", "briefing.tip.5cc6531c", "briefing.tip.10c90984",
        "briefing.tip.91c028a8", "briefing.tip.b3f32dd3", "briefing.tip.db8403d4", "briefing.tip.0253d3d2",
        "briefing.tip.594df230", "briefing.tip.b018f580", "briefing.tip.9f0ce819", "briefing.tip.17a7b142",
        "briefing.tip.92c52b1f", "briefing.tip.9746a8fe", "briefing.tip.bfc063a1", "briefing.tip.e211848b",
        "briefing.tip.6c32ef1c", "briefing.tip.2fea83f0", "briefing.tip.0c8b09c5", "briefing.tip.8549734e",
        "briefing.tip.c388b658", "briefing.tip.7ec9bb59", "briefing.tip.e482958d", "briefing.tip.028a7195"
    };

    private static readonly Dictionary<string, string> TurkishOverrides =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "ui.main_menu", "ANA MENÜ" },
            { "hud.level", "BÖLÜM {0}" },
            { "menu.level", "BÖLÜM {0}" },
            { "briefing.level_title", "BÖLÜM {0} — {1}" },
            { "objective.reach_score", "{0} SKORA ULAŞ" },
            { "objective.survive_time", "{0} SANİYE HAYATTA KAL" },
            { "objective.reach_score_in_time", "{1} SANİYE İÇİNDE {0} SKORA ULAŞ" },
            { "objective.complete_mission", "GÖREVİ TAMAMLA" },
            { "completion.level40_thank_you", "OYUNU OYNADIĞIN İÇİN TEŞEKKÜRLER\nFATEFUL RUSH'I TAMAMLADIN" },
            { "intro.win_condition", "KAZANMA KOŞULU" },
            { "intro.tap_to_start", "BAŞLAMAK İÇİN DOKUN" },
            { "intro.click_space_to_start", "BAŞLAMAK İÇİN TIKLA VEYA SPACE'E BAS" },
            { "intro.new", "YENİ" },
            { "result.you_survived", "HAYATTA KALDIN" },
            { "result.you_survived_for", "HAYATTA KALDIĞIN SÜRE" },
            { "mechanic.score_mode", "SKOR MODU" },
            { "mechanic.survival_mode", "HAYATTA KALMA MODU" },
            { "mechanic.timed_score", "SÜRELİ SKOR" },
            { "mechanic.dash", "DASH" },
            { "mechanic.clone", "KLON" },
            { "mechanic.combo", "KOMBO" },
            { "mechanic.coins", "COINLER" },
            { "mechanic.gold_coins", "ALTIN COINLER" },
            { "mechanic.rare_coins", "NADİR COINLER" },
            { "mechanic.obstacles", "ENGELLER" },
            { "mechanic.stalker", "STALKER" },
            { "mechanic.blaster", "BLASTER" },
            { "mechanic.hunter", "HUNTER" },
            { "mechanic.boss", "BOSS" },
            { "mechanic.beacon", "BEACON" },
            { "mechanic.armor", "ZIRH" },
            { "mechanic.slow", "YAVAŞLATMA" },
            { "mechanic.vertical_laser", "DİKEY LAZER" },
            { "mechanic.horizontal_laser", "YATAY LAZER" },
            { "mechanic.space_bomb", "UZAY BOMBASI" },

            { "level.name.01", "İlk Sinyal" },
            { "level.name.02", "Zincirleme Tepki" },
            { "level.name.03", "İlk Takip" },
            { "level.name.04", "Kırık Yörünge" },
            { "level.name.05", "Acil İtiş" },
            { "level.name.06", "Altın Rota" },
            { "level.name.07", "Çapraz Ateş" },
            { "level.name.08", "Bir Şans Daha" },
            { "level.name.09", "Kırmızı Hat" },
            { "level.name.10", "Void'a Karşı Dur" },
            { "level.name.11", "Bölünmüş Ufuk" },
            { "level.name.12", "İlahi Saate Karşı" },
            { "level.name.13", "Zaman Bozulması" },
            { "level.name.14", "Nadir Fırsat" },
            { "level.name.15", "Avcının İşareti" },
            { "level.name.16", "Sahte Hedef" },
            { "level.name.17", "Patlama Yarıçapı" },
            { "level.name.18", "Sinyal Güçlendirme" },
            { "level.name.19", "Ağır Geliş" },
            { "level.name.20", "Sistem Kontrolü" },
            { "level.name.21", "Kıl Payı Kaçış" },
            { "level.name.22", "Risk Devresi" },
            { "level.name.23", "Güçlendirilmiş Takip" },
            { "level.name.24", "Düşen Yıldızlar" },
            { "level.name.25", "Dönüm Noktası Krizi" },
            { "level.name.26", "Hayalet Protokolü" },
            { "level.name.27", "Hız Zinciri" },
            { "level.name.28", "Lazer Izgarası" },
            { "level.name.29", "Aşırı Yüklü" },
            { "level.name.30", "Kırılma Noktası" },
            { "level.name.31", "Güvenli Yörünge Yok" },
            { "level.name.32", "Son Tarih" },
            { "level.name.33", "Mayın Tarlası Koşusu" },
            { "level.name.34", "Rezonans" },
            { "level.name.35", "İkiz Çöküş" },
            { "level.name.36", "Olay Ufku" },
            { "level.name.37", "Son Direniş" },
            { "level.name.38", "Kusursuz Zincir" },
            { "level.name.39", "Son Önleme" },
            { "level.name.40", "Tekillik" },
            { "briefing.tip.0c148bdf", "Hareketi öğren ve süre bitmeden güvenli şekilde coin topla." },
            { "briefing.tip.4204a777", "Stalker hızlanırken coinleri zincirleyerek kombo yap." },
            { "briefing.tip.083542a0", "İki Stalker'ı birlikte tut, sonra açık alandaki coinlere geç." },
            { "briefing.tip.e4869cf4", "Engeller rotaları kapatıyor. Her zaman ikinci bir çıkış bırak." },
            { "briefing.tip.057cf834", "Dash'i kapanan rotalardan ve yakın tehditlerden kaçmak için kullan." },
            { "briefing.tip.a5ae9dea", "Altın Coinler daha değerlidir. Yalnızca rota güvenliyse al." },
            { "briefing.tip.b5a52989", "Blasterlar hareketini tahmin eder. Ateş ettikten sonra yön değiştir." },
            { "briefing.tip.a4d5f554", "Zırh ölümcül bir darbeyi engeller. Kısa bağışıklıkla hemen uzaklaş." },
            { "briefing.tip.e35801b9", "Uzay Bombaları alanları ölümcül yapar. Kuruldukları yeri takip et." },
            { "briefing.tip.6873f782", "Bomba baskısında hayatta kal ve Zırhı acil durumlar için sakla." },
            { "briefing.tip.c1a289b4", "Lazer uyarı alanından erken çık, sonra Blaster atışlarına tepki ver." },
            { "briefing.tip.9dfd489a", "Hızlı kombolar kur ama arena kapanırken zinciri bırak." },
            { "briefing.tip.afc4480c", "Yavaşlatmayı birkaç tehdit üst üste geldiğinde kullan." },
            { "briefing.tip.59c45885", "Nadir Coinler değerlidir ama kötü bir rota için risk alma." },
            { "briefing.tip.b936ba74", "Hunter hücumu kesinleşince kaç. İki Hunter'ı da takip et." },
            { "briefing.tip.1fa93703", "Klonu baskıyı dağıtmak ve tehlikeli düzeni sıfırlamak için kullan." },
            { "briefing.tip.d77bf53b", "Dikey Lazer uyarısından erken çık, Klonu çakışan tehditlere sakla." },
            { "briefing.tip.f0779c3f", "Beacon güçlendirmeleri tehlikeli olmadan önce Dash ile Beacon'ı yok et." },
            { "briefing.tip.5cc6531c", "Boss 75 skorda gelir. Boss aşaması için Zırhı hazır tut." },
            { "briefing.tip.10c90984", "Boss 50 skorda gelir; Lazerler ve Bombalar arenayı daraltır." },
            { "briefing.tip.91c028a8", "İki Lazer yönü de aktif. Uyarı çıkınca temiz tarafa geç." },
            { "briefing.tip.b3f32dd3", "Bombalar ve Hunterlar alanı daraltır. Kısa ve kontrollü rotalar kullan." },
            { "briefing.tip.db8403d4", "Tekrarlayan Beacon'ı yok et, sakin aralıkta skor kas." },
            { "briefing.tip.0253d3d2", "Lazerler, Bombalar ve Hunterlar alanı daraltır. Bombalar kurulmadan geç." },
            { "briefing.tip.594df230", "Boss 100 skorda gelir. O aşamaya Dash hazır gir." },
            { "briefing.tip.b018f580", "Yavaşlatma ve Klonu aynı anda değil, ayrı baskı anlarında kullan." },
            { "briefing.tip.9f0ce819", "Blaster, Hunter ve Bombalar çakışır. Saldırılar kesinleşince yön değiştir." },
            { "briefing.tip.17a7b142", "Lazerler zincirlenirken güvenli bölgeden coin topla; uzağı zorlama." },
            { "briefing.tip.92c52b1f", "Beacon ve Hunterlar baskı dalgaları yaratır. Önce Beacon'ı temizle." },
            { "briefing.tip.9746a8fe", "Boss 90 skorda gelir. Uzun rotalara girmeden Beacon güçlendirmesini kaldır." },
            { "briefing.tip.bfc063a1", "Lazerler ve Bombalar güvenli alan bırakmaz. Zırh ve Klonu ayrı sakla." },
            { "briefing.tip.e211848b", "Uzun komboya girmeden önce tekrarlayan Beacon'ı temizle." },
            { "briefing.tip.6c32ef1c", "Bombalar ve engeller arenayı mayın tarlasına çevirir. Bölge bölge ilerle." },
            { "briefing.tip.2fea83f0", "Beacon, Hunterlar ve Dikey Lazer tekrar eden tehlike dalgaları oluşturur." },
            { "briefing.tip.0c8b09c5", "Boss 120 skorda gelir. Boss aşamasından önce ivme kazan." },
            { "briefing.tip.8549734e", "Her Beacon yok edişini Lazer ve Bombalar arasında skor fırsatı olarak kullan." },
            { "briefing.tip.c388b658", "Tüm tehlike türlerinde hayatta kal. Zırh, Yavaşlatma ve Klonu sırayla kullan." },
            { "briefing.tip.7ec9bb59", "Önce Beacon güçlendirmesini kaldır, sonra kısa kombolar kur." },
            { "briefing.tip.e482958d", "Boss 105 skorda gelir. O aşama için bir savunma seçeneğini sakla." },
            { "briefing.tip.028a7195", "Son görev: 70 saniye hayatta kal. Boss 35. saniyede gelir." }
        };

    public static string CurrentLocaleCode
    {
        get
        {
            // The language selected in the menu is already persisted before
            // GameScene loads. Prefer that saved value so a newly loaded scene
            // can never render one English frame while Unity Localization is
            // still restoring SelectedLocale asynchronously.
            string saved = PlayerPrefs.GetString(
                "SelectedLocaleCode",
                string.Empty
            );

            if (string.IsNullOrWhiteSpace(saved))
            {
                saved = PlayerPrefs.GetString(
                    "Language",
                    string.Empty
                );
            }

            if (!string.IsNullOrWhiteSpace(saved))
                return saved.Trim().ToLowerInvariant();

            // Fresh install fallback: use Unity Localization only when there
            // is no persisted user choice yet.
            try
            {
                var selected = LocalizationSettings.SelectedLocale;

                if (selected != null &&
                    !string.IsNullOrWhiteSpace(selected.Identifier.Code))
                {
                    return selected.Identifier.Code.Trim().ToLowerInvariant();
                }
            }
            catch
            {
                // Localization may still be initializing.
            }

            return "en";
        }
    }

    public static bool IsTurkish =>
        CurrentLocaleCode.StartsWith("tr", StringComparison.OrdinalIgnoreCase);

    public static bool ApplySavedLocaleIfPossible()
    {
        string savedCode = PlayerPrefs.GetString(
            "SelectedLocaleCode",
            string.Empty
        );

        if (string.IsNullOrWhiteSpace(savedCode))
        {
            savedCode = PlayerPrefs.GetString(
                "Language",
                string.Empty
            );
        }

        if (string.IsNullOrWhiteSpace(savedCode))
            return false;

        try
        {
            string wantedRoot = GetLocaleRoot(savedCode);
            IReadOnlyList<Locale> locales =
                LocalizationSettings.AvailableLocales.Locales;

            Locale match = null;

            for (int i = 0; i < locales.Count; i++)
            {
                Locale locale = locales[i];

                if (locale == null ||
                    string.IsNullOrWhiteSpace(locale.Identifier.Code))
                {
                    continue;
                }

                string localeCode =
                    locale.Identifier.Code.Trim();

                if (string.Equals(
                        localeCode,
                        savedCode.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    match = locale;
                    break;
                }

                if (match == null &&
                    string.Equals(
                        GetLocaleRoot(localeCode),
                        wantedRoot,
                        StringComparison.OrdinalIgnoreCase))
                {
                    match = locale;
                }
            }

            if (match == null)
                return false;

            Locale selected =
                LocalizationSettings.SelectedLocale;

            if (selected == null ||
                !string.Equals(
                    selected.Identifier.Code,
                    match.Identifier.Code,
                    StringComparison.OrdinalIgnoreCase))
            {
                LocalizationSettings.SelectedLocale = match;
                ClearCache();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetLocaleRoot(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        string normalized =
            code.Trim().ToLowerInvariant();

        int separator = normalized.IndexOf('-');

        if (separator < 0)
            separator = normalized.IndexOf('_');

        return separator > 0
            ? normalized.Substring(0, separator)
            : normalized;
    }

    public static void ClearCache()
    {
        Cache.Clear();
    }

    public static string Text(string key, string fallback, params object[] args)
    {
        string value = Resolve(key, fallback);

        if (args == null || args.Length == 0)
            return value;

        try
        {
            return string.Format(value, args);
        }
        catch (FormatException)
        {
            try
            {
                return string.Format(fallback, args);
            }
            catch
            {
                return fallback;
            }
        }
    }

    public static string LevelName(int levelNumber, string fallback)
    {
        return Text($"level.name.{levelNumber:00}", fallback);
    }

    public static string BriefingTip(int levelNumber, string fallback)
    {
        if (levelNumber < 1 || levelNumber > BriefingTipKeys.Length)
            return fallback;

        // LevelConfig.briefingPages is the authoritative English source.
        // Do not let a stale English localization-table entry overwrite it.
        // Turkish still resolves through the localized/override entry.
        if (!IsTurkish)
            return fallback ?? string.Empty;

        return Text(BriefingTipKeys[levelNumber - 1], fallback);
    }

    private static string Resolve(string key, string fallback)
    {
        if (string.IsNullOrWhiteSpace(key))
            return fallback ?? string.Empty;

        string localeCode = CurrentLocaleCode;
        string cacheKey = localeCode + "|" + key;

        if (Cache.TryGetValue(cacheKey, out string cached))
            return cached;

        if (localeCode.StartsWith("tr", StringComparison.OrdinalIgnoreCase) &&
            TurkishOverrides.TryGetValue(key, out string trOverride))
        {
            Cache[cacheKey] = trOverride;
            return trOverride;
        }

        try
        {
            var operation = LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync(TableName, key);

            string localized = operation.IsDone
                ? operation.Result
                : operation.WaitForCompletion();

            if (IsValid(localized))
            {
                Cache[cacheKey] = localized;
                return localized;
            }
        }
        catch
        {
            // The fallback keeps the UI usable if localization is not initialized yet.
        }

        // Never cache a fallback. If localization was not ready yet,
        // a cached English fallback under the Turkish locale would survive
        // after the tables finished loading and cause visible EN -> TR swaps.
        return fallback ?? string.Empty;
    }

    private static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.StartsWith("No translation found", StringComparison.OrdinalIgnoreCase))
            return false;

        if (value.StartsWith("No table found", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
