using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class PlayerInputController : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement;

    [Header("Joystick")]
    public RectTransform joystickBG;
    public RectTransform joystickHandle;

    [Tooltip(
        "Optional larger invisible touch area. " +
        "If empty, Joystick BG is used."
    )]
    public RectTransform joystickTouchArea;

    [Header("Joystick Feel")]
    [Min(1f)]
    public float joystickRange = 120f;

    [Range(0f, 0.3f)]
    public float deadZone = 0.08f;

    [Range(0.5f, 2f)]
    public float inputCurve = 0.9f;

    [Min(0f)]
    public float handleFollowSpeed = 30f;

    [Min(0f)]
    public float handleReturnSpeed = 24f;

    [Min(0f)]
    public float dynamicRangeExtra = 25f;

    [Header("Dynamic Joystick")]
    public bool enableDynamicJoystick = true;

    [Min(0f)]
    public float dynamicCenterFollowSpeed = 18f;

    [Min(0f)]
    public float dynamicMaxCenterOffset = 45f;

    private enum ControlSource
    {
        None,
        Touch,
        Mouse,
        Keyboard
    }

    private ControlSource controlSource = ControlSource.None;

    private bool isPointerActive;
    private int activeTouchId = -1;

    private Vector2 joystickStartLocalPosition;
    private Vector2 joystickCenterLocalPosition;

    private Vector2 rawInput;
    private Vector2 targetHandlePosition;
    private Vector2 visualHandlePosition;

    private Vector2 originalBGAnchoredPosition;
    private Vector2 targetBGAnchoredPosition;

    private RectTransform joystickParent;
    private Camera uiCamera;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        RefreshJoystickBasePosition();
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        RefreshJoystickBasePosition();
    }

    private void OnDisable()
    {
        ForceStopInput();
    }

    private void Update()
    {
        if (playerMovement == null)
            return;

        if (Time.timeScale == 0f ||
            playerMovement.IsGameOver)
        {
            ForceStopInput();
            return;
        }

        rawInput = Vector2.zero;

        HandleTouchInput();
        HandleMouseInput();

        if (!isPointerActive)
        {
            rawInput = GetKeyboardInput();

            if (rawInput.sqrMagnitude > 0.001f)
                controlSource = ControlSource.Keyboard;
            else if (controlSource == ControlSource.Keyboard)
                controlSource = ControlSource.None;
        }

        // Gameplay input is intentionally sent without smoothing.
        // Only the joystick visuals are smoothed below.
        playerMovement.SetMoveInput(rawInput);

        UpdateJoystickVisual();
    }

    public void RefreshJoystickBasePosition()
    {
        if (joystickBG == null)
            return;

        uiCamera = GetUICamera();
        joystickParent = joystickBG.parent as RectTransform;

        originalBGAnchoredPosition =
            joystickBG.anchoredPosition;

        targetBGAnchoredPosition =
            originalBGAnchoredPosition;

        joystickBG.anchoredPosition =
            originalBGAnchoredPosition;

        ResetHandleInstant();
    }

    private Vector2 GetKeyboardInput()
    {
        if (Keyboard.current == null)
            return Vector2.zero;

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            input.y += 1f;

        if (Keyboard.current.sKey.isPressed)
            input.y -= 1f;

        if (Keyboard.current.dKey.isPressed)
            input.x += 1f;

        if (Keyboard.current.aKey.isPressed)
            input.x -= 1f;

        return Vector2.ClampMagnitude(input, 1f);
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null)
            return;

        if (controlSource == ControlSource.Mouse)
            return;

        foreach (var touch in
                 UnityEngine.InputSystem.EnhancedTouch
                     .Touch.activeTouches)
        {
            int touchId = touch.touchId;
            Vector2 touchPosition = touch.screenPosition;

            bool canTakeTouchControl =
                controlSource == ControlSource.None ||
                controlSource == ControlSource.Keyboard;

            if (canTakeTouchControl &&
                touch.phase ==
                UnityEngine.InputSystem.TouchPhase.Began &&
                IsPointerInsideJoystick(touchPosition))
            {
                BeginJoystick(
                    ControlSource.Touch,
                    touchId,
                    touchPosition
                );
            }

            if (controlSource != ControlSource.Touch ||
                touchId != activeTouchId)
            {
                continue;
            }

            if (touch.phase ==
                    UnityEngine.InputSystem.TouchPhase.Began ||
                touch.phase ==
                    UnityEngine.InputSystem.TouchPhase.Moved ||
                touch.phase ==
                    UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                ReadJoystickInput(touchPosition);
            }

            if (touch.phase ==
                    UnityEngine.InputSystem.TouchPhase.Ended ||
                touch.phase ==
                    UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                ReleaseJoystick();
            }

            return;
        }

        if (controlSource == ControlSource.Touch)
            ReleaseJoystick();
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null)
            return;

        if (controlSource == ControlSource.Touch)
            return;

        bool canTakeMouseControl =
            controlSource == ControlSource.None ||
            controlSource == ControlSource.Keyboard;

        if (canTakeMouseControl &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            if (IsPointerInsideJoystick(mousePosition))
            {
                BeginJoystick(
                    ControlSource.Mouse,
                    -1,
                    mousePosition
                );
            }
        }

        if (controlSource == ControlSource.Mouse &&
            Mouse.current.leftButton.isPressed)
        {
            ReadJoystickInput(
                Mouse.current.position.ReadValue()
            );
        }

        if (controlSource == ControlSource.Mouse &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ReleaseJoystick();
        }
    }

    private void BeginJoystick(
        ControlSource source,
        int touchId,
        Vector2 screenPosition
    )
    {
        if (!TryGetPointerLocalPosition(
                screenPosition,
                out Vector2 localPosition))
        {
            return;
        }

        controlSource = source;
        activeTouchId = touchId;
        isPointerActive = true;

        joystickStartLocalPosition = localPosition;
        joystickCenterLocalPosition = localPosition;

        rawInput = Vector2.zero;
        targetHandlePosition = Vector2.zero;
    }

    private void ReadJoystickInput(
        Vector2 currentScreenPosition
    )
    {
        if (!TryGetPointerLocalPosition(
                currentScreenPosition,
                out Vector2 currentLocalPosition))
        {
            return;
        }

        if (enableDynamicJoystick)
        {
            UpdateDynamicJoystickCenter(
                currentLocalPosition
            );
        }

        Vector2 direction =
            currentLocalPosition -
            joystickCenterLocalPosition;

        float currentRange = joystickRange;
        float directionMagnitude = direction.magnitude;

        if (dynamicRangeExtra > 0f &&
            directionMagnitude > joystickRange)
        {
            float maximumRange =
                joystickRange + dynamicRangeExtra;

            float rangeT = Mathf.InverseLerp(
                joystickRange,
                maximumRange,
                directionMagnitude
            );

            currentRange = Mathf.Lerp(
                joystickRange,
                maximumRange,
                rangeT
            );
        }

        Vector2 normalizedInput =
            Vector2.ClampMagnitude(
                direction / currentRange,
                1f
            );

        rawInput =
            ApplyScaledRadialDeadZone(
                normalizedInput
            );

        targetHandlePosition =
            rawInput * joystickRange;
    }

    private void UpdateDynamicJoystickCenter(
        Vector2 currentLocalPosition
    )
    {
        Vector2 centerToFinger =
            currentLocalPosition -
            joystickCenterLocalPosition;

        float distance = centerToFinger.magnitude;

        if (distance <= joystickRange)
            return;

        Vector2 overflowDirection =
            centerToFinger.normalized;

        float overflowDistance =
            distance - joystickRange;

        Vector2 desiredCenterPosition =
            joystickCenterLocalPosition +
            overflowDirection * overflowDistance;

        Vector2 offsetFromStart =
            Vector2.ClampMagnitude(
                desiredCenterPosition -
                joystickStartLocalPosition,
                dynamicMaxCenterOffset
            );

        joystickCenterLocalPosition =
            joystickStartLocalPosition +
            offsetFromStart;
    }

    private Vector2 ApplyScaledRadialDeadZone(
        Vector2 input
    )
    {
        float magnitude = input.magnitude;

        if (magnitude <= deadZone)
            return Vector2.zero;

        float scaledMagnitude =
            Mathf.InverseLerp(
                deadZone,
                1f,
                magnitude
            );

        scaledMagnitude = Mathf.Pow(
            scaledMagnitude,
            inputCurve
        );

        return input.normalized * scaledMagnitude;
    }

    private void UpdateJoystickVisual()
    {
        UpdateJoystickBGVisual();
        UpdateJoystickHandleVisual();
    }

    private void UpdateJoystickBGVisual()
    {
        if (joystickBG == null)
            return;

        if (!isPointerActive ||
            !enableDynamicJoystick)
        {
            targetBGAnchoredPosition =
                originalBGAnchoredPosition;
        }
        else
        {
            Vector2 centerOffset =
                joystickCenterLocalPosition -
                joystickStartLocalPosition;

            targetBGAnchoredPosition =
                originalBGAnchoredPosition +
                centerOffset;
        }

        float damping = GetDampingFactor(
            dynamicCenterFollowSpeed
        );

        joystickBG.anchoredPosition =
            Vector2.Lerp(
                joystickBG.anchoredPosition,
                targetBGAnchoredPosition,
                damping
            );
    }

    private void UpdateJoystickHandleVisual()
    {
        if (joystickHandle == null)
            return;

        Vector2 targetPosition =
            isPointerActive
                ? targetHandlePosition
                : Vector2.zero;

        float followSpeed =
            isPointerActive
                ? handleFollowSpeed
                : handleReturnSpeed;

        float damping = GetDampingFactor(followSpeed);

        visualHandlePosition = Vector2.Lerp(
            visualHandlePosition,
            targetPosition,
            damping
        );

        joystickHandle.anchoredPosition =
            visualHandlePosition;
    }

    private void ReleaseJoystick()
    {
        controlSource = ControlSource.None;
        activeTouchId = -1;
        isPointerActive = false;

        rawInput = Vector2.zero;
        targetHandlePosition = Vector2.zero;

        joystickCenterLocalPosition =
            joystickStartLocalPosition;

        targetBGAnchoredPosition =
            originalBGAnchoredPosition;
    }

    private void ForceStopInput()
    {
        controlSource = ControlSource.None;
        activeTouchId = -1;
        isPointerActive = false;

        rawInput = Vector2.zero;
        targetHandlePosition = Vector2.zero;
        visualHandlePosition = Vector2.zero;

        targetBGAnchoredPosition =
            originalBGAnchoredPosition;

        if (joystickBG != null)
        {
            joystickBG.anchoredPosition =
                originalBGAnchoredPosition;
        }

        if (playerMovement != null)
            playerMovement.SetMoveInput(Vector2.zero);

        ResetHandleInstant();
    }

    private void ResetHandleInstant()
    {
        if (joystickHandle != null)
        {
            joystickHandle.anchoredPosition =
                Vector2.zero;
        }

        visualHandlePosition = Vector2.zero;
        targetHandlePosition = Vector2.zero;
    }

    private bool IsPointerInsideJoystick(
        Vector2 screenPosition
    )
    {
        RectTransform touchArea =
            joystickTouchArea != null
                ? joystickTouchArea
                : joystickBG;

        if (touchArea == null)
            return false;

        return RectTransformUtility
            .RectangleContainsScreenPoint(
                touchArea,
                screenPosition,
                uiCamera
            );
    }

    private bool TryGetPointerLocalPosition(
        Vector2 screenPosition,
        out Vector2 localPosition
    )
    {
        if (joystickParent == null)
        {
            joystickParent =
                joystickBG != null
                    ? joystickBG.parent as RectTransform
                    : null;
        }

        if (joystickParent == null)
        {
            localPosition = Vector2.zero;
            return false;
        }

        return RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                joystickParent,
                screenPosition,
                uiCamera,
                out localPosition
            );
    }

    private float GetDampingFactor(float speed)
    {
        if (speed <= 0f)
            return 1f;

        return 1f - Mathf.Exp(
            -speed * Time.unscaledDeltaTime
        );
    }

    private Camera GetUICamera()
    {
        if (joystickBG == null)
            return null;

        Canvas canvas =
            joystickBG.GetComponentInParent<Canvas>();

        if (canvas == null ||
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    private void OnValidate()
    {
        joystickRange = Mathf.Max(1f, joystickRange);
        dynamicRangeExtra =
            Mathf.Max(0f, dynamicRangeExtra);

        handleFollowSpeed =
            Mathf.Max(0f, handleFollowSpeed);

        handleReturnSpeed =
            Mathf.Max(0f, handleReturnSpeed);

        dynamicCenterFollowSpeed =
            Mathf.Max(0f, dynamicCenterFollowSpeed);

        dynamicMaxCenterOffset =
            Mathf.Max(0f, dynamicMaxCenterOffset);
    }
}