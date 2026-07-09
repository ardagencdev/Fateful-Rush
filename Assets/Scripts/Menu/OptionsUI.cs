using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private UIPanelFadeSwitcher fadeSwitcher;

    [Header("Audio Buttons")]
    [SerializeField] private Button soundOnButton;
    [SerializeField] private Button soundOffButton;
    [SerializeField] private Button menuMusicOnButton;
    [SerializeField] private Button menuMusicOffButton;

    [Header("Audio Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Value Texts")]
    [SerializeField] private TMP_Text musicValueText;
    [SerializeField] private TMP_Text sfxValueText;
    [SerializeField] private TMP_Text hudOpacityValueText;

    [Header("Game Buttons")]
    [SerializeField] private Button vibrationOnButton;
    [SerializeField] private Button vibrationOffButton;
    [SerializeField] private Button fps30Button;
    [SerializeField] private Button fps60Button;
    [SerializeField] private Button joystickLeftButton;
    [SerializeField] private Button joystickRightButton;

    [Header("Gameplay UI")]
    [SerializeField] private Slider hudOpacitySlider;

    private SettingsManager settings;

    private void Awake()
    {
        if (fadeSwitcher == null)
            fadeSwitcher = GetComponent<UIPanelFadeSwitcher>();

        settings = FindAnyObjectByType<SettingsManager>();
    }

    private void Start()
    {
        LoadSettingsToUI();

        if (settings != null)
            settings.ApplyAllSettings();

        Application.targetFrameRate = PlayerPrefs.GetInt("FPSMode", 60);

        if (fadeSwitcher != null)
        {
            fadeSwitcher.SetInstant(mainMenuPanel, true);
            fadeSwitcher.SetInstant(optionsPanel, false);
        }
        else
        {
            SetPanel(mainMenuPanel, true);
            SetPanel(optionsPanel, false);
        }
    }

    public void OpenOptions()
    {
        Switch(mainMenuPanel, optionsPanel);
        LoadSettingsToUI();
    }

    public void CloseOptions()
    {
        Switch(optionsPanel, mainMenuPanel);
    }

    public void SoundOn()
    {
        PlayerPrefs.SetInt("SoundOn", 1);
        PlayerPrefs.Save();

        AudioListener.volume = 1f;

        SoundManager soundManager = FindAnyObjectByType<SoundManager>();
        if (soundManager != null)
            soundManager.ApplySFXVolume();

        RefreshButtonStates();
    }

    public void SoundOff()
    {
        PlayerPrefs.SetInt("SoundOn", 0);
        PlayerPrefs.Save();

        AudioListener.volume = 0f;

        SoundManager soundManager = FindAnyObjectByType<SoundManager>();
        if (soundManager != null)
            soundManager.ApplySFXVolume();

        RefreshButtonStates();
    }

    private void UpdatePercentText(TMP_Text text, float value)
    {
        if (text == null) return;

        text.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    public void MenuMusicOn()
    {
        if (settings != null)
            settings.SetMenuMusic(true);

        RefreshButtonStates();
    }

    public void MenuMusicOff()
    {
        if (settings != null)
            settings.SetMenuMusic(false);

        RefreshButtonStates();
    }

    public void ChangeMusicVolume(float value)
    {
        if (settings != null)
            settings.SetMusicVolume(value);

        UpdatePercentText(musicValueText, value);
    }

    public void ChangeSFXVolume(float value)
    {
        if (settings != null)
            settings.SetSFXVolume(value);

        UpdatePercentText(sfxValueText, value);
    }

    public void VibrationOn()
    {
        PlayerPrefs.SetInt("VibrationEnabled", 1);
        PlayerPrefs.Save();

        VibrationManager vibration = FindAnyObjectByType<VibrationManager>();
        if (vibration != null)
            vibration.SetVibration(true);

        RefreshButtonStates();
    }

    public void VibrationOff()
    {
        PlayerPrefs.SetInt("VibrationEnabled", 0);
        PlayerPrefs.Save();

        VibrationManager vibration = FindAnyObjectByType<VibrationManager>();
        if (vibration != null)
            vibration.SetVibration(false);

        RefreshButtonStates();
    }

    public void SetFPS30()
    {
        PlayerPrefs.SetInt("FPSMode", 30);
        PlayerPrefs.Save();

        Application.targetFrameRate = 30;

        RefreshButtonStates();
    }

    public void SetFPS60()
    {
        PlayerPrefs.SetInt("FPSMode", 60);
        PlayerPrefs.Save();

        Application.targetFrameRate = 60;

        RefreshButtonStates();
    }

    public void SetJoystickLeft()
    {
        ControlLayoutManager layout = FindAnyObjectByType<ControlLayoutManager>();

        if (layout != null)
            layout.SetJoystickLeft();

        RefreshButtonStates();
    }

    public void SetJoystickRight()
    {
        ControlLayoutManager layout = FindAnyObjectByType<ControlLayoutManager>();

        if (layout != null)
            layout.SetJoystickRight();

        RefreshButtonStates();
    }

    public void ChangeHUDOpacity(float value)
    {
        if (settings != null)
            settings.SetHUDOpacity(value);

        UpdatePercentText(hudOpacityValueText, value);
    }

    private void LoadSettingsToUI()
    {
        if (settings == null)
            settings = FindAnyObjectByType<SettingsManager>();

        if (settings != null)
        {
            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(settings.GetMusicVolume());
                UpdatePercentText(musicValueText, musicSlider.value);
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(settings.GetSFXVolume());
                UpdatePercentText(sfxValueText, sfxSlider.value);
            }

            if (hudOpacitySlider != null)
            {
                hudOpacitySlider.SetValueWithoutNotify(settings.GetHUDOpacity());
                UpdatePercentText(hudOpacityValueText, hudOpacitySlider.value);
            }
        }

        RefreshButtonStates();
    }

    private void RefreshButtonStates()
    {
        bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

        SetButtonState(soundOnButton, soundOn);
        SetButtonState(soundOffButton, !soundOn);

        if (settings != null)
        {
            SetButtonState(menuMusicOnButton, settings.GetMenuMusic());
            SetButtonState(menuMusicOffButton, !settings.GetMenuMusic());
        }

        bool vibrationOn = PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;

        SetButtonState(vibrationOnButton, vibrationOn);
        SetButtonState(vibrationOffButton, !vibrationOn);

        int fps = PlayerPrefs.GetInt("FPSMode", 60);

        SetButtonState(fps30Button, fps == 30);
        SetButtonState(fps60Button, fps == 60);

        ControlLayoutManager layout = FindAnyObjectByType<ControlLayoutManager>();

        if (layout != null)
        {
            bool left =
                layout.GetSavedSide() == ControlLayoutManager.JoystickSide.Left;

            SetButtonState(joystickLeftButton, left);
            SetButtonState(joystickRightButton, !left);
        }
    }

    private void SetButtonState(Button button, bool selected)
    {
        if (button == null)
            return;

        CanvasGroup group = button.GetComponent<CanvasGroup>();

        if (group == null)
            group = button.gameObject.AddComponent<CanvasGroup>();

        group.alpha = selected ? 1f : 0.4f;
    }

    private void Switch(GameObject fromPanel, GameObject toPanel)
    {
        if (fadeSwitcher != null)
            fadeSwitcher.SwitchPanel(fromPanel, toPanel);
        else
        {
            SetPanel(fromPanel, false);
            SetPanel(toPanel, true);
        }
    }

    public bool IsOptionsOpen()
    {
        return optionsPanel != null && optionsPanel.activeSelf;
    }

    public bool HandleEscapeBack()
    {
        if (!IsOptionsOpen())
            return false;

        CloseOptions();
        return true;
    }

    private void SetPanel(GameObject panel, bool state)
    {
        if (panel != null)
            panel.SetActive(state);
    }
}