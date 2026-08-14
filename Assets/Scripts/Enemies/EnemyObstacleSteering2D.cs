using UnityEngine;

/// <summary>
/// Lightweight local obstacle steering shared by moving enemies.
/// It anticipates collisions with the enemy's real Collider2D shape and
/// chooses a clear direction that keeps as much progress as possible.
/// </summary>
public static class EnemyObstacleSteering2D
{
    private const float MinimumDirectionSqr = 0.0001f;

    public static LayerMask BuildNavigationMask(LayerMask configuredMask)
    {
        int mask = configuredMask.value;

        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer >= 0)
            mask |= 1 << obstacleLayer;

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer >= 0)
            mask |= 1 << wallLayer;

        return mask;
    }

    public static Vector2 GetSteeredDirection(
        Collider2D selfCollider,
        Vector2 desiredDirection,
        Vector2 goalDirection,
        ContactFilter2D solidFilter,
        RaycastHit2D[] hitBuffer,
        float probeDistance,
        float movementDistance,
        float castSkin,
        int angleAttempts,
        float outwardBias,
        ref int preferredSide)
    {
        if (desiredDirection.sqrMagnitude <= MinimumDirectionSqr)
            return Vector2.zero;

        desiredDirection.Normalize();

        if (goalDirection.sqrMagnitude <= MinimumDirectionSqr)
            goalDirection = desiredDirection;
        else
            goalDirection.Normalize();

        if (selfCollider == null || hitBuffer == null || hitBuffer.Length == 0)
            return desiredDirection;

        movementDistance = Mathf.Max(0f, movementDistance);
        castSkin = Mathf.Max(0f, castSkin);

        float minimumClearance = movementDistance + castSkin + 0.005f;
        float effectiveProbeDistance = Mathf.Max(
            probeDistance,
            minimumClearance + 0.05f
        );

        float directClearance = GetClearance(
            selfCollider,
            desiredDirection,
            solidFilter,
            hitBuffer,
            effectiveProbeDistance + castSkin,
            out RaycastHit2D directHit
        );

        // Clear path: do not disturb the enemy's normal movement at all.
        float fullProbeDistance = effectiveProbeDistance + castSkin;
        if (directClearance >= fullProbeDistance - 0.001f)
            return desiredDirection;

        int preferredSideLocal = preferredSide >= 0 ? 1 : -1;
        angleAttempts = Mathf.Clamp(angleAttempts, 2, 8);
        outwardBias = Mathf.Clamp(outwardBias, 0f, 1f);

        Vector2 obstacleNormal = directHit.collider != null
            ? directHit.normal.normalized
            : -desiredDirection;

        Vector2 bestDirection = Vector2.zero;
        float bestScore = float.NegativeInfinity;
        int bestSide = preferredSideLocal;

        // First try directions that naturally slide along the blocking surface.
        Vector2 surfaceTangent = new Vector2(
            -obstacleNormal.y,
            obstacleNormal.x
        );

        EvaluateCandidate(
            surfaceTangent * preferredSideLocal,
            preferredSideLocal,
            true
        );

        EvaluateCandidate(
            surfaceTangent * -preferredSideLocal,
            -preferredSideLocal,
            true
        );

        // Then fan outward from the desired direction. Looking farther than a
        // single physics step prevents the old "touch obstacle, stop, react" feel.
        for (int i = 0; i < angleAttempts; i++)
        {
            float t = angleAttempts <= 1
                ? 1f
                : i / (float)(angleAttempts - 1);

            float angle = Mathf.Lerp(25f, 110f, t);

            int firstSide = preferredSideLocal;
            int secondSide = -preferredSideLocal;

            EvaluateCandidate(
                Rotate(desiredDirection, angle * firstSide),
                firstSide,
                false
            );

            EvaluateCandidate(
                Rotate(desiredDirection, angle * secondSide),
                secondSide,
                false
            );
        }

        if (bestDirection.sqrMagnitude > MinimumDirectionSqr)
        {
            preferredSide = bestSide;
            return bestDirection.normalized;
        }

        // Last immediate escape attempt for cases where the collider is already
        // touching/overlapping an obstacle. The slower stuck detector remains a
        // final safety net after this.
        if (obstacleNormal.sqrMagnitude > MinimumDirectionSqr)
        {
            float awayClearance = GetClearance(
                selfCollider,
                obstacleNormal,
                solidFilter,
                hitBuffer,
                minimumClearance,
                out _
            );

            if (awayClearance >= minimumClearance)
                return obstacleNormal;
        }

        return Vector2.zero;

        void EvaluateCandidate(
            Vector2 candidateDirection,
            int side,
            bool surfaceSlide)
        {
            if (candidateDirection.sqrMagnitude <= MinimumDirectionSqr)
                return;

            candidateDirection.Normalize();

            // A small outward component keeps the collider from rubbing along
            // the obstacle edge while it is trying to pass it.
            if (obstacleNormal.sqrMagnitude > MinimumDirectionSqr)
            {
                float bias = surfaceSlide
                    ? Mathf.Max(outwardBias, 0.2f)
                    : outwardBias;

                candidateDirection = (
                    candidateDirection +
                    obstacleNormal * bias
                ).normalized;
            }

            float clearance = GetClearance(
                selfCollider,
                candidateDirection,
                solidFilter,
                hitBuffer,
                fullProbeDistance,
                out _
            );

            if (clearance < minimumClearance)
                return;

            float clearanceScore = Mathf.Clamp01(
                clearance / effectiveProbeDistance
            );

            float desiredProgress = Vector2.Dot(
                candidateDirection,
                desiredDirection
            );

            float goalProgress = Vector2.Dot(
                candidateDirection,
                goalDirection
            );

            float sideCommitmentBonus = side == preferredSideLocal
                ? 0.12f
                : 0f;

            float score =
                clearanceScore * 1.35f +
                desiredProgress * 0.75f +
                goalProgress * 0.55f +
                sideCommitmentBonus;

            if (score <= bestScore)
                return;

            bestScore = score;
            bestDirection = candidateDirection;
            bestSide = side >= 0 ? 1 : -1;
        }
    }

    /// <summary>
    /// Returns true when the collider can travel the requested distance in
    /// the given direction without hitting the configured navigation layers.
    /// Uses the same shape-cast rules as GetSteeredDirection.
    /// </summary>
    /// <summary>
    /// Returns the free travel distance for the collider in a direction,
    /// using the exact same shape-cast rules as local steering.
    /// Useful for long-range route planning without duplicating physics code.
    /// </summary>
    public static float GetPathClearance(
        Collider2D selfCollider,
        Vector2 direction,
        ContactFilter2D solidFilter,
        RaycastHit2D[] hitBuffer,
        float distance)
    {
        if (distance <= 0f)
            return 0f;

        if (direction.sqrMagnitude <= MinimumDirectionSqr)
            return 0f;

        if (selfCollider == null ||
            hitBuffer == null ||
            hitBuffer.Length == 0)
        {
            return distance;
        }

        return GetClearance(
            selfCollider,
            direction.normalized,
            solidFilter,
            hitBuffer,
            distance,
            out _
        );
    }

    public static bool IsPathClear(
        Collider2D selfCollider,
        Vector2 direction,
        ContactFilter2D solidFilter,
        RaycastHit2D[] hitBuffer,
        float distance,
        float castSkin = 0f)
    {
        if (distance <= 0f)
            return true;

        if (direction.sqrMagnitude <= MinimumDirectionSqr)
            return true;

        if (selfCollider == null ||
            hitBuffer == null ||
            hitBuffer.Length == 0)
        {
            return true;
        }

        float requiredDistance =
            Mathf.Max(0f, distance) +
            Mathf.Max(0f, castSkin);

        float clearance = GetClearance(
            selfCollider,
            direction.normalized,
            solidFilter,
            hitBuffer,
            requiredDistance,
            out _
        );

        return clearance >= requiredDistance - 0.001f;
    }

    private static float GetClearance(
        Collider2D selfCollider,
        Vector2 direction,
        ContactFilter2D solidFilter,
        RaycastHit2D[] hitBuffer,
        float distance,
        out RaycastHit2D closestHit)
    {
        closestHit = default;

        if (direction.sqrMagnitude <= MinimumDirectionSqr || distance <= 0f)
            return distance;

        int hitCount = selfCollider.Cast(
            direction.normalized,
            solidFilter,
            hitBuffer,
            distance
        );

        float closestDistance = distance;
        bool foundBlockingHit = false;
        Rigidbody2D selfBody = selfCollider.attachedRigidbody;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = hitBuffer[i];
            Collider2D hitCollider = hit.collider;

            if (hitCollider == null)
                continue;

            if (hitCollider == selfCollider)
                continue;

            if (selfBody != null && hitCollider.attachedRigidbody == selfBody)
                continue;

            if (hit.distance >= closestDistance)
                continue;

            closestDistance = Mathf.Max(0f, hit.distance);
            closestHit = hit;
            foundBlockingHit = true;
        }

        return foundBlockingHit
            ? closestDistance
            : distance;
    }

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos
        );
    }
}
