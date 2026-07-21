using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerDash : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement;
    public SoundManager soundManager;
    public Rigidbody2D rb;

    [Header("Dash")]
    [Min(0f)]
    public float dashDistance = 2.5f;

    [Min(0.01f)]
    public float dashDuration = 0.12f;

    [Min(0f)]
    public float dashCooldown = 2f;

    [Min(0f)]
    public float boundsPadding = 0.4f;

    [Header("Dash Feel")]
    [Tooltip("Dash hareketinin hızlanma ve yavaşlama eğrisi.")]
    public AnimationCurve dashMovementCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Dash başladığında trail üzerindeki eski izi temizler.")]
    public bool clearTrailOnDash = true;

    [Header("Visual")]
    public TrailRenderer trail;

    [Header("UI")]
    public Image cooldownFill;
    public TMP_Text cooldownText;

    private const float TextRefreshInterval = 0.1f;

    private Coroutine dashRoutine;

    private bool canDash = true;
    private bool isDashing;
    private bool gameOverHandled;

    private float cooldownTimer;
    private float textRefreshTimer;

    public bool IsDashing => isDashing;
    public bool CanDash => canDash && !isDashing;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (soundManager == null)
            soundManager = FindAnyObjectByType<SoundManager>();

        SetTrail(false);
        HideCooldownUI();
    }

    private void OnEnable()
    {
        ResetDashState();
    }

    private void Update()
    {
        if (IsGameOver())
        {
            HandleGameOver();
            return;
        }

        HandleKeyboardInput();
        UpdateCooldown();
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            TryDash();
    }

    public void TryDash()
    {
        if (IsGameOver())
            return;

        if (!CanDash)
            return;

        Vector2 dashDirection = GetDashDirection();

        if (dashDirection.sqrMagnitude <= 0.001f)
            return;

        StartDash(dashDirection);
    }

    private void StartDash(Vector2 dashDirection)
    {
        if (dashRoutine != null)
            StopCoroutine(dashRoutine);

        canDash = false;
        isDashing = true;

        cooldownTimer = dashCooldown;
        textRefreshTimer = 0f;

        if (dashCooldown > 0f)
            ShowCooldownUI();

        if (soundManager != null)
            soundManager.PlayDashSound();

        VibrationManager.Instance?.VibrateLight();
        StatsManager.AddDashUse();

        if (trail != null && clearTrailOnDash)
            trail.Clear();

        SetTrail(true);

        dashRoutine = StartCoroutine(
            DashRoutine(dashDirection.normalized)
        );
    }

    private IEnumerator DashRoutine(Vector2 dashDirection)
    {
        Vector2 startPosition = GetCurrentPosition();

        Vector2 targetPosition =
            startPosition + dashDirection * dashDistance;

        targetPosition = ClampToBounds(targetPosition);

        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            if (IsGameOver())
            {
                StopDash();
                yield break;
            }

            elapsedTime += Time.fixedDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsedTime / dashDuration);

            float curvedTime = dashMovementCurve != null
                ? dashMovementCurve.Evaluate(normalizedTime)
                : normalizedTime;

            Vector2 nextPosition = Vector2.Lerp(
                startPosition,
                targetPosition,
                curvedTime
            );

            MovePlayer(nextPosition);

            yield return new WaitForFixedUpdate();
        }

        MovePlayer(targetPosition);

        isDashing = false;
        dashRoutine = null;

        SetTrail(false);

        TryFinishCooldown();
    }

    private void UpdateCooldown()
    {
        if (canDash)
            return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            cooldownTimer = Mathf.Max(0f, cooldownTimer);

            UpdateCooldownUI();
        }

        TryFinishCooldown();
    }

    private void TryFinishCooldown()
    {
        if (isDashing)
            return;

        if (cooldownTimer > 0f)
            return;

        cooldownTimer = 0f;
        canDash = true;

        HideCooldownUI();
    }

    private void UpdateCooldownUI()
    {
        if (cooldownFill != null)
        {
            cooldownFill.fillAmount = dashCooldown > 0f
                ? Mathf.Clamp01(cooldownTimer / dashCooldown)
                : 0f;
        }

        textRefreshTimer -= Time.deltaTime;

        if (textRefreshTimer > 0f)
            return;

        textRefreshTimer = TextRefreshInterval;

        if (cooldownText != null)
        {
            cooldownText.text = cooldownTimer > 0f
                ? cooldownTimer.ToString("F1")
                : string.Empty;
        }
    }

    private Vector2 GetDashDirection()
    {
        if (playerMovement == null)
            return Vector2.right;

        Vector2 direction =
            playerMovement.LastMoveDirection;

        if (direction.sqrMagnitude <= 0.001f)
            return Vector2.right;

        return direction.normalized;
    }

    private Vector2 GetCurrentPosition()
    {
        if (rb != null)
            return rb.position;

        return transform.position;
    }

    private void MovePlayer(Vector2 position)
    {
        position = ClampToBounds(position);

        if (rb != null)
        {
            rb.MovePosition(position);
            return;
        }

        transform.position = position;
    }

    private Vector2 ClampToBounds(Vector2 position)
    {
        CameraWorldBounds bounds =
            CameraWorldBounds.Instance;

        if (bounds == null)
            return position;

        float minimumX = bounds.MinX + boundsPadding;
        float maximumX = bounds.MaxX - boundsPadding;
        float minimumY = bounds.MinY + boundsPadding;
        float maximumY = bounds.MaxY - boundsPadding;

        position.x = Mathf.Clamp(
            position.x,
            minimumX,
            maximumX
        );

        position.y = Mathf.Clamp(
            position.y,
            minimumY,
            maximumY
        );

        return position;
    }

    private bool IsGameOver()
    {
        return playerMovement != null &&
               playerMovement.IsGameOver;
    }

    private void HandleGameOver()
    {
        if (gameOverHandled)
            return;

        gameOverHandled = true;

        StopDash();
        enabled = false;
    }

    private void ShowCooldownUI()
    {

        if (cooldownFill != null)
        {
            cooldownFill.gameObject.SetActive(true);

            cooldownFill.fillAmount = dashCooldown > 0f
                ? 1f
                : 0f;
        }

        UpdateCooldownUI();
    }

    private void SetTrail(bool state)
    {
        if (trail != null)
            trail.emitting = state;
    }

    private void HideCooldownUI()
    {

        if (cooldownFill != null)
        {
            cooldownFill.fillAmount = 0f;
            cooldownFill.gameObject.SetActive(false);
        }

        if (cooldownText != null)
            cooldownText.text = string.Empty;
    }

    public void ResetDashState()
    {
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        canDash = true;
        isDashing = false;
        gameOverHandled = false;

        cooldownTimer = 0f;
        textRefreshTimer = 0f;

        SetTrail(false);
        HideCooldownUI();
    }

    public void StopDash()
    {
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        isDashing = false;
        canDash = false;

        cooldownTimer = 0f;
        textRefreshTimer = 0f;

        SetTrail(false);
        HideCooldownUI();
    }

    private void OnDisable()
    {
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        isDashing = false;

        SetTrail(false);
    }

    private void OnValidate()
    {
        dashDistance = Mathf.Max(0f, dashDistance);
        dashDuration = Mathf.Max(0.01f, dashDuration);
        dashCooldown = Mathf.Max(0f, dashCooldown);
        boundsPadding = Mathf.Max(0f, boundsPadding);
    }
}