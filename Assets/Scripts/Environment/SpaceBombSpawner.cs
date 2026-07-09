using UnityEngine;

public class SpaceBombSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject bombPrefab;

    [Header("Spawn Time")]
    public float minSpawnTime = 6f;
    public float maxSpawnTime = 14f;

    [Header("Spawn Rules")]
    public int maxBombCount = 3;
    public float spawnPadding = 1.5f;
    public float checkRadius = 1f;
    public int maxAttempts = 50;
    public float playerSafeDistance = 2.5f;
    public float playerForwardSafeDistance = 7f;
    [Range(0f, 1f)] public float forwardDotLimit = 0.45f;

    [Header("Game State")]
    public PlayerMovement playerMovement;

    private float timer;
    private float nextSpawnTime;

    private int wallLayerIndex;
    private int obstacleLayerIndex;

    private ContactFilter2D spawnFilter;

    private readonly Collider2D[] hits = new Collider2D[16];
    private readonly System.Collections.Generic.List<GameObject> activeBombs = new System.Collections.Generic.List<GameObject>(8);

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
        if (playerMovement != null && playerMovement.IsGameOver) return;

        timer += Time.deltaTime;

        if (timer < nextSpawnTime) return;

        timer = 0f;
        SetNextSpawnTime();

        if (GetActiveBombCount() >= maxBombCount)
            return;

        SpawnBomb();
    }

    public void ApplyLevelSettings(float minTime, float maxTime, int maxCount)
    {
        minSpawnTime = Mathf.Max(0f, minTime);
        maxSpawnTime = Mathf.Max(minSpawnTime, maxTime);
        maxBombCount = Mathf.Max(0, maxCount);

        ResetSpawner();
    }

    public void ResetSpawner()
    {
        timer = 0f;
        activeBombs.Clear();
        SetNextSpawnTime();
    }

    private void SpawnBomb()
    {
        if (bombPrefab == null) return;
        if (CameraWorldBounds.Instance == null) return;

        if (!TryGetValidSpawnPosition(out Vector2 spawnPos)) return;

        GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity);
        activeBombs.Add(bomb);
        SpawnAreaRegistry.Register(bomb, checkRadius);
    }

    private int GetActiveBombCount()
    {
        int count = 0;

        for (int i = activeBombs.Count - 1; i >= 0; i--)
        {
            if (activeBombs[i] == null)
            {
                activeBombs.RemoveAt(i);
                continue;
            }

            if (activeBombs[i].CompareTag("Bomb"))
                count++;
        }

        return count;
    }

    private bool TryGetValidSpawnPosition(out Vector2 spawnPos)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            spawnPos = CameraWorldBounds.Instance.RandomPointInside(spawnPadding);

            if (IsAreaClear(spawnPos))
                return true;
        }

        spawnPos = Vector2.zero;
        return false;
    }

    private bool IsAreaClear(Vector2 pos)
    {
        if (!SpawnAreaRegistry.IsAreaFree(pos, checkRadius))
            return false;

        if (playerMovement != null)
        {
            Vector2 playerPos = playerMovement.transform.position;
            Vector2 toSpawn = pos - playerPos;

            float safeDistanceSqr = playerSafeDistance * playerSafeDistance;

            if (toSpawn.sqrMagnitude < safeDistanceSqr)
                return false;

            Vector2 moveDir = playerMovement.LastMoveDirection;

            if (moveDir.sqrMagnitude > 0.001f)
            {
                Vector2 dir = moveDir.normalized;

                float forwardAmount = Vector2.Dot(toSpawn, dir);

                Vector2 closestPointOnForwardLine = playerPos + dir * forwardAmount;
                float sideDistance = Vector2.Distance(pos, closestPointOnForwardLine);

                float forwardBlockDistance = playerForwardSafeDistance;
                float forwardBlockWidth = 3.5f;

                if (forwardAmount > 0f &&
                    forwardAmount < forwardBlockDistance &&
                    sideDistance < forwardBlockWidth)
                {
                    return false;
                }
            }
        }

        int hitCount = Physics2D.OverlapCircle(pos, checkRadius, spawnFilter, hits);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;

            GameObject obj = hit.gameObject;

            if (hit.CompareTag("Enemy")) return false;
            if (hit.CompareTag("Coin")) return false;
            if (hit.CompareTag("Player")) return false;
            if (hit.CompareTag("PowerUp")) return false;
            if (hit.CompareTag("Bomb")) return false;

            if (obj.layer == wallLayerIndex) return false;
            if (obj.layer == obstacleLayerIndex) return false;
        }

        return true;
    }

    private void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnTime, maxSpawnTime);
    }
}