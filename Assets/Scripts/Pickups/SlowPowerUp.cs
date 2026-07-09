using UnityEngine;

public class SlowPowerUp : MonoBehaviour
{
    [Header("Slow Settings")]
    public float slowMultiplier = 0.4f;
    public float slowDuration = 5f;

    public static bool isSlowActive;
    public static float currentSlowMultiplier = 0.4f;
    public static float slowEndTime;

    private Collider2D col;
    private SpawnScaleEffect spawnEffect;
    private SoundManager soundManager;
    private SlowScreenEffect screenEffect;
    private bool collected;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        spawnEffect = GetComponentInChildren<SpawnScaleEffect>();

        soundManager = FindAnyObjectByType<SoundManager>();
        screenEffect = FindAnyObjectByType<SlowScreenEffect>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        collected = true;

        VibrationManager.Instance?.VibrateMedium();
        StatsManager.AddSlowBuffUse();

        if (soundManager != null)
            soundManager.PlaySlowCollectSound();

        isSlowActive = true;
        currentSlowMultiplier = slowMultiplier;
        slowEndTime = Time.unscaledTime + slowDuration;

        if (TimeSlowController.Instance != null)
            TimeSlowController.Instance.StartSlow(slowMultiplier, slowDuration);

        if (screenEffect != null)
            screenEffect.PlayEffect(slowDuration);

        if (col != null)
            col.enabled = false;

        if (spawnEffect != null)
            spawnEffect.Collect();
        else
            Destroy(gameObject);
    }
}