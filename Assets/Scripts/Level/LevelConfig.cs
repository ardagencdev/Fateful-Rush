using UnityEngine;

[System.Serializable]
public class LevelObstacleOption
{
    public GameObject prefab;
    public bool enabled = true;
}

public enum ObstacleSpawnMode
{
    Fixed,
    Random
}

public enum WinConditionType
{
    ReachScore,
    SurviveTime,
    ReachScoreWithinTime
}

public enum BossSpawnCondition
{
    Score,
    Time
}

[System.Serializable]
public class ComboSpeedStage
{
    [Min(2)]
    public int comboMultiplier = 2;

    [Min(1)]
    public int coinsRequired = 2;

    [Min(1f)]
    public float playerSpeedMultiplier = 1.25f;
}

[CreateAssetMenu(
    fileName = "LevelConfig",
    menuName = "Void Rush/Level Config"
)]
public class LevelConfig : ScriptableObject
{
    [Header("LEVEL INFO")]
    [Min(0)]
    public int levelNumber = 1;

    public string levelName = "Level 1";

    [Header("MISSION BRIEFING")]
    [Tooltip(
        "Main Menu içindeki mission briefing panelinde gösterilecek başlık. " +
        "Boş bırakılırsa level adı kullanılır."
    )]
    public string briefingTitle = "MISSION BRIEFING";

    [Tooltip(
        "Bölüm zorluğu. 0 ile 5 arasında tam yıldız olarak gösterilir."
    )]
    [Range(0, 5)]
    public int missionDifficulty = 1;

    [Tooltip(
        "Oyun modu için özel açıklama. Boş bırakılırsa seçilen win condition'a " +
        "göre otomatik açıklama oluşturulur."
    )]
    [TextArea(2, 5)]
    public string briefingModeDescription = "";

    [Tooltip(
        "Kazanma hedefi için özel açıklama. Boş bırakılırsa win score ve time " +
        "limit değerlerinden otomatik açıklama oluşturulur."
    )]
    [TextArea(2, 5)]
    public string briefingObjectiveDescription = "";

    [Tooltip(
        "İlk otomatik bilgi sayfasından sonra gösterilecek ek briefing sayfaları. " +
        "Yeni enemy, trap ve taktik açıklamalarını buraya yazabilirsin."
    )]
    [TextArea(4, 10)]
    public string[] briefingPages =
    {
        "Mission information..."
    };

    [Header("MUSIC")]
    [Tooltip("Bu level boyunca çalacak gameplay müziği.")]
    public AudioClip gameplayMusic;

    [Header("WIN CONDITION")]
    public WinConditionType winCondition =
        WinConditionType.ReachScore;

    [Min(1)]
    public int winScore = 15;

    [Min(0.1f)]
    public float timeLimit = 35f;

    [Header("HUD")]
    public bool showGameTimerHUD = false;

    [Header("PLAYER")]
    [Min(0f)]
    public float playerMoveSpeed = 7f;

    [Min(1f)]
    public float playerComboSpeedBonus = 1.2f;

    [Header("PLAYER ABILITIES")]
    public bool dashEnabled = true;
    public bool cloneEnabled = false;

    [Header("DASH")]
    [Min(0f)]
    public float dashDistance = 2.5f;

    [Min(0.01f)]
    public float dashDuration = 0.12f;

    [Min(0f)]
    public float dashCooldown = 2f;

    [Header("CLONE")]
    [Min(0.01f)]
    public float cloneDuration = 3f;

    [Min(0f)]
    public float cloneCooldown = 8f;

    [Min(0)]
    public int cloneUses = 1;

    [Header("ARMOR")]
    [Min(0f)]
    public float armorImmuneDuration = 0.8f;

