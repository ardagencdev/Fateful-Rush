using System.Collections;
using UnityEngine;

public class SpawnScaleEffect : MonoBehaviour
{
    [Header("Collect Particle")]
    public GameObject collectParticlePrefab;

    [Header("Spawn Effect")]
    public float spawnDuration = 0.2f;

    [Header("Collect Effect")]
    public float collectDuration = 0.12f;

    private Vector3 targetScale;
    private bool isCollecting;

    private void Awake()
    {
        targetScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    private void Start()
    {
        StartCoroutine(SpawnEffect());
    }

    private IEnumerator SpawnEffect()
    {
        float duration = Mathf.Max(0.01f, spawnDuration);

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = SmoothStep(Mathf.Clamp01(time / duration));

            transform.localScale =
                Vector3.Lerp(
                    Vector3.zero,
                    targetScale,
                    t
                );

            yield return null;
        }

        transform.localScale = targetScale;
    }

    public void Collect()
    {
        if (isCollecting)
            return;

        isCollecting = true;

        StopAllCoroutines();
        StartCoroutine(CollectEffect());
    }

    private IEnumerator CollectEffect()
    {
        Vector3 startScale = transform.localScale;

        float duration = Mathf.Max(0.01f, collectDuration);

        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / duration);
            t *= t;

            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    Vector3.zero,
                    t
                );

            yield return null;
        }

        transform.localScale = Vector3.zero;

        if (collectParticlePrefab != null)
        {
            Instantiate(
                collectParticlePrefab,
                transform.position,
                Quaternion.identity
            );
        }

        Destroy(
            transform.parent != null
                ? transform.parent.gameObject
                : gameObject
        );
    }

    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
}