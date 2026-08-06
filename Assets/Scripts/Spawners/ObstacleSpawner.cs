using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    private const int MaximumObstaclesPerLevel = 5;

    [Header("Obstacle Mode")]
    public ObstacleSpawnMode obstacleSpawnMode = ObstacleSpawnMode.Fixed;

    [Header("Level Obstacles")]
    public LevelObstacleOption[] levelObstacles;

    [Header("Random Obstacles")]
    [Range(0, MaximumObstaclesPerLevel)]
    public int randomObstacleCount = 5;

    [Header("Spawn Settings")]
    [Min(0f)] public float minDistanceBetweenObstacles = 1.2f;
    [Min(0f)] public float playerSafeDistance = 3.5f;
    [Min(0f)] public float edgePadding = 0.6f;
    [Tooltip("Extra empty strip kept between an obstacle footprint and the arena edge.")]
    [Min(0f)] public float arenaEdgeClearance = 1.25f;
    [Min(0f)] public float checkRadius = 0.8f;
    [Min(1)] public int maxAttempts = 120;

    [Header("References")]
    public Transform player;

    [Header("Intro Popups")]
    [Min(0f)] public float obstaclePopupGap = 0.04f;

    private struct SpawnedFootprint
    {
        public Vector2 Position;
        public float Radius;
    }

    private int obstacleLayerIndex;
    private int wallLayerIndex;
    private ContactFilter2D spawnFilter;
    private readonly Collider2D[] spawnHits = new Collider2D[32];
    private readonly List<SpawnedFootprint> spawnedFootprints = new List<SpawnedFootprint>();
    private readonly List<GameObject> spawnedObstacles = new List<GameObject>();

    private void Awake()
    {
        obstacleLayerIndex = LayerMask.NameToLayer("Obstacle");
        wallLayerIndex = LayerMask.NameToLayer("Wall");
        spawnFilter = ContactFilter2D.noFilter;
        spawnFilter.useTriggers = true;
    }

    public void SpawnObstacles()
    {
        if (CameraWorldBounds.Instance == null || levelObstacles == null || levelObstacles.Length == 0)
            return;

        spawnedFootprints.Clear();
        spawnedObstacles.Clear();

        if (obstacleSpawnMode == ObstacleSpawnMode.Random)
            SpawnRandomObstacles();
        else
            SpawnEnabledObstacles();
    }

    private void SpawnEnabledObstacles()
    {
        int spawnedCount = 0;
        foreach (LevelObstacleOption obstacle in levelObstacles)
        {
            if (spawnedCount >= MaximumObstaclesPerLevel)
                break;
            if (obstacle == null || !obstacle.enabled || obstacle.prefab == null)
                continue;
            if (SpawnObstaclePrefab(obstacle.prefab))
                spawnedCount++;
        }
    }

    private void SpawnRandomObstacles()
    {
        List<GameObject> pool = new List<GameObject>();
        foreach (LevelObstacleOption obstacle in levelObstacles)
        {
            if (obstacle != null && obstacle.enabled && obstacle.prefab != null && !pool.Contains(obstacle.prefab))
                pool.Add(obstacle.prefab);
        }

        Shuffle(pool);
        int targetCount = Mathf.Min(randomObstacleCount, pool.Count, MaximumObstaclesPerLevel);
        int spawnedCount = 0;
        for (int i = 0; i < pool.Count && spawnedCount < targetCount; i++)
        {
            if (SpawnObstaclePrefab(pool[i]))
                spawnedCount++;
        }
    }

    private bool SpawnObstaclePrefab(GameObject prefab)
    {
        Vector2 halfExtents = GetPrefabHalfExtents(prefab);
        float footprintRadius = Mathf.Max(halfExtents.x, halfExtents.y, checkRadius);

        if (!TryGetValidPosition(halfExtents, footprintRadius, out Vector2 spawnPos))
        {
            Debug.LogWarning($"[ObstacleSpawner] Safe position not found for {prefab.name}; obstacle skipped.", this);
            return false;
        }

        GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);
        spawnedObstacles.Add(spawned);
        spawnedFootprints.Add(new SpawnedFootprint { Position = spawnPos, Radius = footprintRadius });
        return true;
    }

    private static Vector2 GetPrefabHalfExtents(GameObject prefab)
    {
        Bounds combined = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        Collider2D[] colliders = prefab.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D collider in colliders)
        {
            Bounds bounds = collider.bounds;
            if (bounds.extents.sqrMagnitude <= 0.0001f)
                continue;
            if (!hasBounds) { combined = bounds; hasBounds = true; }
            else combined.Encapsulate(bounds);
        }

        if (!hasBounds)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Bounds bounds = renderer.bounds;
                if (bounds.extents.sqrMagnitude <= 0.0001f)
                    continue;
                if (!hasBounds) { combined = bounds; hasBounds = true; }
                else combined.Encapsulate(bounds);
            }
        }

        if (!hasBounds)
            return Vector2.one * 0.8f;

        return new Vector2(
            Mathf.Max(0.35f, combined.extents.x),
            Mathf.Max(0.35f, combined.extents.y)
        );
    }

    public IEnumerator PlaySpawnedObstaclePopupsAndWait()
    {
        if (spawnedObstacles.Count == 0) yield break;
        List<GameObject> popupList = new List<GameObject>(spawnedObstacles);
        Shuffle(popupList);
        foreach (GameObject obstacle in popupList)
        {
            if (obstacle == null) continue;
            SpawnPopEffect effect = obstacle.GetComponent<SpawnPopEffect>();
            if (effect != null) yield return effect.PlayAndWait();
            else if (obstacle.transform.localScale == Vector3.zero) obstacle.transform.localScale = Vector3.one;
            if (obstaclePopupGap > 0f) yield return new WaitForSecondsRealtime(obstaclePopupGap);
        }
    }

    public void HideSpawnedObstaclesInstant()
    {
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle == null) continue;
            SpawnPopEffect effect = obstacle.GetComponent<SpawnPopEffect>();
            if (effect != null) effect.HideInstant();
        }
    }

    public void ClearObstacles()
    {
        foreach (GameObject obstacle in spawnedObstacles)
            if (obstacle != null) Destroy(obstacle);
        spawnedObstacles.Clear();
        spawnedFootprints.Clear();
    }

    private bool TryGetValidPosition(Vector2 halfExtents, float footprintRadius, out Vector2 spawnPos)
    {
        CameraWorldBounds bounds = CameraWorldBounds.Instance;
        float minX = bounds.MinX + edgePadding + arenaEdgeClearance + halfExtents.x;
        float maxX = bounds.MaxX - edgePadding - arenaEdgeClearance - halfExtents.x;
        float minY = bounds.MinY + edgePadding + arenaEdgeClearance + halfExtents.y;
        float maxY = bounds.MaxY - edgePadding - arenaEdgeClearance - halfExtents.y;

        if (minX >= maxX || minY >= maxY)
        {
            spawnPos = Vector2.zero;
            return false;
        }

        for (int i = 0; i < maxAttempts; i++)
        {
            spawnPos = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            if (IsValidPosition(spawnPos, footprintRadius))
                return true;
        }

        spawnPos = Vector2.zero;
        return false;
    }

    private bool IsValidPosition(Vector2 pos, float footprintRadius)
    {
        if (player != null && Vector2.Distance(player.position, pos) < playerSafeDistance + footprintRadius)
            return false;

        foreach (SpawnedFootprint other in spawnedFootprints)
        {
            if (Vector2.Distance(pos, other.Position) < footprintRadius + other.Radius + minDistanceBetweenObstacles)
                return false;
        }

        float overlapRadius = Mathf.Max(checkRadius, footprintRadius);
        int hitCount = Physics2D.OverlapCircle(pos, overlapRadius, spawnFilter, spawnHits);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = spawnHits[i];
            if (hit != null && IsBlocked(hit))
                return false;
        }
        return true;
    }

    private bool IsBlocked(Collider2D hit)
    {
        GameObject obj = hit.gameObject;
        return hit.CompareTag("Coin") || hit.CompareTag("Player") || hit.CompareTag("PowerUp") ||
               hit.CompareTag("Enemy") || hit.CompareTag("Bomb") ||
               obj.layer == obstacleLayerIndex || obj.layer == wallLayerIndex;
    }

    private static void Shuffle(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int index = Random.Range(i, list.Count);
            GameObject temporary = list[i];
            list[i] = list[index];
            list[index] = temporary;
        }
    }

    private void OnValidate()
    {
        randomObstacleCount = Mathf.Clamp(randomObstacleCount, 0, MaximumObstaclesPerLevel);
        minDistanceBetweenObstacles = Mathf.Max(0f, minDistanceBetweenObstacles);
        playerSafeDistance = Mathf.Max(0f, playerSafeDistance);
        edgePadding = Mathf.Max(0f, edgePadding);
        arenaEdgeClearance = Mathf.Max(0f, arenaEdgeClearance);
        checkRadius = Mathf.Max(0f, checkRadius);
        maxAttempts = Mathf.Max(1, maxAttempts);
        obstaclePopupGap = Mathf.Max(0f, obstaclePopupGap);
    }
}
