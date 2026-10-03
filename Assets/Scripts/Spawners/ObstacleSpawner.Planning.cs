using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in ObstacleSpawner.cs.
public partial class ObstacleSpawner
{
    private List<ObstacleCandidate>
        BuildFixedCandidateSet()
    {
        List<ObstacleCandidate> result =
            new List<ObstacleCandidate>();

        foreach (
            LevelObstacleOption option
            in levelObstacles
        )
        {
            if (option == null ||
                !option.enabled ||
                option.prefab == null)
            {
                continue;
            }

            result.Add(
                CreateCandidate(
                    option.prefab
                )
            );
        }

        SortLargestFirst(
            result
        );

        if (result.Count >
            MaximumObstaclesPerLevel)
        {
            result.RemoveRange(
                MaximumObstaclesPerLevel,
                result.Count -
                MaximumObstaclesPerLevel
            );
        }

        return result;
    }

    private List<ObstacleCandidate>
        BuildRandomCandidateSet()
    {
        List<GameObject> pool =
            new List<GameObject>();

        foreach (
            LevelObstacleOption option
            in levelObstacles
        )
        {
            if (option == null ||
                !option.enabled ||
                option.prefab == null)
            {
                continue;
            }

            if (!pool.Contains(
                    option.prefab))
            {
                pool.Add(
                    option.prefab
                );
            }
        }

        Shuffle(
            pool
        );

        int count =
            Mathf.Min(
                randomObstacleCount,
                pool.Count,
                MaximumObstaclesPerLevel
            );

        List<ObstacleCandidate> result =
            new List<ObstacleCandidate>(
                count
            );

        for (int i = 0;
             i < count;
             i++)
        {
            result.Add(
                CreateCandidate(
                    pool[i]
                )
            );
        }

        SortLargestFirst(
            result
        );

        return result;
    }

    private ObstacleCandidate
        CreateCandidate(
            GameObject prefab)
    {
        return new ObstacleCandidate
        {
            Prefab = prefab,
            Footprint =
                GetPrefabFootprint(
                    prefab
                )
        };
    }

    private bool TryPlanLayout(
        List<ObstacleCandidate> candidates)
    {
        bestPlacements.Clear();

        float preferredEdge =
            Mathf.Max(
                edgePadding,
                arenaEdgeClearance
            );

        float minimumEdge =
            Mathf.Min(
                preferredEdge,
                MinimumEdgeClearance
            );

        for (int pass = 0;
             pass <
             LayoutRelaxationPasses;
             pass++)
        {
            float t =
                LayoutRelaxationPasses == 1
                    ? 0f
                    : pass /
                      (float)(
                          LayoutRelaxationPasses -
                          1
                      );

            activeEdgeClearance =
                Mathf.Lerp(
                    preferredEdge,
                    minimumEdge,
                    t
                );

            for (int i = 0;
                 i < candidates.Count;
                 i++)
            {
                BuildPositionPool(
                    candidates[i]
                );

                if (logPlacementDiagnostics)
                {
                    Debug.Log(
                        $"[ObstacleSpawner] " +
                        $"{candidates[i].Prefab.name} | " +
                        $"size=" +
                        $"{candidates[i].Footprint.HalfExtents * 2f} | " +
                        $"positions=" +
                        $"{candidates[i].Positions.Count} | " +
                        $"edge=" +
                        $"{activeEdgeClearance:0.00}",
                        this
                    );
                }
            }

            candidates.Sort(
                (a, b) =>
                {
                    int compare =
                        a.Positions.Count
                            .CompareTo(
                                b.Positions.Count
                            );

                    if (compare != 0)
                        return compare;

                    float areaA =
                        a.Footprint
                            .HalfExtents.x *
                        a.Footprint
                            .HalfExtents.y;

                    float areaB =
                        b.Footprint
                            .HalfExtents.x *
                        b.Footprint
                            .HalfExtents.y;

                    return areaB
                        .CompareTo(
                            areaA
                        );
                }
            );

            plannedPlacements.Clear();

            layoutSearchNodes = 0;

            if (TryPlaceRecursive(
                    candidates,
                    0))
            {
                if (logPlacementDiagnostics)
                {
                    Debug.Log(
                        $"[ObstacleSpawner] Complete layout solved: " +
                        $"{plannedPlacements.Count}/" +
                        $"{candidates.Count}, " +
                        $"edge={activeEdgeClearance:0.00}, " +
                        $"nodes={layoutSearchNodes}.",
                        this
                    );
                }

                return true;
            }
        }

        plannedPlacements.Clear();

        plannedPlacements.AddRange(
            bestPlacements
        );

        return false;
    }

