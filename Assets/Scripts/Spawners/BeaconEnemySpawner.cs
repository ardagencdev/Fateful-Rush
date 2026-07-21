using System.Collections;
using UnityEngine;

public class BeaconEnemySpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject beaconEnemyPrefab;

    [Header("Level Control")]
    [Min(0)]
    public int maxBeaconCount = 1;

    [Header("Spawn Time")]
    [Min(0f)]
    public float minSpawnTime = 1f;

    [Min(0f)]
    public float maxSpawnTime = 2f;

    [Header("Beacon Buff")]
    [Min(0.1f)]
    public float buffDuration = 15f;

    [Min(0.1f)]
    public float buffSizeMultiplier = 1.25f;

    [Min(0.1f)]
    public float normalSpeedMultiplier = 1.35f;

    [Min(0.1f)]
    public float normalMaxSpeedMultiplier = 1.25f;

    [Min(0.1f)]
    public float projectileMoveMultiplier = 1.2f;

    [Min(0.1f)]
    public float projectileShotMultiplier = 1.25f;

    [Min(0.1f)]
    public float projectileFireMultiplier = 1.25f;

    [Min(0.1f)]
    public float hunterRepositionMultiplier = 0.8f;

    [Min(0.1f)]
    public float hunterWarningMultiplier = 0.8f;

    [Min(0.1f)]
    public float hunterChargeMultiplier = 1.25f;

    [Min(0.1f)]
    public float hunterStunMultiplier = 0.8f;

    [Header("Spawn Rules")]
    [Min(0f)]
    public float spawnPadding = 1f;

    [Min(0f)]
    public float checkRadius = 0.8f;

    [Min(0f)]
    public float minDistanceFromPlayer = 4f;

    [Min(1)]
    public int maxAttempts = 50;

    [Header("References")]
    public Transform player;
    public PlayerMovement playerMovement;

    private float timer;
    private int totalSpawnedCount;
    private bool initialized;

    private int wallLayerIndex;
    private int obstacleLayerIndex;

    private ContactFilter2D spawnFilter;

    private readonly Collider2D[] spawnHits =
        new Collider2D[32];

    private void Awake()
    {
        RefreshPlayerReferences();

        wallLayerIndex =
            LayerMask.NameToLayer("Wall");

        obstacleLayerIndex =
            LayerMask.NameToLayer("Obstacle");

        spawnFilter = ContactFilter2D.noFilter;
        spawnFilter.useTriggers = true;
    }

    private IEnumerator Start()
    {
        /*
         * LevelManager'ın ayarları uygulayabilmesi için
         * bir frame bekliyoruz.
         */
        yield return null;

        if (!initialized)
            InitializeSpawner();
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (!GameStateManager.IsGameplayStarted)
            return;

        if (maxBeaconCount <= 0)
            return;

        if (totalSpawnedCount >= maxBeaconCount)
            return;

        if (playerMovement != null &&
            playerMovement.IsGameOver)
        {
            return;
        }

        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        if (TrySpawnBeacon())
        {
            totalSpawnedCount++;

            if (totalSpawnedCount < maxBeaconCount)
                ResetSpawnerTimer();
        }
        else
        {
            /*
             * Uygun bir konum bulunamadığında her frame
             * tekrar denemek yerine kısa süre bekliyoruz.
             */
            timer = 0.25f;
        }
    }

    public void ApplyLevelSettings(
        int count,
        float minTime,
        float maxTime
    )
    {
        maxBeaconCount = Mathf.Max(
            0,
            count
        );

        minSpawnTime = Mathf.Max(
            0f,
            minTime
        );

        maxSpawnTime = Mathf.Max(
            minSpawnTime,
            maxTime
        );

        InitializeSpawner();
    }

    public void ApplyBuffSettings(
        float duration,
        float sizeMult,
        float nSpeedMult,
        float nMaxMult,
        float pMoveMult,
        float pShotMult,
        float pFireMult,
        float hRepMult,
        float hWarnMult,
        float hChargeMult,
        float hStunMult
    )
    {
        buffDuration = Mathf.Max(
            0.1f,
            duration
        );

        buffSizeMultiplier = Mathf.Max(
            0.1f,
            sizeMult
        );

        normalSpeedMultiplier = Mathf.Max(
            0.1f,
            nSpeedMult
        );

        normalMaxSpeedMultiplier = Mathf.Max(
            0.1f,
            nMaxMult
        );

        projectileMoveMultiplier = Mathf.Max(
            0.1f,
            pMoveMult
        );

        projectileShotMultiplier = Mathf.Max(
            0.1f,
            pShotMult
        );

        projectileFireMultiplier = Mathf.Max(
            0.1f,
            pFireMult
        );

        hunterRepositionMultiplier = Mathf.Max(
            0.1f,
            hRepMult
        );

        hunterWarningMultiplier = Mathf.Max(
            0.1f,
            hWarnMult
        );

        hunterChargeMultiplier = Mathf.Max(
            0.1f,
            hChargeMult
        );

        hunterStunMultiplier = Mathf.Max(
            0.1f,
            hStunMult
        );
    }

    private void InitializeSpawner()
    {
        initialized = true;
        totalSpawnedCount = 0;

        /*
         * GameObject'u kapatmak yerine yalnızca spawner
         * componentini kapatıyoruz. Böylece LevelManager
         * ileride tekrar ayar uygulayabilir.
         */
        enabled = maxBeaconCount > 0;

        if (!enabled)
            return;

        RefreshPlayerReferences();
        ResetSpawnerTimer();
    }

    private void ResetSpawnerTimer()
    {
        timer = Random.Range(
            minSpawnTime,
            maxSpawnTime
        );
    }

    private bool TrySpawnBeacon()
    {
        if (beaconEnemyPrefab == null)
            return false;

        if (CameraWorldBounds.Instance == null)
            return false;

        if (!TryGetSpawnPosition(
                out Vector2 spawnPosition))
        {
            return false;
        }

        GameObject beaconObject = Instantiate(
            beaconEnemyPrefab,
            spawnPosition,
            Quaternion.identity
        );

        BeaconEnemy beaconEnemy =
            beaconObject.GetComponent<BeaconEnemy>();

        if (beaconEnemy == null)
        {
            Debug.LogError(
                $"Beacon prefabında BeaconEnemy componenti bulunamadı: " +
                $"{beaconEnemyPrefab.name}",
                beaconEnemyPrefab
            );

            Destroy(beaconObject);
            return false;
        }

        ConfigureBeacon(beaconEnemy);

        return true;
    }

    private void ConfigureBeacon(
        BeaconEnemy beaconEnemy
    )
    {
        beaconEnemy.player = player;
        beaconEnemy.playerMovement = playerMovement;

        beaconEnemy.buffDuration =
            buffDuration;

        beaconEnemy.buffSizeMultiplier =
            buffSizeMultiplier;

        beaconEnemy.normalSpeedMultiplier =
            normalSpeedMultiplier;

        beaconEnemy.normalMaxSpeedMultiplier =
            normalMaxSpeedMultiplier;

        beaconEnemy.projectileMoveMultiplier =
            projectileMoveMultiplier;

        beaconEnemy.projectileShotMultiplier =
            projectileShotMultiplier;

        beaconEnemy.projectileFireMultiplier =
            projectileFireMultiplier;

        beaconEnemy.hunterRepositionMultiplier =
            hunterRepositionMultiplier;

        beaconEnemy.hunterWarningMultiplier =
            hunterWarningMultiplier;

        beaconEnemy.hunterChargeMultiplier =
            hunterChargeMultiplier;

        beaconEnemy.hunterStunMultiplier =
            hunterStunMultiplier;
    }

    private bool TryGetSpawnPosition(
        out Vector2 spawnPosition
    )
    {
        for (int attempt = 0;
             attempt < maxAttempts;
             attempt++)
        {
            spawnPosition =
                CameraWorldBounds.Instance
                    .RandomPointInside(spawnPadding);

            if (IsValidPosition(spawnPosition))
                return true;
        }

        spawnPosition = Vector2.zero;
        return false;
    }

    private bool IsValidPosition(Vector2 position)
    {
        if (player != null)
        {
            float minimumDistanceSquared =
                minDistanceFromPlayer *
                minDistanceFromPlayer;

            Vector2 playerPosition =
                player.position;

            if ((playerPosition - position)
                    .sqrMagnitude <
                minimumDistanceSquared)
            {
                return false;
            }
        }

        int hitCount = Physics2D.OverlapCircle(
            position,
            checkRadius,
            spawnFilter,
            spawnHits
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = spawnHits[i];

            if (hit == null)
                continue;

            if (BlocksBeaconSpawn(hit))
                return false;
        }

        return true;
    }

    private bool BlocksBeaconSpawn(
        Collider2D hit
    )
    {
        if (hit.CompareTag("Player"))
            return true;

        if (hit.CompareTag("Enemy"))
            return true;

        if (hit.CompareTag("Coin"))
            return true;

        if (hit.CompareTag("PowerUp"))
            return true;

        if (hit.CompareTag("Bomb"))
            return true;

        int objectLayer =
            hit.gameObject.layer;

        if (objectLayer == wallLayerIndex)
            return true;

        if (objectLayer == obstacleLayerIndex)
            return true;

        return false;
    }

    private void RefreshPlayerReferences()
    {
        if (playerMovement == null)
        {
            playerMovement =
                FindAnyObjectByType<PlayerMovement>();
        }

        if (player == null &&
            playerMovement != null)
        {
            player = playerMovement.transform;
        }
    }

    private void OnValidate()
    {
        maxBeaconCount =
            Mathf.Max(0, maxBeaconCount);

        minSpawnTime =
            Mathf.Max(0f, minSpawnTime);

        maxSpawnTime =
            Mathf.Max(
                minSpawnTime,
                maxSpawnTime
            );

        buffDuration =
            Mathf.Max(0.1f, buffDuration);

        buffSizeMultiplier =
            Mathf.Max(
                0.1f,
                buffSizeMultiplier
            );

        normalSpeedMultiplier =
            Mathf.Max(
                0.1f,
                normalSpeedMultiplier
            );

        normalMaxSpeedMultiplier =
            Mathf.Max(
                0.1f,
                normalMaxSpeedMultiplier
            );

        projectileMoveMultiplier =
            Mathf.Max(
                0.1f,
                projectileMoveMultiplier
            );

        projectileShotMultiplier =
            Mathf.Max(
                0.1f,
                projectileShotMultiplier
            );

        projectileFireMultiplier =
            Mathf.Max(
                0.1f,
                projectileFireMultiplier
            );

        hunterRepositionMultiplier =
            Mathf.Max(
                0.1f,
                hunterRepositionMultiplier
            );

        hunterWarningMultiplier =
            Mathf.Max(
                0.1f,
                hunterWarningMultiplier
            );

        hunterChargeMultiplier =
            Mathf.Max(
                0.1f,
                hunterChargeMultiplier
            );

        hunterStunMultiplier =
            Mathf.Max(
                0.1f,
                hunterStunMultiplier
            );

        spawnPadding =
            Mathf.Max(0f, spawnPadding);

        checkRadius =
            Mathf.Max(0f, checkRadius);

        minDistanceFromPlayer =
            Mathf.Max(
                0f,
                minDistanceFromPlayer
            );

        maxAttempts =
            Mathf.Max(1, maxAttempts);
    }
}