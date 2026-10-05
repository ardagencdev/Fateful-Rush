using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class GameResultUI : MonoBehaviour
{
    [Header("Main")]
    [SerializeField] private GameObject resultPanel;

    [Header("UI Groups")]
    [SerializeField] private GameObject winUI;
    [SerializeField] private GameObject loseUI;

    [Header("Win Values")]
    [SerializeField] private TextMeshProUGUI winScoreValue;
    [SerializeField] private TextMeshProUGUI winTimeValue;

    [Header("Lose Values")]
    [SerializeField] private TextMeshProUGUI destroyedByText;
    [SerializeField] private TextMeshProUGUI loseScoreValue;
    [SerializeField] private TextMeshProUGUI loseSurvivedValue;

    [Header("Level Mode")]
    [SerializeField] private GameObject nextLevelButton;
    [SerializeField] private LevelConfig[] levels;
    [SerializeField] private string gameSceneName = "GameScene";

    private const string CreditsSceneName = "CreditsScene";

    [Header("Buttons")]
    [SerializeField] private GameObject tryAgainButton;
    [SerializeField] private GameObject menuButton;

    [Header("Result Buttons Intro")]
    [Tooltip("Result panelinin giriş animasyonu bittikten sonra ilk butonun başlamadan önce bekleyeceği süre.")]
    [SerializeField, Min(0f)] private float resultButtonStartDelay = 0.18f;

    [SerializeField, Min(0.05f)] private float resultButtonAnimationDuration = 0.28f;
    [SerializeField, Min(0f)] private float resultButtonStagger = 0.09f;
    [SerializeField, Min(0f)] private float resultButtonSlideDistance = 30f;
    [SerializeField, Range(0.85f, 1f)] private float resultButtonStartScale = 0.96f;

    [Header("Main Menu Confirmation")]
    [SerializeField] private GameObject menuConfirmationPanel;

    [SerializeField, Min(0.05f)]
    private float menuConfirmationAnimationDuration = 0.18f;

    [SerializeField, Range(0.8f, 1f)]
    private float menuConfirmationStartScale = 0.94f;

    [Header("Skin Unlock Reward")]
    [SerializeField] private PlayerSkinCatalog playerSkinCatalog;
    [SerializeField] private GameObject skinUnlockUI;
    [SerializeField] private TextMeshProUGUI skinUnlockedTitleText;
    [SerializeField] private TextMeshProUGUI unlockedSkinNameText;
    [SerializeField] private CanvasGroup skinUnlockCanvasGroup;
    [SerializeField] private RectTransform skinUnlockRect;

    [Header("Skin Unlock Animation")]
    [SerializeField, Min(0f)] private float skinUnlockDelay = 0.25f;

    [Tooltip("New Skin panelinin Victory SFX bitmeden ne kadar once gelmeye baslayacagi.")]
    [SerializeField, Min(0f)] private float skinUnlockWinSoundTailOverlap = 0.65f;

    [SerializeField, Min(0.05f)] private float skinUnlockAnimationDuration = 0.42f;
    [SerializeField, Min(0f)] private float skinUnlockSlideDistance = 180f;

    [Header("New Best Time Reward")]
    [SerializeField] private GameObject newBestTimeUI;
    [SerializeField] private TextMeshProUGUI newBestTimeText;
    [SerializeField] private CanvasGroup newBestTimeCanvasGroup;
    [SerializeField] private RectTransform newBestTimeRect;

    [Header("Result Edge Glow")]
    [SerializeField] private Image resultEdgeGlow;
    [SerializeField] private Color winEdgeGlowColor = new Color32(70, 255, 120, 255);
    [SerializeField] private Color loseEdgeGlowColor = new Color32(255, 70, 70, 255);
    [SerializeField, Range(0f, 1f)] private float edgeGlowMinAlpha = 0.12f;
    [SerializeField, Range(0f, 1f)] private float edgeGlowMaxAlpha = 0.32f;
    [SerializeField, Min(0.25f)] private float edgeGlowBreathDuration = 2.8f;
    [SerializeField, Min(0f)] private float edgeGlowFadeInDuration = 0.35f;

    [Tooltip("Result intro animasyonu bittikten sonra kenar glow'un başlamadan önce bekleyeceği süre.")]
    [SerializeField, Min(0f)] private float edgeGlowStartDelay = 2f;

    [Header("Result Edge Glow Shape")]
    [SerializeField, Min(0f)] private float edgeGlowCornerRadius = 28f;
    [SerializeField, Min(1f)] private float edgeGlowSoftness = 32f;
    private Material resultEdgeGlowMaterial;
    private Rect resultEdgeGlowLastRect;
    private float resultEdgeGlowLastRadius = -1f, resultEdgeGlowLastSoftness = -1f;
    private static readonly int GlowOpacityId = Shader.PropertyToID("_GlowOpacity");
    private static readonly int GlowBoundsId = Shader.PropertyToID("_GlowBounds");
    private static readonly int GlowRadiusId = Shader.PropertyToID("_GlowCornerRadius");
    private static readonly int GlowSoftnessId = Shader.PropertyToID("_GlowSoftness");

    [Header("Result Intro Animation")]
    [SerializeField, Min(0.05f)] private float resultIntroDuration = 0.22f;
    [SerializeField, Range(0.85f, 1f)] private float resultIntroStartScale = 0.94f;

    private Coroutine skinUnlockRoutine;
    private Vector2 skinUnlockRestPosition;
    private bool skinUnlockPositionCached;

    private Coroutine newBestTimeRoutine;
    private Vector2 newBestRestPosition;
    private bool newBestPositionCached;
    private Coroutine resultEdgeGlowRoutine;

    private Coroutine resultIntroRoutine;
    private CanvasGroup resultPanelCanvasGroup;
    private Vector3 winUIRestScale = Vector3.one;
    private Vector3 loseUIRestScale = Vector3.one;
    private bool resultScalesCached;

    private Coroutine resultButtonsIntroRoutine;

    private sealed class ResultButtonIntroState
    {
        public GameObject gameObject;
        public RectTransform rect;
        public CanvasGroup canvasGroup;
        public Button button;
        public Vector2 restPosition;
        public Vector3 restScale = Vector3.one;
        public bool cached;
    }

    private readonly ResultButtonIntroState nextLevelButtonIntroState =
        new ResultButtonIntroState();

    private readonly ResultButtonIntroState tryAgainButtonIntroState =
        new ResultButtonIntroState();

    private readonly ResultButtonIntroState menuButtonIntroState =
        new ResultButtonIntroState();

    private LevelManager levelManager;

    // Cached result metric layout.
    // Survive Time missions temporarily center the time metric,
    // then the original Inspector positions are restored for every other mode.
    private RectTransform winTimeLabelRect;
    private RectTransform winTimeValueRect;
    private RectTransform loseSurvivedLabelRect;
    private RectTransform loseSurvivedValueRect;

    private TextMeshProUGUI winTimeLabel;
    private TextMeshProUGUI loseSurvivedLabel;
    private string winTimeLabelDefaultText;
    private string loseSurvivedLabelDefaultText;

    private Color winTimeLabelDefaultColor;
    private Color winTimeValueDefaultColor;
    private Color loseSurvivedLabelDefaultColor;
    private Color loseSurvivedValueDefaultColor;

    private static readonly Color SurviveWinColor =
        new Color32(70, 255, 120, 255);

    private static readonly Color SurviveLoseColor =
        new Color32(255, 70, 70, 255);

    private Vector2 winTimeLabelDefaultPosition;
    private Vector2 winTimeValueDefaultPosition;
    private Vector2 loseSurvivedLabelDefaultPosition;
    private Vector2 loseSurvivedValueDefaultPosition;

    private bool metricLayoutCached;
    private bool isSceneChangeRequested;

    private Coroutine menuConfirmationRoutine;
    private CanvasGroup menuConfirmationCanvasGroup;
    private Vector3 menuConfirmationRestScale = Vector3.one;
    private bool menuConfirmationScaleCached;
    private bool menuConfirmationOpenedFromPause;
    private bool pauseConfirmationActivatedResultPanel;

    // Runtime modal layer. Pause and Result UI may live on different Canvases.
    // Re-parenting the dialog to a dedicated top-level overlay Canvas makes
    // both draw order and raycast priority deterministic.
    private GameObject menuConfirmationModalRoot;
    private Canvas menuConfirmationModalCanvas;
    private CanvasScaler menuConfirmationModalScaler;
    private GraphicRaycaster menuConfirmationModalRaycaster;
    private GameObject menuConfirmationInputShield;
    private Transform menuConfirmationOriginalParent;
    private int menuConfirmationOriginalSiblingIndex;
    private bool menuConfirmationDetachedToModal;
    private bool isMenuConfirmationOpen;
    private GameQuit cachedGameQuit;

    public bool IsMenuConfirmationOpen => isMenuConfirmationOpen;

    private Material resultOverlayMaterial;

    private void Awake()
    {
        PrepareResultOverlayMaterial();
        levelManager =
            FindAnyObjectByType<LevelManager>();

        if (resultPanel == null)
        {
            Debug.LogError(
                "[GameResultUI] Result Panel atanmamış.",
                this
            );

            return;
        }

        if (resultPanel == gameObject)
        {
            Debug.LogWarning(
                "[GameResultUI] Result Panel, scriptin bulunduğu GameObject ile aynı. " +
                "Script root objede, Result Panel ise alt objede bulunmalı.",
                this
            );
        }

        CacheMetricLayout();

        PrepareSkinUnlockUI();
        PrepareNewBestTimeUI();
        PrepareResultIntroUI();
        PrepareMenuConfirmationUI();

        HideSkinUnlockImmediate();
        HideNewBestTimeImmediate();

        RefreshLocalizedText();
        LocalizedUILayoutPolish.RequestRefresh();
        HideResultEdgeGlowImmediate();
        HideMenuConfirmationImmediate();
        Hide();
    }

    public void ShowWin(int score, float time)
    {
        ShowWin(
            score,
            time,
            0,
            false
        );
    }

    public void ShowWin(
        int score,
        float time,
        int completedLevelNumber,
        bool isFirstCompletion)
    {
        ShowWin(
            score,
            time,
            completedLevelNumber,
            isFirstCompletion,
            IsCurrentRunBestTime(time)
        );
    }

    public void ShowWin(
        int score,
        float time,
        int completedLevelNumber,
        bool isFirstCompletion,
        bool isNewBestTime)
    {
        ShowPanel();
        SetResultState(true);
        StartResultIntro(true);
        StartResultEdgeGlow(true);

        if (winScoreValue != null)
        {
            winScoreValue.text =
                score.ToString();
        }

        if (winTimeValue != null)
        {
            winTimeValue.text =
                FormatTime(time);
        }

        ApplyMetricVisibility(true);
        UpdateNextLevelButton();
        StartResultButtonsIntro(true);

        UpdateSkinUnlockReward(
            completedLevelNumber,
            isFirstCompletion
        );

        // Caller false gonderse bile GameResultUI mevcut kaydi kontrol eder.
        // Ilk kaydedilebilir tamamlama da doğal olarak yeni best time'dir.
        LevelConfig currentLevel =
            GetCurrentLevel();

        bool firstCompletionIsBestTime =
            SelectedLevelData.IsLevelMode &&
            isFirstCompletion &&
            currentLevel != null &&
            currentLevel.CanSaveBestTime;

        bool shouldShowNewBestTime =
            isNewBestTime ||
            firstCompletionIsBestTime ||
            IsCurrentRunBestTime(time);

        UpdateNewBestTimeReward(
            shouldShowNewBestTime
        );

        RefreshLocalizedText();
        LocalizedUILayoutPolish.RequestRefresh();
    }

    public void ShowLose(int score, float time)
    {
        ShowLose(
            score,
            time,
            LastDeathInfo.Cause
        );
    }

    public void ShowLose(
        int score,
        float time,
        string cause)
    {
        ShowPanel();
        SetResultState(false);
        StartResultIntro(false);
        StartResultEdgeGlow(false);

        if (destroyedByText != null)
        {
            displayedDeathCause = string.IsNullOrWhiteSpace(cause) ? "UNKNOWN" : cause;
            destroyedByText.text = FatefulRushLocalization.DeathCause(displayedDeathCause);
        }

        if (loseScoreValue != null)
        {
            loseScoreValue.text =
                score.ToString();
        }

        if (loseSurvivedValue != null)
        {
            loseSurvivedValue.text =
                FormatTime(time);
        }

        ApplyMetricVisibility(false);

        if (nextLevelButton != null)
        {
            nextLevelButton.SetActive(false);
        }

        StartResultButtonsIntro(false);

        HideSkinUnlockImmediate();
        HideNewBestTimeImmediate();

        RefreshLocalizedText();
        LocalizedUILayoutPolish.RequestRefresh();
    }

    private void SetResultState(bool won)
    {
        if (winUI != null)
            winUI.SetActive(won);

        if (loseUI != null)
            loseUI.SetActive(!won);
    }

    public void Hide()
    {
        StopResultIntro();
        StopResultButtonsIntro();
        RestoreResultButtonsState();

        HideSkinUnlockImmediate();
        HideNewBestTimeImmediate();

        RefreshLocalizedText();
        LocalizedUILayoutPolish.RequestRefresh();
        HideResultEdgeGlowImmediate();
        HideMenuConfirmationImmediate();

        RestoreResultIntroState();

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void ShowPanel()
    {
        if (resultPanel == null)
            return;

        isSceneChangeRequested = false;
        menuConfirmationOpenedFromPause = false;
        pauseConfirmationActivatedResultPanel = false;
        HideMenuConfirmationImmediate();
        SetSceneButtonsInteractable(true);

        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling();

        CanvasGroup canvasGroup =
            resultPanel.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (tryAgainButton != null)
        {
            tryAgainButton.SetActive(true);
        }

        if (menuButton != null)
        {
            menuButton.SetActive(true);
        }
    }

    private void OnValidate()
    {
        menuConfirmationAnimationDuration =
            Mathf.Max(
                0.05f,
                menuConfirmationAnimationDuration
            );

        menuConfirmationStartScale =
            Mathf.Clamp(
                menuConfirmationStartScale,
                0.8f,
                1f
            );

        skinUnlockDelay = Mathf.Max(0f, skinUnlockDelay);
        skinUnlockWinSoundTailOverlap =
            Mathf.Max(0f, skinUnlockWinSoundTailOverlap);
        skinUnlockAnimationDuration =
            Mathf.Max(0.05f, skinUnlockAnimationDuration);
        skinUnlockSlideDistance =
            Mathf.Max(0f, skinUnlockSlideDistance);

        edgeGlowMinAlpha = Mathf.Clamp01(edgeGlowMinAlpha);
        edgeGlowMaxAlpha = Mathf.Clamp(edgeGlowMaxAlpha, edgeGlowMinAlpha, 1f);
        edgeGlowBreathDuration = Mathf.Max(0.25f, edgeGlowBreathDuration);
        edgeGlowFadeInDuration = Mathf.Max(0f, edgeGlowFadeInDuration);
        edgeGlowStartDelay = Mathf.Max(0f, edgeGlowStartDelay);

        resultButtonStartDelay = Mathf.Max(0f, resultButtonStartDelay);
        resultButtonAnimationDuration =
            Mathf.Max(0.05f, resultButtonAnimationDuration);
        resultButtonStagger = Mathf.Max(0f, resultButtonStagger);
        resultButtonSlideDistance = Mathf.Max(0f, resultButtonSlideDistance);
        resultButtonStartScale =
            Mathf.Clamp(resultButtonStartScale, 0.85f, 1f);

        resultIntroDuration =
            Mathf.Max(0.05f, resultIntroDuration);

        resultIntroStartScale =
            Mathf.Clamp(
                resultIntroStartScale,
                0.85f,
                1f
            );
    }

    private void OnDisable()
    {
        StopResultIntro();
        StopResultButtonsIntro();
        RestoreResultButtonsState();

        HideSkinUnlockImmediate();
        HideNewBestTimeImmediate();

        RefreshLocalizedText();
        LocalizedUILayoutPolish.RequestRefresh();
        HideResultEdgeGlowImmediate();
        HideMenuConfirmationImmediate();

        RestoreResultIntroState();

        if (menuConfirmationModalRoot != null)
        {
            Destroy(menuConfirmationModalRoot);
            menuConfirmationModalRoot = null;
            menuConfirmationModalCanvas = null;
            menuConfirmationModalScaler = null;
            menuConfirmationModalRaycaster = null;
            menuConfirmationInputShield = null;
        }
    }

    private void OnDestroy()
    {
        if (resultEdgeGlowMaterial != null) Destroy(resultEdgeGlowMaterial);
        if (resultOverlayMaterial != null) Destroy(resultOverlayMaterial);
    }

    private void PrepareResultOverlayMaterial()
    {
        if (resultPanel == null) return;
        Image[] images = resultPanel.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            // Respect custom art/materials; optimize only the existing solid fill.
            if (image.name != "DarkOverlay" || image.sprite != null || image.overrideSprite != null
                || image.material != image.defaultMaterial) continue;
            Shader shader = Resources.Load<Shader>("ResultEdgeGlow/ResultOverlay");
            if (shader == null || !shader.isSupported) return;
            if (resultOverlayMaterial == null)
                resultOverlayMaterial = new Material(shader) { name = "Result solid overlay" };
            image.material = resultOverlayMaterial;
        }
    }

    private string rewardSkinId;
    private string rewardSkinFallback;
    private string displayedDeathCause;

}
