using UnityEngine;
using System.Collections.Generic;

public class CoinManager : MonoBehaviour
{
    [Header("Coin Prefabs")]
    public GameObject normalCoin;
    public GameObject goldCoin;
    public GameObject rareCoin;

    [Header("Spawn Settings")]
    public float spawnInterval = 1f;
    public int maxCoinCount = 8;

    [Header("Coin Chances")]
    [Range(0f, 100f)] public float normalCoinChance = 70f;
    [Range(0f, 100f)] public float goldCoinChance = 25f;
    [Range(0f, 100f)] public float rareCoinChance = 5f;

    [Header("Coin Values")]
    public int normalCoinValue = 1;
    public int goldCoinValue = 3;
    public int rareCoinValue = 5;

    [Header("Spawn Rules")]
    public float spawnPadding = 0.8f;
    public float checkRadius = 0.6f;
    public float playerSafeDistance = 2.5f;
    public int maxTry = 100;

    [Header("References")]
    public PlayerMovement playerMovement;

    private float timer;
    private int obstacleLayerIndex;
    private int wallLayerIndex;

    private ContactFilter2D spawnFilter;

    private readonly List<GameObject> activeCoins = new List<GameObject>(32);
    private readonly Collider2D[] spawnCheckHits = new Collider2D[12];

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        obstacleLayerIndex = LayerMask.NameToLayer("Obstacle");
        wallLayerIndex = LayerMask.NameToLayer("Wall");

        spawnFilter = ContactFilter2D.noFilter;
        spawnFilter.useTriggers = true;
    }

    private void Update()
    {
        if (!GameStateManager.IsGameplayStarted) return;
        if (playerMovement != null && playerMovement.IsGameOver) return;

        HandleCoinSpawn();
    }

    public void ResetSpawner()
    {
        timer = 0f;
        activeCoins.Clear();
    }

    private void HandleCoinSpawn()
    {
        timer += Time.deltaTime;

        if (timer < spawnInterval) return;

        timer = 0f;

        if (GetActiveCoinCount() >= maxCoinCount) return;

        SpawnCoin();
    }

    private int GetActiveCoinCount()
    {
        int count = 0;

        for (int i = activeCoins.Count - 1; i >= 0; i--)
        {
            if (activeCoins[i] == null)
            {
                activeCoins.RemoveAt(i);
                continue;
            }

            if (activeCoins[i].CompareTag("Coin"))
                count++;
        }

        return count;
    }

    private void SpawnCoin()
    {
        if (CameraWorldBounds.Instance == null) return;
        if (!TryGetValidSpawnPosition(out Vector2 spawnPos)) return;

        GameObject coinToSpawn = GetRandomCoin();
        if (coinToSpawn == null) return;

        GameObject coinObj = Instantiate(coinToSpawn, spawnPos, Quaternion.identity);
        activeCoins.Add(coinObj);
        SpawnAreaRegistry.Register(coinObj, checkRadius);

        ApplyCoinValue(coinObj, coinToSpawn);
    }

    private void ApplyCoinValue(GameObject coinObj, GameObject prefab)
    {
        Coin coin = coinObj.GetComponent<Coin>();
        if (coin == null) return;

        if (prefab == normalCoin)
            coin.value = normalCoinValue;
        else if (prefab == goldCoin)
            coin.value = goldCoinValue;
        else if (prefab == rareCoin)
            coin.value = rareCoinValue;
    }

    private bool TryGetValidSpawnPosition(out Vector2 spawnPos)
    {
        for (int i = 0; i < maxTry; i++)
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
        if (!SpawnAreaRegistry.IsAreaFree(pos, checkRadius))
            return false;

        if (playerMovement != null)
        {
            Vector2 playerPos = playerMovement.transform.position;
            float safeDistanceSqr = playerSafeDistance * playerSafeDistance;

            if (((Vector2)pos - playerPos).sqrMagnitude < safeDistanceSqr)
                return false;
        }

        int hitCount = Physics2D.OverlapCircle(pos, checkRadius, spawnFilter, spawnCheckHits);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = spawnCheckHits[i];
            if (hit == null) continue;

            if (hit.CompareTag("Coin")) return false;
            if (hit.CompareTag("Enemy")) return false;
            if (hit.CompareTag("Player")) return false;
            if (hit.CompareTag("PowerUp")) return false;
            if (hit.CompareTag("Bomb")) return false;

            if (hit.gameObject.layer == obstacleLayerIndex) return false;
            if (hit.gameObject.layer == wallLayerIndex) return false;
        }

        return true;
    }

    private GameObject GetRandomCoin()
    {
        float normalChance = normalCoin != null ? normalCoinChance : 0f;
        float goldChance = goldCoin != null ? goldCoinChance : 0f;
        float rareChance = rareCoin != null ? rareCoinChance : 0f;

        float totalChance = normalChance + goldChance + rareChance;

        if (totalChance <= 0f)
            return null;

        float rand = Random.Range(0f, totalChance);

        if (rand < normalChance)
            return normalCoin;

        rand -= normalChance;

        if (rand < goldChance)
            return goldCoin;

        return rareCoin;
    }
}