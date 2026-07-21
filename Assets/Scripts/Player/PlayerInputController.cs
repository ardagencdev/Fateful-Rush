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

    [Header("Joystick Settings")]
    [Min(1f)]
    public float joystickRange = 120f;

    [Range(0f, 0.3f)]
    public float deadZone = 0.08f;

    [Range(0.5f, 2f)]
    public float inputCurve = 0.85f;

    [Min(0f)]
    public float inputSmoothTime = 0.035f;

    [Min(0f)]
    public float handleReturnSpeed = 22f;

    [Min(0f)]
    public float handleFollowSpeed = 28f;

    [Header("Advanced Joystick Feel")]
    [Range(0.7f, 1.6f)]
    public float finalInputCurve = 1.1f;

    [Range(0f, 0.35f)]
    public float predictionStrength = 0.12f;

    [Range(0f, 0.08f)]
    public float inputBufferTime = 0.035f;

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

    private bool isTouching;
    private int activeTouchId = -1;

    private Vector2 joystickStartPos;
    private Vector2 joystickCenterScreenPos;

    private Vector2 rawInput;
    private Vector2 processedInput;
    private Vector2 smoothInput;
    private Vector2 inputSmoothVelocity;

    private Vector2 previousRawInput;
    private Vector2 bufferedInput;
    private float lastInputTime;

    private Vector2 targetHandlePosition;
    private Vector2 visualHandlePosition;

    private Vector2 originalBGAnchoredPosition;
    private Vector2 targetBGAnchoredPosition;

    private Camera uiCamera;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        uiCamera = GetUICamera();

        RefreshJoystickBasePosition();
        ResetHandleInstant();
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

        if (Time.timeScale == 0f || playerMovement.IsGameOver)
        {
            ForceStopInput();
            return;
        }

        rawInput = Vector2.zero;

        HandleTouchInput();
        HandleMouseInput();

        if (!isTouching)
        {
            rawInput = GetKeyboardInput();

            if (rawInput.sqrMagnitude > 0.001f)
                controlSource = ControlSource.Keyboard;
            else if (controlSource == ControlSource.Keyboard)
                controlSource = ControlSource.None;
        }

        processedInput = ProcessFinalInput(rawInput);

        smoothInput = Vector2.SmoothDamp(
            smoothInput,
            processedInput,
            ref inputSmoothVelocity,
            inputSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );

        playerMovement.SetMoveInput(smoothInput);

        UpdateJoystickVisual();

        previousRawInput = rawInput;
    }

    public void RefreshJoystickBasePosition()
    {
        if (joystickBG == null)
            return;

        uiCamera = GetUICamera();

        originalBGAnchoredPosition = joystickBG.anchoredPosition;
        targetBGAnchoredPosition = originalBGAnchoredPosition;
        joystickBG.anchoredPosition = originalBGAnchoredPosition;

        ResetHandleInstant();
    }

    private Vector2 GetKeyboardInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current == null)
            return input;

        if (Keyboard.current.wKey.isPressed)
            input.y += 1f;

        if (Keyboard.current.sKey.isPressed)
            input.y -= 1f;

        if (Keyboard.current.dKey.isPressed)
            input.x += 1f;

        if (Keyboard.current.aKey.isPressed)
            input.x -= 1f;

        return input.sqrMagnitude > 1f
            ? input.normalized
            : input;
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null)
            return;

        if (controlSource == ControlSource.Mouse)
            return;

        foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
        {
            int touchId = touch.touchId;
            Vector2 touchPos = touch.screenPosition;

            if (controlSource == ControlSource.None &&
                touch.phase == UnityEngine.InputSystem.TouchPhase.Began &&
                IsPointerInsideJoystick(touchPos))
            {
                BeginJoystick(
                    ControlSource.Touch,
                    touchId,
                    touchPos
                );
            }

            if (controlSource != ControlSource.Touch ||
                touchId != activeTouchId)
            {
                continue;
            }

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary ||
                touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                ReadJoystickInput(touchPos);
            }

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
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

        if (controlSource == ControlSource.None &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos =
                Mouse.current.position.ReadValue();

            if (IsPointerInsideJoystick(mousePos))
            {
                BeginJoystick(
                    ControlSource.Mouse,
                    -1,
                    mousePos
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
        Vector2 screenPos
    )
    {
        controlSource = source;
        activeTouchId = touchId;
        isTouching = true;

        joystickStartPos = screenPos;
        joystickCenterScreenPos = screenPos;

        previousRawInput = Vector2.zero;
        bufferedInput = Vector2.zero;
        lastInputTime = Time.unscaledTime;

        RefreshJoystickBasePosition();
    }

    private void ReadJoystickInput(
        Vector2 currentScreenPos
    )
    {
        if (enableDynamicJoystick)
            UpdateDynamicJoystickCenter(currentScreenPos);

        Vector2 direction =
            currentScreenPos - joystickCenterScreenPos;

        float currentRange = joystickRange;

        if (dynamicRangeExtra > 0f &&
            direction.magnitude > joystickRange)
        {
            float maxRange =
                joystickRange + dynamicRangeExtra;

            float rangeT = Mathf.InverseLerp(
                joystickRange,
                maxRange,
                direction.magnitude
            );

            currentRange = Mathf.Lerp(
                joystickRange,
                maxRange,
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
        Vector2 currentScreenPos
    )
    {
        Vector2 fromCenterToFinger =
            currentScreenPos -
            joystickCenterScreenPos;

        float distance =
            fromCenterToFinger.magnitude;

        if (distance <= joystickRange)
            return;

        Vector2 overflowDirection =
            fromCenterToFinger.normalized;

        float overflowDistance =
            distance - joystickRange;

        Vector2 desiredCenterScreenPos =
            joystickCenterScreenPos +
            overflowDirection * overflowDistance;

        Vector2 maxOffsetFromStart =
            Vector2.ClampMagnitude(
                desiredCenterScreenPos -
                joystickStartPos,
                dynamicMaxCenterOffset
            );

        joystickCenterScreenPos =
            joystickStartPos +
            maxOffsetFromStart;
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

        scaledMagnitude =
            Mathf.Pow(
                scaledMagnitude,
                inputCurve
            );

        return input.normalized *
               scaledMagnitude;
    }

    private Vector2 ProcessFinalInput(
        Vector2 input
    )
    {
        Vector2 result = input;

        if (isTouching)
        {
            result =
                ApplyInputPrediction(result);

            result =
                ApplyInputBuffer(result);
        }

        result =
            ApplyFinalInputCurve(result);

        return Vector2.ClampMagnitude(
            result,
            1f
        );
    }

    private Vector2 ApplyInputPrediction(
        Vector2 input
    )
    {
        if (predictionStrength <= 0f)
            return input;

        Vector2 inputDelta =
            input - previousRawInput;

        Vector2 predictedInput =
            input +
            inputDelta * predictionStrength;

        return Vector2.ClampMagnitude(
            predictedInput,
            1f
        );
    }

    private Vector2 ApplyInputBuffer(
        Vector2 input
    )
    {
        if (inputBufferTime <= 0f)
            return input;

        if (input.sqrMagnitude > 0.001f)
        {
            bufferedInput = input;
            lastInputTime = Time.unscaledTime;

            return input;
        }

        if (isTouching &&
            Time.unscaledTime - lastInputTime <=
            inputBufferTime)
        {
            return bufferedInput;
        }

        return Vector2.zero;
    }

    private Vector2 ApplyFinalInputCurve(
        Vector2 input
    )
    {
        float magnitude = input.magnitude;

        if (magnitude <= 0.001f)
            return Vector2.zero;

        float curvedMagnitude =
            Mathf.Pow(
                magnitude,
                finalInputCurve
            );

        return input.normalized *
               curvedMagnitude;
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

        if (!isTouching)
        {
            targetBGAnchoredPosition =
                originalBGAnchoredPosition;
        }
        else if (enableDynamicJoystick)
        {
            Vector2 centerOffsetScreen =
                joystickCenterScreenPos -
                joystickStartPos;

            targetBGAnchoredPosition =
                originalBGAnchoredPosition +
                centerOffsetScreen;
        }

        joystickBG.anchoredPosition =
            Vector2.Lerp(
                joystickBG.anchoredPosition,
                targetBGAnchoredPosition,
                dynamicCenterFollowSpeed *
                Time.unscaledDeltaTime
            );
    }

    private void UpdateJoystickHandleVisual()
    {
        if (joystickHandle == null)
            return;

        Vector2 targetPosition =
            isTouching
                ? targetHandlePosition
                : Vector2.zero;

        float speed =
            isTouching
                ? handleFollowSpeed
                : handleReturnSpeed;

        visualHandlePosition =
            Vector2.Lerp(
                visualHandlePosition,
                targetPosition,
                speed * Time.unscaledDeltaTime
            );

        joystickHandle.anchoredPosition =
            visualHandlePosition;
    }

    private void ReleaseJoystick()
    {
        controlSource = ControlSource.None;
        activeTouchId = -1;
        isTouching = false;

        rawInput = Vector2.zero;
        processedInput = Vector2.zero;
        targetHandlePosition = Vector2.zero;
        previousRawInput = Vector2.zero;
        bufferedInput = Vector2.zero;

        joystickCenterScreenPos =
            joystickStartPos;

        targetBGAnchoredPosition =
            originalBGAnchoredPosition;
    }

    private void ForceStopInput()
    {
        controlSource = ControlSource.None;
        activeTouchId = -1;
        isTouching = false;

        rawInput = Vector2.zero;
        processedInput = Vector2.zero;
        smoothInput = Vector2.zero;
        inputSmoothVelocity = Vector2.zero;
        targetHandlePosition = Vector2.zero;
        visualHandlePosition = Vector2.zero;
        previousRawInput = Vector2.zero;
        bufferedInput = Vector2.zero;

        targetBGAnchoredPosition =
            originalBGAnchoredPosition;

        if (playerMovement != null)
            playerMovement.SetMoveInput(Vector2.zero);

        ResetHandleInstant();
    }

    private void ResetHandleInstant()
    {
        if (joystickHandle != null)
            joystickHandle.anchoredPosition =
                Vector2.zero;

        visualHandlePosition = Vector2.zero;
        targetHandlePosition = Vector2.zero;
    }

    private bool IsPointerInsideJoystick(
        Vector2 screenPos
    )
    {
        if (joystickBG == null)
            return false;

        return RectTransformUtility
            .RectangleContainsScreenPoint(
                joystickBG,
                screenPos,
                uiCamera
            );
    }

    private Camera GetUICamera()
    {
        if (joystickBG == null)
            return null;

        Canvas canvas =
            joystickBG.GetComponentInParent<Canvas>();

        if (canvas == null)
            return null;

        if (canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }
}