using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string SoundKey = "SoundOn";
    private const string MenuMusicKey = "MenuMusicOn";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SFXVolumeKey = "SFXVolume";
    private const string VibrationKey = "VibrationEnabled";
    private const string JoystickSideKey = "JoystickSide"; // 0 = Left, 1 = Right
    private const string FPSKey = "FPSMode"; // 30 / 60
    private const string HUDOpacityKey = "HUDOpacity";
    private const string LanguageKey = "Language"; // EN / TR / RU / CN

    [Header("HUD Layout")]
    public RectTransform joystick;
    public RectTransform dashButton;
    public RectTransform cloneButton;

    [Header("HUD Opacity")]
    public CanvasGroup hudCanvasGroup;

    [Header("Positions")]
    public Vector2 joystickLeftPos = new Vector2(170f, 170f);
    public Vector2 joystickRightPos = new Vector2(-170f, 170f);
    public Vector2 buttonsLeftPos = new Vector2(170f, 150f);
    public Vector2 buttonsRightPos = new Vector2(-170f, 150f);
    public Vector2 cloneOffset = new Vector2(-120f, 0f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ApplyAllSettings();
    }

    public void ApplyAllSettings()
    {
        ApplySound();
        ApplyMenuMusic();
        ApplySFX();
        ApplyFPS();
        ApplyHUDOpacity();
        ApplyJoystickLayout();
    }

    public void SetSound(bool value)
    {
        PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
        PlayerPrefs.Save();

        ApplySound();
        ApplyMenuMusic();
        ApplySFX();
    }

    public bool GetSound()
    {
        return PlayerPrefs.GetInt(SoundKey, 1) == 1;
    }

    public void SetMenuMusic(bool value)
    {
        PlayerPrefs.SetInt(MenuMusicKey, value ? 1 : 0);
        PlayerPrefs.Save();

        ApplyMenuMusic();
    }

    public bool GetMenuMusic()
    {
        return PlayerPrefs.GetInt(MenuMusicKey, 1) == 1;
    }

    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        PlayerPrefs.Save();

        ApplyMenuMusic();
        ApplyAudioAppliers();
    }

    public float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
    }

    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat(SFXVolumeKey, value);
        PlayerPrefs.Save();

        ApplySFX();
        ApplyAudioAppliers();
    }

    public float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat(SFXVolumeKey, 1f);
    }

    public void SetVibration(bool value)
    {
        PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0);
        PlayerPrefs.Save();

        if (VibrationManager.Instance != null)
            VibrationManager.Instance.SetVibration(value);
    }

    public bool GetVibration()
    {
        return PlayerPrefs.GetInt(VibrationKey, 1) == 1;
    }

    public void SetJoystickLeft()
    {
        PlayerPrefs.SetInt(JoystickSideKey, 0);
        PlayerPrefs.Save();

        ApplyJoystickLayout();
    }

    public void SetJoystickRight()
    {
        PlayerPrefs.SetInt(JoystickSideKey, 1);
        PlayerPrefs.Save();

        ApplyJoystickLayout();
    }

    public int GetJoystickSide()
    {
        return PlayerPrefs.GetInt(JoystickSideKey, 1);
    }

    public void SetFPS30()
    {
        PlayerPrefs.SetInt(FPSKey, 30);
        PlayerPrefs.Save();

        ApplyFPS();
    }

    public void SetFPS60()
    {
        PlayerPrefs.SetInt(FPSKey, 60);
        PlayerPrefs.Save();

        ApplyFPS();
    }

    public int GetFPS()
    {
        return PlayerPrefs.GetInt(FPSKey, 60);
    }

    public void SetHUDOpacity(float value)
    {
        PlayerPrefs.SetFloat(HUDOpacityKey, value);
        PlayerPrefs.Save();

        ApplyHUDOpacity();
    }

    public float GetHUDOpacity()
    {
        return PlayerPrefs.GetFloat(HUDOpacityKey, 1f);
    }

    public void SetLanguage(string languageCode)
    {
        PlayerPrefs.SetString(LanguageKey, languageCode);
        PlayerPrefs.Save();
    }

    public string GetLanguage()
    {
        return PlayerPrefs.GetString(LanguageKey, "EN");
    }

    private void ApplySound()
    {
        AudioListener.volume = GetSound() ? 1f : 0f;
    }

    private void ApplyMenuMusic()
    {
        MenuMusicApply menuMusic = FindAnyObjectByType<MenuMusicApply>();

        if (menuMusic != null)
            menuMusic.ApplyMusicVolume();
    }

    private void ApplySFX()
    {
        SoundManager soundManager = FindAnyObjectByType<SoundManager>();

        if (soundManager != null)
            soundManager.ApplySFXVolume();
    }

    private void ApplyAudioAppliers()
    {
       AudioSettingsApply[] audioAppliers = FindObjectsByType<AudioSettingsApply>(FindObjectsInactive.Exclude);

        foreach (AudioSettingsApply applier in audioAppliers)
            applier.Apply();
    }

    private void ApplyFPS()
    {
        Application.targetFrameRate = GetFPS();
    }

    private void ApplyHUDOpacity()
    {
        if (hudCanvasGroup != null)
            hudCanvasGroup.alpha = GetHUDOpacity();
    }

    private void ApplyJoystickLayout()
    {
        int side = GetJoystickSide();

        bool joystickOnLeft = side == 0;

        if (joystick != null)
        {
            joystick.anchorMin = joystickOnLeft ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            joystick.anchorMax = joystickOnLeft ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            joystick.pivot = joystickOnLeft ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            joystick.anchoredPosition = joystickOnLeft ? joystickLeftPos : joystickRightPos;
        }

        Vector2 buttonAnchor = joystickOnLeft ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
        Vector2 buttonPivot = joystickOnLeft ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
        Vector2 buttonPos = joystickOnLeft ? buttonsRightPos : buttonsLeftPos;

        if (dashButton != null)
        {
            dashButton.anchorMin = buttonAnchor;
            dashButton.anchorMax = buttonAnchor;
            dashButton.pivot = buttonPivot;
            dashButton.anchoredPosition = buttonPos;
        }

        if (cloneButton != null)
        {
            cloneButton.anchorMin = buttonAnchor;
            cloneButton.anchorMax = buttonAnchor;
            cloneButton.pivot = buttonPivot;
            cloneButton.anchoredPosition = buttonPos + cloneOffset;
        }
    }
}