using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerDash : MonoBehaviour
{
    private static readonly WaitForFixedUpdate CachedWaitForFixedUpdate =
        new WaitForFixedUpdate();
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

    [Header("Dash Hit Detection")]
    [Min(0.05f)]
    [Tooltip("Dash boyunca Beacon kontrolü için kullanılan örnekleme aralığı.")]
    public float beaconHitSampleSpacing = 0.15f;

    [Min(0.05f)]
    [Tooltip("Player collider bulunamazsa Beacon kontrolünde kullanılacak yarıçap.")]
    public float beaconHitFallbackRadius = 0.35f;

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


    private float trailStartAlpha = 1f;
    private float trailEndAlpha;
    private bool trailAlphaCached;

    private Collider2D playerCollider;
    private ContactFilter2D dashHitFilter;
    private readonly Collider2D[] dashHitResults = new Collider2D[16];

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

        playerCollider = GetComponent<Collider2D>();

        dashHitFilter = ContactFilter2D.noFilter;

        CacheTrailAlpha();
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
            soundManager.PlayDashSound(transform.position);

        VibrationManager.Instance?.VibrateDash();
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

            Vector2 previousPosition = GetCurrentPosition();

            MovePlayer(nextPosition);
            TryHitBeaconsAlongDash(previousPosition, nextPosition);

            yield return CachedWaitForFixedUpdate;
        }

        Vector2 finalPreviousPosition = GetCurrentPosition();
        MovePlayer(targetPosition);
        TryHitBeaconsAlongDash(finalPreviousPosition, targetPosition);


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

    private void TryHitBeaconsAlongDash(
        Vector2 fromPosition,
        Vector2 toPosition)
    {
        if (!isDashing)
            return;

        float radius = GetDashHitRadius();
        float distance = Vector2.Distance(
            fromPosition,
            toPosition
        );

        float spacing = Mathf.Max(
            0.05f,
            beaconHitSampleSpacing
        );

        int sampleCount = Mathf.Max(
            1,
            Mathf.CeilToInt(distance / spacing)
        );

        for (int sampleIndex = 0;
             sampleIndex <= sampleCount;
             sampleIndex++)
        {
            float t = sampleCount <= 0
                ? 1f
                : (float)sampleIndex / sampleCount;

            Vector2 samplePosition = Vector2.Lerp(
                fromPosition,
                toPosition,
                t
            );

            int hitCount = Physics2D.OverlapCircle(
                samplePosition,
                radius,
                dashHitFilter,
                dashHitResults
            );

            for (int hitIndex = 0;
                 hitIndex < hitCount;
                 hitIndex++)
            {
                Collider2D hit = dashHitResults[hitIndex];
                dashHitResults[hitIndex] = null;

                if (hit == null || hit == playerCollider)
                    continue;

                BeaconEnemy beacon =
                    hit.GetComponent<BeaconEnemy>();

                if (beacon == null)
                {
                    beacon =
                        hit.GetComponentInParent<BeaconEnemy>();
                }

                if (beacon != null)
                    beacon.TryDieFromDash(this);
            }
        }
    }

    private float GetDashHitRadius()
    {
        if (playerCollider == null)
        {
            return Mathf.Max(
                0.05f,
                beaconHitFallbackRadius
            );
        }

        Bounds bounds = playerCollider.bounds;

        float colliderRadius = Mathf.Max(
            bounds.extents.x,
            bounds.extents.y
        );

        return Mathf.Max(
            0.05f,
            colliderRadius
        );
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


    public void ApplyTrailColor(Color color)
    {
        if (trail == null)
            return;

        CacheTrailAlpha();

        Color startColor = color;
        startColor.a = trailStartAlpha;

        Color endColor = color;
        endColor.a = trailEndAlpha;

        trail.startColor = startColor;
        trail.endColor = endColor;
    }

    private void CacheTrailAlpha()
    {
        if (trail == null || trailAlphaCached)
            return;

        trailStartAlpha = trail.startColor.a;
        trailEndAlpha = trail.endColor.a;
        trailAlphaCached = true;
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