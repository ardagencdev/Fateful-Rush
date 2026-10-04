using System.Collections.Generic;

// Two physical labels: ThreatUnknownText, NoReturnVectorText. Former SignalLostText phrases are evenly retained.
public static class AmbientSkinMessages
{
    private struct Phrase
    {
        public readonly string En, Tr;
        public Phrase(string en, string tr) { En = en; Tr = tr; }
    }
    private static readonly Dictionary<string, Phrase[][]> Profiles = new Dictionary<string, Phrase[][]>
    {
        { "white", new Phrase[][] {
            new Phrase[] { new Phrase("THREAT: UNKNOWN", "TEHDİT: BİLİNMİYOR"), new Phrase("SCANNING SECTOR", "BÖLGE TARANIYOR"), new Phrase("HOLD YOUR COURSE", "ROTANI KORU"), new Phrase("STAY ALERT", "TETİKTE KAL"), new Phrase("SILENCE AHEAD", "İLERİDE SESSİZLİK"), new Phrase("EYES ON THE VOID", "GÖZLER BOŞLUKTA"), new Phrase("SIGNAL LOST", "SİNYAL KAYIP"), new Phrase("DISTANT ECHO", "UZAK YANKI"), new Phrase("CHANNEL OPEN", "KANAL AÇIK") },
            new Phrase[] { new Phrase("BEYOND THE HORIZON", "UFKUN ÖTESİNDE"), new Phrase("HOME IS BEHIND YOU", "EVİN GERİDE KALDI"), new Phrase("A NEW COURSE", "YENİ BİR ROTA"), new Phrase("THE JOURNEY BEGINS", "YOLCULUK BAŞLIYOR"), new Phrase("FOLLOW THE STARS", "YILDIZLARI İZLE"), new Phrase("INTO THE UNKNOWN", "BİLİNMEYENE DOĞRU"), new Phrase("ORIGIN: EARTH", "KÖKEN: DÜNYA"), new Phrase("AWAITING CONTACT", "BAĞLANTI BEKLENİYOR"), new Phrase("BEACON ONLINE", "İŞARETÇİ AKTİF") },
        } },
        { "blue", new Phrase[][] {
            new Phrase[] { new Phrase("CALM UNDER PRESSURE", "BASKI ALTINDA SAKİN"), new Phrase("READ THE CURRENT", "AKINTIYI OKU"), new Phrase("FOCUS HOLDS", "ODAK KORUNUYOR"), new Phrase("DEPTH: UNKNOWN", "DERİNLİK: BİLİNMİYOR"), new Phrase("WATCH THE DISTANCE", "MESAFEYİ KOLLA"), new Phrase("SILENT APPROACH", "SESSİZ YAKLAŞIM"), new Phrase("DEEP SPACE SIGNAL", "DERİN UZAY SİNYALİ"), new Phrase("BLUE FREQUENCY", "MAVİ FREKANS"), new Phrase("ECHO IN THE DARK", "KARANLIKTA YANKI") },
            new Phrase[] { new Phrase("INTO THE DEEP", "DERİNLİĞE DOĞRU"), new Phrase("ACROSS THE BLUE", "MAVİLİĞİN ÖTESİNE"), new Phrase("KEEP A STEADY COURSE", "ROTANI SABİT TUT"), new Phrase("DISTANT SHORES", "UZAK KIYILAR"), new Phrase("BEYOND THE SILENCE", "SESSİZLİĞİN ÖTESİNDE"), new Phrase("DRIFT WITH PURPOSE", "HEDEFE DOĞRU SÜZÜL"), new Phrase("QUIET CHANNEL", "SESSİZ KANAL"), new Phrase("COLD TRANSMISSION", "SOĞUK İLETİM"), new Phrase("DISTANT CONTACT", "UZAK BAĞLANTI") },
        } },
        { "orange", new Phrase[][] {
            new Phrase[] { new Phrase("PRESSURE RISING", "BASINÇ YÜKSELİYOR"), new Phrase("KEEP THE FIRE ALIVE", "ATEŞİ CANLI TUT"), new Phrase("BURN THROUGH FEAR", "KORKUYU YAKIP GEÇ"), new Phrase("CORE: UNSTABLE", "ÇEKİRDEK: KARARSIZ"), new Phrase("HEAT AHEAD", "İLERİDE ISI VAR"), new Phrase("NO TIME TO COOL", "SOĞUMAYA VAKİT YOK"), new Phrase("IGNITION SIGNAL", "ATEŞLEME SİNYALİ"), new Phrase("EMBER FREQUENCY", "KÖZ FREKANSI"), new Phrase("CORE WARMING", "ÇEKİRDEK ISINIYOR") },
            new Phrase[] { new Phrase("CHASE THE SUN", "GÜNEŞİN PEŞİNDEN"), new Phrase("THROUGH THE EMBERS", "KÖZLERİN ARASINDAN"), new Phrase("FUEL THE JOURNEY", "YOLCULUĞU BESLE"), new Phrase("LEAVE A BURNING TRAIL", "ARDINDA ATEŞ BIRAK"), new Phrase("FORWARD IN FLAMES", "ALEVLERLE İLERLE"), new Phrase("BEYOND THE HEAT", "ISININ ÖTESİNDE"), new Phrase("SOLAR CONTACT", "GÜNEŞ BAĞLANTISI"), new Phrase("FURNACE ONLINE", "OCAK AKTİF"), new Phrase("HEAT SIGNATURE", "ISI İZİ") },
        } },
        { "red", new Phrase[][] {
            new Phrase[] { new Phrase("DANGER APPROACHES", "TEHLİKE YAKLAŞIYOR"), new Phrase("ADRENALINE RISING", "ADRENALİN YÜKSELİYOR"), new Phrase("NEVER BACK DOWN", "ASLA GERİ ÇEKİLME"), new Phrase("THREAT: ACTIVE", "TEHDİT: AKTİF"), new Phrase("BREAK THE LINE", "HATTI YAR"), new Phrase("STAY ONE STEP AHEAD", "BİR ADIM ÖNDE KAL"), new Phrase("ALERT SIGNAL", "UYARI SİNYALİ"), new Phrase("RED FREQUENCY", "KIRMIZI FREKANS"), new Phrase("PULSE DETECTED", "NABIZ ALGILANDI") },
            new Phrase[] { new Phrase("STRAIGHT INTO DANGER", "TEHLİKENİN İÇİNE"), new Phrase("PUSH THE LIMIT", "SINIRI ZORLA"), new Phrase("NO HESITATION", "TEREDDÜDE YER YOK"), new Phrase("CLAIM YOUR PATH", "YOLUNU ELE GEÇİR"), new Phrase("THROUGH THE CROSSFIRE", "ÇAPRAZ ATEŞİN İÇİNDEN"), new Phrase("COURAGE SETS THE COURSE", "ROTAYI CESARET ÇİZER"), new Phrase("WARNING RECEIVED", "UYARI ALINDI"), new Phrase("COMBAT CHANNEL", "ÇATIŞMA KANALI"), new Phrase("CONTACT: HOSTILE", "BAĞLANTI: DÜŞMAN") },
        } },
        { "green", new Phrase[][] {
            new Phrase[] { new Phrase("ADAPT AND SURVIVE", "UYUM SAĞLA VE YAŞA"), new Phrase("LIFE FINDS A WAY", "YAŞAM BİR YOL BULUR"), new Phrase("GROW THROUGH CHAOS", "KAOSTA GELİŞ"), new Phrase("INSTINCT: ACTIVE", "İÇGÜDÜ: AKTİF"), new Phrase("ROOTS HOLD FIRM", "KÖKLER SAĞLAM"), new Phrase("RECOVERY IN PROGRESS", "YENİLENME SÜRÜYOR"), new Phrase("LIFE SIGNAL", "YAŞAM SİNYALİ"), new Phrase("BIOSPHERE ONLINE", "BİYOSFER AKTİF"), new Phrase("VITAL SIGNS STABLE", "YAŞAM BULGULARI SABİT") },
            new Phrase[] { new Phrase("SEEK NEW WORLDS", "YENİ DÜNYALAR ARA"), new Phrase("FOLLOW THE LIFELINE", "YAŞAM İZİNİ TAKİP ET"), new Phrase("GROW BEYOND HOME", "EVİN ÖTESİNDE GELİŞ"), new Phrase("A WORLD TO DISCOVER", "KEŞFEDİLECEK BİR DÜNYA"), new Phrase("LEAVE LIFE BEHIND YOU", "ARDINDA YAŞAM BIRAK"), new Phrase("TOWARD NEW BEGINNINGS", "YENİ BAŞLANGIÇLARA"), new Phrase("NEW LIFE DETECTED", "YENİ YAŞAM ALGILANDI"), new Phrase("VERDANT FREQUENCY", "YEŞİL FREKANS"), new Phrase("SEED OF A SIGNAL", "SİNYALİN TOHUMU") },
        } },
        { "pink", new Phrase[][] {
            new Phrase[] { new Phrase("FOLLOW YOUR RHYTHM", "RİTMİNİ TAKİP ET"), new Phrase("CHAOS HAS A BEAT", "KAOSUN BİR RİTMİ VAR"), new Phrase("KEEP YOUR SPARK", "KIVILCIMINI KORU"), new Phrase("ENERGY: RISING", "ENERJİ: YÜKSELİYOR"), new Phrase("DARE TO STAND OUT", "FARK EDİLMEYE CESARET ET"), new Phrase("BEAUTY UNDER PRESSURE", "BASKI ALTINDA ZARAFET"), new Phrase("NEON SIGNAL", "NEON SİNYALİ"), new Phrase("PULSE IN THE STATIC", "PARAZİTTE NABIZ"), new Phrase("ROSE FREQUENCY", "PEMBE FREKANS") },
            new Phrase[] { new Phrase("PAINT YOUR OWN PATH", "KENDİ YOLUNU ÇİZ"), new Phrase("CHASE THE NEON", "NEONUN PEŞİNDEN"), new Phrase("MOVE WITH THE PULSE", "NABIZLA HAREKET ET"), new Phrase("BRIGHTER THAN THE VOID", "BOŞLUKTAN DAHA PARLAK"), new Phrase("LEAVE YOUR MARK", "İZİNİ BIRAK"), new Phrase("A DIFFERENT HORIZON", "FARKLI BİR UFUK"), new Phrase("HEARTBEAT ONLINE", "KALP ATIŞI AKTİF"), new Phrase("VIVID TRANSMISSION", "CANLI İLETİM"), new Phrase("COLOR IN THE VOID", "BOŞLUKTA RENK") },
        } },
        { "yellow", new Phrase[][] {
            new Phrase[] { new Phrase("LIGHT CUTS THROUGH", "IŞIK YOL AÇAR"), new Phrase("STAY BRIGHT", "PARLAK KAL"), new Phrase("HOPE: ACTIVE", "UMUT: AKTİF"), new Phrase("ENERGY SURGE", "ENERJİ DALGASI"), new Phrase("A SPARK IS ENOUGH", "BİR KIVILCIM YETER"), new Phrase("OUTSHINE THE FEAR", "KORKUYU IŞIĞINLA AŞ"), new Phrase("SOLAR SIGNAL", "GÜNEŞ SİNYALİ"), new Phrase("DAYBREAK FREQUENCY", "ŞAFAK FREKANSI"), new Phrase("LIGHT RECEIVED", "IŞIK ALINDI") },
            new Phrase[] { new Phrase("TOWARD THE DAWN", "ŞAFAĞA DOĞRU"), new Phrase("FOLLOW THE LIGHT", "IŞIĞI İZLE"), new Phrase("CHASE THE HORIZON", "UFKUN PEŞİNDEN"), new Phrase("A BRIGHTER TOMORROW", "DAHA AYDINLIK BİR YARIN"), new Phrase("THROUGH THE SOLAR WIND", "GÜNEŞ RÜZGÂRININ İÇİNDEN"), new Phrase("DAYBREAK AWAITS", "ŞAFAK SENİ BEKLİYOR"), new Phrase("RADIANT CONTACT", "PARLAK BAĞLANTI"), new Phrase("NEW DAWN DETECTED", "YENİ ŞAFAK ALGILANDI"), new Phrase("BEACON AT FULL POWER", "İŞARETÇİ TAM GÜÇTE") },
        } },
        { "cyan", new Phrase[][] {
            new Phrase[] { new Phrase("REFLEXES ENGAGED", "REFLEKSLER DEVREDE"), new Phrase("PRECISION MATTERS", "HASSASİYET ÖNEMLİ"), new Phrase("STAY IN SYNC", "UYUMU KORU"), new Phrase("CHARGE: RISING", "YÜK: YÜKSELİYOR"), new Phrase("READ THE PATTERN", "DESENİ OKU"), new Phrase("EVERY FRAME COUNTS", "HER KARE ÖNEMLİ"), new Phrase("ION SIGNAL", "İYON SİNYALİ"), new Phrase("CRYSTAL FREQUENCY", "KRİSTAL FREKANS"), new Phrase("DATA STREAM OPEN", "VERİ AKIŞI AÇIK") },
            new Phrase[] { new Phrase("RIDE THE CURRENT", "AKINTIYA KATIL"), new Phrase("TRACE A CLEAN PATH", "NET BİR YOL ÇİZ"), new Phrase("THROUGH THE ION FIELD", "İYON ALANININ İÇİNDEN"), new Phrase("BEYOND THE CIRCUIT", "DEVRENİN ÖTESİNDE"), new Phrase("FLOW WITHOUT LIMITS", "SINIRSIZCA AK"), new Phrase("THE FUTURE IS OPEN", "GELECEK AÇIK"), new Phrase("LINK SYNCHRONIZED", "BAĞLANTI EŞZAMANLI"), new Phrase("ELECTRIC CONTACT", "ELEKTRİK BAĞLANTISI"), new Phrase("CIRCUIT ONLINE", "DEVRE AKTİF") },
        } },
        { "purple", new Phrase[][] {
            new Phrase[] { new Phrase("REALITY IS SHIFTING", "GERÇEKLİK DEĞİŞİYOR"), new Phrase("TRUST YOUR INSTINCT", "İÇGÜDÜNE GÜVEN"), new Phrase("EXPECT THE UNEXPECTED", "BEKLENMEYENE HAZIR OL"), new Phrase("ANOMALY: ACTIVE", "ANOMALİ: AKTİF"), new Phrase("READ BETWEEN THE STARS", "YILDIZLARIN ARASINI OKU"), new Phrase("NOTHING IS CERTAIN", "HİÇBİR ŞEY KESİN DEĞİL"), new Phrase("UNKNOWN FREQUENCY", "BİLİNMEYEN FREKANS"), new Phrase("ECHOES BEYOND SIGHT", "GÖRÜNMEYEN YANKILAR"), new Phrase("ANOMALY DETECTED", "ANOMALİ ALGILANDI") },
            new Phrase[] { new Phrase("BEYOND REALITY", "GERÇEKLİĞİN ÖTESİNDE"), new Phrase("ENTER THE MYSTERY", "GİZEME ADIM AT"), new Phrase("FOLLOW THE UNKNOWN", "BİLİNMEYENİ İZLE"), new Phrase("WHERE STARS FALL SILENT", "YILDIZLARIN SUSTUĞU YER"), new Phrase("CROSS THE THRESHOLD", "EŞİĞİ AŞ"), new Phrase("A HIDDEN PATH", "GİZLİ BİR YOL"), new Phrase("VEILED TRANSMISSION", "ÖRTÜLÜ İLETİM"), new Phrase("SIGNAL OUT OF PHASE", "SİNYAL FAZ DIŞINDA"), new Phrase("THE VOID ANSWERS", "BOŞLUK YANIT VERİYOR") },
        } },
        { "dark", new Phrase[][] {
            new Phrase[] { new Phrase("YOU ARE THE THREAT", "TEHDİT SENSİN"), new Phrase("MERCY ENDS HERE", "MERHAMET BURADA BİTER"), new Phrase("FEAR KNOWS YOUR NAME", "KORKU ADINI BİLİYOR"), new Phrase("DARKNESS BOWS TO YOU", "KARANLIK ÖNÜNDE EĞİLİR"), new Phrase("THE HUNT BELONGS TO YOU", "AV ARTIK SENİN"), new Phrase("NO LIGHT CAN HIDE THEM", "HİÇBİR IŞIK ONLARI GİZLEYEMEZ"), new Phrase("THE VOID KNOWS YOUR NAME", "BOŞLUK ADINI BİLİYOR"), new Phrase("WHISPERS FROM THE ABYSS", "UÇURUMDAN FISILTILAR"), new Phrase("THE DARKNESS ANSWERS", "KARANLIK YANIT VERİYOR") },
            new Phrase[] { new Phrase("LET THE STARS TREMBLE", "YILDIZLAR TİTRESİN"), new Phrase("CLAIM THE SHADOWS", "GÖLGELERE HÜKMET"), new Phrase("BEYOND THE LAST LIGHT", "SON IŞIĞIN ÖTESİNDE"), new Phrase("LEAVE ONLY SILENCE", "ARDINDA YALNIZCA SESSİZLİK"), new Phrase("THE VOID IS YOUR DOMAIN", "BOŞLUK SENİN HÜKÜMRANLIĞIN"), new Phrase("WHERE HOPE FALLS SILENT", "UMUDUN SUSTUĞU YER"), new Phrase("A CURSED SIGNAL", "LANETLİ BİR SİNYAL"), new Phrase("SILENCE HAS A MASTER", "SESSİZLİĞİN BİR EFENDİSİ VAR"), new Phrase("THE ABYSS HAS AWAKENED", "UÇURUM UYANDI") },
        } },
        { "golden", new Phrase[][] {
            new Phrase[] { new Phrase("LIGHT BOWS TO YOU", "IŞIK ÖNÜNDE EĞİLİR"), new Phrase("DIVINE WILL PREVAILS", "İLAHİ İRADE GALİP GELİR"), new Phrase("THE HEAVENS STAND WITH YOU", "GÖKLER SENİN YANINDA"), new Phrase("LET FAITH CONQUER FEAR", "İNANÇ KORKUYU YENSİN"), new Phrase("GUARDIAN OF THE DAWN", "ŞAFAĞIN KORUYUCUSU"), new Phrase("DARKNESS YIELDS TO GRACE", "KARANLIK LÜTFA BOYUN EĞER"), new Phrase("THE HEAVENS ANSWER", "GÖKLER YANIT VERİYOR"), new Phrase("A SACRED LIGHT AWAKENS", "KUTSAL BİR IŞIK UYANIYOR"), new Phrase("FAITH OUTSHINES THE VOID", "İNANÇ BOŞLUĞU AYDINLATIR") },
            new Phrase[] { new Phrase("ASCEND BEYOND FATE", "KADERİN ÖTESİNE YÜKSEL"), new Phrase("WALK THE SACRED PATH", "KUTSAL YOLDA YÜRÜ"), new Phrase("CARRY THE LIGHT FORWARD", "IŞIĞI İLERİYE TAŞI"), new Phrase("WHERE ANGELS GUIDE YOU", "MELEKLERİN REHBERLİĞİNDE"), new Phrase("RISE INTO ETERNAL LIGHT", "EBEDİ IŞIĞA YÜKSEL"), new Phrase("LET THE HEAVENS OPEN", "GÖKLERİN KAPILARI AÇILSIN"), new Phrase("ANGELS BREAK THE SILENCE", "MELEKLER SESSİZLİĞİ BOZAR"), new Phrase("A CELESTIAL CALL", "GÖKLERDEN BİR ÇAĞRI"), new Phrase("GRACE AMONG THE STARS", "YILDIZLAR ARASINDA LÜTUF") },
        } },
    };
    public static string Normalize(string id)
    {
        id = (id ?? "white").Trim().ToLowerInvariant().Replace("_", "").Replace("-", "").Replace(" ", "");
        if (id == "black") id = "dark";
        if (id == "gold") id = "golden";
        if (id == "lightblue") id = "cyan";
        if (id == "deeppink" || id == "hotpink") id = "pink";
        return Profiles.ContainsKey(id) ? id : "white";
    }
    public static int Count(string profile, int slot)
    {
        return Profiles[Normalize(profile)][slot].Length;
    }
    public static string Get(string profile, int slot, int index, bool turkish)
    {
        Phrase[] pool = Profiles[Normalize(profile)][slot];
        if (index < 0 || index >= pool.Length) index = 0;
        return turkish ? pool[index].Tr : pool[index].En;
    }
}
