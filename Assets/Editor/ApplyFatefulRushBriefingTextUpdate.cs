using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ApplyFatefulRushBriefingTextUpdate
{
    private const string SelfPath = "Assets/Editor/ApplyFatefulRushBriefingTextUpdate.cs";

    private static readonly string[] Keys =
    {
            "briefing.tip.0c148bdf",
            "briefing.tip.4204a777",
            "briefing.tip.083542a0",
            "briefing.tip.e4869cf4",
            "briefing.tip.057cf834",
            "briefing.tip.a5ae9dea",
            "briefing.tip.b5a52989",
            "briefing.tip.a4d5f554",
            "briefing.tip.e35801b9",
            "briefing.tip.6873f782",
            "briefing.tip.c1a289b4",
            "briefing.tip.9dfd489a",
            "briefing.tip.afc4480c",
            "briefing.tip.59c45885",
            "briefing.tip.b936ba74",
            "briefing.tip.1fa93703",
            "briefing.tip.d77bf53b",
            "briefing.tip.f0779c3f",
            "briefing.tip.5cc6531c",
            "briefing.tip.10c90984",
            "briefing.tip.91c028a8",
            "briefing.tip.b3f32dd3",
            "briefing.tip.db8403d4",
            "briefing.tip.0253d3d2",
            "briefing.tip.594df230",
            "briefing.tip.b018f580",
            "briefing.tip.9f0ce819",
            "briefing.tip.17a7b142",
            "briefing.tip.92c52b1f",
            "briefing.tip.9746a8fe",
            "briefing.tip.bfc063a1",
            "briefing.tip.e211848b",
            "briefing.tip.6c32ef1c",
            "briefing.tip.2fea83f0",
            "briefing.tip.0c8b09c5",
            "briefing.tip.8549734e",
            "briefing.tip.c388b658",
            "briefing.tip.7ec9bb59",
            "briefing.tip.e482958d",
            "briefing.tip.028a7195"
    };

    private static readonly string[] English =
    {
            "Learn the movement and collect safely before the timer runs out.",
            "Chain coin pickups while the Stalker keeps gaining speed.",
            "Keep both Stalkers together, then cut across open space for coins.",
            "Obstacles now block routes. Always keep a second exit.",
            "Use Dash to escape blocked routes and close threats.",
            "Gold Coins are worth more. Take them only when the route is safe.",
            "Blasters predict your movement. Change direction after they fire.",
            "Armor blocks one lethal hit. Use the brief immunity to escape.",
            "Space Bombs turn areas deadly. Watch where they arm before moving in.",
            "Survive the Bomb pressure and save Armor for emergencies.",
            "Leave Laser warning lanes early, then react to Blaster fire.",
            "Build quick combos, but break the chain when the arena closes in.",
            "Use Slow when several threats overlap, not while the arena is calm.",
            "Rare Coins are valuable, but never enter a bad route just for one.",
            "Let Hunter charges commit before dodging. Track both Hunters.",
            "Use Clone to pull pressure away and reset dangerous formations.",
            "Move out of Vertical Laser warnings early and save Clone for overlaps.",
            "Destroy Beacons with Dash before their buffs turn the arena dangerous.",
            "The Boss arrives at 75 score. Keep Armor ready for the Boss phase.",
            "The Boss arrives at 50 score while Lasers and Bombs crowd the arena.",
            "Both Laser directions are active. Move only after a warning appears.",
            "Bombs and Hunters leave little room. Keep routes short and controlled.",
            "Destroy the recurring Beacon, then score during the safer window.",
            "Lasers, Bombs and Hunters shrink safe space. Move before Bombs arm.",
            "The Boss arrives at 100 score. Enter that phase with Dash ready.",
            "Use Slow and Clone on separate pressure spikes, not at the same time.",
            "Blasters, Hunters and Bombs overlap. Change direction after attacks commit.",
            "Use the safest quadrant and abandon distant coins when Lasers chain.",
            "Recurring Beacons and Hunters create pressure waves. Clear the Beacon first.",
            "The Boss arrives at 90 score. Remove Beacon buffs before forcing routes.",
            "Lasers and Bombs leave no safe orbit. Save Armor and Clone separately.",
            "Clear the recurring Beacon before committing to a long combo.",
            "Bombs and obstacles turn the arena into a minefield. Move sector by sector.",
            "Beacon buffs, Hunters and a Vertical Laser create repeated danger spikes.",
            "The Boss arrives at 120 score. Build momentum before the Boss phase.",
            "Use each Beacon takedown as a scoring window between Lasers and Bombs.",
            "Survive the full hazard mix. Rotate Armor, Slow and Clone carefully.",
            "Clear Beacon buffs first, then build short combos through the minefield.",
            "The Boss arrives at 105 score. Keep one defensive option for that phase.",
            "Final challenge: survive 70 seconds. The Boss enters at 35 seconds."
    };

    private static readonly string[] Turkish =
    {
            "Hareketi öğren ve süre bitmeden güvenli şekilde coin topla.",
            "Stalker hızlanırken coinleri zincirleyerek kombo yap.",
            "İki Stalker'ı birlikte tut, sonra açık alandaki coinlere geç.",
            "Engeller rotaları kapatıyor. Her zaman ikinci bir çıkış bırak.",
            "Dash'i kapanan rotalardan ve yakın tehditlerden kaçmak için kullan.",
            "Altın Coinler daha değerlidir. Yalnızca rota güvenliyse al.",
            "Blasterlar hareketini tahmin eder. Ateş ettikten sonra yön değiştir.",
            "Zırh ölümcül bir darbeyi engeller. Kısa bağışıklıkla hemen uzaklaş.",
            "Uzay Bombaları alanları ölümcül yapar. Kuruldukları yeri takip et.",
            "Bomba baskısında hayatta kal ve Zırhı acil durumlar için sakla.",
            "Lazer uyarı alanından erken çık, sonra Blaster atışlarına tepki ver.",
            "Hızlı kombolar kur ama arena kapanırken zinciri bırak.",
            "Yavaşlatmayı birkaç tehdit üst üste geldiğinde kullan.",
            "Nadir Coinler değerlidir ama kötü bir rota için risk alma.",
            "Hunter hücumu kesinleşince kaç. İki Hunter'ı da takip et.",
            "Klonu baskıyı dağıtmak ve tehlikeli düzeni sıfırlamak için kullan.",
            "Dikey Lazer uyarısından erken çık, Klonu çakışan tehditlere sakla.",
            "Beacon güçlendirmeleri tehlikeli olmadan önce Dash ile Beacon'ı yok et.",
            "Boss 75 skorda gelir. Boss aşaması için Zırhı hazır tut.",
            "Boss 50 skorda gelir; Lazerler ve Bombalar arenayı daraltır.",
            "İki Lazer yönü de aktif. Uyarı çıkınca temiz tarafa geç.",
            "Bombalar ve Hunterlar alanı daraltır. Kısa ve kontrollü rotalar kullan.",
            "Tekrarlayan Beacon'ı yok et, sakin aralıkta skor kas.",
            "Lazerler, Bombalar ve Hunterlar alanı daraltır. Bombalar kurulmadan geç.",
            "Boss 100 skorda gelir. O aşamaya Dash hazır gir.",
            "Yavaşlatma ve Klonu aynı anda değil, ayrı baskı anlarında kullan.",
            "Blaster, Hunter ve Bombalar çakışır. Saldırılar kesinleşince yön değiştir.",
            "Lazerler zincirlenirken güvenli bölgeden coin topla; uzağı zorlama.",
            "Beacon ve Hunterlar baskı dalgaları yaratır. Önce Beacon'ı temizle.",
            "Boss 90 skorda gelir. Uzun rotalara girmeden Beacon güçlendirmesini kaldır.",
            "Lazerler ve Bombalar güvenli alan bırakmaz. Zırh ve Klonu ayrı sakla.",
            "Uzun komboya girmeden önce tekrarlayan Beacon'ı temizle.",
            "Bombalar ve engeller arenayı mayın tarlasına çevirir. Bölge bölge ilerle.",
            "Beacon, Hunterlar ve Dikey Lazer tekrar eden tehlike dalgaları oluşturur.",
            "Boss 120 skorda gelir. Boss aşamasından önce ivme kazan.",
            "Her Beacon yok edişini Lazer ve Bombalar arasında skor fırsatı olarak kullan.",
            "Tüm tehlike türlerinde hayatta kal. Zırh, Yavaşlatma ve Klonu sırayla kullan.",
            "Önce Beacon güçlendirmesini kaldır, sonra kısa kombolar kur.",
            "Boss 105 skorda gelir. O aşama için bir savunma seçeneğini sakla.",
            "Son görev: 70 saniye hayatta kal. Boss 35. saniyede gelir."
    };

    static ApplyFatefulRushBriefingTextUpdate()
    {
        EditorApplication.delayCall += Apply;
    }

    private static void Apply()
    {
        try
        {
            if (Keys.Length != 40 || English.Length != 40 || Turkish.Length != 40)
                throw new Exception("Briefing arrays must contain exactly 40 entries.");

            UpdateLevelConfigs();
            UpdateStringTable("Assets/Localization/UI_en.asset", English);
            UpdateStringTable("Assets/Localization/UI_tr.asset", Turkish);
            UpdateTurkishOverrides();
            UpdateTranslationReference();

            AssetDatabase.Refresh();
            Debug.Log("[Fateful Rush] 40 mission briefing texts updated in English and Turkish.");

            EditorApplication.delayCall += () =>
            {
                AssetDatabase.DeleteAsset(SelfPath);
                AssetDatabase.Refresh();
                Debug.Log("[Fateful Rush] Temporary briefing updater removed.");
            };
        }
        catch (Exception ex)
        {
            Debug.LogError("[Fateful Rush] Briefing update failed: " + ex);
        }
    }

    private static void UpdateLevelConfigs()
    {
        for (int i = 0; i < 40; i++)
        {
            string path = $"Assets/LevelConfigs/Level_{i + 1:00}_Config.asset";
            if (!File.Exists(path))
                throw new FileNotFoundException("Missing level config", path);

            string text = File.ReadAllText(path, Encoding.UTF8);
            string replacement =
                "  briefingPages:\n" +
                "  - " + QuoteYaml(English[i]) + "\n";

            var regex = new Regex(
                @"^  briefingPages:\s*\n(?:  - .*?(?:\n    .*?)*\n)?(?=  gameplayMusic:)",
                RegexOptions.Multiline | RegexOptions.Singleline);
            string updated = regex.Replace(text, replacement, 1);

            if (updated == text)
                throw new Exception("Could not update briefingPages in " + path);

            File.WriteAllText(path, updated, new UTF8Encoding(false));
        }
    }

    private static void UpdateStringTable(string path, string[] values)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Missing localization table", path);

        string text = File.ReadAllText(path, Encoding.UTF8);

        for (int i = 0; i < 40; i++)
        {
            long id = 85017371521024L + i;
            string pattern =
                @"(?ms)(^  - m_Id: " + id + @"\s*\n    m_Localized:).*?(?=\n    m_Metadata:)";

            string replacement = "$1 " + QuoteYaml(values[i]);
            string updated = new Regex(pattern).Replace(text, replacement, 1);

            if (updated == text)
                throw new Exception($"Could not update localization entry {id} in {path}.");

            text = updated;
        }

        File.WriteAllText(path, text, new UTF8Encoding(false));
    }

    private static void UpdateTurkishOverrides()
    {
        string path = "Assets/Scripts/Localization/FatefulRushLocalization.cs";
        if (!File.Exists(path))
            throw new FileNotFoundException("Missing localization script", path);

        string text = File.ReadAllText(path, Encoding.UTF8);

        for (int i = 0; i < 40; i++)
        {
            string fullKey = Keys[i];
            string pattern =
                @"(?m)^\s*\{\s*""" + Regex.Escape(fullKey) +
                @""",\s*"".*?""\s*\},\s*$";

            string replacement =
                "            { " + CsQuote(fullKey) + ", " + CsQuote(Turkish[i]) + " },";

            string updated = new Regex(pattern).Replace(text, replacement, 1);

            if (updated == text)
                throw new Exception("Could not update Turkish override: " + fullKey);

            text = updated;
        }

        File.WriteAllText(path, text, new UTF8Encoding(false));
    }

    private static void UpdateTranslationReference()
    {
        string path = "Assets/Localization/BriefingTextsForTranslation.txt";
        if (!File.Exists(path))
            return;

        string text = File.ReadAllText(path, Encoding.UTF8);

        for (int i = 0; i < 40; i++)
        {
            int level = i + 1;
            string pattern =
                @"(?m)^(PAGE 1 EN:)\s*.*$";

            // Restrict replacement to the matching level block.
            string header = $"=== LEVEL {level:00} ===";
            int start = text.IndexOf(header, StringComparison.Ordinal);
            if (start < 0) continue;

            int end = text.IndexOf("=== LEVEL ", start + header.Length, StringComparison.Ordinal);
            if (end < 0) end = text.Length;

            string block = text.Substring(start, end - start);
            string updatedBlock = new Regex(pattern).Replace(
                block,
                "$1 " + English[i],
                1);

            text = text.Substring(0, start) + updatedBlock + text.Substring(end);
        }

        File.WriteAllText(path, text, new UTF8Encoding(true));
    }

    private static string QuoteYaml(string value)
    {
        return "\"" +
            value.Replace("\\", "\\\\").Replace("\"", "\\\"") +
            "\"";
    }

    private static string CsQuote(string value)
    {
        return "\"" +
            value.Replace("\\", "\\\\").Replace("\"", "\\\"") +
            "\"";
    }
}
