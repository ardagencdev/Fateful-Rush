using System.Collections;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in BossEnemyFollow.cs.
public partial class BossEnemyFollow
{
    private IEnumerator AoeStrikeRoutine()
    {
        if (isChargingAoe ||
            stopped ||
            isSplitting)
        {
            yield break;
        }

        isChargingAoe = true;
        GameAudioMixerController.SetBossDanger(this, true);
        aoeChargeProgress = 0f;
        aoeChargeCenter = rb != null
            ? rb.position
            : (Vector2)transform.position;

        ResetStuckCheck();
        routeCommitTimer = 0f;
        ZeroVelocity();

        // Boss 3 saniyelik charge boyunca sabit kalir.
        // Inspector'da eski bir deger kalmis olsa bile 3 saniyenin altina dusmez.
        float duration = Mathf.Max(3f, aoeChargeDuration);
        float timer = 0f;

        // Danger alanini charge basinda alpha 0 ile olustur.
        ShowGlobalDangerPreview();
        UpdateGlobalDangerPreviewAlpha(0f);

        SoundManager soundManager = SoundManager.Instance;

        PlayBossSfx(
            soundManager != null
                ? soundManager.bossAoeWarningSound
                : null,
            soundManager != null
                ? soundManager.bossAoeWarningVolume
                : 1f,
            duration
        );

        while (timer < duration)
        {
            if (stopped ||
                isSplitting ||
                (playerMovement != null && playerMovement.IsGameOver))
            {
                CancelAoeCharge();
                yield break;
            }

            timer += Time.deltaTime;

            aoeChargeProgress = Mathf.Clamp01(
                timer / duration
            );

            // Daha profesyonel gorunmesi icin alpha lineer patlamak yerine
            // yumusak bir 0 -> full gecis yapar.
            float visualProgress = Mathf.SmoothStep(
                0f,
                1f,
                aoeChargeProgress
            );

            UpdateGlobalDangerPreviewAlpha(visualProgress);

            yield return null;
        }

        aoeChargeProgress = 1f;
        UpdateGlobalDangerPreviewAlpha(1f);

        RestoreAoeChargeCenter();

        // HASAR TAM BU ANDA UYGULANIR.
        ExecuteGlobalAoeStrike();

        // Strike anini oyuncuya net gostermek icin Boss merkezinden
        // ekran disina dogru hizli bir shockwave halkasi yayilir.
        // Boss shockwave + fade tamamen bitene kadar hareket etmez.
        isAoeFadingOut = true;

        float strikeWaveDuration =
            Mathf.Max(0.05f, dangerStrikeWaveDuration);

        float strikeWaveTimer = 0f;

        UpdateGlobalDangerStrikeWave(0f);

        while (strikeWaveTimer < strikeWaveDuration)
        {
            if (stopped ||
                isSplitting ||
                (playerMovement != null && playerMovement.IsGameOver))
            {
                CancelAoeCharge();
                yield break;
            }

            RestoreAoeChargeCenter();

            strikeWaveTimer += Time.deltaTime;

            float strikeWaveProgress =
                Mathf.Clamp01(
                    strikeWaveTimer / strikeWaveDuration
                );

            UpdateGlobalDangerStrikeWave(
                Mathf.SmoothStep(
                    0f,
                    1f,
                    strikeWaveProgress
                )
            );

            yield return null;
        }

        // Shockwave ekran ucuna ulastiginda kapatilir,
        // ardindan mevcut red danger alan smooth fade-out yapar.
        DisableGlobalDangerStrikeWave();

        float fadeDuration =
            Mathf.Max(0.05f, dangerPreviewFadeOutDuration);

        float fadeTimer = 0f;

        while (fadeTimer < fadeDuration)
        {
            if (stopped ||
                isSplitting ||
                (playerMovement != null && playerMovement.IsGameOver))
            {
                CancelAoeCharge();
                yield break;
            }

            RestoreAoeChargeCenter();

            fadeTimer += Time.deltaTime;

            float fadeProgress = Mathf.Clamp01(
                fadeTimer / fadeDuration
            );

            float opacity =
                1f - Mathf.SmoothStep(
                    0f,
                    1f,
                    fadeProgress
                );

            UpdateGlobalDangerPreviewOpacity(opacity);

            yield return null;
        }

        UpdateGlobalDangerPreviewOpacity(0f);
        HideDangerPreview();

        isAoeFadingOut = false;
        isChargingAoe = false;
        GameAudioMixerController.SetBossDanger(this, false);
        aoeChargeProgress = 0f;
        aoeCooldownTimer = Mathf.Max(0f, aoeCooldown);
        aoeRoutine = null;

        lastPosition = rb != null
            ? rb.position
            : (Vector2)transform.position;
    }

