using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in ProjectileEnemyFollow.cs.
public partial class ProjectileEnemyFollow
{
    private void HandleAttack(
        Transform currentTarget
    )
    {
        if (isReloading)
            return;

        fireCooldown -= Time.deltaTime;

        if (fireCooldown > 0f)
            return;

        if (!CanSeeTarget(currentTarget))
        {
            fireCooldown = GetBlockedShotRetryDelay();
            return;
        }

        bool isFinalShot =
            shotsFiredInBurst + 1 >= Mathf.Max(1, shotsPerBurst);

        if (!ShootProjectile(currentTarget, isFinalShot))
        {
            fireCooldown = GetBlockedShotRetryDelay();
            return;
        }

        shotsFiredInBurst++;

        if (shotsFiredInBurst >= Mathf.Max(1, shotsPerBurst))
        {
            BeginReload();
            return;
        }

        fireCooldown = GetNextFireInterval();
    }

    private float GetNextFireInterval()
    {
        float jitter = Mathf.Clamp(fireIntervalJitter, 0f, 0.30f);
        float randomMultiplier = Random.Range(1f - jitter, 1f + jitter);

        return Mathf.Max(
            0.05f,
            fireRate * perEnemyFireCadenceMultiplier * randomMultiplier
        );
    }

    private float GetNextReloadDuration()
    {
        float jitter = Mathf.Clamp(reloadDurationJitter, 0f, 0.20f);
        float randomMultiplier = Random.Range(1f - jitter, 1f + jitter);

        return Mathf.Max(
            0.05f,
            reloadDuration * perEnemyReloadCadenceMultiplier * randomMultiplier
        );
    }

    private float GetBlockedShotRetryDelay()
    {
        // Randomized retry prevents two enemies that regain line of sight on
        // the same frame from immediately snapping back into sync.
        return Random.Range(0.12f, 0.28f) * perEnemyFireCadenceMultiplier;
    }

    private bool CanSeeTarget(
        Transform currentTarget
    )
    {
        if (firePoint == null ||
            currentTarget == null)
        {
            return false;
        }

        Vector2 targetPosition =
            GetAimPosition(currentTarget);

        Vector2 direction =
            targetPosition -
            (Vector2)firePoint.position;

        float distance =
            direction.magnitude;

        if (distance <= 0.001f)
            return true;

        RaycastHit2D hit =
            Physics2D.Raycast(
                firePoint.position,
                direction.normalized,
                distance,
                obstacleLayer
            );

        return hit.collider == null;
    }

    private Vector2 GetAimPosition(
        Transform currentTarget
    )
    {
        Vector2 targetPosition =
            currentTarget.position;

        if (!predictiveAimEnabled)
            return targetPosition;

        if (targetRigidbody == null)
            return targetPosition;

        float distance =
            Vector2.Distance(
                firePoint != null
                    ? firePoint.position
                    : transform.position,
                targetPosition
            );

        if (distance <
            predictionDistanceThreshold)
        {
            return targetPosition;
        }

        Vector2 predictionOffset =
            targetRigidbody.linearVelocity *
            predictionTime;

        predictionOffset =
            Vector2.ClampMagnitude(
                predictionOffset,
                maxPredictionDistance
            );

        return targetPosition +
               predictionOffset;
    }

    private bool ShootProjectile(
        Transform currentTarget,
        bool isFinalShot
    )
    {
        if (projectilePrefab == null ||
            firePoint == null ||
            currentTarget == null)
        {
            return false;
        }

        Vector2 aimPosition =
            GetAimPosition(currentTarget);

        Vector2 baseDirection =
            aimPosition -
            (Vector2)firePoint.position;

        if (baseDirection.sqrMagnitude <= 0.001f)
            return false;

        baseDirection.Normalize();

        bool firedAnyProjectile;

        if (isFinalShot)
        {
            int finalProjectileCount = GetFinalShotProjectileCount();
            firedAnyProjectile = FireFinalShotSpread(baseDirection, finalProjectileCount);
        }
        else
        {
            firedAnyProjectile =
                LaunchProjectile(baseDirection);
        }

        if (!firedAnyProjectile)
            return false;

        PlayFireSound();

        float recoilMultiplier =
            isFinalShot
                ? Mathf.Max(1f, finalShotRecoilMultiplier)
                : 1f;

        QueueShotRecoil(
            -baseDirection,
            recoilMultiplier
        );

        return true;
    }

    private int GetFinalShotProjectileCount()
    {
        int threshold = Mathf.Max(1, highDangerBurstThreshold);

        return shotsPerBurst >= threshold
            ? Mathf.Max(1, highDangerFinalShotProjectileCount)
            : Mathf.Max(1, lowDangerFinalShotProjectileCount);
    }