    private bool TryPlaceRecursive(
        List<ObstacleCandidate> candidates,
        int index)
    {
        SaveBestLayout();

        if (index >=
            candidates.Count)
        {
            return
                plannedPlacements.Count ==
                candidates.Count;
        }

        if (layoutSearchNodes >=
            maxLayoutSearchNodes)
        {
            return false;
        }

        ObstacleCandidate candidate =
            candidates[index];

        for (int i = 0;
             i <
             candidate.Positions.Count;
             i++)
        {
            if (layoutSearchNodes++ >=
                maxLayoutSearchNodes)
            {
                break;
            }

            Vector2 position =
                candidate.Positions[i];

            if (!IsCompatibleWithPlannedLayout(
                    position,
                    candidate.Footprint))
            {
                continue;
            }

            plannedPlacements.Add(
                new PlannedPlacement
                {
                    Candidate =
                        candidate,

                    RootPosition =
                        position
                }
            );

            SaveBestLayout();

            if (TryPlaceRecursive(
                    candidates,
                    index + 1))
            {
                return true;
            }

            plannedPlacements.RemoveAt(
                plannedPlacements.Count - 1
            );
        }

        // Bir prefab gerçekten sığmıyorsa
        // diğer valid obstacle'ları çöpe atma.
        return TryPlaceRecursive(
            candidates,
            index + 1
        );
    }

    private void SaveBestLayout()
    {
        if (plannedPlacements.Count <=
            bestPlacements.Count)
        {
            return;
        }

        bestPlacements.Clear();

        bestPlacements.AddRange(
            plannedPlacements
        );
    }

    private void BuildPositionPool(
        ObstacleCandidate candidate)
    {
        candidate.Positions.Clear();

        if (!TryGetRootLimits(
                candidate.Footprint,
                out float minX,
                out float maxX,
                out float minY,
                out float maxY))
        {
            return;
        }

        HashSet<long> unique =
            new HashSet<long>();

        // Önce random sample:
        // başarılı layoutlar fazla grid gibi görünmesin.
        for (int i = 0;
             i < maxAttempts &&
             candidate.Positions.Count <
             MaximumCandidatePositionsPerObstacle;
             i++)
        {
            Vector2 position =
                new Vector2(
                    Random.Range(
                        minX,
                        maxX
                    ),
                    Random.Range(
                        minY,
                        maxY
                    )
                );

            TryAddPosition(
                candidate,
                position,
                unique
            );
        }

        // Ardından tüm kullanılabilir alanı
        // deterministic grid ile tara.
        for (int y = 0;
             y < PlacementGridSteps &&
             candidate.Positions.Count <
             MaximumCandidatePositionsPerObstacle;
             y++)
        {
            for (int x = 0;
                 x < PlacementGridSteps &&
                 candidate.Positions.Count <
                 MaximumCandidatePositionsPerObstacle;
                 x++)
            {
                float tx =
                    (x + 0.5f) /
                    PlacementGridSteps;

                float ty =
                    (y + 0.5f) /
                    PlacementGridSteps;

                Vector2 position =
                    new Vector2(
                        Mathf.Lerp(
                            minX,
                            maxX,
                            tx
                        ),
                        Mathf.Lerp(
                            minY,
                            maxY,
                            ty
                        )
                    );

                TryAddPosition(
                    candidate,
                    position,
                    unique
                );
            }
        }

        Shuffle(
            candidate.Positions
        );
    }

    private void TryAddPosition(
        ObstacleCandidate candidate,
        Vector2 position,
        HashSet<long> unique)
    {
        if (!IsStaticPositionValid(
                position,
                candidate.Footprint))
        {
            return;
        }

        int qx =
            Mathf.RoundToInt(
                position.x * 25f
            );

        int qy =
            Mathf.RoundToInt(
                position.y * 25f
            );

        long key =
            ((long)qx << 32) ^
            (uint)qy;

        if (unique.Add(key))
        {
            candidate.Positions.Add(
                position
            );
        }
    }

