using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
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

    private Button button;
    private MainMenu continueMainMenu;

    public void ConfigureAsContinue(MainMenu mainMenu)
    {
        soundType = ButtonSoundType.Continue;
        continueMainMenu = mainMenu;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.RemoveListener(PlayClickSound);
        button.onClick.AddListener(PlayClickSound);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        // Equip butonunun kilit/açık sesini PlayerSkinPanelUI yönetiyor.
        // Burada tekrar çalınırsa çift ses oluşur.
        if (soundType == ButtonSoundType.SkinEquip)
            return;

        SoundManager soundManager = SoundManager.Instance;

        if (soundManager == null)
            soundManager = FindAnyObjectByType<SoundManager>();

        if (soundManager == null)
        {
            Debug.LogWarning("UIButtonSound: Sahnedeki SoundManager bulunamadı.", this);
            return;
        }

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