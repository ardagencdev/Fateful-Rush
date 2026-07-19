using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("CURRENT LEVEL")]
    public LevelConfig currentLevel;

    [Header("CORE MANAGERS")]
    public PlayerCoinCollector coinCollector;
    public CoinManager coinManager;
    public ObstacleSpawner obstacleSpawner;
    public EnemySpawner enemySpawner;
    public PowerUpSpawner powerUpSpawner;
    public BeaconEnemySpawner beaconEnemySpawner;

    [Header("LASERS / TRAPS")]
    public LaserWallSpawner verticalLaserSpawner;
    public HorizontalLaserWallSpawner horizontalLaserSpawner;
    public SpaceBombSpawner bombTrapSpawner;

    [Header("PLAYER")]
    public PlayerMovement playerMovement;
    public PlayerDash playerDash;
    public PlayerArmor playerArmor;
    public GameObject dashButton;
    public VoidCloneAbility voidCloneAbility;
    public GameObject cloneButton;

    [Header("UI")]
    public ComboUI comboUI;

    [Header("BACKGROUND")]
    public ParticleSystem nearStars;

    private float originalNearStarsSpeed = 1f;
    private float originalNearStarsSize = 1f;

    private bool initialized = false;

    private void Start()
    {
        InitializeLevel();
    }

    public void InitializeLevel()
    {
        if (initialized) return;
        initialized = true;

        CacheStarDefaults();

        if (SelectedLevelData.selectedLevel != null)
            currentLevel = SelectedLevelData.selectedLevel;

        if (currentLevel != null)
        {
            Debug.Log(
                $"[LevelManager] Loaded Config: {currentLevel.name} | Mode: {SelectedLevelData.launchMode}"
            );
        }
        else
        {
            Debug.LogError("[LevelManager] currentLevel NULL! Config atanmadı.");
        }

        ApplyLevelConfig();
    }

    private void ApplyLevelConfig()
    {
        if (currentLevel == null) return;

        ApplyPlayer();
        ApplyUI();
        ApplyBackground();
        ApplyCoins();
        ApplyObstacles();
        ApplyEnemies();
        ApplyBeaconEnemy();
        ApplyPowerUps();
        ApplyLasersAndTraps();
    }

    private void ApplyPlayer()
    {
        if (coinCollector != null)
        {
            coinCollector.winScore = currentLevel.winScore;
            coinCollector.comboTimeLimit = currentLevel.comboTimeLimit;
            coinCollector.maxCombo = currentLevel.maxCombo;
            coinCollector.comboSpeedStages = currentLevel.comboSpeedStages;
            coinCollector.comboSpeedStages = currentLevel.comboSpeedStages;
        }

        if (playerMovement != null)
        {
            playerMovement.speed = currentLevel.playerMoveSpeed;
            playerMovement.comboSpeedBonus = currentLevel.playerComboSpeedBonus;
            playerMovement.comboSpeedStages = currentLevel.comboSpeedStages;
        }

        if (playerDash != null)
        {
            playerDash.enabled = currentLevel.dashEnabled;
            playerDash.dashDistance = currentLevel.dashDistance;
            playerDash.dashDuration = currentLevel.dashDuration;
            playerDash.dashCooldown = currentLevel.dashCooldown;
        }

        if (playerArmor != null)
            playerArmor.immuneDuration = currentLevel.armorImmuneDuration;

        SetUIVisible(dashButton, currentLevel.dashEnabled);
        SetUIVisible(cloneButton, currentLevel.cloneEnabled);

        if (voidCloneAbility != null)
        {
            voidCloneAbility.enabled = currentLevel.cloneEnabled;
            voidCloneAbility.cloneDuration = currentLevel.cloneDuration;
            voidCloneAbility.SetCloneCooldown(currentLevel.cloneCooldown);
            voidCloneAbility.SetCloneUses(currentLevel.cloneUses);
        }
    }

    private void ApplyUI()
    {
        if (coinCollector != null)
        {
            coinCollector.comboEnabled = currentLevel.comboEnabled;
            coinCollector.comboTimeLimit = currentLevel.comboTimeLimit;
            coinCollector.maxCombo = currentLevel.maxCombo;
        }

        if (comboUI != null)
            SetUIVisible(comboUI.gameObject, currentLevel.comboEnabled);
    }

    private void ApplyBackground()
    {
        if (nearStars == null) return;

        var main = nearStars.main;
        main.startColor = currentLevel.nearStarsColor;
        main.startSpeed = originalNearStarsSpeed * currentLevel.nearStarsSpeedMultiplier;
        main.startSize = originalNearStarsSize * currentLevel.nearStarsSizeMultiplier;

        var emission = nearStars.emission;
        emission.rateOverTime = currentLevel.nearStarsEmissionRate;
    }

    private void ApplyCoins()
    {
        if (coinManager == null) return;

        coinManager.spawnInterval = currentLevel.coinSpawnInterval;
        coinManager.maxCoinCount = currentLevel.maxCoinCount;
        coinManager.normalCoinChance = currentLevel.normalCoinEnabled ? currentLevel.normalCoinChance : 0f;
        coinManager.goldCoinChance = currentLevel.goldCoinEnabled ? currentLevel.goldCoinChance : 0f;
        coinManager.rareCoinChance = currentLevel.rareCoinEnabled ? currentLevel.rareCoinChance : 0f;
        coinManager.normalCoinValue = currentLevel.normalCoinValue;
        coinManager.goldCoinValue = currentLevel.goldCoinValue;
        coinManager.rareCoinValue = currentLevel.rareCoinValue;
        coinManager.ResetSpawner();
    }

    private void ApplyObstacles()
    {
        if (obstacleSpawner == null) return;

        obstacleSpawner.levelObstacles = currentLevel.levelObstacles;
        obstacleSpawner.obstacleSpawnMode = currentLevel.obstacleSpawnMode;
        obstacleSpawner.randomObstacleCount = currentLevel.randomObstacleCount;

        obstacleSpawner.ClearObstacles();
        obstacleSpawner.SpawnObstacles();
    }

    private void ApplyEnemies()
    {
        if (enemySpawner == null) return;

        enemySpawner.normalEnemyCount = currentLevel.normalEnemyCount;
        enemySpawner.normalEnemySpawnInterval = currentLevel.normalEnemySpawnInterval;

        enemySpawner.projectileEnemyCount = currentLevel.projectileEnemyCount;
        enemySpawner.projectileEnemySpawnInterval = currentLevel.projectileEnemySpawnInterval;

        enemySpawner.hunterEnemyCount = currentLevel.hunterEnemyCount;
        enemySpawner.hunterEnemySpawnInterval = currentLevel.hunterEnemySpawnInterval;

        enemySpawner.normalMinStartSpeed = currentLevel.normalMinStartSpeed;
        enemySpawner.normalMaxStartSpeed = currentLevel.normalMaxStartSpeed;
        enemySpawner.normalMaxSpeed = currentLevel.normalMaxSpeed;
        enemySpawner.normalSpeedIncreaseRate = currentLevel.normalSpeedIncreaseRate;

        enemySpawner.projectileMoveSpeed = currentLevel.projectileMoveSpeed;
        enemySpawner.projectileStoppingDistance = currentLevel.projectileStoppingDistance;
        enemySpawner.projectileRetreatDistance = currentLevel.projectileRetreatDistance;
        enemySpawner.projectileFireRate = currentLevel.projectileFireRate;
        enemySpawner.projectileSpeed = currentLevel.projectileSpeed;

        enemySpawner.hunterRepositionTime = currentLevel.hunterRepositionTime;
        enemySpawner.hunterWarningDuration = currentLevel.hunterWarningDuration;
        enemySpawner.hunterChargeSpeed = currentLevel.hunterChargeSpeed;
        enemySpawner.hunterStunDuration = currentLevel.hunterStunDuration;

        enemySpawner.bossEnabled = currentLevel.bossEnabled;
        enemySpawner.bossSpawnScore = currentLevel.bossSpawnScore;
        enemySpawner.bossSpeed = currentLevel.bossSpeed;
        enemySpawner.bossCanSplit = currentLevel.bossCanSplit;
        enemySpawner.bossSplitDelay = currentLevel.bossSplitDelay;
        enemySpawner.bossSplitDistance = currentLevel.bossSplitDistance;
        enemySpawner.miniBossSpeed = currentLevel.miniBossSpeed;

        enemySpawner.ResetSpawner();
    }

    private void ApplyBeaconEnemy()
    {
        if (beaconEnemySpawner == null) return;

        beaconEnemySpawner.ApplyLevelSettings(
            currentLevel.beaconEnemyCount,
            currentLevel.beaconMinSpawnTime,
            currentLevel.beaconMaxSpawnTime
        );

        beaconEnemySpawner.ApplyBuffSettings(
            currentLevel.beaconBuffDuration,
            currentLevel.beaconBuffSizeMultiplier,
            currentLevel.beaconNormalSpeedMultiplier,
            currentLevel.beaconNormalMaxSpeedMultiplier,
            currentLevel.beaconProjectileMoveMultiplier,
            currentLevel.beaconProjectileShotMultiplier,
            currentLevel.beaconProjectileFireMultiplier,
            currentLevel.beaconHunterRepositionMultiplier,
            currentLevel.beaconHunterWarningMultiplier,
            currentLevel.beaconHunterChargeMultiplier,
            currentLevel.beaconHunterStunMultiplier
        );
    }

    private void ApplyPowerUps()
    {
        if (powerUpSpawner == null) return;

        powerUpSpawner.ApplyLevelSettings(
            currentLevel.slowEnabled,
            currentLevel.armorEnabled,
            currentLevel.slowMinSpawnTime,
            currentLevel.slowMaxSpawnTime,
            currentLevel.armorMinSpawnTime,
            currentLevel.armorMaxSpawnTime,
            currentLevel.slowMultiplier,
            currentLevel.slowDuration
        );
    }

    private void ApplyLasersAndTraps()
    {
        if (verticalLaserSpawner != null)
        {
            verticalLaserSpawner.gameObject.SetActive(currentLevel.verticalLaserEnabled);

            if (currentLevel.verticalLaserEnabled)
            {
                verticalLaserSpawner.ApplyLevelSettings(
                    currentLevel.verticalLaserMinSpawnTime,
                    currentLevel.verticalLaserMaxSpawnTime,
                    currentLevel.verticalLaserWarningDuration,
                    currentLevel.verticalLaserLifeTime,
                    currentLevel.verticalLaserWidth,
                    currentLevel.verticalLaserHeightExtra
                );
            }
        }

        if (horizontalLaserSpawner != null)
        {
            if (!currentLevel.horizontalLaserEnabled)
            {
                horizontalLaserSpawner.StopLaserSystem();
                horizontalLaserSpawner.gameObject.SetActive(false);
            }
            else
            {
                horizontalLaserSpawner.gameObject.SetActive(true);

                horizontalLaserSpawner.ApplyLevelSettings(
                    currentLevel.horizontalLaserMinSpawnTime,
                    currentLevel.horizontalLaserMaxSpawnTime,
                    currentLevel.horizontalLaserWarningDuration,
                    currentLevel.horizontalLaserLifeTime,
                    currentLevel.horizontalLaserWidth,
                    currentLevel.horizontalLaserWidthExtra
                );
            }
        }

        if (bombTrapSpawner != null)
        {
            bombTrapSpawner.gameObject.SetActive(currentLevel.bombTrapEnabled);

            if (currentLevel.bombTrapEnabled)
            {
                bombTrapSpawner.ApplyLevelSettings(
                    currentLevel.bombMinSpawnTime,
                    currentLevel.bombMaxSpawnTime,
                    currentLevel.maxBombCount
                );
            }
        }
    }

    private void CacheStarDefaults()
    {
        if (nearStars == null) return;

        var main = nearStars.main;
        originalNearStarsSpeed = main.startSpeed.constant;
        originalNearStarsSize = main.startSize.constant;
    }

    private void SetUIVisible(GameObject obj, bool visible)
    {
        if (obj == null) return;

        obj.SetActive(true);

        CanvasGroup group = obj.GetComponent<CanvasGroup>();
        if (group == null)
            group = obj.AddComponent<CanvasGroup>();

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}