using System.Collections;
using UnityEngine;

public class PlayerArmor : MonoBehaviour
{
    [Header("References")]
    public SoundManager soundManager;
    public SpriteRenderer playerSpriteRenderer;

    [Header("Visual")]
    public GameObject shieldVisual;

    [Header("Break Effect")]
    [Min(0.01f)]
    public float breakScaleDuration = 0.18f;

    [Header("Immune After Break")]
    [Min(0f)]
    public float immuneDuration = 0.8f;

    [Range(0f, 1f)]
    public float immuneMinimumAlpha = 0.3f;

    [Min(0.01f)]
    public float immuneBlinkSpeed = 12f;

    private Vector3 shieldOriginalScale;
    private float playerOriginalAlpha = 1f;

    private Coroutine breakRoutine;
    private Coroutine immuneRoutine;

    public bool IsImmune { get; private set; }
    public bool HasArmor { get; private set; }

    private void Awake()
    {
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponent<SpriteRenderer>();

        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (playerSpriteRenderer != null)
            playerOriginalAlpha = playerSpriteRenderer.color.a;

        if (soundManager == null)
            soundManager = FindAnyObjectByType<SoundManager>();

        if (shieldVisual != null)
        {
            shieldOriginalScale = shieldVisual.transform.localScale;
            shieldVisual.SetActive(false);
        }
    }

    public void ActivateArmor()
    {
        HasArmor = true;

        if (breakRoutine != null)
        {
            StopCoroutine(breakRoutine);
            breakRoutine = null;
        }

        if (shieldVisual != null)
        {
            shieldVisual.transform.localScale = shieldOriginalScale;
            shieldVisual.SetActive(true);
        }
    }

    public void BreakArmor()
    {
        if (!HasArmor)
            return;

        HasArmor = false;

        StatsManager.AddArmorKill();

        if (soundManager != null)
            soundManager.PlayArmorBreakSound();

        StartImmunity();

        if (shieldVisual == null)
            return;

        if (breakRoutine != null)
            StopCoroutine(breakRoutine);

        breakRoutine = StartCoroutine(BreakScaleEffect());
    }

    private void StartImmunity()
    {
        if (immuneRoutine != null)
            StopCoroutine(immuneRoutine);

        immuneRoutine = StartCoroutine(ImmuneRoutine());
    }

    private IEnumerator ImmuneRoutine()
    {
        IsImmune = true;

        if (immuneDuration <= 0f)
        {
            SetPlayerAlpha(playerOriginalAlpha);

            IsImmune = false;
            immuneRoutine = null;

            yield break;
        }

        float timer = 0f;

        while (timer < immuneDuration)
        {
            timer += Time.deltaTime;

            float blink =
                Mathf.PingPong(timer * immuneBlinkSpeed, 1f);

            float alpha =
                Mathf.Lerp(
                    immuneMinimumAlpha,
                    playerOriginalAlpha,
                    blink
                );

            SetPlayerAlpha(alpha);

            yield return null;
        }

        SetPlayerAlpha(playerOriginalAlpha);

        IsImmune = false;
        immuneRoutine = null;
    }

    private IEnumerator BreakScaleEffect()
    {
        if (shieldVisual == null)
        {
            breakRoutine = null;
            yield break;
        }

        Vector3 startScale =
            shieldVisual.transform.localScale;

        float timer = 0f;

        while (timer < breakScaleDuration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(timer / breakScaleDuration);

            t *= t;

            shieldVisual.transform.localScale =
                Vector3.Lerp(
                    startScale,
                    Vector3.zero,
                    t
                );

            yield return null;
        }

        shieldVisual.SetActive(false);
        shieldVisual.transform.localScale = shieldOriginalScale;

        breakRoutine = null;
    }

    private void SetPlayerAlpha(float alpha)
    {
        if (playerSpriteRenderer == null)
            return;

        Color color = playerSpriteRenderer.color;
        color.a = alpha;
        playerSpriteRenderer.color = color;
    }

    private void OnDisable()
    {
        if (immuneRoutine != null)
        {
            StopCoroutine(immuneRoutine);
            immuneRoutine = null;
        }

        if (breakRoutine != null)
        {
            StopCoroutine(breakRoutine);
            breakRoutine = null;
        }

        IsImmune = false;

        SetPlayerAlpha(playerOriginalAlpha);
    }
}