    private bool FireFinalShotSpread(
        Vector2 baseDirection,
        int projectileCount
    )
    {
        projectileCount = Mathf.Max(1, projectileCount);

        if (projectileCount == 1)
            return LaunchProjectile(baseDirection);

        float angleOffset =
            Mathf.Clamp(finalShotAngleOffset, 0f, 20f);

        bool firedAnyProjectile = false;

        if (projectileCount == 2)
        {
            firedAnyProjectile |=
                LaunchProjectile(
                    RotateDirection(baseDirection, -angleOffset)
                );

            firedAnyProjectile |=
                LaunchProjectile(
                    RotateDirection(baseDirection, angleOffset)
                );

            return firedAnyProjectile;
        }

        // Keep the spread symmetric. For 3 projectiles this becomes
        // -angleOffset, 0, +angleOffset. Higher counts are distributed
        // evenly across the same total spread.
        for (int i = 0; i < projectileCount; i++)
        {
            float t =
                projectileCount <= 1
                    ? 0.5f
                    : i / (float)(projectileCount - 1);

            float angle =
                Mathf.Lerp(-angleOffset, angleOffset, t);

            firedAnyProjectile |=
                LaunchProjectile(
                    RotateDirection(baseDirection, angle)
                );
        }

        return firedAnyProjectile;
    }

    private bool LaunchProjectile(Vector2 direction)
    {
        GameObject projectile =
            GetProjectileFromPool(firePoint.position);

        if (projectile == null)
            return false;

        if (direction.sqrMagnitude <= 0.001f)
        {
            ReturnProjectileToPool(projectile);
            return false;
        }

        direction.Normalize();

        EnemyProjectile projectileScript =
            projectile.GetComponent<EnemyProjectile>();

        if (projectileScript != null)
        {
            RegisterActiveProjectile(projectileScript);

            projectileScript.Launch(
                direction,
                projectileSpeed,
                playerMovement
            );
        }
        else
        {
            Rigidbody2D projectileRb =
                projectile.GetComponent<Rigidbody2D>();

            if (projectileRb != null)
            {
                projectileRb.linearVelocity =
                    direction * projectileSpeed;
            }
        }

        return true;
    }

