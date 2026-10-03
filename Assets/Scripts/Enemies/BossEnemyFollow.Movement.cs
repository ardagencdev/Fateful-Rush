using System.Collections;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in BossEnemyFollow.cs.
public partial class BossEnemyFollow
{
    private void RefreshNavigationFilter()
    {
        navigationFilter = new ContactFilter2D();
        navigationFilter.SetLayerMask(
            EnemyObstacleSteering2D.BuildNavigationMask(
                (LayerMask)(solidLayers.value | obstacleLayer.value)
            )
        );
        navigationFilter.useLayerMask = true;
        navigationFilter.useTriggers = false;
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
        {
            if (playerMovement == null)
                playerMovement = player.GetComponent<PlayerMovement>();

            if (playerArmor == null)
                playerArmor = player.GetComponent<PlayerArmor>();

            return;
        }

        GameObject foundPlayer =
            GameObject.FindGameObjectWithTag("Player");

        if (foundPlayer == null)
            return;

        player = foundPlayer.transform;
        playerMovement = foundPlayer.GetComponent<PlayerMovement>();
        playerArmor = foundPlayer.GetComponent<PlayerArmor>();
    }

    private void MoveBoss()
    {
        Vector2 toPlayer =
            (Vector2)player.position - rb.position;

        if (toPlayer.sqrMagnitude <= 0.001f)
        {
            ResetStuckCheck();
            return;
        }

        Vector2 targetDirection = toPlayer.normalized;

        smoothedDirection =
            Vector2.Lerp(
                smoothedDirection == Vector2.zero
                    ? targetDirection
                    : smoothedDirection,
                targetDirection,
                directionSmoothness * Time.fixedDeltaTime
            ).normalized;

        // Kisa menzilli steering'e gelmeden once Boss kendi buyuk collider'i
        // icin uzun menzilli bir rota karari verir. Ozellikle obstacle + ekran
        // kenari arasindaki dar koridorlari daha yaklasmadan eler.
        Vector2 plannedDirection =
            GetBossPlannedDirection(
                smoothedDirection
            );

        if (plannedDirection.sqrMagnitude <= 0.001f)
            plannedDirection = smoothedDirection;

        FlipSprite(plannedDirection);

        Vector2 finalDirection = plannedDirection;

        if (unstuckTimer > 0f)
        {
            unstuckTimer -= Time.fixedDeltaTime;

            Vector2 sideDirection =
                GetPerpendicularDirection(
                    smoothedDirection,
                    unstuckDirection
                );

            finalDirection =
                (smoothedDirection +
                 sideDirection * unstuckSideForce).normalized;
        }

        bool moved = MoveWithCollision(finalDirection);
        HandleStuckCheck(moved);
    }

