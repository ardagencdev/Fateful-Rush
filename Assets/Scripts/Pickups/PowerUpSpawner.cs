using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Power Up Prefabs")]
    public GameObject slowPowerUpPrefab;
    public GameObject armorPowerUpPrefab;

    [Header("Enabled")]
    public bool slowEnabled = true;
    public bool armorEnabled = true;

    [Header("Slow Spawn")]
    public float slowMinSpawnTime = 8f;
    public float slowMaxSpawnTime = 20f;

    [Header("Armor Spawn")]
    public float armorMinSpawnTime = 15f;
    public float armorMaxSpawnTime = 30f;

    [Header("Slow Settings")]
    public float slowMultiplier = 0.4f;
    public float slowDuration = 5f;

    [Header("Spawn Rules")]
    public float spawnPadding = 0.8f;
    public float checkRadius = 0.8f;
    public int maxAttempts = 50;

    [Header("References")]
    public PlayerMovement playerMovement;

    private float slowTimer;
    private float armorTimer;
    private bool slowSpawned;
    private bool armorSpawned;

    private int wallLayerIndex;
    private int obstacleLayerIndex;

    private ContactFilter2D spawnFilter;
    private readonly Collider2D[] hits = new Collider2D[16];

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        wallLayerIndex = LayerMask.NameToLayer("Wall");
        obstacleLayerIndex = LayerMask.NameToLayer("Obstacle");

        spawnFilter = ContactFilter2D.noFilter;
        spawnFilter.useTriggers = true;
    }

    private void Start()
    {
        ResetSpawner();
    }

    private void Update()
    {
        if (!GameStateManager.IsGameplayStarted)
            return;

        if (playerMovement != null && playerMovement.IsGameOver)
            return;

        HandleSlowSpawn();
        HandleArmorSpawn();
    }

    public void ApplyLevelSettings(
        bool slowOn,
        bool armorOn,
        float slowMin,
        float slowMax,
        float armorMin,
        float armorMax,
        float levelSlowMultiplier,
        float levelSlowDuration)
    {
        slowEnabled = slowOn;
        armorEnabled = armorOn;

        slowMinSpawnTime = Mathf.Max(0f, slowMin);
        slowMaxSpawnTime = Mathf.Max(slowMinSpawnTime, slowMax);

        armorMinSpawnTime = Mathf.Max(0f, armorMin);
        armorMaxSpawnTime = Mathf.Max(armorMinSpawnTime, armorMax);

        slowMultiplier = Mathf.Clamp(levelSlowMultiplier, 0.01f, 1f);
        slowDuration = Mathf.Max(0.1f, levelSlowDuration);

        ResetSpawner();
    }

    public void ResetSpawner()
    {
        slowSpawned = false;
        armorSpawned = false;

        slowTimer = Random.Range(slowMinSpawnTime, slowMaxSpawnTime);
        armorTimer = Random.Range(armorMinSpawnTime, armorMaxSpawnTime);
    }

    private void HandleSlowSpawn()
    {
        if (!slowEnabled || slowSpawned) return;

        slowTimer -= Time.deltaTime;

        if (slowTimer <= 0f)
        {
            SpawnPowerUp(slowPowerUpPrefab, true);
            slowSpawned = true;
        }
    }

    private void HandleArmorSpawn()
    {
        if (!armorEnabled || armorSpawned) return;

        armorTimer -= Time.deltaTime;

        if (armorTimer <= 0f)
        {
            SpawnPowerUp(armorPowerUpPrefab, false);
            armorSpawned = true;
        }
    }

    private void SpawnPowerUp(GameObject prefab, bool isSlow)
    {
        if (prefab == null) return;
        if (CameraWorldBounds.Instance == null) return;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 spawnPos = CameraWorldBounds.Instance.RandomPointInside(spawnPadding);

            if (!IsAreaClear(spawnPos))
                continue;

            GameObject powerUp = Instantiate(prefab, spawnPos, Quaternion.identity);
            SpawnAreaRegistry.Register(powerUp, checkRadius);

            if (isSlow)
            {
                SlowPowerUp slow = powerUp.GetComponent<SlowPowerUp>();
                if (slow != null)
                {
                    slow.slowMultiplier = slowMultiplier;
                    slow.slowDuration = slowDuration;
                }
            }

            return;
        }
    }

    private bool IsAreaClear(Vector2 pos)
    {
        if (!SpawnAreaRegistry.IsAreaFree(pos, checkRadius))
            return false;

        int hitCount = Physics2D.OverlapCircle(pos, checkRadius, spawnFilter, hits);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;

            if (hit.CompareTag("Enemy")) return false;
            if (hit.CompareTag("Coin")) return false;
            if (hit.CompareTag("Player")) return false;
            if (hit.CompareTag("PowerUp")) return false;
            if (hit.CompareTag("Bomb")) return false;

            if (hit.gameObject.layer == wallLayerIndex) return false;
            if (hit.gameObject.layer == obstacleLayerIndex) return false;
        }

        return true;
    }
}