using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour
{
    private const string SoundEnabledKey = "SoundOn";
    private const string VibrationEnabledKey = "VibrationEnabled";
    private const string FPSModeKey = "FPSMode";

    private const int DefaultSoundState = 1;
    private const int DefaultVibrationState = 1;
    private const int DefaultFPS = 60;

    private const float SelectedAlpha = 1f;
    private const float UnselectedAlpha = 0.4f;

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

    private readonly Dictionary<Button, CanvasGroup> buttonCanvasGroups =
        new Dictionary<Button, CanvasGroup>();

    private SettingsManager settings;
    private SoundManager soundManager;
    private VibrationManager vibrationManager;
    private ControlLayoutManager controlLayoutManager;

    private void Awake()
    {
        RefreshReferences();
        CacheButtonCanvasGroups();
    }

    private void Start()
    {
        if (settings != null)
            settings.ApplyAllSettings();

        Application.targetFrameRate = GetSavedFPS();

        LoadSettingsToUI();
        SetInitialPanelState();
    }

    public void OpenOptions()
    {
        RefreshReferences();

        SwitchPanels(mainMenuPanel, optionsPanel);
        LoadSettingsToUI();
    }

    public void CloseOptions()
    {
        SwitchPanels(optionsPanel, mainMenuPanel);
    }

    public void SoundOn()
    {
        SetMasterSound(true);
    }

    public void SoundOff()
    {
        SetMasterSound(false);
    }

    public void MenuMusicOn()
    {
        RefreshReferences();

        if (settings != null)
            settings.SetMenuMusic(true);

        RefreshButtonStates();
    }

    public void MenuMusicOff()
    {
        RefreshReferences();

        if (settings != null)
            settings.SetMenuMusic(false);

        RefreshButtonStates();
    }

    public void ChangeMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);

        RefreshReferences();

        if (settings != null)
            settings.SetMusicVolume(value);

        UpdatePercentText(musicValueText, value);
    }

    public void ChangeSFXVolume(float value)
    {
        value = Mathf.Clamp01(value);

        RefreshReferences();

        if (settings != null)
            settings.SetSFXVolume(value);

        UpdatePercentText(sfxValueText, value);
    }

    public void VibrationOn()
    {
        SetVibration(true);
    }

    public void VibrationOff()
    {
        SetVibration(false);
    }

    public void SetFPS30()
    {
        SetFPS(30);
    }

    public void SetFPS60()
    {
        SetFPS(60);
    }

    public void SetJoystickLeft()
    {
        RefreshReferences();

        if (controlLayoutManager != null)
            controlLayoutManager.SetJoystickLeft();

        RefreshButtonStates();
    }

    public void SetJoystickRight()
    {
        RefreshReferences();

        if (controlLayoutManager != null)
            controlLayoutManager.SetJoystickRight();

        RefreshButtonStates();
    }

    public void ChangeHUDOpacity(float value)
    {
        value = Mathf.Clamp01(value);

        RefreshReferences();

        if (settings != null)
            settings.SetHUDOpacity(value);

        UpdatePercentText(hudOpacityValueText, value);
    }

    public bool IsOptionsOpen()
    {
        return optionsPanel != null &&
               optionsPanel.activeSelf;
    }

    public bool HandleEscapeBack()
    {
        if (!IsOptionsOpen())
            return false;

        CloseOptions();
        return true;
    }

    private void SetMasterSound(bool enabled)
    {
        PlayerPrefs.SetInt(
            SoundEnabledKey,
            enabled ? 1 : 0
        );

        PlayerPrefs.Save();

        AudioListener.volume = enabled ? 1f : 0f;

        RefreshReferences();

        if (soundManager != null)
            soundManager.ApplySFXVolume();

        RefreshButtonStates();
    }

    private void SetVibration(bool enabled)
    {
        PlayerPrefs.SetInt(
            VibrationEnabledKey,
            enabled ? 1 : 0
        );

        PlayerPrefs.Save();

        RefreshReferences();

        if (vibrationManager != null)
            vibrationManager.SetVibration(enabled);

        RefreshButtonStates();
    }

    private void SetFPS(int targetFPS)
    {
        PlayerPrefs.SetInt(FPSModeKey, targetFPS);
        PlayerPrefs.Save();

        Application.targetFrameRate = targetFPS;

        RefreshButtonStates();
    }

    private int GetSavedFPS()
    {
        int savedFPS = PlayerPrefs.GetInt(
            FPSModeKey,
            DefaultFPS
        );

        return savedFPS == 30 ? 30 : 60;
    }

    private void LoadSettingsToUI()
    {
        RefreshReferences();

        if (settings != null)
        {
            SetSliderValue(
                musicSlider,
                musicValueText,
                settings.GetMusicVolume()
            );

            SetSliderValue(
                sfxSlider,
                sfxValueText,
                settings.GetSFXVolume()
            );

            SetSliderValue(
                hudOpacitySlider,
                hudOpacityValueText,
                settings.GetHUDOpacity()
            );
        }

        RefreshButtonStates();
    }

    private void SetSliderValue(
        Slider slider,
        TMP_Text valueText,
        float value)
    {
        value = Mathf.Clamp01(value);

        if (slider != null)
            slider.SetValueWithoutNotify(value);

        UpdatePercentText(valueText, value);
    }

    private void RefreshButtonStates()
    {
        RefreshReferences();

        bool soundEnabled =
            PlayerPrefs.GetInt(
                SoundEnabledKey,
                DefaultSoundState
            ) == 1;

        SetButtonState(
            soundOnButton,
            soundEnabled
        );

        SetButtonState(
            soundOffButton,
            !soundEnabled
        );

        if (settings != null)
        {
            bool menuMusicEnabled =
                settings.GetMenuMusic();

            SetButtonState(
                menuMusicOnButton,
                menuMusicEnabled
            );

            SetButtonState(
                menuMusicOffButton,
                !menuMusicEnabled
            );
        }

        bool vibrationEnabled =
            PlayerPrefs.GetInt(
                VibrationEnabledKey,
                DefaultVibrationState
            ) == 1;

        SetButtonState(
            vibrationOnButton,
            vibrationEnabled
        );

        SetButtonState(
            vibrationOffButton,
            !vibrationEnabled
        );

        int fps = GetSavedFPS();

        SetButtonState(
            fps30Button,
            fps == 30
        );

        SetButtonState(
            fps60Button,
            fps == 60
        );

        if (controlLayoutManager != null)
        {
            bool joystickIsLeft =
                controlLayoutManager.GetSavedSide() ==
                ControlLayoutManager.JoystickSide.Left;

            SetButtonState(
                joystickLeftButton,
                joystickIsLeft
            );

            SetButtonState(
                joystickRightButton,
                !joystickIsLeft
            );
        }
    }

    private void SetButtonState(
        Button button,
        bool selected)
    {
        if (button == null)
            return;

        CanvasGroup canvasGroup =
            GetButtonCanvasGroup(button);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = selected
                ? SelectedAlpha
                : UnselectedAlpha;
        }

        UIButtonEffect buttonEffect =
            button.GetComponent<UIButtonEffect>();

        if (buttonEffect != null)
            buttonEffect.SetSelected(selected);
    }

    private CanvasGroup GetButtonCanvasGroup(
        Button button)
    {
        if (buttonCanvasGroups.TryGetValue(
                button,
                out CanvasGroup cachedGroup))
        {
            return cachedGroup;
        }

        CanvasGroup canvasGroup =
            button.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                button.gameObject.AddComponent<CanvasGroup>();
        }

        buttonCanvasGroups[button] = canvasGroup;

        return canvasGroup;
    }

    private void CacheButtonCanvasGroups()
    {
        CacheButtonCanvasGroup(soundOnButton);
        CacheButtonCanvasGroup(soundOffButton);
        CacheButtonCanvasGroup(menuMusicOnButton);
        CacheButtonCanvasGroup(menuMusicOffButton);
        CacheButtonCanvasGroup(vibrationOnButton);
        CacheButtonCanvasGroup(vibrationOffButton);
        CacheButtonCanvasGroup(fps30Button);
        CacheButtonCanvasGroup(fps60Button);
        CacheButtonCanvasGroup(joystickLeftButton);
        CacheButtonCanvasGroup(joystickRightButton);
    }

    private void CacheButtonCanvasGroup(Button button)
    {
        if (button != null)
            GetButtonCanvasGroup(button);
    }

    private void SetInitialPanelState()
    {
        if (fadeSwitcher != null)
        {
            fadeSwitcher.SetInstant(
                mainMenuPanel,
                true
            );

            fadeSwitcher.SetInstant(
                optionsPanel,
                false
            );

            return;
        }

        SetPanel(mainMenuPanel, true);
        SetPanel(optionsPanel, false);
    }

    private void SwitchPanels(
        GameObject fromPanel,
        GameObject toPanel)
    {
        if (fadeSwitcher != null)
        {
            fadeSwitcher.SwitchPanel(
                fromPanel,
                toPanel
            );

            return;
        }

        SetPanel(fromPanel, false);
        SetPanel(toPanel, true);
    }

    private void RefreshReferences()
    {
        if (fadeSwitcher == null)
        {
            fadeSwitcher =
                GetComponent<UIPanelFadeSwitcher>();
        }

        if (settings == null)
        {
            settings =
                FindAnyObjectByType<SettingsManager>();
        }

        if (soundManager == null)
        {
            soundManager =
                SoundManager.Instance != null
                    ? SoundManager.Instance
                    : FindAnyObjectByType<SoundManager>();
        }

        if (vibrationManager == null)
        {
            vibrationManager =
                FindAnyObjectByType<VibrationManager>();
        }

        if (controlLayoutManager == null)
        {
            controlLayoutManager =
                FindAnyObjectByType<ControlLayoutManager>();
        }
    }

    private static void UpdatePercentText(
        TMP_Text text,
        float value)
    {
        if (text == null)
            return;

        int percentage = Mathf.RoundToInt(
            Mathf.Clamp01(value) * 100f
        );

        text.SetText("{0}%", percentage);
    }

    private static void SetPanel(
        GameObject panel,
        bool state)
    {
        if (panel != null)
            panel.SetActive(state);
    }
}