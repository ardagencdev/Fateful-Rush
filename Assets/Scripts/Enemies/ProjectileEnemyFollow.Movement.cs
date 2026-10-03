using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in ProjectileEnemyFollow.cs.
public partial class ProjectileEnemyFollow
{
    private Transform GetCurrentTarget()
    {
        if (VoidCloneAbility.ActiveCloneTarget != null)
            return VoidCloneAbility.ActiveCloneTarget;

        return player;
    }

    private void UpdateCachedTarget(Transform currentTarget)
    {
        if (cachedCurrentTarget == currentTarget)
            return;

        cachedCurrentTarget = currentTarget;

        targetRigidbody = currentTarget != null
            ? currentTarget.GetComponent<Rigidbody2D>()
            : null;
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
        {
            if (playerMovement == null)
                playerMovement =
                    player.GetComponent<PlayerMovement>();

            return;
        }

        GameObject foundPlayer =
            GameObject.FindGameObjectWithTag("Player");

        if (foundPlayer == null)
            return;

        player = foundPlayer.transform;

        playerMovement =
            foundPlayer.GetComponent<PlayerMovement>();
    }

    private void HandleMovement(Transform currentTarget)
    {
        Vector2 targetPosition =
            currentTarget.position;

        Vector2 toTarget =
            targetPosition - rb.position;

        if (toTarget.sqrMagnitude <= 0.001f)
        {
            ResetStuckCheck();
            return;
        }

        float distance = toTarget.magnitude;
        Vector2 targetDirection = toTarget.normalized;

        Vector2 desiredDirection = Vector2.zero;
        float speedMultiplier = 1f;
        bool retreatingForReload = false;

        if (isReloading)
        {
            float safeReloadDistance =
                Mathf.Max(stoppingDistance, reloadRetreatDistance);

            if (distance < safeReloadDistance)
            {
                desiredDirection = -targetDirection;
                speedMultiplier = reloadMoveSpeedMultiplier;
                retreatingForReload = true;
            }
            else if (strafeEnabled)
            {
                Vector2 sideDirection =
                    new Vector2(
                        -targetDirection.y,
                        targetDirection.x
                    ) * strafeDirection;

                desiredDirection = sideDirection;
                speedMultiplier =
                    strafeSpeedMultiplier *
                    reloadMoveSpeedMultiplier;
            }
            else
            {
                ResetStuckCheck();
                return;
            }
        }
        else if (distance > stoppingDistance)
        {
            desiredDirection = targetDirection;
        }
        else if (distance < retreatDistance)
        {
            desiredDirection = -targetDirection;
        }
        else if (strafeEnabled)
        {
            desiredDirection =
                GetStrafeDirection(
                    targetDirection,
                    distance
                );

            speedMultiplier = strafeSpeedMultiplier;
        }
        else
        {
            ResetStuckCheck();
            return;
        }

        Vector2 waveDirection =
            GetWaveDirection(targetDirection);

        Vector2 separationDirection =
            GetSeparationDirection();

        Vector2 finalDirection =
            desiredDirection +
            waveDirection +
            separationDirection * separationStrength;

        if (finalDirection.sqrMagnitude <= 0.001f)
            finalDirection = desiredDirection;

        finalDirection.Normalize();

        if (unstuckTimer > 0f)
        {
            unstuckTimer -= Time.fixedDeltaTime;

            Vector2 sideDirection =
                new Vector2(
                    -targetDirection.y,
                    targetDirection.x
                ) * unstuckDirection;

            finalDirection =
                (
                    finalDirection +
                    sideDirection * unstuckSideForce
                ).normalized;
        }

        float movementDistance =
            moveSpeed *
            speedMultiplier *
            Time.fixedDeltaTime;

        if (retreatingForReload)
        {
            float safeReloadDistance =
                Mathf.Max(stoppingDistance, reloadRetreatDistance);

            float missingDistance =
                safeReloadDistance - distance;

            movementDistance =
                Mathf.Min(
                    movementDistance,
                    missingDistance
                );
        }
        else if (distance > stoppingDistance)
        {
            float excessDistance =
                distance - stoppingDistance;

            movementDistance =
                Mathf.Min(
                    movementDistance,
                    excessDistance
                );
        }
        else if (distance < retreatDistance)
        {
            float missingDistance =
                retreatDistance - distance;

            movementDistance =
                Mathf.Min(
                    movementDistance,
                    missingDistance
                );
        }

        Move(finalDirection, desiredDirection, movementDistance);
    }

    private Vector2 GetStrafeDirection(
        Vector2 targetDirection,
        float distance
    )
    {
        Vector2 sideDirection =
            new Vector2(
                -targetDirection.y,
                targetDirection.x
            ) * strafeDirection;

        float idealDistance =
            (stoppingDistance + retreatDistance) * 0.5f;

        float distanceDifference =
            distance - idealDistance;

        Vector2 distanceCorrection =
            Vector2.zero;

        if (Mathf.Abs(distanceDifference) >
            strafeDistanceTolerance)
        {
            float correctionStrength =
                Mathf.InverseLerp(
                    strafeDistanceTolerance,
                    Mathf.Max(
                        stoppingDistance - retreatDistance,
                        strafeDistanceTolerance + 0.01f
                    ),
                    Mathf.Abs(distanceDifference)
                );

            if (distanceDifference > 0f)
            {
                distanceCorrection =
                    targetDirection * correctionStrength;
            }
            else
            {
                distanceCorrection =
                    -targetDirection * correctionStrength;
            }
        }

        Vector2 finalDirection =
            sideDirection + distanceCorrection;

        if (finalDirection.sqrMagnitude <= 0.001f)
            return sideDirection;

        return finalDirection.normalized;
    }

