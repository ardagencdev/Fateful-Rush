using System.Collections;
using UnityEngine;

public class HorizontalLaserWallSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject laserWarningPrefab;
    public GameObject laserWallPrefab;

    [Header("Spawn Time")]
    public float minSpawnTime = 8f;
    public float maxSpawnTime = 25f;

    [Header("Warning")]
    public float warningDuration = 2f;

    [Header("Laser Settings")]
    public float laserLifeTime = 1.5f;
    public float laserWidth = 0.5f;
    public float edgePadding = 0.4f;
    public float widthExtra = 1f;

    [Header("Game State")]
    public PlayerMovement playerMovement;

    private Coroutine spawnCoroutine;
    private GameObject activeWarning;
    private GameObject activeLaser;
    private bool systemActive;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();
    }

    private void OnDisable()
    {
        StopLaserSystem();
    }

    private void Update()
    {
        if (!systemActive) return;

        if (playerMovement != null && playerMovement.IsGameOver)
            StopLaserSystem();
    }

    public void ApplyLevelSettings(float minTime, float maxTime, float warningTime, float lifeTime, float width, float extraWidth)
    {
        minSpawnTime = Mathf.Max(0f, minTime);
        maxSpawnTime = Mathf.Max(minSpawnTime, maxTime);
        warningDuration = Mathf.Max(0f, warningTime);
        laserLifeTime = Mathf.Max(0.1f, lifeTime);
        laserWidth = Mathf.Max(0.01f, width);
        widthExtra = Mathf.Max(0f, extraWidth);

        StartLaserSystem();
    }

    public void StartLaserSystem()
    {
        StopLaserSystem();

        systemActive = true;
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    public void StopLaserSystem()
    {
        systemActive = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        DestroyActiveWarning();

        if (activeLaser != null)
        {
            Destroy(activeLaser);
            activeLaser = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (systemActive)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));

            if (!systemActive || IsGameOver())
                yield break;

            yield return SpawnHorizontalLaser();
        }
    }

    private IEnumerator SpawnHorizontalLaser()
    {
        CameraWorldBounds bounds = CameraWorldBounds.Instance;
        if (bounds == null) yield break;

        float yPos = Random.Range(bounds.MinY + edgePadding, bounds.MaxY - edgePadding);

        Vector3 position = new Vector3(bounds.Center.x, yPos, 0f);
        Vector3 scale = new Vector3(laserWidth, bounds.Width + widthExtra, 1f);
        Quaternion rotation = Quaternion.Euler(0f, 0f, 90f);

        activeWarning = Instantiate(laserWarningPrefab, position, rotation);
        activeWarning.transform.localScale = scale;

        yield return PlayWarning();

        DestroyActiveWarning();

        if (!systemActive || IsGameOver())
            yield break;

        activeLaser = Instantiate(laserWallPrefab, position, rotation);
        activeLaser.transform.localScale = scale;

        LaserWall laserWall = activeLaser.GetComponent<LaserWall>();
        if (laserWall != null)
            laserWall.lifeTime = laserLifeTime;
    }

    private IEnumerator PlayWarning()
    {
        if (activeWarning == null)
        {
            yield return new WaitForSeconds(warningDuration);
            yield break;
        }

        LaserWarning warning = activeWarning.GetComponent<LaserWarning>();

        if (warning != null)
        {
            warning.blinkDuration = warningDuration;
            yield return warning.PlayWarning();
        }
        else
        {
            yield return new WaitForSeconds(warningDuration);
        }
    }

    private void DestroyActiveWarning()
    {
        if (activeWarning == null) return;

        Destroy(activeWarning);
        activeWarning = null;
    }

    private bool IsGameOver()
    {
        return playerMovement != null && playerMovement.IsGameOver;
    }
}