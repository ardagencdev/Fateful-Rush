using UnityEngine;

public class ObstacleIdleAnimation : MonoBehaviour
{
    [Header("Rotation")]
    public bool rotate = true;
    public float rotateSpeed = 30f;

    [Header("Scale Pulse")]
    public bool pulseScale = false;
    public float pulseAmount = 0.08f;
    public float pulseSpeed = 2f;

    [Header("Color Pulse")]
    public bool pulseColor = false;
    public Color color1 = Color.white;
    public Color color2 = Color.cyan;

    [Header("Shake")]
    public bool shake = false;
    public float shakeAmount = 0.05f;
    public float shakeSpeed = 20f;

    private SpriteRenderer sr;

    private Vector3 startScale;
    private Vector3 startLocalPosition;
    private Color startColor;

    private void Awake()
    {
        startScale = transform.localScale;
        startLocalPosition = transform.localPosition;

        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
            startColor = sr.color;
    }

    private void Update()
    {
        float currentTime = Time.time;

        UpdateRotation();
        UpdateScalePulse(currentTime);
        UpdateColorPulse(currentTime);
        UpdateShake(currentTime);
    }

    private void UpdateRotation()
    {
        if (!rotate)
            return;

        transform.Rotate(
            0f,
            0f,
            rotateSpeed * Time.deltaTime
        );
    }

    private void UpdateScalePulse(float currentTime)
    {
        if (!pulseScale)
            return;

        float scaleMultiplier =
            1f +
            Mathf.Sin(currentTime * pulseSpeed) *
            pulseAmount;

        transform.localScale =
            startScale * scaleMultiplier;
    }

    private void UpdateColorPulse(float currentTime)
    {
        if (sr == null)
            return;

        if (!pulseColor)
        {
            sr.color = startColor;
            return;
        }

        float t =
            (Mathf.Sin(currentTime * pulseSpeed) + 1f) *
            0.5f;

        sr.color = Color.Lerp(color1, color2, t);
    }

    private void UpdateShake(float currentTime)
    {
        if (!shake)
        {
            transform.localPosition = startLocalPosition;
            return;
        }

        float x =
            Mathf.PerlinNoise(
                currentTime * shakeSpeed,
                0f
            ) - 0.5f;

        float y =
            Mathf.PerlinNoise(
                0f,
                currentTime * shakeSpeed
            ) - 0.5f;

        Vector3 offset =
            new Vector3(x, y, 0f) *
            shakeAmount;

        transform.localPosition =
            startLocalPosition + offset;
    }

    private void OnDisable()
    {
        transform.localScale = startScale;
        transform.localPosition = startLocalPosition;

        if (sr != null)
            sr.color = startColor;
    }

    private void OnValidate()
    {
        pulseAmount = Mathf.Max(0f, pulseAmount);
        pulseSpeed = Mathf.Max(0f, pulseSpeed);

        shakeAmount = Mathf.Max(0f, shakeAmount);
        shakeSpeed = Mathf.Max(0f, shakeSpeed);
    }
}