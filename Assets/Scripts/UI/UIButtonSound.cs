using UnityEngine;
using UnityEngine.UI;

public class UIButtonSound : MonoBehaviour
{
    public enum ButtonSoundType
    {
        Menu,
        Back,
        Option
    }

    [SerializeField] private ButtonSoundType soundType = ButtonSoundType.Menu;

    private Button button;
    private SoundManager soundManager;

    private void Awake()
    {
        button = GetComponent<Button>();
        soundManager = FindAnyObjectByType<SoundManager>();

        if (button != null)
            button.onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        if (soundManager != null)
        {
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
            }
        }

        VibrationManager.Instance?.VibrateLight();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClickSound);
    }
}