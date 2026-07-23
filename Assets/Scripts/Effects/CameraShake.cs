using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Shake Quality")]
    [SerializeField, Min(1f)]
    private float frequency = 34f;

    [SerializeField, Range(1f, 5f)]
    private float decayPower = 1.35f;

    private Coroutine shakeRoutine;

    private Vector3 originalLocalPosition;

    private float noiseSeedX;
    private float noiseSeedY;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        originalLocalPosition =
            transform.localPosition;

        noiseSeedX =
            Random.Range(0f, 1000f);

        noiseSeedY =
            Random.Range(0f, 1000f);
    }

    public void Shake(
        float duration,
        float strength
    )
    {
        if (duration <= 0f ||
            strength <= 0f ||
            !isActiveAndEnabled)
        {
            return;
        }

        StopCurrentShake();

        shakeRoutine = StartCoroutine(
            ShakeRoutine(
                duration,
                strength
            )
        );
    }

    private IEnumerator ShakeRoutine(
        float duration,
        float strength
    )
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime / duration
                );

            float decay =
                Mathf.Pow(
                    1f - progress,
                    decayPower
                );

            float sampleTime =
                Time.unscaledTime *
                frequency;

            float noiseX =
                Mathf.PerlinNoise(
                    noiseSeedX,
                    sampleTime
                ) * 2f - 1f;

            float noiseY =
                Mathf.PerlinNoise(
                    noiseSeedY,
                    sampleTime
                ) * 2f - 1f;

            Vector3 offset =
                new Vector3(
                    noiseX,
                    noiseY,
                    0f
                ) *
                strength *
                decay;

            transform.localPosition =
                originalLocalPosition +
                offset;

            yield return null;
        }

        transform.localPosition =
            originalLocalPosition;

        shakeRoutine = null;
    }

    private void StopCurrentShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        transform.localPosition =
            originalLocalPosition;
    }

    private void OnDisable()
    {
        StopCurrentShake();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnValidate()
    {
        frequency =
            Mathf.Max(1f, frequency);

        decayPower =
            Mathf.Clamp(
                decayPower,
                1f,
                5f
            );
    }
}