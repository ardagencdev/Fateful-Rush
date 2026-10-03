using System.Collections;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in MainMenuStarColorRandomizer.cs.
public partial class MainMenuStarColorRandomizer
{
    private void ConfigureScreenEdgeFlow()
    {
        if (!useScreenEdgeNearStars || nearStars == null)
            return;

        float minLifetime = Mathf.Max(
            1f,
            Mathf.Min(
                nearStarsLifetimeRange.x,
                nearStarsLifetimeRange.y
            )
        );

        float maxLifetime = Mathf.Max(
            minLifetime,
            Mathf.Max(
                nearStarsLifetimeRange.x,
                nearStarsLifetimeRange.y
            )
        );

        ParticleSystem.MainModule main =
            nearStars.main;

        main.startLifetime =
            new ParticleSystem.MinMaxCurve(
                minLifetime,
                maxLifetime
            );

        main.prewarm = false;

        ParticleSystem.EmissionModule emission =
            nearStars.emission;

        emission.enabled = false;

        ParticleSystem.ShapeModule shape =
            nearStars.shape;

        shape.enabled = false;
    }

    private void InitializeScreenEdgeFlow()
    {
        if (!useScreenEdgeNearStars || nearStars == null)
            return;

        ResolveCamera();

        if (nearStarsCamera == null)
            return;

        ConfigureScreenEdgeFlow();

        nearStars.Clear(true);

        if (!nearStars.isPlaying)
            nearStars.Play(true);

        EnsureParticleBuffer();
        SeedInitialNearStars();

        nearEmissionAccumulator = 0f;
        nearFlowInitialized = true;
    }

    private void SuspendNearStarsFlow()
    {
        if (!useScreenEdgeNearStars ||
            nearStars == null ||
            nearStarsSuspended)
        {
            return;
        }

        nearStarsSuspended = true;
        nearEmissionAccumulator = 0f;

        if (nearStars.isPlaying)
            nearStars.Pause(true);
    }

    private void ResumeNearStarsFlow()
    {
        if (!nearStarsSuspended)
            return;

        nearStarsSuspended = false;
        nearEmissionAccumulator = 0f;

        if (!useScreenEdgeNearStars || nearStars == null)
            return;

        ResolveCamera();
        if (nearStarsCamera == null)
            return;

        if (!nearFlowInitialized)
        {
            InitializeScreenEdgeFlow();
            return;
        }

        // Returning from Alt-Tab/Home can produce a very large first-frame
        // unscaled delta. Re-seeding avoids an edge burst / stacked particles.
        nearStars.Clear(true);
        nearStars.Play(true);
        EnsureParticleBuffer();
        SeedInitialNearStars();
    }

    private void ResolveCamera()
    {
        if (nearStarsCamera == null)
            nearStarsCamera = Camera.main;
    }

    private void EmitNearStars(float deltaTime)
    {
        if (deltaTime <= 0f ||
            currentEmissionRate <= 0f)
        {
            return;
        }

        nearEmissionAccumulator +=
            currentEmissionRate * deltaTime;

        int emitCount =
            Mathf.FloorToInt(nearEmissionAccumulator);

        if (emitCount <= 0)
            return;

        nearEmissionAccumulator -= emitCount;

        int availableSlots = Mathf.Max(
            0,
            nearStars.main.maxParticles -
            nearStars.particleCount
        );

        emitCount =
            Mathf.Min(emitCount, availableSlots);

        emitCount =
            Mathf.Min(emitCount, 32);

        if (emitCount <= 0)
            return;

        StarfieldFlowUtility.Bounds bounds =
            GetCameraBounds();

        Vector2 flow =
            GetNearStarsWorldFlow();

        for (int i = 0; i < emitCount; i++)
        {
            Vector3 worldPosition =
                GetRandomEntryPosition(
                    bounds,
                    flow
                );

            EmitNearStarAtWorldPosition(
                worldPosition
            );
        }
    }