    private Vector2 GetWaveDirection(
        Vector2 targetDirection
    )
    {
        Vector2 sideDirection =
            new Vector2(
                -targetDirection.y,
                targetDirection.x
            );

        float wave =
            Mathf.Sin(
                (Time.time + movementOffset) *
                sideMoveSpeed
            );

        return sideDirection *
               wave *
               sideMoveAmount;
    }

    private Vector2 GetSeparationDirection()
    {
        if (!separationEnabled)
            return Vector2.zero;

        if (separationRadius <= 0f)
            return Vector2.zero;

        ContactFilter2D filter =
            new ContactFilter2D();

        filter.SetLayerMask(enemyLayer);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        int hitCount =
            Physics2D.OverlapCircle(
                rb.position,
                separationRadius,
                filter,
                separationHits
            );

        if (hitCount <= 0)
            return Vector2.zero;

        Vector2 separationDirection =
            Vector2.zero;

        int validCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit =
                separationHits[i];

            if (hit == null)
                continue;

            if (hit.attachedRigidbody == rb)
                continue;

            ProjectileEnemyFollow other =
                hit.GetComponentInParent<
                    ProjectileEnemyFollow
                >();

            if (other == null || other == this)
                continue;

            Vector2 awayDirection =
                rb.position -
                (Vector2)other.transform.position;

            float sqrDistance =
                awayDirection.sqrMagnitude;

            if (sqrDistance <= 0.001f)
            {
                awayDirection =
                    Random.insideUnitCircle.normalized;

                sqrDistance = 0.001f;
            }

            float distance =
                Mathf.Sqrt(sqrDistance);

            float proximityStrength =
                1f -
                Mathf.Clamp01(
                    distance / separationRadius
                );

            separationDirection +=
                awayDirection.normalized *
                proximityStrength;

            validCount++;
        }

        if (validCount <= 0)
            return Vector2.zero;

        separationDirection /= validCount;

