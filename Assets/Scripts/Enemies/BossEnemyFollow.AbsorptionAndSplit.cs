using System.Collections;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in BossEnemyFollow.cs.
public partial class BossEnemyFollow
{
    public void BeginAbsorptionOfCurrentStalkers()
    {
        if (absorptionStarted)
            return;

        absorptionStarted = true;
        pendingStalkerAbsorptions = 0;

        EnemyFollow[] stalkers = UnityFindCompat.FindObjectsByType<EnemyFollow>();

        for (int i = 0; i < stalkers.Length; i++)
        {
            EnemyFollow stalker = stalkers[i];

            if (stalker == null ||
                !stalker.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (stalker.BeginBossAbsorption(this))
                pendingStalkerAbsorptions++;
        }

        if (pendingStalkerAbsorptions <= 0)
        {
            // Boss sahneye geldiginde emilecek Stalker yoksa power-up
            // animasyonu/SFX'i oynatma. Boss direkt AOE kullanmaya hazir olsun.
            UnlockAoeImmediatelyWithoutPowerUp();
        }
    }

    private void UnlockAoeImmediatelyWithoutPowerUp()
    {
        if (aoeUnlocked || stopped || isSplitting)
            return;

        DestroyPowerUpPreviewGhost();

        aoeUnlocked = true;

        // Stalker yoksa sadece absorption/power-up asamasini atla.
        // Ilk AOE zamanlamasi normal sekilde firstAoeDelay kullanmaya devam eder.
        aoeCooldownTimer = Mathf.Max(0f, firstAoeDelay);
    }

    public void NotifyStalkerAbsorbed(EnemyFollow stalker)
    {
        if (!absorptionStarted || aoeUnlocked)
            return;

        pendingStalkerAbsorptions =
            Mathf.Max(0, pendingStalkerAbsorptions - 1);

        if (pendingStalkerAbsorptions == 0)
            CompleteAbsorptionPhase();
    }

    private void CompleteAbsorptionPhase()
    {
        if (aoeUnlocked || powerUpRoutine != null)
            return;

        powerUpRoutine = StartCoroutine(
            PowerUpAfterAbsorptionRoutine()
        );
    }

    private IEnumerator PowerUpAfterAbsorptionRoutine()
    {
        float scaleMultiplier =
            Mathf.Max(1f, powerUpScaleMultiplier);

        if (spriteRenderer != null &&
            powerUpPreviewFlashes > 0 &&
            scaleMultiplier > 1f)
        {
            powerUpPreviewGhost =
                CreatePowerUpPreviewGhost(scaleMultiplier);

            if (powerUpPreviewGhost != null)
            {
                for (int i = 0;
                     i < powerUpPreviewFlashes;
                     i++)
                {
                    if (stopped || isSplitting)
                        break;

                    powerUpPreviewGhost.SetActive(true);

                    yield return new WaitForSeconds(
                        Mathf.Max(
                            0.01f,
                            powerUpPreviewOnDuration
                        )
                    );

                    if (powerUpPreviewGhost == null)
                        break;

                    powerUpPreviewGhost.SetActive(false);

                    yield return new WaitForSeconds(
                        Mathf.Max(
                            0.01f,
                            powerUpPreviewOffDuration
                        )
                    );
                }
            }
        }

        DestroyPowerUpPreviewGhost();

        if (stopped || isSplitting)
        {
            powerUpRoutine = null;
            yield break;
        }

        PlayBossSfx(powerUpSfx);

        Vector3 startMagnitude = currentScaleMagnitude;
        Vector3 targetMagnitude =
            startMagnitude * scaleMultiplier;

        float growDuration =
            Mathf.Max(0.01f, powerUpGrowDuration);

        float timer = 0f;

        while (timer < growDuration)
        {
            if (stopped || isSplitting)
            {
                powerUpRoutine = null;
                yield break;
            }

            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / growDuration
            );

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            currentScaleMagnitude = Vector3.Lerp(
                startMagnitude,
                targetMagnitude,
                smoothT
            );

            ApplyCurrentScaleMagnitude();

            yield return null;
        }

        currentScaleMagnitude = targetMagnitude;
        ApplyCurrentScaleMagnitude();

        aoeUnlocked = true;
        aoeCooldownTimer = Mathf.Max(0f, firstAoeDelay);
        powerUpRoutine = null;
    }

