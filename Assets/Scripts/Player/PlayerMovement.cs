using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 7f;
    public float comboSpeedBonus = 1.2f;

    [Tooltip("Level configden gelir. Boşsa eski comboSpeedBonus sistemi kullanılır.")]
    public ComboSpeedStage[] comboSpeedStages;

    [Header("Movement Feel")]
    public float acceleration = 55f;
    public float deceleration = 75f;
    public float turnAcceleration = 90f;

    [Header("Advanced Movement Feel")]
    [Range(0.3f, 1f)] public float lowInputAccelerationMultiplier = 0.65f;
    [Range(1f, 2f)] public float highInputAccelerationMultiplier = 1.15f;
    [Range(0.5f, 2f)] public float sharpTurnBoost = 1.25f;

    [Range(0f, 0.5f)] public float minInputToMove = 0.03f;

    [Header("Combo")]
    public PlayerCoinCollector coinCollector;

    public GameStateManager gameStateManager;

    private Rigidbody2D rb;
    private DeathFadeEffect deathFade;

    private Vector2 moveInput;
    private Vector2 currentVelocity;

    private Vector3 originalScale;
    private int facingDirection = 1;

    public Vector2 LastMoveDirection { get; private set; } = Vector2.right;
    public Vector2 CurrentVelocity => currentVelocity;
    public Vector2 CurrentMoveInput => moveInput;

    public float CurrentMoveSpeed => GetCurrentSpeed();

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        deathFade = GetComponent<DeathFadeEffect>();
        originalScale = transform.localScale;

        if (rb != null)
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0f)
        {
            StopMovement();
            return;
        }

        float delta = GetPlayerDeltaTime();

        if (!GameStateManager.IsGameplayStarted || IsGameOver)
        {
            SmoothStop(delta);
            return;
        }

        float currentSpeed = GetCurrentSpeed();
        Vector2 targetVelocity = moveInput * currentSpeed;

        float accelRate = GetAdaptiveAccelerationRate();

        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            accelRate * delta
        );

        if (currentVelocity.sqrMagnitude <= 0.0001f)
        {
            currentVelocity = Vector2.zero;
            return;
        }

        rb.MovePosition(rb.position + currentVelocity * delta);
    }

    private float GetAdaptiveAccelerationRate()
    {
        return MovementMath2D.GetAdaptiveAcceleration(
            currentVelocity,
            moveInput,
            acceleration,
            deceleration,
            turnAcceleration,
            lowInputAccelerationMultiplier,
            highInputAccelerationMultiplier,
            sharpTurnBoost
        );
    }

    private float GetCurrentSpeed()
    {
        float currentSpeed = speed;

        if (coinCollector == null)
            return currentSpeed;

        float multiplier = GetComboSpeedMultiplier(coinCollector.Combo);

        if (multiplier > 0f)
            return currentSpeed * multiplier;

        if (coinCollector.Combo >= 3)
            currentSpeed += comboSpeedBonus;

        return currentSpeed;
    }

    private float GetComboSpeedMultiplier(int currentCombo)
    {
        if (currentCombo <= 1 || comboSpeedStages == null || comboSpeedStages.Length == 0)
            return 0f;

        float bestMultiplier = 0f;
        int bestCombo = 1;

        for (int i = 0; i < comboSpeedStages.Length; i++)
        {
            ComboSpeedStage stage = comboSpeedStages[i];

            if (stage == null) continue;
            if (stage.comboMultiplier <= bestCombo) continue;
            if (currentCombo < stage.comboMultiplier) continue;

            bestCombo = stage.comboMultiplier;
            bestMultiplier = Mathf.Max(1f, stage.playerSpeedMultiplier);
        }

        return bestMultiplier;
    }

    public void SetMoveInput(Vector2 input)
    {
        if (Time.timeScale == 0f || !GameStateManager.IsGameplayStarted || IsGameOver)
        {
            moveInput = Vector2.zero;
            return;
        }

        input = Vector2.ClampMagnitude(input, 1f);

        if (input.magnitude < minInputToMove)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = input;

        LastMoveDirection = moveInput.normalized;
        UpdateFacing(moveInput.x);
    }

    private void SmoothStop(float delta)
    {
        moveInput = Vector2.zero;

        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            Vector2.zero,
            deceleration * delta
        );

        if (currentVelocity.sqrMagnitude <= 0.0001f)
            currentVelocity = Vector2.zero;
    }

    private void UpdateFacing(float x)
    {
        if (x > 0.05f && facingDirection != 1)
        {
            facingDirection = 1;
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else if (x < -0.05f && facingDirection != -1)
        {
            facingDirection = -1;
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }

    public void StopMovement()
    {
        moveInput = Vector2.zero;
        currentVelocity = Vector2.zero;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    public void SetGameOver(bool value)
    {
        IsGameOver = value;

        if (value)
            StopMovement();
    }

    public void GameOver(string deathCause = "UNKNOWN")
    {
        if (IsGameOver) return;

        LastDeathInfo.Cause = deathCause;

        VibrationManager.Instance?.VibrateHeavy();

        if (deathFade != null)
            deathFade.Play();

        int finalScore = coinCollector != null ? coinCollector.Score : 0;

        if (gameStateManager != null)
            gameStateManager.GameOver(finalScore);
        else
            SetGameOver(true);
    }

    private float GetPlayerDeltaTime()
    {
        if (Time.timeScale <= 0f)
            return Time.fixedDeltaTime;

        return Time.fixedDeltaTime / Time.timeScale;
    }
}