    private bool TryGetRootLimits(
        PrefabFootprint footprint,
        out float minX,
        out float maxX,
        out float minY,
        out float maxY)
    {
        CameraWorldBounds bounds =
            CameraWorldBounds.Instance;

        if (bounds == null)
        {
            minX = 0f;
            maxX = 0f;
            minY = 0f;
            maxY = 0f;

            return false;
        }

        Vector2 halfExtents =
            footprint.HalfExtents;

        Vector2 offset =
            footprint.CenterOffset;

        minX =
            bounds.MinX +
            activeEdgeClearance +
            halfExtents.x -
            offset.x;

        maxX =
            bounds.MaxX -
            activeEdgeClearance -
            halfExtents.x -
            offset.x;

        minY =
            bounds.MinY +
            activeEdgeClearance +
            halfExtents.y -
            offset.y;

        maxY =
            bounds.MaxY -
            activeEdgeClearance -
            halfExtents.y -
            offset.y;

        return
            minX <= maxX &&
            minY <= maxY;
    }

    private bool IsStaticPositionValid(
        Vector2 rootPosition,
        PrefabFootprint footprint)
    {
        Vector2 center =
            rootPosition +
            footprint.CenterOffset;

        Vector2 halfExtents =
            footprint.HalfExtents;

        if (player != null)
        {
            Vector2 playerPosition =
                player.position;

            float centerSafe =
                Mathf.Max(
                    0f,
                    playerSafeDistance
                );

            if ((center -
                 playerPosition)
                    .sqrMagnitude <
                centerSafe *
                centerSafe)
            {
                return false;
            }

            // Player ile collider'ın gerçek kenarı
            // arasında da minimum güvenlik payı bırak.
            if (DistancePointToAabb(
                    playerPosition,
                    center,
                    halfExtents) <
                MinimumPlayerEdgeClearance)
            {
                return false;
            }
        }

        Vector2 queryHalfExtents =
            new Vector2(
                Mathf.Max(
                    0.02f,
                    halfExtents.x -
                    0.01f
                ),
                Mathf.Max(
                    0.02f,
                    halfExtents.y -
                    0.01f
                )
            );

        int hitCount =
            Physics2D.OverlapBox(
                center,
                queryHalfExtents * 2f,
                0f,
                spawnFilter,
                spawnHits
            );

        for (int i = 0;
             i < hitCount;
             i++)
        {
            Collider2D hit =
                spawnHits[i];

            if (hit != null &&
                IsBlocked(hit))
            {
                return false;
            }
        }

        return true;
    }

    private bool
        IsCompatibleWithPlannedLayout(
            Vector2 rootPosition,
            PrefabFootprint footprint)
    {
        Vector2 center =
            rootPosition +
            footprint.CenterOffset;

        Vector2 halfExtents =
            footprint.HalfExtents;

        for (int i = 0;
             i <
             plannedPlacements.Count;
             i++)
        {
            PlannedPlacement other =
                plannedPlacements[i];

            Vector2 otherCenter =
                other.RootPosition +
                other.Candidate
                    .Footprint
                    .CenterOffset;

            Vector2 otherHalfExtents =
                other.Candidate
                    .Footprint
                    .HalfExtents;

            // Gerçek collider footprintleri
            // birbirinin içine giremez.
            float edgeDistance =
                DistanceBetweenAabbs(
                    center,
                    halfExtents,
                    otherCenter,
                    otherHalfExtents
                );

            if (edgeDistance <
                MinimumObstacleEdgeClearance)
            {
                return false;
            }

            // Inspector'daki eski 1.2 değeri artık tekrar
            // mantıklı şekilde center-to-center.
            float centerGap =
                Mathf.Max(
                    0f,
                    minDistanceBetweenObstacles
                );

            if ((center -
                 otherCenter)
                    .sqrMagnitude <
                centerGap *
                centerGap)
            {
                return false;
            }
        }

        return true;
    }

    private static void
        SortLargestFirst(
            List<ObstacleCandidate>
                candidates)
    {
        candidates.Sort(
            (a, b) =>
            {
                float areaA =
                    a.Footprint
                        .HalfExtents.x *
                    a.Footprint
                        .HalfExtents.y;

                float areaB =
                    b.Footprint
                        .HalfExtents.x *
                    b.Footprint
                        .HalfExtents.y;

                return areaB
                    .CompareTo(
                        areaA
                    );
            }
        );
    }

    private static void Shuffle<T>(
        List<T> list)
    {
        for (int i = 0;
             i < list.Count;
             i++)
        {
            int index =
                Random.Range(
                    i,
                    list.Count
                );

            T temp =
                list[i];

            list[i] =
                list[index];

            list[index] =
                temp;
        }
    }
}
