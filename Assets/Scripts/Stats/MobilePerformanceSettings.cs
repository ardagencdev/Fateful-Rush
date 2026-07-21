using UnityEngine;

public class MobilePerformanceSettings : MonoBehaviour
{
    [Header("Frame Rate")]
    [Min(30)]
    [SerializeField] private int targetFrameRate = 60;

    [Header("Device Behaviour")]
    [SerializeField] private bool preventScreenSleep = true;
    [SerializeField] private bool runInBackground = false;

    private void Awake()
    {
        ApplySettings();
    }

    private void OnEnable()
    {
        Application.lowMemory += HandleLowMemory;
    }

    private void OnDisable()
    {
        Application.lowMemory -= HandleLowMemory;
    }

    private void ApplySettings()
    {
        /*
         * Mobil platformlarda vSyncCount yok sayılır.
         * PC test build'lerinde targetFrameRate'in
         * kullanılmasını sağlamak için 0 bırakıyoruz.
         */
        QualitySettings.vSyncCount = 0;

        Application.targetFrameRate =
            Mathf.Max(30, targetFrameRate);

        Application.runInBackground =
            runInBackground;

        Screen.sleepTimeout =
            preventScreenSleep
                ? SleepTimeout.NeverSleep
                : SleepTimeout.SystemSetting;
    }

    private void HandleLowMemory()
    {
        /*
         * Unity, Android veya iOS düşük bellek uyarısı
         * gönderdiğinde bu metodu çağırır.
         *
         * Kullanılmayan assetleri temizleme işlemi
         * anlık takılma oluşturabileceğinden yalnızca
         * düşük bellek uyarısında çalıştırıyoruz.
         */
        Resources.UnloadUnusedAssets();

        Debug.LogWarning(
            "Low memory warning received. " +
            "Unused assets are being unloaded."
        );
    }

    private void OnValidate()
    {
        targetFrameRate = Mathf.Max(30, targetFrameRate);

        if (Application.isPlaying)
            ApplySettings();
    }
}