    [Header("SLOW POWER UP")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.4f;

    [Min(0.01f)]
    public float slowDuration = 5f;

    [Header("UI")]
    public bool comboEnabled = true;

    [Min(0.01f)]
    public float comboTimeLimit = 2f;

    [Min(1)]
    public int maxCombo = 3;

    [Tooltip(
        "Combo level, kaç coin chain ile açılacağı ve player speed çarpanı. " +
        "Boş bırakırsan eski 2x/3x sistemi çalışır."
    )]
    public ComboSpeedStage[] comboSpeedStages =
    {
        new ComboSpeedStage
        {
            comboMultiplier = 2,
            coinsRequired = 2,
            playerSpeedMultiplier = 1.25f
        },
        new ComboSpeedStage
        {
            comboMultiplier = 3,
            coinsRequired = 5,
            playerSpeedMultiplier = 1.5f
        }
    };

    [Header("BACKGROUND / NEAR STARS")]
    public bool randomizeNearStarsColor = false;

    [ColorUsage(false, true)]
    public Color nearStarsColor = Color.white;

    [Min(0f)]
    public float nearStarsSpeedMultiplier = 1f;

    [Min(0f)]
    public float nearStarsSizeMultiplier = 1f;

    [Min(0f)]
    public float nearStarsEmissionRate = 30f;

    [Header("COINS")]
    [Min(0.01f)]
    public float coinSpawnInterval = 1f;

    [Min(0)]
    public int maxCoinCount = 8;

    [Range(0f, 100f)]
    public float normalCoinChance = 70f;

    [Range(0f, 100f)]
    public float goldCoinChance = 25f;

    [Range(0f, 100f)]
    public float rareCoinChance = 5f;

    [Min(1)]
    public int normalCoinValue = 1;

    [Min(1)]
    public int goldCoinValue = 3;

    [Min(1)]
    public int rareCoinValue = 5;

    public bool normalCoinEnabled = true;
    public bool goldCoinEnabled = true;
    public bool rareCoinEnabled = true;

    [Header("OBSTACLES")]
    public ObstacleSpawnMode obstacleSpawnMode =
        ObstacleSpawnMode.Fixed;

    public LevelObstacleOption[] levelObstacles;

    [Header("RANDOM OBSTACLES")]
    [Min(0)]
    public int randomObstacleCount = 5;

    [Header("NORMAL ENEMY")]
    [Min(0)]
    public int normalEnemyCount = 0;

    [Min(0.01f)]
    public float normalEnemySpawnInterval = 2.5f;

    [Min(0f)]
    public float normalMinStartSpeed = 1.5f;

    [Min(0f)]
    public float normalMaxStartSpeed = 2.5f;

    [Min(0f)]
    public float normalMaxSpeed = 7f;

    [Min(0f)]
    public float normalSpeedIncreaseRate = 0.1f;

    [Header("NORMAL ENEMY AI")]
    public bool normalPredictionEnabled = true;

    [Min(0f)]
    public float normalPredictionDistanceThreshold = 2.5f;

    [Min(0f)]
    public float normalPredictionTime = 0.25f;

    [Min(0f)]
    public float normalMaxPredictionDistance = 1.5f;

    public bool normalSeparationEnabled = true;

    [Min(0f)]
    public float normalSeparationRadius = 0.75f;

    [Min(0f)]
    public float normalSeparationStrength = 0.65f;

    [Header("PROJECTILE ENEMY")]
    [Min(0)]
    public int projectileEnemyCount = 0;

    [Min(0.01f)]
    public float projectileEnemySpawnInterval = 5f;

    [Min(0f)]
    public float projectileMoveSpeed = 3f;

    [Min(0f)]
    public float projectileStoppingDistance = 7f;

    [Min(0f)]
    public float projectileRetreatDistance = 4f;

    [Min(0.01f)]
    public float projectileFireRate = 1.5f;

    [Min(0f)]
    public float projectileSpeed = 6f;

    [Header("HUNTER ENEMY")]
    [Min(0)]
    public int hunterEnemyCount = 0;

    [Min(0.01f)]
    public float hunterEnemySpawnInterval = 8f;

    [Min(0f)]
    public float hunterRepositionTime = 1.2f;

    [Min(0f)]
    public float hunterWarningDuration = 1f;

    [Min(0f)]
    public float hunterChargeSpeed = 15f;

    [Min(0f)]
    public float hunterStunDuration = 1f;

    [Header("BOSS")]
    public bool bossEnabled = false;

    public BossSpawnCondition bossSpawnCondition =
        BossSpawnCondition.Score;

    [Min(0)]
    public int bossSpawnScore = 75;

    [Min(0f)]
    public float bossSpawnTime = 30f;

    [Min(0f)]
    public float bossSpeed = 1.2f;

    public bool bossCanSplit = true;

    [Min(0f)]
    public float bossSplitDelay = 0.8f;

    [Min(0f)]
    public float bossSplitDistance = 1.2f;

    [Min(0f)]
    public float miniBossSpeed = 2.5f;

    [Header("BEACON ENEMY")]
    [Min(0)]
    public int beaconEnemyCount = 0;

    [Min(0f)]
    public float beaconMinSpawnTime = 10f;

    [Min(0f)]
    public float beaconMaxSpawnTime = 20f;

    [Header("BEACON BUFF")]
    [Min(0.01f)]
    public float beaconBuffDuration = 15f;

    [Min(0f)]
    public float beaconBuffSizeMultiplier = 1.25f;

    [Min(0f)]
    public float beaconNormalSpeedMultiplier = 1.35f;

    [Min(0f)]
    public float beaconNormalMaxSpeedMultiplier = 1.25f;

    [Min(0f)]
    public float beaconProjectileMoveMultiplier = 1.2f;

    [Min(0f)]
    public float beaconProjectileShotMultiplier = 1.25f;

    [Min(0f)]
    public float beaconProjectileFireMultiplier = 1.25f;

    [Min(0f)]
    public float beaconHunterRepositionMultiplier = 0.8f;

    [Min(0f)]
    public float beaconHunterWarningMultiplier = 0.8f;

    [Min(0f)]
    public float beaconHunterChargeMultiplier = 1.25f;

    [Min(0f)]
    public float beaconHunterStunMultiplier = 0.8f;

    [Header("POWER UPS")]
    public bool armorEnabled = false;
    public bool slowEnabled = false;

    [Min(0f)]
    public float armorMinSpawnTime = 15f;

    [Min(0f)]
    public float armorMaxSpawnTime = 30f;

    [Min(0f)]
    public float slowMinSpawnTime = 8f;

    [Min(0f)]
    public float slowMaxSpawnTime = 20f;

    [Header("VERTICAL LASER")]
    public bool verticalLaserEnabled = false;

    [Min(0f)]
    public float verticalLaserMinSpawnTime = 8f;

    [Min(0f)]
    public float verticalLaserMaxSpawnTime = 25f;

    [Min(0f)]
    public float verticalLaserWarningDuration = 2f;

    [Min(0f)]
    public float verticalLaserLifeTime = 1.5f;

    [Min(0f)]
    public float verticalLaserWidth = 0.5f;

    [Min(0f)]
    public float verticalLaserHeightExtra = 1f;

    [Header("HORIZONTAL LASER")]
    public bool horizontalLaserEnabled = false;

    [Min(0f)]
    public float horizontalLaserMinSpawnTime = 8f;

    [Min(0f)]
    public float horizontalLaserMaxSpawnTime = 25f;

    [Min(0f)]
    public float horizontalLaserWarningDuration = 2f;

    [Min(0f)]
    public float horizontalLaserLifeTime = 1.5f;

    [Min(0f)]
    public float horizontalLaserWidth = 0.5f;

    [Min(0f)]
    public float horizontalLaserWidthExtra = 1f;

    [Header("BOMBS")]
    public bool bombTrapEnabled = false;

    [Min(0f)]
    public float bombMinSpawnTime = 6f;

    [Min(0f)]
    public float bombMaxSpawnTime = 14f;

    [Min(0)]
    public int maxBombCount = 3;


    /*
     * Runtime sistemleri bu ortak değerleri kullanır. Böylece win condition
     * kontrolleri farklı scriptlerde birbirinden kopmaz.
     */
    public bool UsesScore =>
        winCondition == WinConditionType.ReachScore ||
        winCondition == WinConditionType.ReachScoreWithinTime;

    public bool UsesTime =>
        winCondition == WinConditionType.SurviveTime ||
        winCondition == WinConditionType.ReachScoreWithinTime;

    public bool IsPureSurvivalMode =>
        winCondition == WinConditionType.SurviveTime;

    public bool IsTimedScoreMode =>
        winCondition == WinConditionType.ReachScoreWithinTime;

    public bool CoinsEnabled => UsesScore;

    public bool EffectiveComboEnabled =>
        UsesScore && comboEnabled;

    public bool ScoreHUDEnabled => UsesScore;

    public bool TimerHUDEnabled => UsesTime;

    public bool CanSaveBestTime => UsesScore;

    public BossSpawnCondition EffectiveBossSpawnCondition
    {
        get
        {
            switch (winCondition)
            {
                case WinConditionType.ReachScore:
                    return BossSpawnCondition.Score;

                case WinConditionType.SurviveTime:
                    return BossSpawnCondition.Time;

                case WinConditionType.ReachScoreWithinTime:
                    return bossSpawnCondition;

                default:
                    return BossSpawnCondition.Score;
            }
        }
    }

    public int SafeWinScore => Mathf.Max(1, winScore);

    public float SafeTimeLimit => Mathf.Max(0.1f, timeLimit);

    public int SafeBossSpawnScore =>
        Mathf.Clamp(bossSpawnScore, 0, SafeWinScore);

    public float SafeBossSpawnTime =>
        Mathf.Clamp(bossSpawnTime, 0f, SafeTimeLimit);

    public int SafeMissionDifficulty =>
        Mathf.Clamp(missionDifficulty, 0, 5);

    public string EffectiveBriefingTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(briefingTitle))
                return briefingTitle.Trim();

