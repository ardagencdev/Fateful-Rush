using UnityEngine;

/// <summary>Shared star geometry; menu and gameplay retain separate state and appearance policies.</summary>
public static class StarfieldFlowUtility
{
    public struct Bounds
    {
        public float left;
        public float right;
        public float bottom;
        public float top;
        public float planeZ;
    }

    public static Bounds GetCameraBounds(ParticleSystem nearStars, Camera nearStarsCamera)
    {
        float planeZ = nearStars != null
            ? nearStars.transform.position.z
            : 0f;

        float depth = Mathf.Abs(
            planeZ - nearStarsCamera.transform.position.z
        );

        Vector3 bottomLeft = nearStarsCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, depth)
        );

        Vector3 topRight = nearStarsCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, depth)
        );

        return new Bounds
        {
            left = Mathf.Min(bottomLeft.x, topRight.x),
            right = Mathf.Max(bottomLeft.x, topRight.x),
            bottom = Mathf.Min(bottomLeft.y, topRight.y),
            top = Mathf.Max(bottomLeft.y, topRight.y),
            planeZ = planeZ
        };
    }

    public static Vector3 GetRandomEntryPosition(
        Bounds bounds,
        Vector2 flow, float nearStarsSpawnPadding)
    {
        bool hasHorizontalFlow = Mathf.Abs(flow.x) > 0.0001f;
        bool hasVerticalFlow = Mathf.Abs(flow.y) > 0.0001f;

        if (!hasHorizontalFlow && !hasVerticalFlow)
        {
            return new Vector3(
                Random.Range(bounds.left, bounds.right),
                bounds.top + nearStarsSpawnPadding,
                bounds.planeZ
            );
        }

        bool useVerticalEdge;

        if (!hasHorizontalFlow)
        {
            useVerticalEdge = true;
        }
        else if (!hasVerticalFlow)
        {
            useVerticalEdge = false;
        }
        else
        {
            float width = Mathf.Max(0.01f, bounds.right - bounds.left);
            float height = Mathf.Max(0.01f, bounds.top - bounds.bottom);

            float verticalWeight = width * Mathf.Abs(flow.y);
            float horizontalWeight = height * Mathf.Abs(flow.x);
            float totalWeight = verticalWeight + horizontalWeight;

            useVerticalEdge =
                Random.value < verticalWeight / Mathf.Max(0.0001f, totalWeight);
        }

        if (useVerticalEdge)
        {
            float y = flow.y < 0f
                ? bounds.top + nearStarsSpawnPadding
                : bounds.bottom - nearStarsSpawnPadding;

            return new Vector3(
                Random.Range(bounds.left, bounds.right),
                y,
                bounds.planeZ
            );
        }

        float x = flow.x > 0f
            ? bounds.left - nearStarsSpawnPadding
            : bounds.right + nearStarsSpawnPadding;

        return new Vector3(
            x,
            Random.Range(bounds.bottom, bounds.top),
            bounds.planeZ
        );
    }

    public static Vector3 WorldToSimulationPosition(ParticleSystem nearStars, Vector3 worldPosition)
    {
        ParticleSystem.MainModule main = nearStars.main;

        switch (main.simulationSpace)
        {
            case ParticleSystemSimulationSpace.Local:
                return nearStars.transform.InverseTransformPoint(worldPosition);

            case ParticleSystemSimulationSpace.Custom:
                if (main.customSimulationSpace != null)
                    return main.customSimulationSpace.InverseTransformPoint(worldPosition);
                return worldPosition;

            default:
                return worldPosition;
        }
    }

    public static Vector3 SimulationToWorldPosition(ParticleSystem nearStars, Vector3 simulationPosition)
    {
        ParticleSystem.MainModule main = nearStars.main;

        switch (main.simulationSpace)
        {
            case ParticleSystemSimulationSpace.Local:
                return nearStars.transform.TransformPoint(simulationPosition);

            case ParticleSystemSimulationSpace.Custom:
                if (main.customSimulationSpace != null)
                    return main.customSimulationSpace.TransformPoint(simulationPosition);
                return simulationPosition;

            default:
                return simulationPosition;
        }
    }

    public static ParticleSystem.MinMaxCurve ScaleCurve(
        ParticleSystem.MinMaxCurve source,
        float multiplier)
    {
        multiplier = Mathf.Max(0f, multiplier);

        switch (source.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return new ParticleSystem.MinMaxCurve(
                    source.constant * multiplier
                );

            case ParticleSystemCurveMode.TwoConstants:
                return new ParticleSystem.MinMaxCurve(
                    source.constantMin * multiplier,
                    source.constantMax * multiplier
                );

            case ParticleSystemCurveMode.Curve:
                return new ParticleSystem.MinMaxCurve(
                    source.curveMultiplier * multiplier,
                    source.curve
                );

            case ParticleSystemCurveMode.TwoCurves:
                return new ParticleSystem.MinMaxCurve(
                    source.curveMultiplier * multiplier,
                    source.curveMin,
                    source.curveMax
                );

            default:
                return source;
        }
    }

    public static float GetRepresentativeCurveValue(
        ParticleSystem.MinMaxCurve curve)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return curve.constant;

            case ParticleSystemCurveMode.TwoConstants:
                return (curve.constantMin + curve.constantMax) * 0.5f;

            case ParticleSystemCurveMode.Curve:
                return curve.curve != null
                    ? curve.curve.Evaluate(0.5f) * curve.curveMultiplier
                    : 0f;

            case ParticleSystemCurveMode.TwoCurves:
                float minValue = curve.curveMin != null
                    ? curve.curveMin.Evaluate(0.5f)
                    : 0f;

                float maxValue = curve.curveMax != null
                    ? curve.curveMax.Evaluate(0.5f)
                    : 0f;

                return (minValue + maxValue) * 0.5f * curve.curveMultiplier;

            default:
                return 0f;
        }
    }

    public static void EmitAtWorldPosition(ParticleSystem system, Vector3 worldPosition)
    {
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
        {
            position = WorldToSimulationPosition(system, worldPosition),
            applyShapeToPosition = false
        };
        system.Emit(emitParams, 1);
    }

    public static void SeedInitial(ParticleSystem system, Camera camera, float fill)
    {
        int targetCount = Mathf.RoundToInt(system.main.maxParticles * fill);
        if (targetCount <= 0) return;
        Bounds bounds = GetCameraBounds(system, camera);
        for (int i = 0; i < targetCount; i++)
        {
            Vector3 position = new Vector3(Random.Range(bounds.left, bounds.right),
                Random.Range(bounds.bottom, bounds.top), bounds.planeZ);
            EmitAtWorldPosition(system, position);
        }
    }

}