    private static Vector2 RotateDirection(
        Vector2 direction,
        float degrees
    )
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos
        ).normalized;
    }

    private void PlayFireSound()
    {
        if (fireSound == null || audioSource == null)
            return;

        audioSource.volume = SoundManager.SFXVolume;
        audioSource.pitch = SoundManager.GetVariedPitch(
            1f,
            firePitchJitter
        );

        audioSource.PlayOneShot(
            fireSound,
            SoundManager.GetVariedVolumeMultiplier(
                1f,
                fireVolumeJitter
            )
        );
    }

    private void QueueShotRecoil(
        Vector2 recoilDirection,
        float multiplier
    )
    {
        if (shotRecoilDistance <= 0f ||
            recoilDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        float distance =
            shotRecoilDistance * Mathf.Max(1f, multiplier);

        pendingShotRecoil +=
            recoilDirection.normalized * distance;

        pendingShotRecoil = Vector2.ClampMagnitude(
            pendingShotRecoil,
            shotRecoilDistance * 2f
        );
    }

    private bool ApplyPendingShotRecoil()
    {
        if (pendingShotRecoil.sqrMagnitude <= 0.000001f)
            return false;

        Vector2 displacement =
            ClampDisplacementToArena(pendingShotRecoil);
        pendingShotRecoil = Vector2.zero;

        if (displacement.sqrMagnitude <= 0.000001f)
        {
            shotRecoilPauseTimer =
                Mathf.Max(shotRecoilPauseTimer, shotRecoilPause);

            ResetStuckCheck();
            return true;
        }

        EnemyObstacleSteering2D.MoveDisplacementWithPhysicsSlide(
            rb,
            col,
            displacement,
            Time.fixedDeltaTime,
            navigationFilter,
            3
        );

        shotRecoilPauseTimer =
            Mathf.Max(shotRecoilPauseTimer, shotRecoilPause);

        ResetStuckCheck();
        return true;
    }

    private void BeginReload()
    {
        if (isReloading || stopped)
            return;

        isReloading = true;
        activeReloadDuration = GetNextReloadDuration();
        reloadTimer = activeReloadDuration;
        reloadVisualTime = 0f;
        fireCooldown = 0f;

        PlayReloadSound();
        UpdateReloadVisual();
    }

    private void UpdateReloadState()
    {
        if (!isReloading)
            return;

        reloadTimer -= Time.deltaTime;
        reloadVisualTime += Time.deltaTime;
        UpdateReloadVisual();
        UpdateReloadSoundFade();

        if (reloadTimer > 0f)
            return;

        FinishReload();
    }

    private void FinishReload()
    {
        isReloading = false;
        reloadTimer = 0f;
        reloadVisualTime = 0f;
        activeReloadDuration = 0f;
        shotsFiredInBurst = 0;

        RestoreReloadVisuals();
        StopReloadSound();

        float minDelay = Mathf.Max(0f, postReloadFireDelayMin);
        float maxDelay = Mathf.Max(minDelay, postReloadFireDelayMax);

        fireCooldown =
            Random.Range(minDelay, maxDelay) +
            GetNextFireInterval() * Random.Range(0.10f, 0.25f);
    }

    private void PlayReloadSound()
    {
        if (reloadSound == null || reloadAudioSource == null)
            return;

        float minVolume = Mathf.Clamp(
            reloadVolumeMinMultiplier,
            0.5f,
            1f
        );

        float maxVolume = Mathf.Clamp(
            reloadVolumeMaxMultiplier,
            minVolume,
            1f
        );

        float minPitch = Mathf.Clamp(
            reloadPitchMin,
            0.9f,
            1.1f
        );

        float maxPitch = Mathf.Clamp(
            reloadPitchMax,
            minPitch,
            1.1f
        );

        float randomVolumeMultiplier =
            Random.Range(minVolume, maxVolume);

        float randomPitch =
            Random.Range(minPitch, maxPitch);

        activeReloadSfxVolume =
            SoundManager.SFXVolume * randomVolumeMultiplier;

        reloadAudioSource.Stop();
        reloadAudioSource.clip = reloadSound;
        reloadAudioSource.loop = false;
        reloadAudioSource.pitch = randomPitch;
        reloadAudioSource.volume = activeReloadSfxVolume;
        reloadAudioSource.Play();
    }

    private void UpdateReloadSoundFade()
    {
        if (reloadAudioSource == null ||
            !reloadAudioSource.isPlaying)
        {
            return;
        }

        float fadeDuration = Mathf.Max(
            0.01f,
            Mathf.Min(
                reloadSfxFadeOutDuration,
                activeReloadDuration > 0f
                    ? activeReloadDuration
                    : reloadDuration
            )
        );

        if (reloadTimer > fadeDuration)
        {
            reloadAudioSource.volume = activeReloadSfxVolume;
            return;
        }

        float t = Mathf.Clamp01(reloadTimer / fadeDuration);
        t = t * t * (3f - 2f * t);

        reloadAudioSource.volume =
            activeReloadSfxVolume * t;
    }

    private void StopReloadSound()
    {
        if (reloadAudioSource == null)
            return;

        reloadAudioSource.Stop();
        reloadAudioSource.clip = null;
        reloadAudioSource.volume = 1f;
        reloadAudioSource.pitch = 1f;
        activeReloadSfxVolume = 0f;
    }

    private void CacheReloadRenderers()
    {
        reloadRenderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        if (reloadRenderers == null || reloadRenderers.Length == 0)
        {
            reloadRendererBaseAlphas = null;
            return;
        }

        reloadRendererBaseAlphas =
            new float[reloadRenderers.Length];

        for (int i = 0; i < reloadRenderers.Length; i++)
        {
            SpriteRenderer renderer = reloadRenderers[i];

            reloadRendererBaseAlphas[i] =
                renderer != null
                    ? renderer.color.a
                    : 1f;
        }
    }

    private void UpdateReloadVisual()
    {
        if (reloadRenderers == null ||
            reloadRendererBaseAlphas == null)
        {
            return;
        }

        float frequency = Mathf.Max(0.1f, reloadBlinkFrequency);
        float pulse =
            0.5f -
            0.5f * Mathf.Cos(
                reloadVisualTime * frequency * Mathf.PI * 2f
            );

        pulse = Mathf.SmoothStep(0f, 1f, pulse);

        float alphaMultiplier = Mathf.Lerp(
            Mathf.Clamp(reloadBlinkMinAlpha, 0.05f, 1f),
            1f,
            pulse
        );

        for (int i = 0; i < reloadRenderers.Length; i++)
        {
            SpriteRenderer renderer = reloadRenderers[i];

            if (renderer == null)
                continue;

            Color color = renderer.color;
            color.a = reloadRendererBaseAlphas[i] * alphaMultiplier;
            renderer.color = color;
        }
    }

    private void RestoreReloadVisuals()
    {
        if (reloadRenderers == null ||
            reloadRendererBaseAlphas == null)
        {
            return;
        }

        int count = Mathf.Min(
            reloadRenderers.Length,
            reloadRendererBaseAlphas.Length
        );

        for (int i = 0; i < count; i++)
        {
            SpriteRenderer renderer = reloadRenderers[i];

            if (renderer == null)
                continue;

            Color color = renderer.color;
            color.a = reloadRendererBaseAlphas[i];
            renderer.color = color;
        }
    }
}
