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

[System.Serializable]
public class ComboSpeedStage
{
    [Min(2)] public int comboMultiplier = 2;
    [Min(1)] public int coinsRequired = 2;
    [Min(1f)] public float playerSpeedMultiplier = 1.25f;
}

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Void Rush/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("LEVEL INFO")]
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

    [Header("WIN")]
    public int winScore = 15;

    [Header("HUD")]
    public bool showGameTimerHUD = false;

    [Header("PLAYER")]
    public float playerMoveSpeed = 7f;
    public float playerComboSpeedBonus = 1.2f;

    [Header("PLAYER ABILITIES")]
    public bool dashEnabled = true;
    public bool cloneEnabled = false;

    [Header("DASH")]
    public float dashDistance = 2.5f;
    public float dashDuration = 0.12f;
    public float dashCooldown = 2f;

    [Header("CLONE")]
    public float cloneDuration = 3f;
    public float cloneCooldown = 8f;
    public int cloneUses = 1;

    [Header("ARMOR")]
    public float armorImmuneDuration = 0.8f;

    [Header("SLOW POWER UP")]
    public float slowMultiplier = 0.4f;
    public float slowDuration = 5f;

    [Header("UI")]
    public bool comboEnabled = true;
    public float comboTimeLimit = 2f;
    public int maxCombo = 3;

    [Tooltip("Combo level, kaç coin chain ile açılacağı ve player speed çarpanı. Boş bırakırsan eski 2x/3x sistemi çalışır.")]
    public ComboSpeedStage[] comboSpeedStages =
    {
        new ComboSpeedStage { comboMultiplier = 2, coinsRequired = 2, playerSpeedMultiplier = 1.25f },
        new ComboSpeedStage { comboMultiplier = 3, coinsRequired = 5, playerSpeedMultiplier = 1.5f }
    };

    [Header("BACKGROUND")]
    public Color nearStarsColor = Color.white;
    public float nearStarsSpeedMultiplier = 1f;
    public float nearStarsSizeMultiplier = 1f;
    public float nearStarsEmissionRate = 30f;

    [Header("COINS")]
    public float coinSpawnInterval = 1f;
    public int maxCoinCount = 8;
    [Range(0f, 100f)] public float normalCoinChance = 70f;
    [Range(0f, 100f)] public float goldCoinChance = 25f;
    [Range(0f, 100f)] public float rareCoinChance = 5f;
    public int normalCoinValue = 1;
    public int goldCoinValue = 3;
    public int rareCoinValue = 5;
    public bool normalCoinEnabled = true;
    public bool goldCoinEnabled = true;
    public bool rareCoinEnabled = true;

    [Header("OBSTACLES")]
    public ObstacleSpawnMode obstacleSpawnMode = ObstacleSpawnMode.Fixed;
    public LevelObstacleOption[] levelObstacles;

    [Header("RANDOM OBSTACLES")]
    public int randomObstacleCount = 5;

    [Header("NORMAL ENEMY")]
    public int normalEnemyCount = 0;
    public float normalEnemySpawnInterval = 2.5f;
    public float normalMinStartSpeed = 1.5f;
    public float normalMaxStartSpeed = 2.5f;
    public float normalMaxSpeed = 7f;
    public float normalSpeedIncreaseRate = 0.1f;

    [Header("NORMAL ENEMY AI")]
    public bool normalPredictionEnabled = true;
    public float normalPredictionDistanceThreshold = 2.5f;
    public float normalPredictionTime = 0.25f;
    public float normalMaxPredictionDistance = 1.5f;

    public bool normalSeparationEnabled = true;
    public float normalSeparationRadius = 0.75f;
    public float normalSeparationStrength = 0.65f;

    [Header("PROJECTILE ENEMY")]
    public int projectileEnemyCount = 0;
    public float projectileEnemySpawnInterval = 5f;
    public float projectileMoveSpeed = 3f;
    public float projectileStoppingDistance = 7f;
    public float projectileRetreatDistance = 4f;
    public float projectileFireRate = 1.5f;
    public float projectileSpeed = 6f;

    [Header("HUNTER ENEMY")]
    public int hunterEnemyCount = 0;
    public float hunterEnemySpawnInterval = 8f;
    public float hunterRepositionTime = 1.2f;
    public float hunterWarningDuration = 1f;
    public float hunterChargeSpeed = 15f;
    public float hunterStunDuration = 1f;

    [Header("BOSS")]
    public bool bossEnabled = false;
    public int bossSpawnScore = 75;
    public float bossSpeed = 1.2f;
    public bool bossCanSplit = true;
    public float bossSplitDelay = 0.8f;
    public float bossSplitDistance = 1.2f;
    public float miniBossSpeed = 2.5f;

    [Header("BEACON ENEMY")]
    public int beaconEnemyCount = 0;
    public float beaconMinSpawnTime = 10f;
    public float beaconMaxSpawnTime = 20f;

    [Header("BEACON BUFF")]
    public float beaconBuffDuration = 15f;
    public float beaconBuffSizeMultiplier = 1.25f;
    public float beaconNormalSpeedMultiplier = 1.35f;
    public float beaconNormalMaxSpeedMultiplier = 1.25f;
    public float beaconProjectileMoveMultiplier = 1.2f;
    public float beaconProjectileShotMultiplier = 1.25f;
    public float beaconProjectileFireMultiplier = 1.25f;
    public float beaconHunterRepositionMultiplier = 0.8f;
    public float beaconHunterWarningMultiplier = 0.8f;
    public float beaconHunterChargeMultiplier = 1.25f;
    public float beaconHunterStunMultiplier = 0.8f;

    [Header("POWER UPS")]
    public bool armorEnabled = false;
    public bool slowEnabled = false;
    public float armorMinSpawnTime = 15f;
    public float armorMaxSpawnTime = 30f;
    public float slowMinSpawnTime = 8f;
    public float slowMaxSpawnTime = 20f;

    [Header("VERTICAL LASER")]
    public bool verticalLaserEnabled = false;
    public float verticalLaserMinSpawnTime = 8f;
    public float verticalLaserMaxSpawnTime = 25f;
    public float verticalLaserWarningDuration = 2f;
    public float verticalLaserLifeTime = 1.5f;
    public float verticalLaserWidth = 0.5f;
    public float verticalLaserHeightExtra = 1f;

    [Header("HORIZONTAL LASER")]
    public bool horizontalLaserEnabled = false;
    public float horizontalLaserMinSpawnTime = 8f;
    public float horizontalLaserMaxSpawnTime = 25f;
    public float horizontalLaserWarningDuration = 2f;
    public float horizontalLaserLifeTime = 1.5f;
    public float horizontalLaserWidth = 0.5f;
    public float horizontalLaserWidthExtra = 1f;

    [Header("BOMBS")]
    public bool bombTrapEnabled = false;
    public float bombMinSpawnTime = 6f;
    public float bombMaxSpawnTime = 14f;
    public int maxBombCount = 3;
}