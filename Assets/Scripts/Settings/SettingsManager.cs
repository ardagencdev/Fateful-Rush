using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string SoundKey = "SoundOn";
    private const string MenuMusicKey = "MenuMusicOn";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SFXVolumeKey = "SFXVolume";
    private const string VibrationKey = "VibrationEnabled";
    private const string FPSKey = "FPSMode";
    private const string HUDOpacityKey = "HUDOpacity";
    private const string LanguageKey = "Language";

    private const int DefaultFPS = 60;
    private const string DefaultLanguage = "EN";

    [Header("HUD Opacity")]
    [SerializeField]
    private CanvasGroup hudCanvasGroup;

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
        ApplyVibration();
        ApplyFPS();
        ApplyHUDOpacity();
        ApplyJoystickLayout();
    }

    #region Sound

    public void SetSound(bool value)
    {
        PlayerPrefs.SetInt(
            SoundKey,
            value ? 1 : 0
        );

        PlayerPrefs.Save();

        ApplySound();
        ApplyMenuMusic();
        ApplySFX();
        ApplyAudioAppliers();
    }

    public bool GetSound()
    {
        return PlayerPrefs.GetInt(
            SoundKey,
            1
        ) == 1;
    }

    private void ApplySound()
    {
        AudioListener.volume =
            GetSound() ? 1f : 0f;
    }

    #endregion

    #region Menu Music

    public void SetMenuMusic(bool value)
    {
        PlayerPrefs.SetInt(
            MenuMusicKey,
            value ? 1 : 0
        );

        PlayerPrefs.Save();

        ApplyMenuMusic();
    }

    public bool GetMenuMusic()
    {
        return PlayerPrefs.GetInt(
            MenuMusicKey,
            1
        ) == 1;
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            MusicVolumeKey,
            value
        );

        PlayerPrefs.Save();

        ApplyMenuMusic();
        ApplyAudioAppliers();
    }

    public float GetMusicVolume()
    {
        return Mathf.Clamp01(
            PlayerPrefs.GetFloat(
                MusicVolumeKey,
                1f
            )
        );
    }

    private void ApplyMenuMusic()
    {
        MenuMusicApply menuMusic =
            FindAnyObjectByType<MenuMusicApply>();

        if (menuMusic != null)
            menuMusic.ApplyMusicVolume();
    }

    #endregion

    #region SFX

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            SFXVolumeKey,
            value
        );

        PlayerPrefs.Save();

        ApplySFX();
        ApplyAudioAppliers();
    }

    public float GetSFXVolume()
    {
        return Mathf.Clamp01(
            PlayerPrefs.GetFloat(
                SFXVolumeKey,
                1f
            )
        );
    }

    private void ApplySFX()
    {
        SoundManager soundManager =
            FindAnyObjectByType<SoundManager>();

        if (soundManager != null)
            soundManager.ApplySFXVolume();
    }

    private void ApplyAudioAppliers()
    {
        AudioSettingsApply[] audioAppliers =
    FindObjectsByType<AudioSettingsApply>(
        FindObjectsInactive.Exclude
    );

        foreach (AudioSettingsApply applier in audioAppliers)
        {
            if (applier != null)
                applier.Apply();
        }
    }

    #endregion

    #region Vibration

    public void SetVibration(bool value)
    {
        PlayerPrefs.SetInt(
            VibrationKey,
            value ? 1 : 0
        );

        PlayerPrefs.Save();

        ApplyVibration();
    }

    public bool GetVibration()
    {
        return PlayerPrefs.GetInt(
            VibrationKey,
            1
        ) == 1;
    }

    private void ApplyVibration()
    {
        if (VibrationManager.Instance != null)
        {
            VibrationManager.Instance.SetVibration(
                GetVibration()
            );
        }
    }

    #endregion

    #region Joystick Layout

    public void SetJoystickLeft()
    {
        if (ControlLayoutManager.Instance != null)
        {
            ControlLayoutManager.Instance
                .SetJoystickLeft();
        }
        else
        {
            SaveJoystickSideFallback(
                ControlLayoutManager.JoystickSide.Left
            );
        }
    }

    public void SetJoystickRight()
    {
        if (ControlLayoutManager.Instance != null)
        {
            ControlLayoutManager.Instance
                .SetJoystickRight();
        }
        else
        {
            SaveJoystickSideFallback(
                ControlLayoutManager.JoystickSide.Right
            );
        }
    }

    public int GetJoystickSide()
    {
        return PlayerPrefs.GetInt(
            "JoystickSide",
            (int)ControlLayoutManager
                .JoystickSide.Right
        );
    }

    private void ApplyJoystickLayout()
    {
        if (ControlLayoutManager.Instance != null)
        {
            ControlLayoutManager.Instance
                .ApplySavedLayout();
        }
    }

    private static void SaveJoystickSideFallback(
        ControlLayoutManager.JoystickSide side
    )
    {
        /*
         * Settings sahnesinde ControlLayoutManager yoksa
         * ayarı yine de kaydediyoruz. Gameplay sahnesi
         * açıldığında ControlLayoutManager uygular.
         */
        PlayerPrefs.SetInt(
            "JoystickSide",
            (int)side
        );

        PlayerPrefs.Save();
    }

    #endregion

    #region FPS

    public void SetFPS30()
    {
        SetFPS(30);
    }

    public void SetFPS60()
    {
        SetFPS(60);
    }

    private void SetFPS(int fps)
    {
        int validatedFPS =
            fps == 30 ? 30 : 60;

        PlayerPrefs.SetInt(
            FPSKey,
            validatedFPS
        );

        PlayerPrefs.Save();

        ApplyFPS();
    }

    public int GetFPS()
    {
        int savedFPS = PlayerPrefs.GetInt(
            FPSKey,
            DefaultFPS
        );

        return savedFPS == 30 ? 30 : 60;
    }

    private void ApplyFPS()
    {
        Application.targetFrameRate = GetFPS();
    }

    #endregion

    #region HUD Opacity

    public void SetHUDOpacity(float value)
    {
        value = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            HUDOpacityKey,
            value
        );

        PlayerPrefs.Save();

        ApplyHUDOpacity();
    }

    public float GetHUDOpacity()
    {
        return Mathf.Clamp01(
            PlayerPrefs.GetFloat(
                HUDOpacityKey,
                1f
            )
        );
    }

    private void ApplyHUDOpacity()
    {
        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha =
                GetHUDOpacity();
        }
    }

    #endregion

    #region Language

    public void SetLanguage(string languageCode)
    {
        string validatedLanguage =
            ValidateLanguageCode(languageCode);

        PlayerPrefs.SetString(
            LanguageKey,
            validatedLanguage
        );

        PlayerPrefs.Save();
    }

    public string GetLanguage()
    {
        string savedLanguage =
            PlayerPrefs.GetString(
                LanguageKey,
                DefaultLanguage
            );

        return ValidateLanguageCode(savedLanguage);
    }

    private static string ValidateLanguageCode(
        string languageCode
    )
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return DefaultLanguage;

        string normalizedCode =
            languageCode.Trim().ToUpperInvariant();

        return normalizedCode switch
        {
            "TR" => "TR",
            "RU" => "RU",
            "CN" => "CN",
            _ => DefaultLanguage
        };
    }

    #endregion

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}