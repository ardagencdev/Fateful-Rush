using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ObstacleSpawner : MonoBehaviour
{
    private const int MaximumObstaclesPerLevel = 5;
    private const int PlacementGridSteps = 21;
    private const int MaximumCandidatePositionsPerObstacle = 560;
    private const int LayoutRelaxationPasses = 3;
    private const int RandomSetRerolls = 4;

    private const float MinimumEdgeClearance = 0.35f;
    private const float MinimumPlayerEdgeClearance = 0.35f;
    private const float MinimumObstacleEdgeClearance = 0.05f;

    [Header("Obstacle Mode")]
    public ObstacleSpawnMode obstacleSpawnMode =
        ObstacleSpawnMode.Fixed;

    [Header("Level Obstacles")]
    public LevelObstacleOption[] levelObstacles;

    [Header("Random Obstacles")]
    [Range(0, MaximumObstaclesPerLevel)]
    public int randomObstacleCount = 5;

    [Header("Spawn Settings")]
    [Tooltip(
        "Minimum center-to-center distance. " +
        "Real obstacle footprints are checked separately."
    )]
    [Min(0f)]
    public float minDistanceBetweenObstacles = 1.2f;

    [Tooltip(
        "Minimum center-to-center distance from the player. " +
        "Real obstacle footprint clearance is checked separately."
    )]
    [Min(0f)]
    public float playerSafeDistance = 3.5f;

    [Min(0f)]
    public float edgePadding = 0.6f;

    [Tooltip(
        "Preferred clearance between the obstacle footprint and " +
        "CameraWorldBounds. If a complete layout cannot fit, " +
        "only this soft margin is gradually relaxed."
    )]
    [Min(0f)]
    public float arenaEdgeClearance = 1.25f;

    [Tooltip(
        "Fallback size only when a prefab has no usable Collider2D geometry."
    )]
    [Min(0f)]
    public float checkRadius = 0.8f;

    [Tooltip(
        "Small safety expansion added to the real collider footprint."
    )]
    [Min(0f)]
    public float footprintPadding = 0.08f;

    [Tooltip(
        "Extra random samples in addition to deterministic grid samples."
    )]
    [Min(1)]
    public int maxAttempts = 120;

    [Tooltip(
        "Maximum backtracking work per layout pass."
    )]
    [Min(1000)]
    public int maxLayoutSearchNodes = 30000;

    [Header("References")]
    public Transform player;

    [Header("Intro Popups")]
    [Min(0f)]
    public float obstaclePopupGap = 0.04f;

    [Header("Diagnostics")]
    public bool logPlacementDiagnostics;

    private struct PrefabFootprint
    {
        public Vector2 CenterOffset;
        public Vector2 HalfExtents;
    }

    private sealed class ObstacleCandidate
    {
        public GameObject Prefab;
        public PrefabFootprint Footprint;

        public readonly List<Vector2> Positions =
            new List<Vector2>(
                MaximumCandidatePositionsPerObstacle
            );
    }

    private struct PlannedPlacement
    {
        public ObstacleCandidate Candidate;
        public Vector2 RootPosition;
    }

    private int obstacleLayerIndex;
    private int wallLayerIndex;

    private ContactFilter2D spawnFilter;

    private readonly Collider2D[] spawnHits =
        new Collider2D[64];

    private readonly List<GameObject> spawnedObstacles =
        new List<GameObject>(
            MaximumObstaclesPerLevel
        );

    private readonly List<PlannedPlacement> plannedPlacements =
        new List<PlannedPlacement>(
            MaximumObstaclesPerLevel
        );

    private readonly List<PlannedPlacement> bestPlacements =
        new List<PlannedPlacement>(
            MaximumObstaclesPerLevel
        );

    private readonly Dictionary<GameObject, PrefabFootprint> footprintCache =
        new Dictionary<GameObject, PrefabFootprint>();

    private int layoutSearchNodes;
    private float activeEdgeClearance;

    private void Awake()
    {
        obstacleLayerIndex =
            LayerMask.NameToLayer("Obstacle");

        wallLayerIndex =
            LayerMask.NameToLayer("Wall");

        spawnFilter =
            ContactFilter2D.noFilter;

        spawnFilter.useTriggers = true;
    }

    public void SpawnObstacles()
    {
        CameraWorldBounds bounds =
            CameraWorldBounds.Instance;

        if (bounds == null)
        {
            Debug.LogWarning(
                "[ObstacleSpawner] CameraWorldBounds bulunamadı.",
                this
            );

            return;
        }

        if (levelObstacles == null ||
            levelObstacles.Length == 0)
        {
            return;
        }

        spawnedObstacles.Clear();
        plannedPlacements.Clear();
        bestPlacements.Clear();

        int setAttempts =
            obstacleSpawnMode ==
            ObstacleSpawnMode.Random
                ? RandomSetRerolls
                : 1;

        int requestedCount = 0;

        List<PlannedPlacement> bestOverall =
            new List<PlannedPlacement>(
                MaximumObstaclesPerLevel
            );

        for (int attempt = 0;
             attempt < setAttempts;
             attempt++)
        {
            List<ObstacleCandidate> candidates =
                obstacleSpawnMode ==
                ObstacleSpawnMode.Random
                    ? BuildRandomCandidateSet()
                    : BuildFixedCandidateSet();

            if (candidates.Count == 0)
                return;

            requestedCount =
                candidates.Count;

            bool complete =
                TryPlanLayout(candidates);

            if (plannedPlacements.Count >
                bestOverall.Count)
            {
                bestOverall.Clear();

                bestOverall.AddRange(
                    plannedPlacements
                );
            }

            if (complete)
            {
                InstantiateLayout(
                    plannedPlacements
                );

                return;
            }
        }

        if (bestOverall.Count > 0)
        {
            InstantiateLayout(
                bestOverall
            );

            Debug.LogWarning(
                $"[ObstacleSpawner] Complete layout could not fit. " +
                $"Spawned best valid subset: " +
                $"{bestOverall.Count}/{requestedCount}. " +
                "This means the selected prefab sizes and safety " +
                "rules are physically incompatible with the current arena.",
                this
            );
        }
        else
        {
            Debug.LogWarning(
                $"[ObstacleSpawner] No valid obstacle position exists. " +
                $"Arena={bounds.Width:0.00}x{bounds.Height:0.00}. " +
                "Enable diagnostics to inspect prefab footprints.",
                this
            );
        }
    }

    private void InstantiateLayout(
        List<PlannedPlacement> layout)
    {
        spawnedObstacles.Clear();

        for (int i = 0;
             i < layout.Count;
             i++)
        {
            PlannedPlacement placement =
                layout[i];

            GameObject spawned =
                Instantiate(
                    placement
                        .Candidate
                        .Prefab,
                    placement
                        .RootPosition,
                    Quaternion.identity
                );

            spawnedObstacles.Add(
                spawned
            );
        }

        // Aynı frame içindeki Physics2D sorgularını
        // güncel transformlarla senkronize et.
        Physics2D.SyncTransforms();

        if (logPlacementDiagnostics)
        {
            Debug.Log(
                $"[ObstacleSpawner] Spawned " +
                $"{spawnedObstacles.Count} obstacle(s).",
                this
            );
        }
    }

    public IEnumerator
        PlaySpawnedObstaclePopupsAndWait()
    {
        if (spawnedObstacles.Count == 0)
            yield break;

        List<GameObject> popupList =
            new List<GameObject>(
                spawnedObstacles
            );

        Shuffle(
            popupList
        );

        foreach (
            GameObject obstacle
            in popupList)
        {
            if (obstacle == null)
                continue;

            SpawnPopEffect effect =
                obstacle
                    .GetComponent
                    <SpawnPopEffect>();

            if (effect != null)
            {
                yield return
                    effect.PlayAndWait();
            }
            else if (
                obstacle
                    .transform
                    .localScale ==
                Vector3.zero)
            {
                obstacle
                    .transform
                    .localScale =
                    Vector3.one;
            }

            if (obstaclePopupGap >
                0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        obstaclePopupGap
                    );
            }
        }
    }

    public void
        HideSpawnedObstaclesInstant()
    {
        foreach (
            GameObject obstacle
            in spawnedObstacles)
        {
            if (obstacle == null)
                continue;

            SpawnPopEffect effect =
                obstacle
                    .GetComponent
                    <SpawnPopEffect>();

            if (effect != null)
            {
                effect.HideInstant();
            }
        }
    }

    public void ClearObstacles()
    {
        foreach (
            GameObject obstacle
            in spawnedObstacles)
        {
            if (obstacle == null)
                continue;

            // Destroy frame sonunda çalışır.
            // Aynı frame'de eski collider'ı blocker olarak görme.
            obstacle.SetActive(
                false
            );

            Destroy(
                obstacle
            );
        }

        spawnedObstacles.Clear();
        plannedPlacements.Clear();
        bestPlacements.Clear();
    }

    private void OnValidate()
    {
        randomObstacleCount =
            Mathf.Clamp(
                randomObstacleCount,
                0,
                MaximumObstaclesPerLevel
            );

        minDistanceBetweenObstacles =
            Mathf.Max(
                0f,
                minDistanceBetweenObstacles
            );

        playerSafeDistance =
            Mathf.Max(
                0f,
                playerSafeDistance
            );

        edgePadding =
            Mathf.Max(
                0f,
                edgePadding
            );

        arenaEdgeClearance =
            Mathf.Max(
                0f,
                arenaEdgeClearance
            );

        checkRadius =
            Mathf.Max(
                0f,
                checkRadius
            );

        footprintPadding =
            Mathf.Max(
                0f,
                footprintPadding
            );

        maxAttempts =
            Mathf.Max(
                1,
                maxAttempts
            );

        maxLayoutSearchNodes =
            Mathf.Max(
                1000,
                maxLayoutSearchNodes
            );

        obstaclePopupGap =
            Mathf.Max(
                0f,
                obstaclePopupGap
            );
    }
}