            if (!string.IsNullOrWhiteSpace(levelName))
                return levelName.Trim();

            return $"LEVEL {levelNumber}";
        }
    }

    public string GetDefaultModeDescription()
    {
        switch (winCondition)
        {
            case WinConditionType.ReachScore:
                return "SCORE MISSION";

            case WinConditionType.SurviveTime:
                return "SURVIVAL MISSION";

            case WinConditionType.ReachScoreWithinTime:
                return "TIMED SCORE MISSION";

            default:
                return "MISSION";
        }
    }

    public string GetEffectiveModeDescription()
    {
        if (!string.IsNullOrWhiteSpace(briefingModeDescription))
            return briefingModeDescription.Trim();

        return GetDefaultModeDescription();
    }

    public string GetDefaultObjectiveDescription()
    {
        switch (winCondition)
        {
            case WinConditionType.ReachScore:
                return $"Collect {SafeWinScore} points to complete the mission.";

            case WinConditionType.SurviveTime:
                return $"Stay alive for {FormatSeconds(SafeTimeLimit)}. Coins and combos are disabled in this mission.";

            case WinConditionType.ReachScoreWithinTime:
                return $"Collect {SafeWinScore} points before the {FormatSeconds(SafeTimeLimit)} countdown reaches zero.";

            default:
                return "Complete the mission objective.";
        }
    }

    public string GetEffectiveObjectiveDescription()
    {
        if (!string.IsNullOrWhiteSpace(briefingObjectiveDescription))
            return briefingObjectiveDescription.Trim();

        return GetDefaultObjectiveDescription();
    }

    private static string FormatSeconds(float seconds)
    {
        float safeSeconds = Mathf.Max(0f, seconds);

        if (Mathf.Approximately(safeSeconds, Mathf.Round(safeSeconds)))
            return $"{Mathf.RoundToInt(safeSeconds)} seconds";

        return $"{safeSeconds:0.#} seconds";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        levelNumber = Mathf.Max(0, levelNumber);
        winScore = Mathf.Max(1, winScore);
        timeLimit = Mathf.Max(0.1f, timeLimit);

        missionDifficulty = Mathf.Clamp(
            missionDifficulty,
            0,
            5
        );

        bossSpawnScore = Mathf.Max(0, bossSpawnScore);
        bossSpawnTime = Mathf.Max(0f, bossSpawnTime);

        if (winCondition == WinConditionType.ReachScore)
        {
            bossSpawnCondition = BossSpawnCondition.Score;
        }
        else if (winCondition == WinConditionType.SurviveTime)
        {
            bossSpawnCondition = BossSpawnCondition.Time;
        }

        // Eski serialized alan tutuluyor fakat artık win condition ile senkron.
        showGameTimerHUD = UsesTime;
    }
#endif
}