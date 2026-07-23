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

    [Header("TUTORIAL")]
    public bool showTutorial = true;

    public string tutorialTitle = "LEVEL BRIEFING";

    [TextArea(4, 10)]
    public string[] tutorialPages =
    {
        "Tutorial text..."
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

    [Min(0)]
    public int bossSpawnScore = 75;

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
}