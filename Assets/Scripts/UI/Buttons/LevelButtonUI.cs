using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelButtonUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    private const string UnlockedLevelKey = "UnlockedLevel";
    private const string CompletedLevelKeyPrefix = "CompletedLevel_";

    [Header("References")]
    public Button button;
    public Image buttonImage;
    public TMP_Text levelText;

    [Header("Unlocked Sprites")]
    public Sprite unlockedNormalSprite;
    public Sprite unlockedHighlightedSprite;

    [Header("Locked Sprites")]
    public Sprite lockedNormalSprite;
    public Sprite lockedHighlightedSprite;

    [Header("Completed Sprites")]
    public Sprite completedNormalSprite;
    public Sprite completedHighlightedSprite;

    [Header("Text Colors")]
    public Color unlockedTextColor = Color.white;
    public Color completedTextColor =
        new Color32(255, 200, 70, 255);

    [Header("Scale")]
    [Min(0f)]
    public float hoverScale = 1.08f;

    [Min(0f)]
    public float clickScale = 0.95f;

    [Min(0f)]
    public float transitionSpeed = 10f;

    private LevelConfig config;
    private LevelSelectPanel panel;

    private bool unlocked;
    private bool completed;
    private bool hovering;
    private bool pressing;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        RefreshReferences();

        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        if (transitionSpeed <= 0f)
        {
            transform.localScale = targetScale;
            return;
        }

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            transitionSpeed * Time.unscaledDeltaTime
        );
    }

    private void OnDisable()
    {
        hovering = false;
        pressing = false;

        targetScale = originalScale;
        transform.localScale = originalScale;

        ApplyNormalSprite();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayLevel);
    }

    public void Setup(
        LevelConfig levelConfig,
        LevelSelectPanel owner)
    {
        config = levelConfig;
        panel = owner;

        RefreshReferences();

        if (button != null)
        {
            button.onClick.RemoveListener(PlayLevel);
            button.onClick.AddListener(PlayLevel);
        }

        Refresh();
    }

    public void Refresh()
    {
        if (config == null)
        {
            SetInvalidState();
            return;
        }

        int unlockedLevel = PlayerPrefs.GetInt(
            UnlockedLevelKey,
            1
        );

        unlocked =
            config.levelNumber <= unlockedLevel;

        completed = PlayerPrefs.GetInt(
            CompletedLevelKeyPrefix + config.levelNumber,
            0
        ) == 1;

        if (button != null)
            button.interactable = unlocked;

        ApplyCurrentSprite();
        RefreshLevelText();
        RefreshTargetScale();
    }

    private void PlayLevel()
    {
        if (!unlocked || config == null)
            return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance
                .PlayMissionSelectSound();
        }

        if (panel != null)
        {
            panel.StartLevel(config);
        }
        else
        {
            Debug.LogWarning(
                "LevelButtonUI has no LevelSelectPanel reference.",
                this
            );
        }
    }

    private void RefreshLevelText()
    {
        if (levelText == null)
            return;

        if (!unlocked)
        {
            levelText.text = string.Empty;
            levelText.alpha = 0f;
            return;
        }

        levelText.SetText(
            "{0}",
            config.levelNumber
        );

        levelText.alpha = 1f;
        levelText.color = completed
            ? completedTextColor
            : unlockedTextColor;
    }

    private void ApplyCurrentSprite()
    {
        if (hovering)
            ApplyHighlightedSprite();
        else
            ApplyNormalSprite();
    }

    private void ApplyNormalSprite()
    {
        if (buttonImage == null)
            return;

        if (!unlocked)
        {
            buttonImage.sprite = lockedNormalSprite;
            return;
        }

        if (completed &&
            completedNormalSprite != null)
        {
            buttonImage.sprite =
                completedNormalSprite;

            return;
        }

        buttonImage.sprite =
            unlockedNormalSprite;
    }

    private void ApplyHighlightedSprite()
    {
        if (buttonImage == null)
            return;

        if (!unlocked)
        {
            buttonImage.sprite =
                lockedHighlightedSprite != null
                    ? lockedHighlightedSprite
                    : lockedNormalSprite;

            return;
        }

        if (completed)
        {
            buttonImage.sprite =
                completedHighlightedSprite != null
                    ? completedHighlightedSprite
                    : completedNormalSprite != null
                        ? completedNormalSprite
                        : unlockedNormalSprite;

            return;
        }

        buttonImage.sprite =
            unlockedHighlightedSprite != null
                ? unlockedHighlightedSprite
                : unlockedNormalSprite;
    }

    private void RefreshTargetScale()
    {
        if (pressing)
        {
            targetScale =
                originalScale * clickScale;

            return;
        }

        targetScale = hovering
            ? originalScale * hoverScale
            : originalScale;
    }

    private void SetInvalidState()
    {
        unlocked = false;
        completed = false;

        if (button != null)
            button.interactable = false;

        if (levelText != null)
        {
            levelText.text = string.Empty;
            levelText.alpha = 0f;
        }

        ApplyNormalSprite();
        RefreshTargetScale();
    }

    private void RefreshReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (buttonImage == null &&
            button != null)
        {
            buttonImage =
                button.targetGraphic as Image;
        }

        if (levelText == null)
        {
            levelText =
                GetComponentInChildren<TMP_Text>(true);
        }
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        hovering = true;

        ApplyHighlightedSprite();
        RefreshTargetScale();
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        hovering = false;
        pressing = false;

        ApplyNormalSprite();
        RefreshTargetScale();
    }

    public void OnPointerDown(
        PointerEventData eventData)
    {
        if (eventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }

        pressing = true;
        RefreshTargetScale();
    }

    public void OnPointerUp(
        PointerEventData eventData)
    {
        if (eventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }

        pressing = false;
        RefreshTargetScale();
    }

    private void OnValidate()
    {
        hoverScale =
            Mathf.Max(0f, hoverScale);

        clickScale =
            Mathf.Max(0f, clickScale);

        transitionSpeed =
            Mathf.Max(0f, transitionSpeed);
    }
}