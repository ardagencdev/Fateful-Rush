using System.Collections;
using UnityEngine;

public class BeaconEnemySpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject beaconEnemyPrefab;

    [Header("Level Control")]
    public int maxBeaconCount = 1;

    [Header("Spawn Time")]
    public float minSpawnTime = 1f;
    public float maxSpawnTime = 2f;

    [Header("Beacon Buff")]
    public float buffDuration = 15f;
    public float buffSizeMultiplier = 1.25f;
    public float normalSpeedMultiplier = 1.35f;
    public float normalMaxSpeedMultiplier = 1.25f;
    public float projectileMoveMultiplier = 1.2f;
    public float projectileShotMultiplier = 1.25f;
    public float projectileFireMultiplier = 1.25f;
    public float hunterRepositionMultiplier = 0.8f;
    public float hunterWarningMultiplier = 0.8f;
    public float hunterChargeMultiplier = 1.25f;
    public float hunterStunMultiplier = 0.8f;

    [Header("Spawn Rules")]
    public float spawnPadding = 1f;
    public float checkRadius = 0.8f;
    public float minDistanceFromPlayer = 4f;
    public int maxAttempts = 50;

    [Header("References")]
    public Transform player;
    public PlayerMovement playerMovement;

    private float timer;
    private int spawnedCount;
    private bool initialized;

    private int wallLayerIndex;
    private int obstacleLayerIndex;

    private ContactFilter2D spawnFilter;
    private readonly Collider2D[] spawnHits = new Collider2D[16];

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (player == null && playerMovement != null)
            player = playerMovement.transform;

        wallLayerIndex = LayerMask.NameToLayer("Wall");
        obstacleLayerIndex = LayerMask.NameToLayer("Obstacle");

        spawnFilter = ContactFilter2D.noFilter;
        spawnFilter.useTriggers = true;
    }

    private IEnumerator Start()
    {
        yield return null;

        if (!initialized)
            InitializeSpawner();
    }

    private void Update()
    {
        if (!GameStateManager.IsGameplayStarted) return;
        if (!initialized) return;
        if (maxBeaconCount <= 0) return;
        if (spawnedCount >= maxBeaconCount) return;
        if (playerMovement != null && playerMovement.IsGameOver) return;

        timer -= Time.deltaTime;

        if (timer > 0f) return;

        if (TrySpawnBeacon())
        {
            spawnedCount++;

            if (spawnedCount < maxBeaconCount)
                ResetSpawnerTimer();
        }
        else
        {
            timer = 0.25f;
        }
    }

    public void ApplyLevelSettings(int count, float minTime, float maxTime)
    {
        maxBeaconCount = Mathf.Max(0, count);
        minSpawnTime = Mathf.Max(0f, minTime);
        maxSpawnTime = Mathf.Max(minSpawnTime, maxTime);

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
        float hStunMult)
    {
        buffDuration = Mathf.Max(0.1f, duration);
        buffSizeMultiplier = Mathf.Max(0.1f, sizeMult);
        normalSpeedMultiplier = Mathf.Max(0.1f, nSpeedMult);
        normalMaxSpeedMultiplier = Mathf.Max(0.1f, nMaxMult);
        projectileMoveMultiplier = Mathf.Max(0.1f, pMoveMult);
        projectileShotMultiplier = Mathf.Max(0.1f, pShotMult);
        projectileFireMultiplier = Mathf.Max(0.1f, pFireMult);
        hunterRepositionMultiplier = Mathf.Max(0.1f, hRepMult);
        hunterWarningMultiplier = Mathf.Max(0.1f, hWarnMult);
        hunterChargeMultiplier = Mathf.Max(0.1f, hChargeMult);
        hunterStunMultiplier = Mathf.Max(0.1f, hStunMult);
    }

    private void InitializeSpawner()
    {
        initialized = true;
        spawnedCount = 0;

        gameObject.SetActive(maxBeaconCount > 0);

        if (maxBeaconCount > 0)
            ResetSpawnerTimer();
    }

    private void ResetSpawnerTimer()
    {
        timer = Random.Range(minSpawnTime, maxSpawnTime);
    }

    private bool TrySpawnBeacon()
    {
        if (beaconEnemyPrefab == null) return false;
        if (CameraWorldBounds.Instance == null) return false;

        if (!TryGetSpawnPosition(out Vector2 spawnPos))
            return false;

        GameObject beacon = Instantiate(beaconEnemyPrefab, spawnPos, Quaternion.identity);

        BeaconEnemy beaconEnemy = beacon.GetComponent<BeaconEnemy>();

        if (beaconEnemy != null)
        {
            beaconEnemy.player = player;
            beaconEnemy.playerMovement = playerMovement;

            beaconEnemy.buffDuration = buffDuration;
            beaconEnemy.buffSizeMultiplier = buffSizeMultiplier;
            beaconEnemy.normalSpeedMultiplier = normalSpeedMultiplier;
            beaconEnemy.normalMaxSpeedMultiplier = normalMaxSpeedMultiplier;
            beaconEnemy.projectileMoveMultiplier = projectileMoveMultiplier;
            beaconEnemy.projectileShotMultiplier = projectileShotMultiplier;
            beaconEnemy.projectileFireMultiplier = projectileFireMultiplier;
            beaconEnemy.hunterRepositionMultiplier = hunterRepositionMultiplier;
            beaconEnemy.hunterWarningMultiplier = hunterWarningMultiplier;
            beaconEnemy.hunterChargeMultiplier = hunterChargeMultiplier;
            beaconEnemy.hunterStunMultiplier = hunterStunMultiplier;
        }

        return true;
    }

    private bool TryGetSpawnPosition(out Vector2 spawnPos)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            spawnPos = CameraWorldBounds.Instance.RandomPointInside(spawnPadding);

            if (IsValidPosition(spawnPos))
                return true;
        }

        spawnPos = Vector2.zero;
        return false;
    }

    private bool IsValidPosition(Vector2 pos)
    {
        if (player != null)
        {
            float minDistanceSqr = minDistanceFromPlayer * minDistanceFromPlayer;

            if (((Vector2)player.position - pos).sqrMagnitude < minDistanceSqr)
                return false;
        }

        int hitCount = Physics2D.OverlapCircle(pos, checkRadius, spawnFilter, spawnHits);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = spawnHits[i];
            if (hit == null) continue;

            GameObject obj = hit.gameObject;

            if (hit.CompareTag("Player")) return false;
            if (hit.CompareTag("Enemy")) return false;
            if (hit.CompareTag("Coin")) return false;
            if (hit.CompareTag("PowerUp")) return false;
            if (hit.CompareTag("Bomb")) return false;

            if (obj.layer == wallLayerIndex) return false;
            if (obj.layer == obstacleLayerIndex) return false;
        }

        return true;
    }
}