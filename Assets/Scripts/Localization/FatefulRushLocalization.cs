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

            { "briefing.tip.0c148bdf", "İlk sinyal alındı. Hareketi öğrenerek ve güvenli coin rotaları oluşturarak görevine başla." },
            { "briefing.tip.4204a777", "Kombolar art arda coin topladığında ödül verir. Yeni Stalker ise durmadan seni kovalar ve zamanla hızlanır." },
            { "briefing.tip.083542a0", "Artık iki Stalker aynı anda baskı kuruyor. Onları bir tarafta topla, sonra coinler için açık alana geç." },
            { "briefing.tip.e4869cf4", "Sabit engeller artık rotaları kapatıyor. Stalker yollarını bozmak için kullan ama her zaman ikinci bir çıkış bırak." },
            { "briefing.tip.057cf834", "Dash artık kullanılabilir. Açık alanda harcamak yerine kapanan rotalar veya yakın tehditler için sakla." },
            { "briefing.tip.a5ae9dea", "Altın Coinler 3 puan değerinde. Rota güvenliyse al; ekstra değer uğruna kendini tuzağa sokma." },
            { "briefing.tip.b5a52989", "Blasterlar mesafeyi korur ve hareketini tahmin ederek ateş eder. Atış kesinleştikten sonra yön değiştir; düşmanları mermi kalkanı gibi kullanma." },
            { "briefing.tip.a4d5f554", "Zırh ölümcül bir darbeyi emer ve kırıldıktan sonra kısa süreli bağışıklık verir. O pencereyi hemen kaçmak için kullan." },
            { "briefing.tip.e35801b9", "Uzay Bombaları kısa bir gecikmeden sonra kurulur ve ölümcül alanlara dönüşür. Coin rotasına girmeden önce mayın alanını kontrol et." },
            { "briefing.tip.6873f782", "Bu ilk Hayatta Kalma görevin. Uzay Bombaları birikirken açık alanı koru ve Zırhı yalnızca toparlanmak için kullan." },
            { "briefing.tip.c1a289b4", "Yatay Lazerler ateş etmeden önce şeritlerini gösterir. Uyarı alanından erken çık, ardından gelen Blaster atışlarına tepki ver." },
            { "briefing.tip.9dfd489a", "Bu ilk Süreli Skor görevin. Düzenli kombolar kur ama arena kapanmaya başladığında zinciri bırakmaktan çekinme." },
            { "briefing.tip.afc4480c", "Yavaşlatma kısa süreliğine oyun hızını düşürür ve nefes alma alanı açar. Arena sakinken değil, birkaç tehdit üst üste bindiğinde kullan." },
            { "briefing.tip.59c45885", "Nadir Coinler 5 puan değerinde. Ulaşılabiliyorsa al ama ekstra skor için kapalı bir rotaya girme." },
            { "briefing.tip.b936ba74", "Hunterlar saldırmadan önce uyarı çizgisi gösterir. Çizginin yönü kesinleşsin, hamleden sıyrıl ve Dash kullanmadan önce iki Hunterı da takip et." },
            { "briefing.tip.1fa93703", "Klon düşmanların hedefini kısa süreliğine değiştirir. Bekleme süresi Klon kaybolduktan sonra başlar; çöken düzeni yeniden kurmak için kullan." },
            { "briefing.tip.d77bf53b", "Dikey Lazerler ateş etmeden önce bir sütunu işaretler. Erken çık ve Klonu Hunter veya Blaster baskısının çakıştığı anlar için sakla." },
            { "briefing.tip.f0779c3f", "Beaconlar yakındaki düşmanları güçlendirir. Beaconı Dash ile çarparak yok et; aktif güçlendirmeler anında kaybolur ama yeni bir Beacon geri gelebilir." },
            { "briefing.tip.5cc6531c", "İlk Boss 90 skorda gelir. Zırh darbesini emebilir; böyle olursa Boss iki Mini-Bossa bölünür." },
            { "briefing.tip.10c90984", "İki Lazer yönü, Uzay Bombaları ve 50 skorda gelen Boss burada üst üste biner. Erken skor yap, arena dolduğunda hayatta kalmaya öncelik ver." },
            { "briefing.tip.91c028a8", "Bu Hayatta Kalma görevinde iki Lazer yönü de aktif kalır. Uyarı çıkana kadar merkezde kal, sonra daha temiz tarafa yönel." },
            { "briefing.tip.b3f32dd3", "Uzay Bombaları Zırh veya Yavaşlatma desteği olmadan geri dönüyor. Rotaları kısa tut ve yalnızca Hunter hücumu kesinleştikten sonra yön değiştir." },
            { "briefing.tip.db8403d4", "Tekrarlayan bir Beacon düşman dalgasını destekler. Dash ile yok et, sonra geri gelmeden önceki sakin aralıkta kontrolü yeniden kur." },
            { "briefing.tip.0253d3d2", "Dikey Lazerler, Uzay Bombaları ve Hunterlar güvenli alanı giderek küçültür. Bir Bomba kurulmadan önce bölge değiştir; son anda kaçmaya çalışma." },
            { "briefing.tip.594df230", "Boss 100 skorda gelirken Yatay Lazerler çıkışları kısıtlar. O aşamaya Dash hazır şekilde gir ve skor rotalarını açık tut." },
            { "briefing.tip.b018f580", "Uzun bir Hayatta Kalma görevinde iki Lazer yönü de aktif. Bir baskı dalgasında Yavaşlatma, sonraki çakışmada Klon kullan." },
            { "briefing.tip.9f0ce819", "Yoğun Blaster ve Hunter baskısı Uzay Bombalarıyla birleşiyor. Kontrollü yaylar çiz ve yakın atışlar kesinleştikten sonra yön değiştir." },
            { "briefing.tip.17a7b142", "Bu Süreli Skor görevinde iki Lazer yönü art arda gelebilir. Güvenli çeyrekten coin topla ve uzaktaki coinlerden erken vazgeç." },
            { "briefing.tip.92c52b1f", "Tekrarlayan Beacon ve iki Hunter sürekli baskı dalgaları oluşturur. Güvenli bir Dash hattı açıldığında Beaconı yok et." },
            { "briefing.tip.9746a8fe", "Süre bitmeden önce Beacon, Uzay Bombaları ve 90 skorda gelen Boss üst üste biner. Uzun skor rotalarına girmeden önce Beacon güçlendirmelerini kaldır." },
            { "briefing.tip.bfc063a1", "Uzun Hayatta Kalma görevinde iki Lazer yönü ve Uzay Bombaları birlikte çalışır. Zırh ve Klonu ayrı acil durumlar için sakla." },
            { "briefing.tip.e211848b", "Tekrarlayan Beacon ve Uzay Bombaları bu Süreli Skor görevini bozar. Uzun bir kombo zincirine başlamadan önce Beaconı temizle." },
            { "briefing.tip.6c32ef1c", "Beş engel ve hızlı Uzay Bombası baskısı bu bölümü bir mayın tarlası skor sınavına çevirir. Bir açık sektörden diğerine ilerle." },
            { "briefing.tip.2fea83f0", "Beacon güçlendirmeleri, Dikey Lazer ve Hunterlar tekrar eden baskı dalgaları oluşturur. Mümkünse Yavaşlatmayı harcamadan önce Beaconı yok et." },
            { "briefing.tip.0c8b09c5", "Boss 120 skorda gelirken Uzay Bombaları güvenli rotaları küçültmeye devam eder. Boss aşamasından önce tempo kazan." },
            { "briefing.tip.8549734e", "Tekrarlayan Beacon, iki Lazer yönü ve Uzay Bombaları bu Süreli Skor görevini paylaşır. Her Beacon yok oluşunu bir skor fırsatı olarak kullan." },
            { "briefing.tip.c388b658", "İki Lazer yönü ve Uzay Bombaları uzun bir Hayatta Kalma görevi boyunca aktif kalır. Zırh, Yavaşlatma ve Klonu farklı acil durumlara dağıt." },
            { "briefing.tip.7ec9bb59", "Tekrarlayan Beacon, Uzay Bombaları ve beş engel en yüksek saf skor hedefini korur. Komboları yakın bölgede tut ve önce Beacon güçlendirmelerini temizle." },
            { "briefing.tip.e482958d", "Boss 105 skorda gelirken Dikey Lazerler, Uzay Bombaları, Hunterlar ve Blasterlar süre üzerinde baskı kurar. O aşama için bir savunma seçeneği sakla." },
            { "briefing.tip.028a7195", "Final meydan okuması. Tekrarlayan Beaconlar ve 35. saniyede gelen Boss ile tüm büyük tehlikelerden sağ çık. Zırh, Yavaşlatma ve Klonu dikkatle sırala." }
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