    private Vector2 GetBossPlannedDirection(
        Vector2 goalDirection)
    {
        if (goalDirection.sqrMagnitude <= 0.001f)
            return Vector2.zero;

        goalDirection.Normalize();

        float lookAhead =
            Mathf.Max(
                obstacleProbeDistance,
                routeLookAheadDistance
            );

        float directClearance =
            GetBossRouteClearance(
                goalDirection,
                lookAhead
            );

        bool directRouteClear =
            directClearance >= lookAhead - 0.02f;

        if (routeCommitTimer > 0f)
            routeCommitTimer -= Time.fixedDeltaTime;

        // Tam uzunlukta yol acildiysa artik detour'a gerek yok.
        if (directRouteClear)
        {
            routeCommitTimer = 0f;
            return goalDirection;
        }

        int preferredSide =
            committedRouteSide == 0
                ? (unstuckDirection >= 0 ? 1 : -1)
                : committedRouteSide;

        Vector2 bestDirection = Vector2.zero;
        float bestScore = float.NegativeInfinity;
        int bestSide = preferredSide;

        int samples =
            Mathf.Clamp(
                routeAngleSamples,
                4,
                9
            );

        // Commit aktifken once ayni taraftaki genis detour acilarini test et.
        // Bu sayede Boss evade'den sonra ayni dar araliga tekrar yonelmez.
        EvaluateSide(preferredSide, true);

        // Tercih edilen taraf tamamen kapaliysa diger tarafa izin ver.
        EvaluateSide(-preferredSide, false);

        if (bestDirection.sqrMagnitude > 0.001f)
        {
            bool changedSide =
                bestSide != committedRouteSide;

            if (routeCommitTimer <= 0f ||
                committedRouteSide == 0 ||
                changedSide)
            {
                committedRouteSide = bestSide;
                routeCommitTimer =
                    Mathf.Max(
                        0.1f,
                        routeCommitDuration
                    );
            }

            return bestDirection.normalized;
        }

        // Uzun menzilde iyi rota bulunamazsa mevcut local steering/stuck
        // sistemi son guvenlik kati olarak calismaya devam eder.
        return goalDirection;

        void EvaluateSide(
            int side,
            bool preferred)
        {
            side = side >= 0 ? 1 : -1;

            for (int i = 0; i < samples; i++)
            {
                float t =
                    samples <= 1
                        ? 0f
                        : i / (float)(samples - 1);

                // Boss buyuk oldugu icin kucuk 10-15 derecelik sapmalar yerine
                // obstacle'i gercekten dolanabilecek daha genis acilar kullan.
                float angle =
                    Mathf.Lerp(
                        28f,
                        118f,
                        t
                    );

                Vector2 candidate =
                    RotateDirection(
                        goalDirection,
                        angle * side
                    );

                float clearance =
                    GetBossRouteClearance(
                        candidate,
                        lookAhead
                    );

                // En az Boss'un kendi capina yakin bir ilerleme alani yoksa
                // bu rota bir "dar koridor" kabul edilir.
                float bossDiameter =
                    GetBossDiameter();

                float minimumUsefulClearance =
                    Mathf.Max(
                        0.8f,
                        bossDiameter * 0.9f
                    );

                if (clearance < minimumUsefulClearance)
                    continue;

                float clearanceScore =
                    Mathf.Clamp01(
                        clearance / lookAhead
                    );

                float goalProgress =
                    Vector2.Dot(
                        candidate,
                        goalDirection
                    );

                // Uzağa giden ve player yonunde makul ilerleme saglayan rota
                // tercih edilir. Commit edilen tarafa ek bonus verilir.
                float sideBonus =
                    side == committedRouteSide
                        ? routeSideCommitment
                        : 0f;

                if (routeCommitTimer > 0f &&
                    preferred)
                {
                    sideBonus += 0.18f;
                }

                float score =
                    clearanceScore * 2.2f +
                    goalProgress * 0.65f +
                    sideBonus;

                // Tam look-ahead boyunca acik rota ciddi bonus alir.
                if (clearance >= lookAhead - 0.02f)
                    score += 0.55f;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestDirection = candidate;
                bestSide = side;
            }
        }
    }

    private float GetBossRouteClearance(
        Vector2 direction,
        float requestedDistance)
    {
        if (direction.sqrMagnitude <= 0.001f ||
            requestedDistance <= 0f)
        {
            return 0f;
        }

        direction.Normalize();

        float physicsClearance =
            EnemyObstacleSteering2D.GetPathClearance(
                bossCollider,
                direction,
                navigationFilter,
                routePlanningHits,
                requestedDistance
            );

        float arenaClearance =
            GetArenaClearance(
                direction,
                requestedDistance
            );

        return Mathf.Min(
            physicsClearance,
            arenaClearance
        );
    }

    private float GetArenaClearance(
        Vector2 direction,
        float requestedDistance)
    {
        CameraWorldBounds bounds =
            CameraWorldBounds.Instance;

        if (bounds == null ||
            rb == null ||
            bossCollider == null)
        {
            return requestedDistance;
        }

        direction.Normalize();

        Bounds colliderBounds =
            bossCollider.bounds;

        float safeMinX =
            bounds.MinX +
            colliderBounds.extents.x +
            Mathf.Max(0f, arenaEdgePadding);

        float safeMaxX =
            bounds.MaxX -
            colliderBounds.extents.x -
            Mathf.Max(0f, arenaEdgePadding);

        float safeMinY =
            bounds.MinY +
            colliderBounds.extents.y +
            Mathf.Max(0f, arenaEdgePadding);

        float safeMaxY =
            bounds.MaxY -
            colliderBounds.extents.y -
            Mathf.Max(0f, arenaEdgePadding);

        Vector2 position =
            rb.position;

        float clearance =
            requestedDistance;

        const float epsilon =
            0.0001f;

        if (direction.x > epsilon)
        {
            float xDistance =
                (safeMaxX - position.x) /
                direction.x;

            clearance =
                Mathf.Min(
                    clearance,
                    Mathf.Max(0f, xDistance)
                );
        }
        else if (direction.x < -epsilon)
        {
            float xDistance =
                (safeMinX - position.x) /
                direction.x;

            clearance =
                Mathf.Min(
                    clearance,
                    Mathf.Max(0f, xDistance)
                );
        }

        if (direction.y > epsilon)
        {
            float yDistance =
                (safeMaxY - position.y) /
                direction.y;

            clearance =
                Mathf.Min(
                    clearance,
                    Mathf.Max(0f, yDistance)
                );
        }
        else if (direction.y < -epsilon)
        {
            float yDistance =
                (safeMinY - position.y) /
                direction.y;

            clearance =
                Mathf.Min(
                    clearance,
                    Mathf.Max(0f, yDistance)
                );
        }

        return Mathf.Clamp(
            clearance,
            0f,
            requestedDistance
        );
    }

