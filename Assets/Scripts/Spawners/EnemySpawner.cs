using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public PlayerMovement playerMovement;
    public BossScreenEffect bossScreenEffect;
    public ParticleSystem nearStars;

    [Header("Prefabs")]
    public GameObject normalEnemyPrefab;
    public GameObject projectileEnemyPrefab;
    public GameObject hunterEnemyPrefab;
    public GameObject bossPrefab;

    [Header("Normal Enemy Spawn")]
    public int normalEnemyCount;
    public float normalEnemySpawnInterval = 2.5f;

    [Header("Projectile Enemy Spawn")]
    public int projectileEnemyCount;
    public float projectileEnemySpawnInterval = 5f;

    [Header("Hunter Enemy Spawn")]
    public int hunterEnemyCount;
    public float hunterEnemySpawnInterval = 8f;

    [Header("Normal Enemy Settings")]
    public float normalMinStartSpeed = 1.5f;
    public float normalMaxStartSpeed = 2.5f;
    public float normalMaxSpeed = 7f;
    public float normalSpeedIncreaseRate = 0.1f;

    [Header("Projectile Enemy Settings")]
    public float projectileMoveSpeed = 3f;
    public float projectileStoppingDistance = 7f;
    public float projectileRetreatDistance = 4f;
    public float projectileFireRate = 1.5f;
    public float projectileSpeed = 6f;

    [Header("Hunter Enemy Settings")]
    public float hunterRepositionTime = 1.2f;
    public float hunterWarningDuration = 1f;
    public float hunterChargeSpeed = 15f;
    public float hunterStunDuration = 1f;

    [Header("Boss Settings")]
    public bool bossEnabled;
    public int bossSpawnScore = 75;
    public float bossSpeed = 1.2f;
    public bool bossCanSplit = true;
    public float bossSplitDelay = 0.8f;
    public float bossSplitDistance = 1.2f;
    public float miniBossSpeed = 2.5f;

    [Header("Spawn Area")]
    public float minDistanceFromPlayer = 3f;
    public float edgeOffset = 0.8f;

    [Header("Obstacle Check")]
    public LayerMask obstacleLayer;
    public float spawnCheckRadius = 0.7f;
    public int maxSpawnAttempts = 30;

    private float normalSpawnTimer;
    private float projectileSpawnTimer;
    private float hunterSpawnTimer;

    private bool bossSpawned;

    private int spawnedNormal;
    private int spawnedProjectile;
    private int spawnedHunter;

    private ContactFilter2D obstacleFilter;

    private readonly Collider2D[] spawnCheckHits = new Collider2D[8];
    private readonly System.Collections.Generic.List<GameObject> activeEnemies = new System.Collections.Generic.List<GameObject>(32);

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (player == null && playerMovement != null)
            player = playerMovement.transform;

        obstacleFilter = new ContactFilter2D();
        obstacleFilter.SetLayerMask(obstacleLayer);
        obstacleFilter.useTriggers = true;
    }

    private void Update()
    {
        if (!GameStateManager.IsGameplayStarted) return;
        if (playerMovement == null || playerMovement.IsGameOver) return;

        HandleNormalEnemySpawn();
        HandleProjectileEnemySpawn();
        HandleHunterEnemySpawn();
    }

    public void ResetSpawner()
    {
        normalSpawnTimer = 0f;
        projectileSpawnTimer = 0f;
        hunterSpawnTimer = 0f;

        bossSpawned = false;

        spawnedNormal = 0;
        spawnedProjectile = 0;
        spawnedHunter = 0;

        activeEnemies.Clear();
    }

    private void HandleNormalEnemySpawn()
    {
        if (spawnedNormal >= normalEnemyCount) return;
        if (normalEnemyPrefab == null) return;

        normalSpawnTimer += Time.deltaTime;

        if (normalSpawnTimer < normalEnemySpawnInterval) return;

        normalSpawnTimer = 0f;
        SpawnNormalEnemy();
    }

    private void HandleProjectileEnemySpawn()
    {
        if (spawnedProjectile >= projectileEnemyCount) return;
        if (projectileEnemyPrefab == null) return;

        projectileSpawnTimer += Time.deltaTime;

        if (projectileSpawnTimer < projectileEnemySpawnInterval) return;

        projectileSpawnTimer = 0f;
        SpawnProjectileEnemy();
    }

    private void HandleHunterEnemySpawn()
    {
        if (spawnedHunter >= hunterEnemyCount) return;
        if (hunterEnemyPrefab == null) return;

        hunterSpawnTimer += Time.deltaTime;

        if (hunterSpawnTimer < hunterEnemySpawnInterval) return;

        hunterSpawnTimer = 0f;
        SpawnHunterEnemy();
    }

    private void SpawnNormalEnemy()
    {
        if (!TryGetSafeSpawnPosition(out Vector2 spawnPos)) return;

        GameObject enemy = Instantiate(normalEnemyPrefab, spawnPos, Quaternion.identity);
        spawnedNormal++;
        activeEnemies.Add(enemy);

        ApplyEnemySettings(enemy);
        AssignPlayer(enemy);
        RefreshBuffTarget(enemy);
    }

    private void SpawnProjectileEnemy()
    {
        if (!TryGetSafeSpawnPosition(out Vector2 spawnPos)) return;

        GameObject enemy = Instantiate(projectileEnemyPrefab, spawnPos, Quaternion.identity);
        spawnedProjectile++;
        activeEnemies.Add(enemy);

        ApplyEnemySettings(enemy);
        AssignPlayer(enemy);
        RefreshBuffTarget(enemy);
    }

    private void SpawnHunterEnemy()
    {
        if (!TryGetSafeSpawnPosition(out Vector2 spawnPos)) return;

        GameObject enemy = Instantiate(hunterEnemyPrefab, spawnPos, Quaternion.identity);
        spawnedHunter++;
        activeEnemies.Add(enemy);

        ApplyEnemySettings(enemy);
        AssignPlayer(enemy);
        RefreshBuffTarget(enemy);
    }

    private void ApplyEnemySettings(GameObject enemy)
    {
        EnemyFollow normal = enemy.GetComponent<EnemyFollow>();
        if (normal != null)
        {
            normal.minStartSpeed = normalMinStartSpeed;
            normal.maxStartSpeed = normalMaxStartSpeed;
            normal.maxSpeed = normalMaxSpeed;
            normal.speedIncreaseRate = normalSpeedIncreaseRate;
        }

        ProjectileEnemyFollow projectile = enemy.GetComponent<ProjectileEnemyFollow>();
        if (projectile != null)
        {
            projectile.moveSpeed = projectileMoveSpeed;
            projectile.stoppingDistance = projectileStoppingDistance;
            projectile.retreatDistance = projectileRetreatDistance;
            projectile.fireRate = projectileFireRate;
            projectile.projectileSpeed = projectileSpeed;
        }

        HunterEnemyFollow hunter = enemy.GetComponent<HunterEnemyFollow>();
        if (hunter != null)
        {
            hunter.repositionTime = hunterRepositionTime;
            hunter.warningDuration = hunterWarningDuration;
            hunter.chargeSpeed = hunterChargeSpeed;
            hunter.stunDuration = hunterStunDuration;
        }

        BossEnemyFollow boss = enemy.GetComponent<BossEnemyFollow>();
        if (boss != null)
        {
            boss.speed = bossSpeed;
            boss.canSplit = bossCanSplit;
            boss.splitDelay = bossSplitDelay;
            boss.splitDistance = bossSplitDistance;
            boss.miniBossSpeed = miniBossSpeed;
        }
    }

    private void AssignPlayer(GameObject enemy)
    {
        EnemyFollow normal = enemy.GetComponent<EnemyFollow>();
        if (normal != null)
            normal.player = player;

        ProjectileEnemyFollow projectile = enemy.GetComponent<ProjectileEnemyFollow>();
        if (projectile != null)
            projectile.player = player;

        HunterEnemyFollow hunter = enemy.GetComponent<HunterEnemyFollow>();
        if (hunter != null)
        {
            hunter.player = player;
            hunter.playerMovement = playerMovement;
        }

        BossEnemyFollow boss = enemy.GetComponent<BossEnemyFollow>();
        if (boss != null)
            boss.player = player;
    }

    private void RefreshBuffTarget(GameObject enemy)
    {
        EnemyBuffTarget buffTarget = enemy.GetComponent<EnemyBuffTarget>();

        if (buffTarget != null)
            buffTarget.RefreshBaseValues();
    }

    private bool TryGetSafeSpawnPosition(out Vector2 spawnPos)
    {
        spawnPos = Vector2.zero;

        if (CameraWorldBounds.Instance == null || player == null)
            return false;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            spawnPos = GetRandomEdgePosition();

            if (IsSafePosition(spawnPos))
                return true;
        }

        return false;
    }

    private bool IsSafePosition(Vector2 spawnPos)
    {
        Vector2 playerPos = player.position;
        float minDistanceSqr = minDistanceFromPlayer * minDistanceFromPlayer;

        if ((spawnPos - playerPos).sqrMagnitude < minDistanceSqr)
            return false;

        int hitCount = Physics2D.OverlapCircle(
            spawnPos,
            spawnCheckRadius,
            obstacleFilter,
            spawnCheckHits
        );

        return hitCount == 0;
    }

    private Vector2 GetRandomEdgePosition()
    {
        CameraWorldBounds bounds = CameraWorldBounds.Instance;
        int side = Random.Range(0, 4);

        float minX = bounds.MinX + edgeOffset;
        float maxX = bounds.MaxX - edgeOffset;
        float minY = bounds.MinY + edgeOffset;
        float maxY = bounds.MaxY - edgeOffset;

        switch (side)
        {
            case 0:
                return new Vector2(Random.Range(minX, maxX), maxY);

            case 1:
                return new Vector2(Random.Range(minX, maxX), minY);

            case 2:
                return new Vector2(minX, Random.Range(minY, maxY));

            default:
                return new Vector2(maxX, Random.Range(minY, maxY));
        }
    }

    public void TrySpawnBoss(int currentScore)
    {
        if (!GameStateManager.IsGameplayStarted) return;
        if (!bossEnabled) return;
        if (bossSpawned) return;
        if (currentScore < bossSpawnScore) return;
        if (bossPrefab == null) return;
        if (!TryGetSafeSpawnPosition(out Vector2 spawnPos)) return;

        bossSpawned = true;

        GameObject boss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(boss);

        ApplyEnemySettings(boss);
        AssignPlayer(boss);
        RefreshBuffTarget(boss);

        if (bossScreenEffect != null)
            bossScreenEffect.StartEffect();

        ApplyBossStarEffect();
    }

    private void ApplyBossStarEffect()
    {
        if (nearStars == null) return;

        var main = nearStars.main;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.1f, 0.1f, 0.9f));
    }
}
