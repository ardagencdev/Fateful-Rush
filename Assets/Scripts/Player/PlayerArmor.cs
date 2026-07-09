using UnityEngine;
using System.Collections;

public class PlayerArmor : MonoBehaviour
{
    [Header("References")]
    public SoundManager soundManager;

    [Header("Visual")]
    public GameObject shieldVisual;

    [Header("Break Effect")]
    public float breakScaleDuration = 0.18f;

    [Header("Immune After Break")]
    public float immuneDuration = 0.8f;

    private SpriteRenderer sr;
    private Vector3 shieldOriginalScale;
    private Coroutine breakRoutine;
    private Coroutine immuneRoutine;

    public bool IsImmune { get; private set; }
    public bool HasArmor { get; private set; }

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();

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
        if (!HasArmor) return;

        HasArmor = false;

        StatsManager.AddArmorKill();

        if (soundManager != null)
            soundManager.PlayArmorBreakSound();

        if (immuneRoutine != null)
            StopCoroutine(immuneRoutine);

        immuneRoutine = StartCoroutine(ImmuneRoutine());

        if (shieldVisual == null) return;

        if (breakRoutine != null)
            StopCoroutine(breakRoutine);

        breakRoutine = StartCoroutine(BreakScaleEffect());
    }

    private IEnumerator ImmuneRoutine()
    {
        IsImmune = true;

        float timer = 0f;

        while (timer < immuneDuration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.PingPong(timer * 12f, 0.5f) + 0.3f;
            SetPlayerAlpha(alpha);

            yield return null;
        }

        SetPlayerAlpha(0.65f);

        IsImmune = false;
        immuneRoutine = null;
    }

    private IEnumerator BreakScaleEffect()
    {
        Vector3 startScale = shieldVisual.transform.localScale;

        float time = 0f;

        while (time < breakScaleDuration)
        {
            time += Time.deltaTime;

            float t = time / breakScaleDuration;
            t *= t;

            shieldVisual.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }

        shieldVisual.SetActive(false);
        shieldVisual.transform.localScale = shieldOriginalScale;
        breakRoutine = null;
    }

    private void SetPlayerAlpha(float alpha)
    {
        if (sr == null) return;

        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}