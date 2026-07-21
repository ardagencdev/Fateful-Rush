using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private const float FailedSpawnRetryDelay = 0.25f;

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
    [Min(0)]
    public int normalEnemyCount;

    [Min(0f)]
    public float normalEnemySpawnInterval = 2.5f;

    [Header("Projectile Enemy Spawn")]
    [Min(0)]
    public int projectileEnemyCount;

    [Min(0f)]
    public float projectileEnemySpawnInterval = 5f;

    [Header("Hunter Enemy Spawn")]
    [Min(0)]
    public int hunterEnemyCount;

    [Min(0f)]
    public float hunterEnemySpawnInterval = 8f;

    [Header("Normal Enemy Settings")]
    [Min(0f)]
    public float normalMinStartSpeed = 1.5f;

    [Min(0f)]
    public float normalMaxStartSpeed = 2.5f;

    [Min(0f)]
    public float normalMaxSpeed = 7f;

    [Min(0f)]
    public float normalSpeedIncreaseRate = 0.1f;

    [Header("Projectile Enemy Settings")]
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

    [Header("Hunter Enemy Settings")]
    [Min(0f)]
    public float hunterRepositionTime = 1.2f;

    [Min(0f)]
    public float hunterWarningDuration = 1f;

    [Min(0f)]
    public float hunterChargeSpeed = 15f;

    [Min(0f)]
    public float hunterStunDuration = 1f;

    [Header("Boss Settings")]
    public bool bossEnabled;

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

    [Header("Spawn Area")]
    [Min(0f)]
    public float minDistanceFromPlayer = 3f;

    [Min(0f)]
    public float edgeOffset = 0.8f;

    [Header("Obstacle Check")]
    public LayerMask obstacleLayer;

    [Min(0f)]
    public float spawnCheckRadius = 0.7f;

    [Min(1)]
    public int maxSpawnAttempts = 30;

    private float normalSpawnTimer;
    private float projectileSpawnTimer;
    private float hunterSpawnTimer;

    private bool bossSpawned;

    private int spawnedNormal;
    private int spawnedProjectile;
    private int spawnedHunter;

    private ContactFilter2D obstacleFilter;

    private readonly Collider2D[] spawnCheckHits =
        new Collider2D[16];

    private readonly List<GameObject> activeEnemies =
        new List<GameObject>(32);

    private void Awake()
    {
        RefreshPlayerReferences();
        RefreshObstacleFilter();
    }

    private void Update()
    {
        if (!GameStateManager.IsGameplayStarted)
            return;

        if (playerMovement == null)
        {
            RefreshPlayerReferences();

            if (playerMovement == null)
                return;
        }

        if (playerMovement.IsGameOver)
            return;

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

        /*
         * Sahnedeki canlı enemy referanslarını unutmuyoruz.
         * Yalnızca daha önce yok edilmiş olanları temizliyoruz.
         */
        RemoveDestroyedEnemies();

        RefreshPlayerReferences();
        RefreshObstacleFilter();
    }

    private void HandleNormalEnemySpawn()
    {
        if (spawnedNormal >= normalEnemyCount)
            return;

        if (normalEnemyPrefab == null)
            return;

        normalSpawnTimer += Time.deltaTime;

        if (normalSpawnTimer < normalEnemySpawnInterval)
            return;

        if (TrySpawnNormalEnemy())
        {
            normalSpawnTimer = 0f;
        }
        else
        {
            normalSpawnTimer = Mathf.Max(
                0f,
                normalEnemySpawnInterval -
                FailedSpawnRetryDelay
            );
        }
    }

    private void HandleProjectileEnemySpawn()
    {
        if (spawnedProjectile >= projectileEnemyCount)
            return;

        if (projectileEnemyPrefab == null)
            return;

        projectileSpawnTimer += Time.deltaTime;

        if (projectileSpawnTimer <
            projectileEnemySpawnInterval)
        {
            return;
        }

        if (TrySpawnProjectileEnemy())
        {
            projectileSpawnTimer = 0f;
        }
        else
        {
            projectileSpawnTimer = Mathf.Max(
                0f,
                projectileEnemySpawnInterval -
                FailedSpawnRetryDelay
            );
        }
    }

    private void HandleHunterEnemySpawn()
    {
        if (spawnedHunter >= hunterEnemyCount)
            return;

        if (hunterEnemyPrefab == null)
            return;

        hunterSpawnTimer += Time.deltaTime;

        if (hunterSpawnTimer < hunterEnemySpawnInterval)
            return;

        if (TrySpawnHunterEnemy())
        {
            hunterSpawnTimer = 0f;
        }
        else
        {
            hunterSpawnTimer = Mathf.Max(
                0f,
                hunterEnemySpawnInterval -
                FailedSpawnRetryDelay
            );
        }
    }

    private bool TrySpawnNormalEnemy()
    {
        if (!TryCreateEnemy(
                normalEnemyPrefab,
                out GameObject enemy))
        {
            return false;
        }

        spawnedNormal++;
        ConfigureSpawnedEnemy(enemy);

        return true;
    }

    private bool TrySpawnProjectileEnemy()
    {
        if (!TryCreateEnemy(
                projectileEnemyPrefab,
                out GameObject enemy))
        {
            return false;
        }

        spawnedProjectile++;
        ConfigureSpawnedEnemy(enemy);

        return true;
    }

    private bool TrySpawnHunterEnemy()
    {
        if (!TryCreateEnemy(
                hunterEnemyPrefab,
                out GameObject enemy))
        {
            return false;
        }

        spawnedHunter++;
        ConfigureSpawnedEnemy(enemy);

        return true;
    }

    private bool TryCreateEnemy(
        GameObject enemyPrefab,
        out GameObject enemy)
    {
        enemy = null;

        if (enemyPrefab == null)
            return false;

        if (!TryGetSafeSpawnPosition(
                out Vector2 spawnPosition))
        {
            return false;
        }

        enemy = Instantiate(
            enemyPrefab,
            spawnPosition,
            Quaternion.identity
        );

        activeEnemies.Add(enemy);
        return true;
    }

    private void ConfigureSpawnedEnemy(GameObject enemy)
    {
        if (enemy == null)
            return;

        ApplyEnemySettings(enemy);
        AssignPlayer(enemy);
        RefreshBuffTarget(enemy);
    }

    private void ApplyEnemySettings(GameObject enemy)
    {
        EnemyFollow normal =
            enemy.GetComponent<EnemyFollow>();

        if (normal != null)
        {
            normal.minStartSpeed =
                normalMinStartSpeed;

            normal.maxStartSpeed =
                normalMaxStartSpeed;

            normal.maxSpeed =
                normalMaxSpeed;

            normal.speedIncreaseRate =
                normalSpeedIncreaseRate;
        }

        ProjectileEnemyFollow projectile =
            enemy.GetComponent<ProjectileEnemyFollow>();

        if (projectile != null)
        {
            projectile.moveSpeed =
                projectileMoveSpeed;

            projectile.stoppingDistance =
                projectileStoppingDistance;

            projectile.retreatDistance =
                projectileRetreatDistance;

            projectile.fireRate =
                projectileFireRate;

            projectile.projectileSpeed =
                projectileSpeed;
        }

        HunterEnemyFollow hunter =
            enemy.GetComponent<HunterEnemyFollow>();

        if (hunter != null)
        {
            hunter.repositionTime =
                hunterRepositionTime;

            hunter.warningDuration =
                hunterWarningDuration;

            hunter.chargeSpeed =
                hunterChargeSpeed;

            hunter.stunDuration =
                hunterStunDuration;
        }

        BossEnemyFollow boss =
            enemy.GetComponent<BossEnemyFollow>();

        if (boss != null)
        {
            boss.speed =
                bossSpeed;

            boss.canSplit =
                bossCanSplit;

            boss.splitDelay =
                bossSplitDelay;

            boss.splitDistance =
                bossSplitDistance;

            boss.miniBossSpeed =
                miniBossSpeed;
        }
    }

    private void AssignPlayer(GameObject enemy)
    {
        EnemyFollow normal =
            enemy.GetComponent<EnemyFollow>();

        if (normal != null)
            normal.player = player;

        ProjectileEnemyFollow projectile =
            enemy.GetComponent<ProjectileEnemyFollow>();

        if (projectile != null)
            projectile.player = player;

        HunterEnemyFollow hunter =
            enemy.GetComponent<HunterEnemyFollow>();

        if (hunter != null)
        {
            hunter.player = player;
            hunter.playerMovement = playerMovement;
        }

        BossEnemyFollow boss =
            enemy.GetComponent<BossEnemyFollow>();

        if (boss != null)
            boss.player = player;
    }

    private static void RefreshBuffTarget(
        GameObject enemy)
    {
        EnemyBuffTarget buffTarget =
            enemy.GetComponent<EnemyBuffTarget>();

        if (buffTarget != null)
            buffTarget.RefreshBaseValues();
    }

    private bool TryGetSafeSpawnPosition(
        out Vector2 spawnPosition)
    {
        spawnPosition = Vector2.zero;

        if (CameraWorldBounds.Instance == null)
            return false;

        if (player == null)
        {
            RefreshPlayerReferences();

            if (player == null)
                return false;
        }

        for (int attempt = 0;
             attempt < maxSpawnAttempts;
             attempt++)
        {
            spawnPosition = GetRandomEdgePosition();

            if (IsSafePosition(spawnPosition))
                return true;
        }

        return false;
    }

    private bool IsSafePosition(Vector2 spawnPosition)
    {
        if (player == null)
            return false;

        Vector2 playerPosition = player.position;

        float minimumDistanceSquared =
            minDistanceFromPlayer *
            minDistanceFromPlayer;

        if ((spawnPosition - playerPosition)
                .sqrMagnitude <
            minimumDistanceSquared)
        {
            return false;
        }

        int hitCount = Physics2D.OverlapCircle(
            spawnPosition,
            spawnCheckRadius,
            obstacleFilter,
            spawnCheckHits
        );

        return hitCount == 0;
    }

    private Vector2 GetRandomEdgePosition()
    {
        CameraWorldBounds bounds =
            CameraWorldBounds.Instance;

        float minX = bounds.MinX + edgeOffset;
        float maxX = bounds.MaxX - edgeOffset;
        float minY = bounds.MinY + edgeOffset;
        float maxY = bounds.MaxY - edgeOffset;

        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0:
                return new Vector2(
                    Random.Range(minX, maxX),
                    maxY
                );

            case 1:
                return new Vector2(
                    Random.Range(minX, maxX),
                    minY
                );

            case 2:
                return new Vector2(
                    minX,
                    Random.Range(minY, maxY)
                );

            default:
                return new Vector2(
                    maxX,
                    Random.Range(minY, maxY)
                );
        }
    }

    public void TrySpawnBoss(int currentScore)
    {
        if (!GameStateManager.IsGameplayStarted)
            return;

        if (!bossEnabled)
            return;

        if (bossSpawned)
            return;

        if (currentScore < bossSpawnScore)
            return;

        if (bossPrefab == null)
            return;

        if (!TryGetSafeSpawnPosition(
                out Vector2 spawnPosition))
        {
            return;
        }

        GameObject boss = Instantiate(
            bossPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (boss == null)
            return;

        bossSpawned = true;
        activeEnemies.Add(boss);

        ConfigureSpawnedEnemy(boss);

        if (bossScreenEffect != null)
            bossScreenEffect.StartEffect();

        ApplyBossStarEffect();
    }

    private void ApplyBossStarEffect()
    {
        if (nearStars == null)
            return;

        ParticleSystem.MainModule main =
            nearStars.main;

        main.startColor =
            new ParticleSystem.MinMaxGradient(
                new Color(
                    1f,
                    0.1f,
                    0.1f,
                    0.9f
                )
            );
    }

    private void RefreshPlayerReferences()
    {
        if (playerMovement == null)
        {
            playerMovement =
                FindAnyObjectByType<PlayerMovement>();
        }

        if (playerMovement != null)
        {
            player = playerMovement.transform;
        }
    }

    private void RefreshObstacleFilter()
    {
        obstacleFilter = ContactFilter2D.noFilter;
        obstacleFilter.SetLayerMask(obstacleLayer);
        obstacleFilter.useTriggers = true;
    }

    private void RemoveDestroyedEnemies()
    {
        for (int i = activeEnemies.Count - 1;
             i >= 0;
             i--)
        {
            if (activeEnemies[i] == null)
                activeEnemies.RemoveAt(i);
        }
    }

    private void OnValidate()
    {
        normalEnemyCount =
            Mathf.Max(0, normalEnemyCount);

        projectileEnemyCount =
            Mathf.Max(0, projectileEnemyCount);

        hunterEnemyCount =
            Mathf.Max(0, hunterEnemyCount);

        normalEnemySpawnInterval =
            Mathf.Max(
                0f,
                normalEnemySpawnInterval
            );

        projectileEnemySpawnInterval =
            Mathf.Max(
                0f,
                projectileEnemySpawnInterval
            );

        hunterEnemySpawnInterval =
            Mathf.Max(
                0f,
                hunterEnemySpawnInterval
            );

        normalMinStartSpeed =
            Mathf.Max(0f, normalMinStartSpeed);

        normalMaxStartSpeed =
            Mathf.Max(
                normalMinStartSpeed,
                normalMaxStartSpeed
            );

        normalMaxSpeed =
            Mathf.Max(0f, normalMaxSpeed);

        normalSpeedIncreaseRate =
            Mathf.Max(
                0f,
                normalSpeedIncreaseRate
            );

        projectileMoveSpeed =
            Mathf.Max(0f, projectileMoveSpeed);

        projectileStoppingDistance =
            Mathf.Max(
                0f,
                projectileStoppingDistance
            );

        projectileRetreatDistance =
            Mathf.Max(
                0f,
                projectileRetreatDistance
            );

        projectileFireRate =
            Mathf.Max(0.01f, projectileFireRate);

        projectileSpeed =
            Mathf.Max(0f, projectileSpeed);

        hunterRepositionTime =
            Mathf.Max(0f, hunterRepositionTime);

        hunterWarningDuration =
            Mathf.Max(0f, hunterWarningDuration);

        hunterChargeSpeed =
            Mathf.Max(0f, hunterChargeSpeed);

        hunterStunDuration =
            Mathf.Max(0f, hunterStunDuration);

        bossSpawnScore =
            Mathf.Max(0, bossSpawnScore);

        bossSpeed =
            Mathf.Max(0f, bossSpeed);

        bossSplitDelay =
            Mathf.Max(0f, bossSplitDelay);

        bossSplitDistance =
            Mathf.Max(0f, bossSplitDistance);

        miniBossSpeed =
            Mathf.Max(0f, miniBossSpeed);

        minDistanceFromPlayer =
            Mathf.Max(
                0f,
                minDistanceFromPlayer
            );

        edgeOffset =
            Mathf.Max(0f, edgeOffset);

        spawnCheckRadius =
            Mathf.Max(
                0f,
                spawnCheckRadius
            );

        maxSpawnAttempts =
            Mathf.Max(1, maxSpawnAttempts);

        if (Application.isPlaying)
            RefreshObstacleFilter();
    }
}