    private void ApplyAoeChargeShake()
    {
        if (rb == null)
            return;

        float shake = Mathf.Lerp(
            Mathf.Max(0f, normalShakeAmount),
            Mathf.Max(normalShakeAmount, aoeMaxShakeAmount),
            Mathf.SmoothStep(0f, 1f, aoeChargeProgress)
        );

        Vector2 offset = Random.insideUnitCircle * shake;
        rb.MovePosition(aoeChargeCenter + offset);
    }

    private void RestoreAoeChargeCenter()
    {
        if (rb != null)
        {
            rb.position = aoeChargeCenter;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        else
        {
            transform.position = aoeChargeCenter;
        }
    }

    private void ExecuteGlobalAoeStrike()
    {
        if (player == null || playerMovement == null)
            return;

        PlayBossSfx(aoeSfx);

        // The camera hit happens exactly on the shockwave/strike frame.
        CameraShake.Instance?.Shake(
            0.26f,
            0.20f
        );

        VibrationManager.Instance?.VibrateBossAoe();

        EnemyAreaStrikeUtility.ExecuteStrike(
            transform,
            player,
            playerMovement,
            playerArmor,
            GetAoeCoverLayers(),
            false,
            0f,
            "BOSS"
        );
    }

    private void ShowGlobalDangerPreview()
    {
        HideDangerPreview();

        Vector2 origin = rb != null
            ? aoeChargeCenter
            : (Vector2)transform.position;

        float bossVisualRadius = 0.35f;

        if (bossCollider != null)
        {
            Bounds bounds = bossCollider.bounds;

            bossVisualRadius = Mathf.Max(
                bounds.extents.x,
                bounds.extents.y
            );
        }

        // Shake sirasinda bile kirmizi mesh Boss sprite/collider ustune binmesin.
        float innerRadius =
            bossVisualRadius +
            Mathf.Max(0f, dangerPreviewInnerPadding) +
            Mathf.Max(0f, aoeMaxShakeAmount);

        dangerPreviewObject =
            EnemyDangerPreviewMesh.CreatePreview(
                origin,
                false,
                0f,
                GetAoeCoverLayers(),
                dangerPreviewColor,
                dangerPreviewRayCount,
                dangerPreviewSortingOrder,
                innerRadius,
                dangerPreviewRadialSegments,
                dangerPreviewEdgeSmoothingPasses,
                dangerPreviewInnerAlphaMultiplier,
                dangerPreviewWaveFrontWidth,
                dangerPreviewWaveFrontBoost,
                dangerPreviewInnerBrightness,
                dangerPreviewShader,
                dangerPreviewVisibilityRefreshRate,
                dangerPreviewCoverFeather
            );
    }

    private void UpdateGlobalDangerPreviewAlpha(
        float normalizedAlpha)
    {
        EnemyDangerPreviewMesh.SetPreviewAlpha(
            dangerPreviewObject,
            dangerPreviewColor,
            normalizedAlpha
        );
    }

    private void UpdateGlobalDangerPreviewOpacity(
        float normalizedOpacity)
    {
        EnemyDangerPreviewMesh.SetPreviewOpacity(
            dangerPreviewObject,
            normalizedOpacity
        );
    }

    private void UpdateGlobalDangerStrikeWave(
        float normalizedProgress)
    {
        EnemyDangerPreviewMesh.SetStrikeWave(
            dangerPreviewObject,
            normalizedProgress,
            dangerStrikeWaveWidth,
            dangerStrikeWaveBoost
        );
    }

    private void DisableGlobalDangerStrikeWave()
    {
        EnemyDangerPreviewMesh.SetStrikeWave(
            dangerPreviewObject,
            -1f,
            dangerStrikeWaveWidth,
            dangerStrikeWaveBoost
        );
    }

    private void HideDangerPreview()
    {
        EnemyDangerPreviewMesh.DestroyPreview(
            ref dangerPreviewObject
        );
    }

    private LayerMask GetAoeCoverLayers()
    {
        return aoeCoverLayers.value != 0
            ? aoeCoverLayers
            : obstacleLayer;
    }

    private void CancelAoeCharge()
    {
        HideDangerPreview();
        RestoreAoeChargeCenter();
        isAoeFadingOut = false;
        isChargingAoe = false;
        GameAudioMixerController.SetBossDanger(this, false);
        aoeChargeProgress = 0f;
        aoeRoutine = null;
    }
}
