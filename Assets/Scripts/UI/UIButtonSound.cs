using UnityEngine;
using UnityEngine.UI;

public class UIButtonSound : MonoBehaviour
{
    public enum ButtonSoundType
    {
        Menu,
        Back
    }

    [SerializeField] private ButtonSoundType soundType = ButtonSoundType.Menu;

    private Button button;
    private SoundManager soundManager;

    private void Awake()
    {
        button = GetComponent<Button>();
        soundManager = FindFirstObjectByType<SoundManager>();

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