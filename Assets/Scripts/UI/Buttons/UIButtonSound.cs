using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class UIButtonSound : MonoBehaviour
{
    public enum ButtonSoundType
    {
        Menu = 0,
        Back = 1,
        Option = 2,
        Start = 3,
        Locked = 4,
        Next = 5,
        Previous = 6,

        // Eski Inspector seçimlerini bozmamak için değer korunuyor.
        // Equip sesi PlayerSkinPanelUI tarafından yönetiliyor.
        SkinEquip = 7,

        Custom = 8,
        Exit = 9,
        Continue = 10
    }

    [SerializeField]
    private ButtonSoundType soundType = ButtonSoundType.Menu;

    [Header("Custom Sound")]
    [Tooltip("Yalnızca Sound Type = Custom olduğunda kullanılır.")]
    [SerializeField]
    private AudioClip customSound;

    [Header("Anti Spam")]
    [Tooltip("Aynı butonun SFX'inin çok kısa aralıkta tekrar çalmasını engeller. " +
             "Farklı butonların seslerini etkilemez.")]
    [SerializeField, Min(0f)]
    private float minimumRepeatInterval = 0.075f;

    private Button button;
    private MainMenu continueMainMenu;
    private bool automaticPlayback = true;
    private float lastPlayTime = float.NegativeInfinity;

    public void ConfigureAsContinue(MainMenu mainMenu)
    {
        soundType = ButtonSoundType.Continue;
        continueMainMenu = mainMenu;
    }

    public void ConfigureAsPageNavigation(bool manualPlayback = false)
    {
        // Next and Previous page buttons intentionally use the
        // same navigation SFX.
        soundType = ButtonSoundType.Previous;
        SetAutomaticPlayback(!manualPlayback);
    }

    public void SetAutomaticPlayback(bool enabled)
    {
        automaticPlayback = enabled;
        RefreshClickListener();
    }

    public bool PlayConfiguredSound()
    {
        return TryPlayClickSound();
    }

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        RefreshClickListener();
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClickSound);
    }

    private void RefreshClickListener()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button == null)
            return;

        // Always remove first so enable/disable or runtime configuration
        // can never stack the same listener.
        button.onClick.RemoveListener(PlayClickSound);

        if (automaticPlayback && isActiveAndEnabled)
            button.onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        TryPlayClickSound();
    }

    private bool TryPlayClickSound()
    {
        // Equip butonunun kilit/açık sesini PlayerSkinPanelUI yönetiyor.
        // Burada tekrar çalınırsa çift ses oluşur.
        if (soundType == ButtonSoundType.SkinEquip)
            return false;

        float now = Time.unscaledTime;

        if (minimumRepeatInterval > 0f &&
            now - lastPlayTime < minimumRepeatInterval)
        {
            return false;
        }

        SoundManager soundManager = SoundManager.Instance;

        if (soundManager == null)
            soundManager = FindAnyObjectByType<SoundManager>();

        if (soundManager == null)
        {
            Debug.LogWarning("UIButtonSound: Sahnedeki SoundManager bulunamadı.", this);
            return false;
        }

        lastPlayTime = now;

        switch (soundType)
        {
            case ButtonSoundType.Menu:
                soundManager.PlayMenuButtonSound();
                break;

            case ButtonSoundType.Back:
                soundManager.PlayBackButtonSound();
                break;

            case ButtonSoundType.Option:
                soundManager.PlayOptionButtonSound();
                break;

            case ButtonSoundType.Start:
                soundManager.PlayStartButtonSound();
                break;

            case ButtonSoundType.Locked:
                soundManager.PlayLockedLevelSound();
                break;

            case ButtonSoundType.Next:
                soundManager.PlayNextButtonSound();
                break;

            case ButtonSoundType.Previous:
                soundManager.PlayPreviousButtonSound();
                break;

            case ButtonSoundType.Exit:
                soundManager.PlayExitButtonSound();
                break;

            case ButtonSoundType.Continue:
                PlayContinueSound(soundManager);
                break;

            case ButtonSoundType.Custom:
                soundManager.PlayCustomSound(customSound);
                break;
        }

        VibrationManager.Instance?.VibrateUI();
        return true;
    }

    private void PlayContinueSound(SoundManager soundManager)
    {
        if (continueMainMenu == null)
            continueMainMenu = MainMenu.Instance;

        if (continueMainMenu != null &&
            continueMainMenu.IsContinueAvailable)
        {
            soundManager.PlayStartButtonSound();
        }
        else
        {
            soundManager.PlayLockedLevelSound();
        }
    }
}