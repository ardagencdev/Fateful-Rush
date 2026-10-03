using System.Collections;
using UnityEngine;

public sealed class EnemyDangerPreviewRuntime : MonoBehaviour
{
    private Texture2D visibilityTexture;
    private Color[] visibilityPixels;
    private Material previewMaterial;

    private Vector2 origin;
    private bool useRadius;
    private float radius;
    private LayerMask coverLayers;

    private float minX;
    private float maxX;
    private float minY;
    private float maxY;

    private float maxRange;
    private int angularSamples;

    private Vector2[] rayDirections;
    private float[] rayMaximumDistances;

    private bool useMobileTimeSlicing;
    private float refreshInterval;
    private float nextVisibilityRefreshTime;

    private float samplesPerSecond;
    private float sampleBudget;
    private int sampleCursor;
    private int maxSamplesPerFrame;

    private float currentProgress;
    private float opacityMultiplier = 1f;
    private float strikeWaveProgress = -1f;
    private float strikeWaveWidth = 0.10f;
    private float strikeWaveBoost = 1.35f;

    private const float TwoPi = Mathf.PI * 2f;
    private const float HitSkin = 0.02f;
    private const float MaxBudgetDeltaTime = 0.05f;

    public void Initialize(
        Material material,
        Texture2D visibility,
        Color[] pixels,
        Vector2 worldOrigin,
        bool localRadiusMode,
        float localRadius,
        LayerMask layers,
        float arenaMinX,
        float arenaMaxX,
        float arenaMinY,
        float arenaMaxY,
        float maximumRange,
        int sampleCount,
        float visibilityRefreshRate)
    {
        previewMaterial = material;
        visibilityTexture = visibility;
        visibilityPixels = pixels;

        origin = worldOrigin;
        useRadius = localRadiusMode;
        radius = Mathf.Max(0.01f, localRadius);
        coverLayers = layers;

        minX = arenaMinX;
        maxX = arenaMaxX;
        minY = arenaMinY;
        maxY = arenaMaxY;

        maxRange = Mathf.Max(0.01f, maximumRange);
        angularSamples = Mathf.Max(64, sampleCount);

        BuildRayCache();

        float safeRate = Mathf.Clamp(
            visibilityRefreshRate,
            1f,
            60f
        );

        useMobileTimeSlicing =
            Application.isMobilePlatform;

        // Mobile keeps the same angular resolution and effective refresh
        // target, but spreads the raycasts across multiple rendered frames
        // instead of executing the whole sweep in one frame.
        if (useMobileTimeSlicing)
            safeRate = Mathf.Min(safeRate, 10f);

        refreshInterval = 1f / safeRate;

        samplesPerSecond =
            angularSamples * safeRate;

        maxSamplesPerFrame =
            useMobileTimeSlicing
                ? Mathf.Clamp(
                    Mathf.CeilToInt(
                        angularSamples * 0.5f
                    ),
                    96,
                    256
                )
                : angularSamples;

        sampleBudget = 0f;
        sampleCursor = 0;

        currentProgress = 0f;
        opacityMultiplier = 1f;
        strikeWaveProgress = -1f;

        if (useMobileTimeSlicing)
        {
            // CreatePreview starts fully transparent (_Progress = 0).
            // The first visibility sweep is therefore built progressively
            // during the first few frames instead of causing a spawn spike.
            nextVisibilityRefreshTime = -1f;
        }
        else
        {
            RefreshVisibilityImmediate();
            nextVisibilityRefreshTime =
                Time.unscaledTime + refreshInterval;
        }

        PushVisualState();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (useMobileTimeSlicing)
            RefreshVisibilityTimeSliced();
        else
            RefreshVisibilityScheduled();
    }

    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        PushVisualState();
    }

    public void SetOpacity(float opacity)
    {
        opacityMultiplier = Mathf.Clamp01(opacity);
        PushVisualState();
    }

    public void SetStrikeWave(
        float progress,
        float width,
        float boost)
    {
        strikeWaveProgress = progress < 0f
            ? -1f
            : Mathf.Clamp01(progress);

        strikeWaveWidth =
            Mathf.Clamp(width, 0.02f, 0.35f);

        strikeWaveBoost =
            Mathf.Max(0f, boost);

        PushVisualState();
    }

    public void ReleaseResources()
    {
        if (visibilityTexture != null)
        {
            Destroy(visibilityTexture);
            visibilityTexture = null;
        }

        visibilityPixels = null;
        rayDirections = null;
        rayMaximumDistances = null;
        previewMaterial = null;
    }

    private void PushVisualState()
    {
        if (previewMaterial == null)
            return;

        previewMaterial.SetFloat(
            "_Progress",
            currentProgress
        );

        previewMaterial.SetFloat(
            "_Opacity",
            opacityMultiplier
        );

        previewMaterial.SetFloat(
            "_StrikeWaveProgress",
            strikeWaveProgress
        );

        previewMaterial.SetFloat(
            "_StrikeWaveWidth",
            strikeWaveWidth
        );

        previewMaterial.SetFloat(
            "_StrikeWaveBoost",
            strikeWaveBoost
        );
    }

    private void BuildRayCache()
    {
        rayDirections =
            new Vector2[angularSamples];

        rayMaximumDistances =
            new float[angularSamples];

        for (int i = 0; i < angularSamples; i++)
        {
            float normalized =
                (i + 0.5f) / angularSamples;

            float angle =
                normalized * TwoPi - Mathf.PI;

            Vector2 direction =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                );

            rayDirections[i] = direction;

            float maximumDistance =
                useRadius
                    ? radius
                    : DistanceToRectEdge(
                        origin,
                        direction,
                        minX,
                        maxX,
                        minY,
                        maxY
                    );

            if (maximumDistance <= 0f ||
                float.IsInfinity(maximumDistance) ||
                float.IsNaN(maximumDistance))
            {
                maximumDistance = 0.01f;
            }

            rayMaximumDistances[i] =
                maximumDistance;
        }
    }

    private void RefreshVisibilityScheduled()
    {
        if (!CanRefreshVisibility())
            return;

        float now = Time.unscaledTime;

        if (now < nextVisibilityRefreshTime)
            return;

        nextVisibilityRefreshTime =
            now + refreshInterval;

        RefreshVisibilityImmediate();
    }

    private void RefreshVisibilityTimeSliced()
    {
        if (!CanRefreshVisibility())
            return;

        float deltaTime =
            Mathf.Min(
                Time.unscaledDeltaTime,
                MaxBudgetDeltaTime
            );

        sampleBudget =
            Mathf.Min(
                sampleBudget +
                samplesPerSecond * deltaTime,
                maxSamplesPerFrame
            );

        int samplesThisFrame =
            Mathf.Min(
                Mathf.FloorToInt(sampleBudget),
                maxSamplesPerFrame
            );

        if (samplesThisFrame <= 0)
            return;

        sampleBudget -= samplesThisFrame;

        for (int i = 0; i < samplesThisFrame; i++)
        {
            RefreshVisibilitySample(
                sampleCursor
            );

            sampleCursor++;

            if (sampleCursor < angularSamples)
                continue;

            UploadVisibilityTexture();
            sampleCursor = 0;
        }
    }

    private void RefreshVisibilityImmediate()
    {
        if (!CanRefreshVisibility())
            return;

        for (int i = 0; i < angularSamples; i++)
            RefreshVisibilitySample(i);

        UploadVisibilityTexture();
        sampleCursor = 0;
        sampleBudget = 0f;
    }

    private bool CanRefreshVisibility()
    {
        return visibilityTexture != null &&
               visibilityPixels != null &&
               previewMaterial != null &&
               rayDirections != null &&
               rayMaximumDistances != null;
    }

    private void RefreshVisibilitySample(
        int sampleIndex)
    {
        Vector2 direction =
            rayDirections[sampleIndex];

        float maximumDistance =
            rayMaximumDistances[sampleIndex];

        RaycastHit2D hit =
            Physics2D.Raycast(
                origin,
                direction,
                maximumDistance,
                coverLayers
            );

        float visibleDistance =
            hit.collider != null
                ? Mathf.Max(
                    0.001f,
                    hit.distance - HitSkin
                )
                : maximumDistance;

        float normalizedDistance =
            Mathf.Clamp01(
                visibleDistance / maxRange
            );

        visibilityPixels[sampleIndex] =
            new Color(
                normalizedDistance,
                0f,
                0f,
                1f
            );
    }

    private void UploadVisibilityTexture()
    {
        visibilityTexture.SetPixels(
            visibilityPixels
        );

        visibilityTexture.Apply(
            false,
            false
        );
    }

    private static float DistanceToRectEdge(
        Vector2 origin,
        Vector2 direction,
        float minX,
        float maxX,
        float minY,
        float maxY)
    {
        float distance =
            float.PositiveInfinity;

        const float epsilon = 0.0001f;

        if (direction.x > epsilon)
        {
            float t =
                (maxX - origin.x) /
                direction.x;

            if (t > 0f)
                distance =
                    Mathf.Min(distance, t);
        }
        else if (direction.x < -epsilon)
        {
            float t =
                (minX - origin.x) /
                direction.x;

            if (t > 0f)
                distance =
                    Mathf.Min(distance, t);
        }

        if (direction.y > epsilon)
        {
            float t =
                (maxY - origin.y) /
                direction.y;

            if (t > 0f)
                distance =
                    Mathf.Min(distance, t);
        }
        else if (direction.y < -epsilon)
        {
            float t =
                (minY - origin.y) /
                direction.y;

            if (t > 0f)
                distance =
                    Mathf.Min(distance, t);
        }

        return distance;
    }
}
