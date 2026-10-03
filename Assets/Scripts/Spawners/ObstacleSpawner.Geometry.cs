using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in ObstacleSpawner.cs.
public partial class ObstacleSpawner
{
    private PrefabFootprint
        GetPrefabFootprint(
            GameObject prefab)
    {
        if (prefab == null)
        {
            return new PrefabFootprint
            {
                CenterOffset = Vector2.zero,
                HalfExtents = Vector2.one * Mathf.Max(0.05f, checkRadius)
            };
        }

        if (footprintCache.TryGetValue(prefab, out PrefabFootprint cached))
            return cached;

        Transform root =
            prefab.transform;

        Collider2D[] colliders =
            prefab
                .GetComponentsInChildren
                <Collider2D>(true);

        bool found = false;

        Vector2 min =
            Vector2.zero;

        Vector2 max =
            Vector2.zero;

        foreach (
            Collider2D collider
            in colliders)
        {
            if (collider == null ||
                !collider.enabled)
            {
                continue;
            }

            if (collider is
                PolygonCollider2D polygon)
            {
                AddPolygonCollider(
                    root,
                    polygon,
                    ref found,
                    ref min,
                    ref max
                );
            }
            else if (collider is
                     CircleCollider2D circle)
            {
                AddCircleCollider(
                    root,
                    circle,
                    ref found,
                    ref min,
                    ref max
                );
            }
            else if (collider is
                     CapsuleCollider2D capsule)
            {
                AddRect(
                    root,
                    capsule.transform,
                    capsule.offset,
                    capsule.size * 0.5f,
                    ref found,
                    ref min,
                    ref max
                );
            }
            else if (collider is
                     BoxCollider2D box)
            {
                Vector2 half =
                    box.size *
                    0.5f +
                    Vector2.one *
                    box.edgeRadius;

                AddRect(
                    root,
                    box.transform,
                    box.offset,
                    half,
                    ref found,
                    ref min,
                    ref max
                );
            }
        }

        if (!found)
        {
            float fallback =
                Mathf.Max(
                    0.05f,
                    checkRadius
                );

            PrefabFootprint fallbackFootprint = new PrefabFootprint
            {
                CenterOffset = Vector2.zero,
                HalfExtents = Vector2.one * fallback
            };

            footprintCache[prefab] = fallbackFootprint;
            return fallbackFootprint;
        }

        Vector2 center =
            (min + max) *
            0.5f;

        Vector2 halfExtents =
            (max - min) *
            0.5f;

        halfExtents +=
            Vector2.one *
            footprintPadding;

        halfExtents.x =
            Mathf.Max(
                0.05f,
                halfExtents.x
            );

        halfExtents.y =
            Mathf.Max(
                0.05f,
                halfExtents.y
            );

        PrefabFootprint footprint = new PrefabFootprint
        {
            CenterOffset = center,
            HalfExtents = halfExtents
        };

        footprintCache[prefab] = footprint;
        return footprint;
    }

    private static void AddPolygonCollider(
        Transform root,
        PolygonCollider2D polygon,
        ref bool found,
        ref Vector2 min,
        ref Vector2 max)
    {
        for (int pathIndex = 0;
             pathIndex <
             polygon.pathCount;
             pathIndex++)
        {
            Vector2[] path =
                polygon.GetPath(
                    pathIndex
                );

            for (int i = 0;
                 i < path.Length;
                 i++)
            {
                AddSpawnSpacePoint(
                    root,
                    polygon.transform,
                    path[i] +
                    polygon.offset,
                    ref found,
                    ref min,
                    ref max
                );
            }
        }
    }

    private static void AddCircleCollider(
        Transform root,
        CircleCollider2D circle,
        ref bool found,
        ref Vector2 min,
        ref Vector2 max)
    {
        const int Samples = 16;

        for (int i = 0;
             i < Samples;
             i++)
        {
            float angle =
                i *
                Mathf.PI *
                2f /
                Samples;

            Vector2 direction =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                );

            Vector2 point =
                circle.offset +
                direction *
                circle.radius;

            AddSpawnSpacePoint(
                root,
                circle.transform,
                point,
                ref found,
                ref min,
                ref max
            );
        }
    }

    private static void AddRect(
        Transform root,
        Transform source,
        Vector2 center,
        Vector2 half,
        ref bool found,
        ref Vector2 min,
        ref Vector2 max)
    {
        AddSpawnSpacePoint(
            root,
            source,
            center +
            new Vector2(
                -half.x,
                -half.y
            ),
            ref found,
            ref min,
            ref max
        );

        AddSpawnSpacePoint(
            root,
            source,
            center +
            new Vector2(
                -half.x,
                half.y
            ),
            ref found,
            ref min,
            ref max
        );

        AddSpawnSpacePoint(
            root,
            source,
            center +
            new Vector2(
                half.x,
                -half.y
            ),
            ref found,
            ref min,
            ref max
        );

        AddSpawnSpacePoint(
            root,
            source,
            center +
            new Vector2(
                half.x,
                half.y
            ),
            ref found,
            ref min,
            ref max
        );
    }

    private static void AddSpawnSpacePoint(
        Transform root,
        Transform source,
        Vector2 localPoint,
        ref bool found,
        ref Vector2 min,
        ref Vector2 max)
    {
        Vector3 currentWorld =
            source.TransformPoint(
                localPoint
            );

        Vector3 rootLocal =
            root.InverseTransformPoint(
                currentWorld
            );

        // Instantiate(... Quaternion.identity) root rotationı
        // sıfırlıyor ama prefab scale'ini koruyor.
        // Footprint'i tam bu spawn şekline göre hesapla.
        Vector2 point =
            new Vector2(
                rootLocal.x *
                root.localScale.x,

                rootLocal.y *
                root.localScale.y
            );

        if (!found)
        {
            min = point;
            max = point;
            found = true;

            return;
        }

        min =
            Vector2.Min(
                min,
                point
            );

        max =
            Vector2.Max(
                max,
                point
            );
    }

    private bool IsBlocked(
        Collider2D hit)
    {
        GameObject obj =
            hit.gameObject;

        return
            hit.CompareTag("Coin") ||
            hit.CompareTag("Player") ||
            hit.CompareTag("PowerUp") ||
            hit.CompareTag("Enemy") ||
            hit.CompareTag("Bomb") ||
            obj.layer ==
            obstacleLayerIndex ||
            obj.layer ==
            wallLayerIndex;
    }

    private static float
        DistancePointToAabb(
            Vector2 point,
            Vector2 center,
            Vector2 halfExtents)
    {
        float dx =
            Mathf.Max(
                0f,
                Mathf.Abs(
                    point.x -
                    center.x
                ) -
                halfExtents.x
            );

        float dy =
            Mathf.Max(
                0f,
                Mathf.Abs(
                    point.y -
                    center.y
                ) -
                halfExtents.y
            );

        return Mathf.Sqrt(
            dx * dx +
            dy * dy
        );
    }

    private static float
        DistanceBetweenAabbs(
            Vector2 centerA,
            Vector2 halfA,
            Vector2 centerB,
            Vector2 halfB)
    {
        float dx =
            Mathf.Max(
                0f,
                Mathf.Abs(
                    centerA.x -
                    centerB.x
                ) -
                (
                    halfA.x +
                    halfB.x
                )
            );

        float dy =
            Mathf.Max(
                0f,
                Mathf.Abs(
                    centerA.y -
                    centerB.y
                ) -
                (
                    halfA.y +
                    halfB.y
                )
            );

        return Mathf.Sqrt(
            dx * dx +
            dy * dy
        );
    }
}
