using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
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
    public Color completedTextColor = new Color32(255, 200, 70, 255);

    [Header("Scale")]
    public float hoverScale = 1.08f;
    public float clickScale = 0.95f;
    public float transitionSpeed = 10f;

    private LevelConfig config;
    private LevelSelectPanel panel;

    private bool unlocked;
    private bool completed;
    private bool hovering;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, transitionSpeed * Time.unscaledDeltaTime);
    }

    public void Setup(LevelConfig levelConfig, LevelSelectPanel owner)
    {
        config = levelConfig;
        panel = owner;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(PlayLevel);

        Refresh();
    }

    public void Refresh()
    {
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);

        unlocked = config.levelNumber <= unlockedLevel;
        completed = PlayerPrefs.GetInt("CompletedLevel_" + config.levelNumber, 0) == 1;

        button.interactable = unlocked;

        ApplyNormalSprite();

        levelText.text = unlocked ? config.levelNumber.ToString() : "";
        levelText.alpha = unlocked ? 1f : 0f;

        if (unlocked)
        {
            levelText.color = completed ? completedTextColor : unlockedTextColor;
        }
    }

    private void PlayLevel()
    {
        if (!unlocked) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayMissionSelectSound();

        panel.StartLevel(config);
    }

    private void ApplyNormalSprite()
    {
        if (!unlocked)
        {
            buttonImage.sprite = lockedNormalSprite;
            return;
        }

        if (completed && completedNormalSprite != null)
        {
            buttonImage.sprite = completedNormalSprite;
            return;
        }

        buttonImage.sprite = unlockedNormalSprite;
    }

    private void ApplyHighlightedSprite()
    {
        if (!unlocked)
        {
            buttonImage.sprite = lockedHighlightedSprite != null ? lockedHighlightedSprite : lockedNormalSprite;
            return;
        }

        if (completed)
        {
            buttonImage.sprite = completedHighlightedSprite != null ? completedHighlightedSprite : completedNormalSprite;
            return;
        }

        buttonImage.sprite = unlockedHighlightedSprite != null ? unlockedHighlightedSprite : unlockedNormalSprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        ApplyHighlightedSprite();
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        ApplyNormalSprite();
        targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = hovering ? originalScale * hoverScale : originalScale;
    }
}