    private GameObject CreatePowerUpPreviewGhost(
        float scaleMultiplier)
    {
        if (spriteRenderer == null)
            return null;

        GameObject ghost =
            new GameObject("BossPowerUpScalePreview");

        ghost.transform.SetParent(transform, false);

        if (spriteRenderer.transform == transform)
        {
            ghost.transform.localPosition = Vector3.zero;
            ghost.transform.localRotation = Quaternion.identity;
            ghost.transform.localScale =
                Vector3.one * scaleMultiplier;
        }
        else
        {
            ghost.transform.position =
                spriteRenderer.transform.position;

            ghost.transform.rotation =
                spriteRenderer.transform.rotation;

            Vector3 desiredWorldScale =
                spriteRenderer.transform.lossyScale *
                scaleMultiplier;

            Vector3 parentWorldScale = transform.lossyScale;

            ghost.transform.localScale = new Vector3(
                SafeScaleDivision(
                    desiredWorldScale.x,
                    parentWorldScale.x
                ),
                SafeScaleDivision(
                    desiredWorldScale.y,
                    parentWorldScale.y
                ),
                SafeScaleDivision(
                    desiredWorldScale.z,
                    parentWorldScale.z
                )
            );
        }

        SpriteRenderer ghostRenderer =
            ghost.AddComponent<SpriteRenderer>();

        ghostRenderer.sprite = spriteRenderer.sprite;
        ghostRenderer.sharedMaterial =
            spriteRenderer.sharedMaterial;
        ghostRenderer.flipX = spriteRenderer.flipX;
        ghostRenderer.flipY = spriteRenderer.flipY;
        ghostRenderer.sortingLayerID =
            spriteRenderer.sortingLayerID;
        ghostRenderer.sortingOrder =
            spriteRenderer.sortingOrder + 1;

        Color ghostColor = spriteRenderer.color;
        ghostColor.a *= Mathf.Clamp01(powerUpPreviewAlpha);
        ghostRenderer.color = ghostColor;

        ghost.SetActive(false);
        return ghost;
    }

    private static float SafeScaleDivision(
        float numerator,
        float denominator)
    {
        if (Mathf.Abs(denominator) <= 0.0001f)
            return numerator;

        return numerator / denominator;
    }

    private void DestroyPowerUpPreviewGhost()
    {
        if (powerUpPreviewGhost == null)
            return;

        Destroy(powerUpPreviewGhost);
        powerUpPreviewGhost = null;
    }

    private void ApplyCurrentScaleMagnitude()
    {
        transform.localScale = new Vector3(
            currentScaleMagnitude.x * facingSign,
            currentScaleMagnitude.y * scaleSignY,
            currentScaleMagnitude.z * scaleSignZ
        );
    }

    private IEnumerator SplitRoutine()
    {
        if (isSplitting || stopped)
            yield break;

        isSplitting = true;

        if (aoeRoutine != null)
        {
            StopCoroutine(aoeRoutine);
            aoeRoutine = null;
        }

        if (powerUpRoutine != null)
        {
            StopCoroutine(powerUpRoutine);
            powerUpRoutine = null;
        }

        StopBossSfx();
        DestroyPowerUpPreviewGhost();
        HideDangerPreview();

        if (isChargingAoe)
            RestoreAoeChargeCenter();

        isAoeFadingOut = false;
        isChargingAoe = false;
        aoeChargeProgress = 0f;

        Vector3 splitCenter = transform.position;
        Vector3 splitStartScale = transform.localScale;

        Color splitStartColor =
            spriteRenderer != null
                ? spriteRenderer.color
                : originalColor;

        if (bossCollider != null)
            bossCollider.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        float safeFlashSpeed = Mathf.Max(flashSpeed, 0.01f);

        // Armor Break must finish first. The boss keeps shaking during that
        // time, then the actual split cue plays exactly when the split happens.
        float armorBreakDuration =
            SoundManager.Instance != null
                ? SoundManager.Instance.ArmorBreakSoundDuration
                : 0f;

        float effectiveSplitDelay = Mathf.Max(
            splitDelay,
            armorBreakDuration
        );

        float timer = 0f;

        while (timer < effectiveSplitDelay)
        {
            if (playerMovement != null &&
                playerMovement.IsGameOver)
            {
                StopBoss();
                yield break;
            }

            timer += Time.deltaTime;

            Vector3 shakeOffset = new Vector3(
                Random.Range(-splitShakeAmount, splitShakeAmount),
                Random.Range(-splitShakeAmount, splitShakeAmount),
                0f
            );

            transform.position = splitCenter + shakeOffset;

            if (spriteRenderer != null)
            {
                float flash = Mathf.PingPong(
                    timer / safeFlashSpeed,
                    1f
                );

                spriteRenderer.color = Color.Lerp(
                    splitStartColor,
                    splitFlashColor,
                    flash
                );
            }

            float scaleJitter = Random.Range(
                splitScaleMin,
                splitScaleMax
            );

            transform.localScale = splitStartScale * scaleJitter;
            yield return null;
        }

        transform.position = splitCenter;
        transform.localScale = splitStartScale;

        if (spriteRenderer != null)
            spriteRenderer.color = splitStartColor;

        SoundManager soundManager = SoundManager.Instance;

        PlayBossSfx(
            soundManager != null
                ? soundManager.bossSplitSound
                : null,
            soundManager != null
                ? soundManager.bossSplitVolume
                : 1f
        );

        CameraShake.Instance?.Shake(
            0.22f,
            0.16f
        );

        VibrationManager.Instance?.VibrateBossSplit();

        SpawnMiniBosses(splitCenter);

        if (splitDisappearDuration > 0f)
        {
            yield return SplitDisappearRoutine(
                splitStartScale,
                splitStartColor
            );
        }

        // Split SFX Boss objesiyle birlikte yarida kesilmesin.
        // Pause sirasinda source Pause olur; resume'da kaldigi yerden devam eder.
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        while (bossSfxSource != null &&
               (bossSfxSource.isPlaying ||
                bossSfxPausedByGame))
        {
            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator SplitDisappearRoutine(
        Vector3 startScale,
        Color startColor)
    {
        float timer = 0f;

        while (timer < splitDisappearDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / splitDisappearDuration
            );

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.localScale = Vector3.Lerp(
                startScale,
                Vector3.zero,
                smoothT
            );

            if (spriteRenderer != null)
            {
                Color color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, smoothT);
                spriteRenderer.color = color;
            }

            yield return null;
        }
    }

