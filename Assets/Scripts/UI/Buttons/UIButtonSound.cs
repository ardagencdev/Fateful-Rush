using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    public enum ButtonSoundType
    {
        Menu,
        Back,
        Option
    }

    [SerializeField]
    private ButtonSoundType soundType = ButtonSoundType.Menu;

    private Button button;

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
        SoundManager soundManager = SoundManager.Instance;

        if (soundManager == null)
            soundManager = FindAnyObjectByType<SoundManager>();

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
}