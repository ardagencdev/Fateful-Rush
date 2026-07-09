using UnityEngine;

public class ArmorPowerUp : MonoBehaviour
{
    private Collider2D col;
    private SoundManager soundManager;
    private SpawnScaleEffect spawnEffect;
    private bool collected;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        soundManager = FindAnyObjectByType<SoundManager>();
        spawnEffect = GetComponentInChildren<SpawnScaleEffect>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        collected = true;

        VibrationManager.Instance?.VibrateMedium();
        StatsManager.AddArmorBuffUse();

        if (soundManager != null)
            soundManager.PlayArmorCollectSound();

        PlayerArmor armor = other.GetComponent<PlayerArmor>();

        if (armor != null)
            armor.ActivateArmor();

        if (col != null)
            col.enabled = false;

        if (spawnEffect != null)
            spawnEffect.Collect();
        else
            Destroy(gameObject);
    }
}