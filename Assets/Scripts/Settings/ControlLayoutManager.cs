using System.Collections;
using UnityEngine;

public class ControlLayoutManager : MonoBehaviour
{
    public enum JoystickSide
    {
        Left = 0,
        Right = 1
    }

    private const string JoystickSideKey = "JoystickSide";

    public static ControlLayoutManager Instance { get; private set; }

    [Header("Default")]
    [SerializeField]
    private JoystickSide defaultSide = JoystickSide.Right;

    [Header("References")]
    [SerializeField]
    private PlayerInputController playerInputController;

    [Header("HUD References")]
    [SerializeField]
    private RectTransform joystick;

    [SerializeField]
    private RectTransform dashButton;

    [SerializeField]
    private RectTransform cloneButton;

    [SerializeField]
    private RectTransform pauseButton;

    [Header("Button Positions")]
    [SerializeField]
    private Vector2 dashLeftPos =
        new Vector2(145f, 135f);

    [SerializeField]
    private Vector2 dashRightPos =
        new Vector2(-145f, 135f);

    [SerializeField]
    private Vector2 cloneLeftPos =
        new Vector2(145f, 255f);

    [SerializeField]
    private Vector2 cloneRightPos =
        new Vector2(-145f, 255f);

    [Header("Pause Button Positions")]
    [SerializeField]
    private Vector2 pauseLeftPos =
        new Vector2(20f, -180f);

    [SerializeField]
    private Vector2 pauseRightPos =
        new Vector2(-20f, -180f);

    private Coroutine refreshRoutine;

    public JoystickSide CurrentSide { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (playerInputController == null)
        {
            playerInputController =
                FindAnyObjectByType<PlayerInputController>();
        }

        ApplySavedLayout();
    }

    public void SetJoystickLeft()
    {
        SaveAndApply(JoystickSide.Left);
    }

    public void SetJoystickRight()
    {
        SaveAndApply(JoystickSide.Right);
    }

    private void SaveAndApply(JoystickSide side)
    {
        PlayerPrefs.SetInt(
            JoystickSideKey,
            (int)side
        );

        PlayerPrefs.Save();

        ApplyLayout(side);
    }

    public void ApplySavedLayout()
    {
        ApplyLayout(GetSavedSide());
    }

    public JoystickSide GetSavedSide()
    {
        int savedValue = PlayerPrefs.GetInt(
            JoystickSideKey,
            (int)defaultSide
        );

        return savedValue == (int)JoystickSide.Left
            ? JoystickSide.Left
            : JoystickSide.Right;
    }

    private void ApplyLayout(JoystickSide side)
    {
        CurrentSide = side;

        ResolvePlayerInputController();

        if (playerInputController != null)
        {
            playerInputController
                .PrepareForJoystickLayoutChange();
        }

        // The selected side now represents the HUD/control side directly:
        // Left = skill buttons + pause on the left, Right = on the right.
        bool hudOnLeft =
            side == JoystickSide.Left;

        PrepareFloatingJoystickRect();

        ApplyButton(
            dashButton,
            hudOnLeft,
            dashLeftPos,
            dashRightPos
        );

        ApplyButton(
            cloneButton,
            hudOnLeft,
            cloneLeftPos,
            cloneRightPos
        );

        ResolvePauseButton();
        ApplyPauseButton(hudOnLeft);

        RefreshPlayerInputLayout();
    }

    private void PrepareFloatingJoystickRect()
    {
        if (joystick == null)
            return;

        // Floating joystick is positioned from the touch point at runtime.
        // Keeping its anchor and pivot centered makes the pressed point the
        // visual center of the joystick on every screen size/aspect ratio.
        Vector2 center = new Vector2(0.5f, 0.5f);

        SetRect(
            joystick,
            center,
            center,
            Vector2.zero
        );
    }

    private void ApplyButton(
        RectTransform button,
        bool left,
        Vector2 leftPosition,
        Vector2 rightPosition
    )
    {
        if (button == null)
            return;

        Vector2 anchor =
            left
                ? Vector2.zero
                : new Vector2(1f, 0f);

        Vector2 position =
            left
                ? leftPosition
                : rightPosition;

        SetRect(
            button,
            anchor,
            anchor,
            position
        );
    }

    private void ApplyPauseButton(bool left)
    {
        if (pauseButton == null)
            return;

        Vector2 anchor = left
            ? new Vector2(0f, 1f)
            : Vector2.one;

        Vector2 position = left
            ? pauseLeftPos
            : pauseRightPos;

        SetRect(
            pauseButton,
            anchor,
            anchor,
            position
        );
    }

    private void ResolvePauseButton()
    {
        if (pauseButton != null)
            return;

        GameStateManager gameStateManager =
            FindAnyObjectByType<GameStateManager>();

        if (gameStateManager == null ||
            gameStateManager.pauseButtonHUD == null)
        {
            return;
        }

        pauseButton =
            gameStateManager.pauseButtonHUD.transform
                as RectTransform;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 anchoredPosition
    )
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
    }

    private void RefreshPlayerInputLayout()
    {
        if (refreshRoutine != null)
            StopCoroutine(refreshRoutine);

        Canvas.ForceUpdateCanvases();
        ResolvePlayerInputController();

        if (playerInputController != null)
        {
            playerInputController
                .RefreshJoystickBasePosition();
        }

        refreshRoutine =
            StartCoroutine(
                RefreshPlayerInputNextFrame()
            );
    }

    private IEnumerator RefreshPlayerInputNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();
        ResolvePlayerInputController();

        if (playerInputController != null)
        {
            playerInputController
                .RefreshJoystickBasePosition();
        }

        refreshRoutine = null;
    }

    private void ResolvePlayerInputController()
    {
        if (playerInputController != null)
            return;

        playerInputController =
            FindAnyObjectByType<PlayerInputController>();
    }

    [ContextMenu("Reset Joystick Layout Save")]
    private void ResetJoystickLayoutSave()
    {
        PlayerPrefs.DeleteKey(JoystickSideKey);
        PlayerPrefs.Save();

        ApplySavedLayout();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}