    private void SpawnMiniBosses(Vector2 bossPosition)
    {
        if (miniBossPrefab == null || player == null)
            return;

        StatsManager.AddBossSplit();

        Vector2 playerDirection =
            (Vector2)player.position - bossPosition;

        if (playerDirection.sqrMagnitude <= 0.001f)
            playerDirection = Vector2.right;
        else
            playerDirection.Normalize();

        Vector2 splitDirection =
            Mathf.Abs(playerDirection.x) > Mathf.Abs(playerDirection.y)
                ? Vector2.up
                : Vector2.right;

        Vector2 firstPosition =
            FindSafeMiniBossPosition(
                bossPosition + splitDirection * splitDistance,
                splitDirection
            );

        Vector2 secondPosition =
            FindSafeMiniBossPosition(
                bossPosition - splitDirection * splitDistance,
                -splitDirection
            );

        CreateMiniBoss(
            firstPosition,
            true,
            NormalEnemyPursuitRole.Pursuer,
            0f
        );

        CreateMiniBoss(
            secondPosition,
            false,
            NormalEnemyPursuitRole.Interceptor,
            miniBossAoeStagger
        );
    }

    private Vector2 FindSafeMiniBossPosition(
        Vector2 desiredPosition,
        Vector2 searchDirection)
    {
        Vector2 clampedPosition =
            ClampToCameraBounds(desiredPosition);

        if (IsMiniBossPositionClear(clampedPosition))
            return clampedPosition;

        const int attempts = 8;

        for (int i = 1; i <= attempts; i++)
        {
            float distance = 0.25f * i;

            Vector2 candidate = ClampToCameraBounds(
                clampedPosition +
                searchDirection.normalized * distance
            );

            if (IsMiniBossPositionClear(candidate))
                return candidate;

            Vector2 sideDirection = new Vector2(
                -searchDirection.y,
                searchDirection.x
            );

            candidate = ClampToCameraBounds(
                clampedPosition + sideDirection * distance
            );

            if (IsMiniBossPositionClear(candidate))
                return candidate;

            candidate = ClampToCameraBounds(
                clampedPosition - sideDirection * distance
            );

            if (IsMiniBossPositionClear(candidate))
                return candidate;
        }

        return clampedPosition;
    }

    private bool IsMiniBossPositionClear(Vector2 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(
            position,
            0.35f,
            solidLayers
        );

        return hit == null;
    }

    private Vector2 ClampToCameraBounds(Vector2 position)
    {
        if (CameraWorldBounds.Instance == null)
            return position;

        float padding = Mathf.Max(
            splitDistance * 0.25f,
            0.7f
        );

        position.x = Mathf.Clamp(
            position.x,
            CameraWorldBounds.Instance.MinX + padding,
            CameraWorldBounds.Instance.MaxX - padding
        );

        position.y = Mathf.Clamp(
            position.y,
            CameraWorldBounds.Instance.MinY + padding,
            CameraWorldBounds.Instance.MaxY - padding
        );

        return position;
    }

    private void CreateMiniBoss(
        Vector2 spawnPosition,
        bool canTargetClone,
        NormalEnemyPursuitRole role,
        float extraAoeDelay)
    {
        GameObject miniBoss = Instantiate(
            miniBossPrefab,
            spawnPosition,
            Quaternion.identity
        );

        MiniBossFollow miniScript =
            miniBoss.GetComponent<MiniBossFollow>();

        if (miniScript == null)
            return;

        miniScript.player = player;
        miniScript.solidLayers = solidLayers;
        miniScript.obstacleLayer = obstacleLayer;
        miniScript.speed = miniBossSpeed;
        miniScript.canTargetClone = canTargetClone;
        miniScript.ConfigurePursuitRole(role);
        miniScript.AddInitialAoeDelay(extraAoeDelay);
    }
}