    private float GetBossDiameter()
    {
        if (bossCollider == null)
            return 1f;

        Bounds bounds =
            bossCollider.bounds;

        return Mathf.Max(
            bounds.size.x,
            bounds.size.y
        );
    }

    private static Vector2 RotateDirection(
        Vector2 direction,
        float degrees)
    {
        float radians =
            degrees *
            Mathf.Deg2Rad;

        float sin =
            Mathf.Sin(radians);

        float cos =
            Mathf.Cos(radians);

        return new Vector2(
            direction.x * cos -
            direction.y * sin,
            direction.x * sin +
            direction.y * cos
        ).normalized;
    }

    private bool MoveWithCollision(Vector2 direction)
    {
        float movementDistance =
            speed * Time.fixedDeltaTime;

        if (EnemyObstacleSteering2D.TryGetOverlapRecovery(
                bossCollider,
                navigationFilter,
                out Vector2 overlapDirection,
                out float penetrationDepth))
        {
            float recoveryDistance =
                EnemyObstacleSteering2D.GetOverlapRecoveryDistance(
                    penetrationDepth,
                    movementDistance,
                    castSkin
                );

            rb.MovePosition(
                rb.position +
                overlapDirection * recoveryDistance
            );

            return true;
        }

        if (direction.sqrMagnitude <= 0.001f)
            return false;

        Vector2 steeredDirection =
            EnemyObstacleSteering2D.GetSteeredDirection(
                bossCollider,
                direction,
                direction,
                navigationFilter,
                avoidanceHits,
                obstacleProbeDistance,
                movementDistance,
                castSkin,
                slideDirectionAttempts,
                obstacleOutwardBias,
                ref unstuckDirection
            );

        if (steeredDirection.sqrMagnitude <= 0.001f)
            return false;

        Vector2 intendedMovement =
            steeredDirection * movementDistance;

        Vector2 shakeOffset =
            Random.insideUnitCircle * Mathf.Max(0f, normalShakeAmount);

        Vector2 movement = intendedMovement + shakeOffset;

        if (EnemyObstacleSteering2D.MoveDisplacementWithPhysicsSlide(
                rb,
                bossCollider,
                movement,
                Time.fixedDeltaTime,
                navigationFilter,
                7))
        {
            return true;
        }

        if (TrySlideAroundObstacle(
                steeredDirection,
                intendedMovement.magnitude))
        {
            return true;
        }

        unstuckDirection *= -1;
        return false;
    }

    private bool TrySlideAroundObstacle(
        Vector2 forwardDirection,
        float movementDistance)
    {
        if (movementDistance <= 0f)
            return false;

        Vector2 leftDirection =
            GetPerpendicularDirection(forwardDirection, 1);

        Vector2 rightDirection =
            GetPerpendicularDirection(forwardDirection, -1);

        for (int attempt = 0;
             attempt < slideDirectionAttempts;
             attempt++)
        {
            float blend =
                (attempt + 1f) / slideDirectionAttempts;

            Vector2 firstSide =
                unstuckDirection > 0
                    ? leftDirection
                    : rightDirection;

            Vector2 secondSide =
                unstuckDirection > 0
                    ? rightDirection
                    : leftDirection;

            Vector2 firstDirection =
                Vector2.Lerp(
                    forwardDirection,
                    firstSide,
                    blend
                ).normalized;

            Vector2 firstMovement =
                firstDirection * movementDistance;

            if (CanMove(firstMovement))
            {
                rb.MovePosition(rb.position + firstMovement);
                return true;
            }

            Vector2 secondDirection =
                Vector2.Lerp(
                    forwardDirection,
                    secondSide,
                    blend
                ).normalized;

            Vector2 secondMovement =
                secondDirection * movementDistance;

            if (CanMove(secondMovement))
            {
                rb.MovePosition(rb.position + secondMovement);
                unstuckDirection *= -1;
                return true;
            }
        }

        return false;
    }

