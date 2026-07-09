using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle Mode")]
    public ObstacleSpawnMode obstacleSpawnMode = ObstacleSpawnMode.Fixed;

    [Header("Level Obstacles")]
    public LevelObstacleOption[] levelObstacles;

    [Header("Random Obstacles")]
    public int randomObstacleCount = 5;

    [Header("Spawn Settings")]
    public float minDistanceBetweenObstacles = 2.5f;
    public float playerSafeDistance = 3f;
    public float edgePadding = 1f;
    public float checkRadius = 0.8f;
    public int maxAttempts = 100;

    [Header("References")]
    public Transform player;

    [Header("Intro Popups")]
    public float obstaclePopupGap = 0.04f;

    private int obstacleLayerIndex;
    private int wallLayerIndex;

    private ContactFilter2D spawnFilter;

    private readonly Collider2D[] spawnHits = new Collider2D[16];
    private readonly List<Vector2> spawnedPositions = new List<Vector2>();
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
        if (CameraWorldBounds.Instance == null) return;
        if (levelObstacles == null || levelObstacles.Length == 0) return;

        spawnedPositions.Clear();
        spawnedObstacles.Clear();

        if (obstacleSpawnMode == ObstacleSpawnMode.Random)
            SpawnRandomObstacles();
        else
            SpawnEnabledObstacles();
    }

    private void SpawnEnabledObstacles()
    {
        foreach (LevelObstacleOption obstacle in levelObstacles)
        {
            if (obstacle == null) continue;
            if (!obstacle.enabled) continue;
            if (obstacle.prefab == null) continue;

            SpawnObstaclePrefab(obstacle.prefab);
        }
    }

    private void SpawnRandomObstacles()
    {
        List<GameObject> randomPool = new List<GameObject>();

        foreach (LevelObstacleOption obstacle in levelObstacles)
        {
            if (obstacle == null) continue;
            if (obstacle.prefab == null) continue;

            if (!randomPool.Contains(obstacle.prefab))
                randomPool.Add(obstacle.prefab);
        }

        Shuffle(randomPool);

        int spawnCount = Mathf.Min(randomObstacleCount, randomPool.Count);

        for (int i = 0; i < spawnCount; i++)
            SpawnObstaclePrefab(randomPool[i]);
    }

    private void SpawnObstaclePrefab(GameObject prefab)
    {
        if (prefab == null) return;

        if (!TryGetValidPosition(out Vector2 spawnPos))
            return;

        GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);

        spawnedObstacles.Add(spawned);
        spawnedPositions.Add(spawnPos);
    }


    public IEnumerator PlaySpawnedObstaclePopupsAndWait()
    {
        if (spawnedObstacles.Count == 0)
            yield break;

        List<GameObject> popupList = new List<GameObject>(spawnedObstacles);
        ShuffleGameObjects(popupList);

        foreach (GameObject obstacle in popupList)
        {
            if (obstacle == null) continue;

            SpawnPopEffect popEffect = obstacle.GetComponent<SpawnPopEffect>();

            if (popEffect != null)
                yield return popEffect.PlayAndWait();
            else
                obstacle.transform.localScale = obstacle.transform.localScale == Vector3.zero ? Vector3.one : obstacle.transform.localScale;

            if (obstaclePopupGap > 0f)
                yield return new WaitForSecondsRealtime(obstaclePopupGap);
        }
    }

    public void HideSpawnedObstaclesInstant()
    {
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle == null) continue;

            SpawnPopEffect popEffect = obstacle.GetComponent<SpawnPopEffect>();

            if (popEffect != null)
                popEffect.HideInstant();
        }
    }

    public void ClearObstacles()
    {
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle != null)
                Destroy(obstacle);
        }

        spawnedObstacles.Clear();
        spawnedPositions.Clear();
        spawnedObstacles.Clear();
    }

    private bool TryGetValidPosition(out Vector2 spawnPos)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            spawnPos = CameraWorldBounds.Instance.RandomPointInside(edgePadding);

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
            float safeDistanceSqr = playerSafeDistance * playerSafeDistance;

            if (((Vector2)player.position - pos).sqrMagnitude < safeDistanceSqr)
                return false;
        }

        float obstacleDistanceSqr = minDistanceBetweenObstacles * minDistanceBetweenObstacles;

        for (int i = 0; i < spawnedPositions.Count; i++)
        {
            if ((pos - spawnedPositions[i]).sqrMagnitude < obstacleDistanceSqr)
                return false;
        }

        int hitCount = Physics2D.OverlapCircle(pos, checkRadius, spawnFilter, spawnHits);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = spawnHits[i];
            if (hit == null) continue;

            if (IsBlocked(hit))
                return false;
        }

        return true;
    }

    private bool IsBlocked(Collider2D hit)
    {
        GameObject obj = hit.gameObject;

        return hit.CompareTag("Coin") ||
               hit.CompareTag("Player") ||
               hit.CompareTag("PowerUp") ||
               obj.layer == obstacleLayerIndex ||
               obj.layer == wallLayerIndex;
    }

    private void Shuffle(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            GameObject temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void ShuffleGameObjects(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            GameObject temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}