    private void SeedInitialNearStars()
    {
        StarfieldFlowUtility.SeedInitial(nearStars, nearStarsCamera, nearStarsInitialFill);
    }

    private void EmitNearStarAtWorldPosition(
        Vector3 worldPosition
    )
    {
        StarfieldFlowUtility.EmitAtWorldPosition(nearStars, worldPosition);
    }

    private Vector3 GetRandomEntryPosition(
        StarfieldFlowUtility.Bounds bounds,
        Vector2 flow
    )
    {
        return StarfieldFlowUtility.GetRandomEntryPosition(bounds, flow, nearStarsSpawnPadding);
    }

    private void CullExitedNearStars()
    {
        EnsureParticleBuffer();

        int count =
            nearStars.GetParticles(
                particles
            );

        if (count <= 0)
            return;

        StarfieldFlowUtility.Bounds bounds =
            GetCameraBounds();

        Vector2 flow =
            GetNearStarsWorldFlow();

        bool changed = false;

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPosition =
                SimulationToWorldPosition(
                    particles[i].position
                );

            bool exited = false;

            if (flow.x > 0.0001f &&
                worldPosition.x >
                bounds.right +
                nearStarsExitPadding)
            {
                exited = true;
            }
            else if (
                flow.x < -0.0001f &&
                worldPosition.x <
                bounds.left -
                nearStarsExitPadding)
            {
                exited = true;
            }

            if (flow.y < -0.0001f &&
                worldPosition.y <
                bounds.bottom -
                nearStarsExitPadding)
            {
                exited = true;
            }
            else if (
                flow.y > 0.0001f &&
                worldPosition.y >
                bounds.top +
                nearStarsExitPadding)
            {
                exited = true;
            }

            if (!exited)
                continue;

            particles[i].remainingLifetime = 0f;
            changed = true;
        }

        if (changed)
        {
            nearStars.SetParticles(
                particles,
                count
            );
        }
    }

    private StarfieldFlowUtility.Bounds GetCameraBounds()
    {
        return StarfieldFlowUtility.GetCameraBounds(nearStars, nearStarsCamera);
    }

    private Vector2 GetNearStarsWorldFlow()
    {
        ParticleSystem.VelocityOverLifetimeModule velocity =
            nearStars.velocityOverLifetime;

        Vector3 flow =
            new Vector3(
                GetRepresentativeCurveValue(
                    velocity.x
                ),
                GetRepresentativeCurveValue(
                    velocity.y
                ),
                GetRepresentativeCurveValue(
                    velocity.z
                )
            );

        if (velocity.space ==
            ParticleSystemSimulationSpace.Local)
        {
            flow =
                nearStars.transform.TransformVector(
                    flow
                );
        }
        else if (
            velocity.space ==
            ParticleSystemSimulationSpace.Custom)
        {
            ParticleSystem.MainModule main =
                nearStars.main;

            if (main.customSimulationSpace != null)
            {
                flow =
                    main.customSimulationSpace.TransformVector(
                        flow
                    );
            }
        }

        return new Vector2(
            flow.x,
            flow.y
        );
    }

    private Vector3 WorldToSimulationPosition(
        Vector3 worldPosition
    )
    {
        return StarfieldFlowUtility.WorldToSimulationPosition(nearStars, worldPosition);
    }

    private Vector3 SimulationToWorldPosition(
        Vector3 simulationPosition
    )
    {
        return StarfieldFlowUtility.SimulationToWorldPosition(nearStars, simulationPosition);
    }

    private void EnsureParticleBuffer()
    {
        if (nearStars == null)
            return;

        int requiredSize = Mathf.Max(
            1,
            Mathf.Max(
                nearStars.main.maxParticles,
                nearStars.particleCount
            )
        );

        if (particles == null ||
            particles.Length < requiredSize)
        {
            particles =
                new ParticleSystem.Particle[
                    requiredSize
                ];
        }
    }
}