    private Vector2 GetPerpendicularDirection(
        Vector2 direction,
        int side)
    {
        return new Vector2(-direction.y, direction.x) * side;
    }

    private bool CanMove(Vector2 movement)
    {
        if (bossCollider == null ||
            movement.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        float movementDistance =
            movement.magnitude;

        float arenaClearance =
            GetArenaClearance(
                movement.normalized,
                movementDistance +
                Mathf.Max(castSkin, 0f)
            );

        if (arenaClearance <
            movementDistance - 0.001f)
        {
            return false;
        }

        int hitCount =
            bossCollider.Cast(
                movement.normalized,
                navigationFilter,
                castHits,
                movement.magnitude + Mathf.Max(castSkin, 0f)
            );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = castHits[i].collider;

            if (hitCollider == null ||
                hitCollider == bossCollider)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void HandleStuckCheck(bool attemptedMove)
    {
        stuckTimer += Time.fixedDeltaTime;

        float effectiveStuckCheckTime = Mathf.Min(
            Mathf.Max(0.05f, stuckCheckTime),
            0.25f
        );

        if (stuckTimer < effectiveStuckCheckTime)
            return;

        float movedDistanceSqr =
            (rb.position - lastPosition).sqrMagnitude;

        float requiredDistanceSqr =
            stuckDistance * stuckDistance;

        if (movedDistanceSqr < requiredDistanceSqr)
        {
            Vector2 escapeDirection = GetEscapeDirection();

            if (escapeDirection.sqrMagnitude <= 0.001f)
            {
                Vector2 playerDirection =
                    player != null
                        ? ((Vector2)player.position - rb.position).normalized
                        : Vector2.right;

                Vector2 sideDirection =
                    GetPerpendicularDirection(
                        playerDirection,
                        unstuckDirection
                    );

                escapeDirection =
                    (sideDirection +
                     Random.insideUnitCircle * 0.35f).normalized;
            }

            Vector2 escapeMovement =
                escapeDirection *
                speed *
                escapeSpeedMultiplier *
                Time.fixedDeltaTime;

            if (CanMove(escapeMovement))
            {
                rb.MovePosition(rb.position + escapeMovement);
            }
            else
            {
                unstuckDirection *= -1;
            }

            unstuckTimer = unstuckDuration;
        }

        lastPosition = rb.position;
        stuckTimer = 0f;
    }

    private void ResetStuckCheck()
    {
        stuckTimer = 0f;

        if (rb != null)
            lastPosition = rb.position;
    }

    private Vector2 GetEscapeDirection()
    {
        ContactFilter2D obstacleFilter =
            new ContactFilter2D();

        obstacleFilter.SetLayerMask(
            obstacleLayer | solidLayers
        );

        obstacleFilter.useLayerMask = true;
        obstacleFilter.useTriggers = true;

        int hitCount =
            Physics2D.OverlapCircle(
                rb.position,
                escapeCheckRadius,
                obstacleFilter,
                escapeHits
            );

        if (hitCount <= 0)
            return Vector2.zero;

        Vector2 escapeDirection = Vector2.zero;
        int validHitCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = escapeHits[i];

            if (hit == null || hit == bossCollider)
                continue;

            Vector2 closestPoint =
                hit.ClosestPoint(rb.position);

            Vector2 awayFromObstacle =
                rb.position - closestPoint;

            if (awayFromObstacle.sqrMagnitude <= 0.001f)
            {
                awayFromObstacle =
                    rb.position - (Vector2)hit.bounds.center;
            }

            if (awayFromObstacle.sqrMagnitude <= 0.001f)
                continue;

            float distance = awayFromObstacle.magnitude;
            float weight = 1f / Mathf.Max(distance, 0.05f);

            escapeDirection +=
                awayFromObstacle.normalized * weight;

            validHitCount++;
        }

        if (validHitCount <= 0 ||
            escapeDirection.sqrMagnitude <= 0.001f)
        {
            return Vector2.zero;
        }

        return escapeDirection.normalized;
    }

    private void FlipSprite(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) <= 0.01f)
            return;

        facingSign = direction.x > 0f ? 1 : -1;
        ApplyCurrentScaleMagnitude();
    }

    private void ZeroVelocity()
    {
        if (rb == null)
            return;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void StopBossMovement()
    {
        speed = 0f;
        ZeroVelocity();
    }
}
