using UnityEngine;

public static class MovementMath2D
{
    public static float GetAdaptiveAcceleration(
        Vector2 currentVelocity,
        Vector2 targetInput,
        float acceleration,
        float deceleration,
        float turnAcceleration,
        float lowInputAccelerationMultiplier,
        float highInputAccelerationMultiplier,
        float sharpTurnBoost
    )
    {
        if (targetInput.sqrMagnitude <= 0.001f)
            return deceleration;

        float inputMagnitude =
            Mathf.Clamp01(targetInput.magnitude);

        float analogMultiplier =
            Mathf.Lerp(
                lowInputAccelerationMultiplier,
                highInputAccelerationMultiplier,
                inputMagnitude
            );

        float baseAcceleration =
            acceleration *
            analogMultiplier;

        if (currentVelocity.sqrMagnitude <= 0.01f)
            return baseAcceleration;

        Vector2 currentDirection =
            currentVelocity.normalized;

        Vector2 targetDirection =
            targetInput.normalized;

        float directionDot =
            Vector2.Dot(
                currentDirection,
                targetDirection
            );

        if (directionDot < 0.35f)
        {
            float turnStrength =
                Mathf.InverseLerp(
                    0.35f,
                    -1f,
                    directionDot
                );

            return Mathf.Lerp(
                baseAcceleration,
                turnAcceleration *
                sharpTurnBoost,
                turnStrength
            );
        }

        return baseAcceleration;
    }
}