        return Vector2.ClampMagnitude(
            separationDirection,
            1f
        );
    }

    private void Move(
        Vector2 direction,
        Vector2 tacticalDirection,
        float distance
    )
    {
        if (distance <= 0f)
            return;

        attemptedMovementThisFrame = true;

        if (EnemyObstacleSteering2D.TryGetOverlapRecovery(
                col,
                navigationFilter,
                out Vector2 overlapDirection,
                out float penetrationDepth))
        {
            float recoveryDistance =
                EnemyObstacleSteering2D.GetOverlapRecoveryDistance(
                    penetrationDepth,
                    distance,
                    0.03f
                );

            Vector2 recoveryTarget =
                rb.position +
                overlapDirection * recoveryDistance;

            rb.MovePosition(
                ClampPositionInsideArena(recoveryTarget)
            );

            return;
        }

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Vector2 steeredDirection =
            EnemyObstacleSteering2D.GetSteeredDirection(
                col,
                direction,
                tacticalDirection,
                navigationFilter,
                avoidanceHits,
                obstacleProbeDistance,
                distance,
                0.03f,
                obstacleAvoidanceAttempts,
                obstacleOutwardBias,
                ref obstacleAvoidanceSide
            );

        if (steeredDirection.sqrMagnitude <= 0.001f)
            return;

        Vector2 desiredDisplacement =
            steeredDirection.normalized * distance;

        desiredDisplacement =
            ClampDisplacementToArena(desiredDisplacement);

        if (desiredDisplacement.sqrMagnitude <= 0.000001f)
            return;

        EnemyObstacleSteering2D.MoveDisplacementWithPhysicsSlide(
            rb,
            col,
            desiredDisplacement,
            Time.fixedDeltaTime,
            navigationFilter,
            5
        );
    }

    private Vector2 ClampDisplacementToArena(Vector2 displacement)
    {
        Vector2 desiredPosition = rb.position + displacement;
        Vector2 clampedPosition = ClampPositionInsideArena(desiredPosition);
        return clampedPosition - rb.position;
    }

    private Vector2 ClampPositionInsideArena(Vector2 desiredPosition)
    {
        CameraWorldBounds bounds = CameraWorldBounds.Instance;

        if (bounds == null || col == null)
            return desiredPosition;

        Bounds colliderBounds = col.bounds;
        Vector2 centerOffset =
            (Vector2)colliderBounds.center - rb.position;

        Vector2 extents = colliderBounds.extents;
        float padding = Mathf.Max(0f, arenaEdgePadding);

        float minX = bounds.MinX + extents.x + padding - centerOffset.x;
        float maxX = bounds.MaxX - extents.x - padding - centerOffset.x;
        float minY = bounds.MinY + extents.y + padding - centerOffset.y;
        float maxY = bounds.MaxY - extents.y - padding - centerOffset.y;

        if (minX > maxX)
        {
            float centerX = (bounds.MinX + bounds.MaxX) * 0.5f - centerOffset.x;
            minX = centerX;
            maxX = centerX;
        }

        if (minY > maxY)
        {
            float centerY = (bounds.MinY + bounds.MaxY) * 0.5f - centerOffset.y;
            minY = centerY;
            maxY = centerY;
        }

        return new Vector2(
            Mathf.Clamp(desiredPosition.x, minX, maxX),
            Mathf.Clamp(desiredPosition.y, minY, maxY)
        );
    }

    private void EnforceArenaBounds()
    {
        if (rb == null || isSpawning)
            return;

        Vector2 clampedPosition = ClampPositionInsideArena(rb.position);

        if ((clampedPosition - rb.position).sqrMagnitude <= 0.000001f)
            return;

        rb.position = clampedPosition;
        rb.linearVelocity = Vector2.zero;
        ResetStuckCheck();
    }

    private void HandleStrafeDirectionTimer()
    {
        if (!strafeEnabled)
            return;

        strafeDirectionTimer -=
            Time.fixedDeltaTime;

        if (strafeDirectionTimer > 0f)
            return;

        strafeDirection *= -1;
        ResetStrafeTimer();
    }

    private void ResetStrafeTimer()
    {
        strafeDirectionTimer =
            Random.Range(
                strafeDirectionChangeMinTime,
                strafeDirectionChangeMaxTime
            );
    }

    private void HandleStuckCheck(
        Transform currentTarget
    )
    {
        if (!attemptedMovementThisFrame)
        {
            ResetStuckCheck();
            return;
        }

        if (currentTarget == null)
        {
            ResetStuckCheck();
            return;
        }

        stuckTimer += Time.fixedDeltaTime;

        float effectiveStuckCheckTime = Mathf.Min(
            Mathf.Max(0.05f, stuckCheckTime),
            0.25f
        );

        if (stuckTimer < effectiveStuckCheckTime)
            return;

        float movedSqrDistance =
            (rb.position - lastPosition)
            .sqrMagnitude;

        float stuckSqrDistance =
            stuckDistance * stuckDistance;

        if (movedSqrDistance <
            stuckSqrDistance)
        {
            Vector2 escapeDirection =
                GetEscapeDirection();

            if (escapeDirection != Vector2.zero)
            {
                Vector2 escapeTarget =
                    rb.position +
                    escapeDirection *
                    moveSpeed *
                    escapeSpeedMultiplier *
                    Time.fixedDeltaTime;

                rb.MovePosition(
                    ClampPositionInsideArena(escapeTarget)
                );

                unstuckDirection =
                    Random.Range(0, 2) == 0
                        ? -1
                        : 1;

                unstuckTimer =
                    unstuckDuration;
            }
        }

        lastPosition = rb.position;
        stuckTimer = 0f;
    }

    private Vector2 GetEscapeDirection()
    {
        ContactFilter2D filter =
            new ContactFilter2D();

        filter.SetLayerMask(
            EnemyObstacleSteering2D.BuildNavigationMask(obstacleLayer)
        );
        filter.useLayerMask = true;
        filter.useTriggers = false;

        int hitCount =
            Physics2D.OverlapCircle(
                rb.position,
                escapeCheckRadius,
                filter,
                escapeHits
            );

        if (hitCount <= 0)
            return Vector2.zero;

        Vector2 escapeDirection =
            Vector2.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit =
                escapeHits[i];

            if (hit == null)
                continue;

            Vector2 closestPoint =
                hit.ClosestPoint(rb.position);

            Vector2 awayFromObstacle =
                rb.position -
                closestPoint;

            if (awayFromObstacle.sqrMagnitude <=
                0.001f)
            {
                awayFromObstacle =
                    rb.position -
                    (Vector2)hit.bounds.center;
            }

            if (awayFromObstacle.sqrMagnitude >
                0.001f)
            {
                escapeDirection +=
                    awayFromObstacle.normalized;
            }
        }

        if (escapeDirection.sqrMagnitude <=
            0.001f)
        {
            return Vector2.zero;
        }

        return escapeDirection.normalized;
    }

    private void ResetStuckCheck()
    {
        stuckTimer = 0f;
        lastPosition = rb.position;
    }

    private void FlipSprite(
        Transform currentTarget
    )
    {
        if (currentTarget == null ||
            isSpawning)
        {
            return;
        }

        Vector2 direction =
            currentTarget.position -
            transform.position;

        Vector3 scale =
            transform.localScale;

        float absX =
            Mathf.Abs(scale.x);

        if (absX <= 0.001f)
        {
            absX =
                Mathf.Abs(
                    spawnTargetScale.x
                );
        }

        if (direction.x > 0.01f)
            scale.x = absX;
        else if (direction.x < -0.01f)
            scale.x = -absX;

        transform.localScale = scale;
    }

    private void StopMovementOnly()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }
}
