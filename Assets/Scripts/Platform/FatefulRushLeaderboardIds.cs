using UnityEngine;

/// <summary>
/// Google Play Games leaderboard IDs for Fateful Rush.
///
/// These are raw Play Games IDs, so this file does not depend on the
/// auto-generated GPGSIds.cs resource file.
/// </summary>
public static class FatefulRushLeaderboardIds
{
    public const string Level01 = "CgkIsrqqqssOEAIQRw";
    public const string Level02 = "CgkIsrqqqssOEAIQSA";
    public const string Level03 = "CgkIsrqqqssOEAIQSQ";
    public const string Level04 = "CgkIsrqqqssOEAIQSg";
    public const string Level05 = "CgkIsrqqqssOEAIQSw";
    public const string Level06 = "CgkIsrqqqssOEAIQTA";
    public const string Level07 = "CgkIsrqqqssOEAIQTQ";
    public const string Level08 = "CgkIsrqqqssOEAIQTg";
    public const string Level09 = "CgkIsrqqqssOEAIQTw";

    public const string Level11 = "CgkIsrqqqssOEAIQUA";
    public const string Level12 = "CgkIsrqqqssOEAIQUQ";

    public const string Level14 = "CgkIsrqqqssOEAIQUg";
    public const string Level15 = "CgkIsrqqqssOEAIQUw";
    public const string Level16 = "CgkIsrqqqssOEAIQVA";

    public const string Level18 = "CgkIsrqqqssOEAIQVQ";
    public const string Level19 = "CgkIsrqqqssOEAIQVg";
    public const string Level20 = "CgkIsrqqqssOEAIQVw";

    public const string Level22 = "CgkIsrqqqssOEAIQWA";
    public const string Level23 = "CgkIsrqqqssOEAIQWQ";

    public const string Level25 = "CgkIsrqqqssOEAIQWg";

    public const string Level27 = "CgkIsrqqqssOEAIQWw";
    public const string Level28 = "CgkIsrqqqssOEAIQXA";

    public const string Level30 = "CgkIsrqqqssOEAIQXQ";

    public const string Level32 = "CgkIsrqqqssOEAIQXg";
    public const string Level33 = "CgkIsrqqqssOEAIQXw";

    public const string Level35 = "CgkIsrqqqssOEAIQYA";
    public const string Level36 = "CgkIsrqqqssOEAIQYQ";

    public const string Level38 = "CgkIsrqqqssOEAIQYg";
    public const string Level39 = "CgkIsrqqqssOEAIQYw";

    public static bool TryGetId(
        int levelNumber,
        out string leaderboardId)
    {
        switch (levelNumber)
        {
            case 1: leaderboardId = Level01; return true;
            case 2: leaderboardId = Level02; return true;
            case 3: leaderboardId = Level03; return true;
            case 4: leaderboardId = Level04; return true;
            case 5: leaderboardId = Level05; return true;
            case 6: leaderboardId = Level06; return true;
            case 7: leaderboardId = Level07; return true;
            case 8: leaderboardId = Level08; return true;
            case 9: leaderboardId = Level09; return true;

            case 11: leaderboardId = Level11; return true;
            case 12: leaderboardId = Level12; return true;

            case 14: leaderboardId = Level14; return true;
            case 15: leaderboardId = Level15; return true;
            case 16: leaderboardId = Level16; return true;

            case 18: leaderboardId = Level18; return true;
            case 19: leaderboardId = Level19; return true;
            case 20: leaderboardId = Level20; return true;

            case 22: leaderboardId = Level22; return true;
            case 23: leaderboardId = Level23; return true;

            case 25: leaderboardId = Level25; return true;

            case 27: leaderboardId = Level27; return true;
            case 28: leaderboardId = Level28; return true;

            case 30: leaderboardId = Level30; return true;

            case 32: leaderboardId = Level32; return true;
            case 33: leaderboardId = Level33; return true;

            case 35: leaderboardId = Level35; return true;
            case 36: leaderboardId = Level36; return true;

            case 38: leaderboardId = Level38; return true;
            case 39: leaderboardId = Level39; return true;

            default:
                leaderboardId = null;
                return false;
        }
    }
}
