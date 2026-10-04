using UnityEngine;

// Campaign progress only: no claim that unmeasured combat statistics were recorded.
public static class MenuProgressMessages
{
    private struct Phrase
    {
        public readonly string En, Tr;
        public Phrase(string en, string tr) { En = en; Tr = tr; }
    }
    private static readonly Phrase[][] Stages =
    {
        // Completed mission milestone: 0-9
        new Phrase[]
        {
            new Phrase("Standing still is a bold way to lose.", "Yerinde durmak, kaybetmenin iddialı bir yolu."),
            new Phrase("Watch the gaps. Panic isn't a strategy.", "Boşlukları izle. Panik bir strateji değil."),
            new Phrase("Dash is an escape. Not a plan.", "Dash bir kaçış yolu. Bir plan değil."),
            new Phrase("Survive first. Look impressive later.", "Önce hayatta kal. Gösterişi sonraya bırak."),
            new Phrase("The void doesn't grade on effort.", "Boşluk, çabana puan vermiyor."),
            new Phrase("A rookie mistake still ends a run.", "Acemice bir hata da oyunu bitirir."),
        },
        // Completed mission milestone: 10-19
        new Phrase[]
        {
            new Phrase("For a rookie, you've lasted suspiciously long.", "Bir çömeze göre fazla uzun dayandın."),
            new Phrase("Ten missions down. You're earning your place.", "On bölüm geride. Yerini hak etmeye başlıyorsun."),
            new Phrase("Promising. Try not to make me regret saying that.", "Umut verici. Bunu söylediğime pişman etme."),
            new Phrase("The basics are behind you. So are the excuses.", "Temeller geride kaldı. Bahaneler de öyle."),
            new Phrase("Keep your timing sharp. The easy part is over.", "Zamanlamanı koru. Kolay kısım bitti."),
            new Phrase("You're past the introduction. Now pay attention.", "Girişi geçtin. Şimdi dikkat kesil."),
        },
        // Completed mission milestone: 20-29
        new Phrase[]
        {
            new Phrase("Twenty missions. Luck is a weak explanation now.", "Yirmi bölüm. Bunu şansla açıklamak zor."),
            new Phrase("You've earned respect. Don't trade it for haste.", "Saygıyı hak ettin. Aceleye kurban etme."),
            new Phrase("The void has noticed. That isn't a compliment.", "Boşluk seni fark etti. Bu bir iltifat değil."),
            new Phrase("Halfway through. Nothing left to prove to a rookie.", "Yarı yolu geçtin. Çömezliği geride bıraktın."),
            new Phrase("Precision matters more when the stakes rise.", "Risk büyüdükçe hassasiyet daha çok önem kazanır."),
            new Phrase("You belong here. Staying is the harder part.", "Buraya aitsin. Zor olan burada kalmak."),
        },
        // Completed mission milestone: 30-39
        new Phrase[]
        {
            new Phrase("Thirty missions. The rookie jokes are over.", "Otuz bölüm. Çömez şakaları bitti."),
            new Phrase("The void has stopped underestimating you.", "Boşluk artık seni küçümsemiyor."),
            new Phrase("This far in, discipline outranks bravado.", "Bu noktada disiplin, cesaretten önce gelir."),
            new Phrase("You're close to the end. Stay sharper than ever.", "Sona yaklaştın. Her zamankinden dikkatli ol."),
            new Phrase("You've earned your confidence. Keep your caution.", "Özgüvenini hak ettin. Tedbiri elden bırakma."),
            new Phrase("The final stretch deserves your full attention.", "Son düzlük, tüm dikkatini hak ediyor."),
        },
        // Completed mission milestone: 40
        new Phrase[]
        {
            new Phrase("The final mission is cleared. The last word is yours.", "Son bölüm bitti. Son söz senin."),
            new Phrase("The void set the terms. You wrote the ending.", "Şartları boşluk koydu. Sonunu sen yazdın."),
            new Phrase("You don't chase survival anymore. You set the standard.", "Artık hayatta kalmaya çalışmıyorsun. Ölçütü sen koyuyorsun."),
            new Phrase("Fate made its move. You had the final answer.", "Kader hamlesini yaptı. Son yanıtı sen verdin."),
            new Phrase("The campaign is over. Your limits are still undecided.", "Bölümler bitti. Sınırların henüz belli değil."),
            new Phrase("You've reached the end. Perfection is still ahead.", "Sona ulaştın. Kusursuzluk hâlâ ileride."),
        },
    };
    public static int ReadStage()
    {
        // Same completion flags used by MainMenu and skin unlocks; never writes save data.
        int highest = 0;
        for (int level = 1; level <= 40; level++)
            if (PlayerPrefs.GetInt("CompletedLevel_" + level, 0) == 1) highest = level;
        return StageFor(highest);
    }
    public static int StageFor(int highestCompleted)
    {
        if (highestCompleted >= 40) return 4;
        if (highestCompleted >= 30) return 3;
        if (highestCompleted >= 20) return 2;
        if (highestCompleted >= 10) return 1;
        return 0;
    }
    public static int Count(int stage) { return Stages[Mathf.Clamp(stage, 0, 4)].Length; }
    public static string Get(int stage, int index, bool turkish)
    {
        Phrase[] pool = Stages[Mathf.Clamp(stage, 0, 4)];
        if (index < 0 || index >= pool.Length) index = 0;
        return turkish ? pool[index].Tr : pool[index].En;
    }
}
