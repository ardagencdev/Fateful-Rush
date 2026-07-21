using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public PlayerCoinCollector coinCollector;
    public PlayerDash playerDash;
    public GameStateManager gameStateManager;

    [Header("Movement")]
    [Min(0f)]
    public float speed = 7f;

    [Min(0f)]
    public float comboSpeedBonus = 1.2f;

    [Tooltip(
        "LevelConfig'den gelir. Boşsa eski comboSpeedBonus sistemi kullanılır."
    )]
    public ComboSpeedStage[] comboSpeedStages;

    [Header("Movement Feel")]
    [Min(0f)]
    public float acceleration = 55f;

    [Min(0f)]
    public float deceleration = 75f;

    [Min(0f)]
    public float turnAcceleration = 90f;

    [Header("Advanced Movement Feel")]
    [Range(0.3f, 1f)]
    public float lowInputAccelerationMultiplier = 0.65f;

    [Range(1f, 2f)]
    public float highInputAccelerationMultiplier = 1.15f;

    [Range(0.5f, 2f)]
    public float sharpTurnBoost = 1.25f;

    [Range(0f, 0.5f)]
    public float minInputToMove = 0.03f;

    private Rigidbody2D rb;
    private DeathFadeEffect deathFade;

    private Vector2 moveInput;
    private Vector2 currentVelocity;

    private Vector3 originalScale;
    private int facingDirection = 1;

    public Vector2 LastMoveDirection { get; private set; } =
        Vector2.right;

    public Vector2 CurrentVelocity => currentVelocity;
    public Vector2 CurrentMoveInput => moveInput;
    public float CurrentMoveSpeed => GetCurrentSpeed();

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        deathFade = GetComponent<DeathFadeEffect>();

        if (coinCollector == null)
            coinCollector = GetComponent<PlayerCoinCollector>();

        if (playerDash == null)
            playerDash = GetComponent<PlayerDash>();

        if (gameStateManager == null)
            gameStateManager =
                FindAnyObjectByType<GameStateManager>();

        originalScale = transform.localScale;

        rb.interpolation =
            RigidbodyInterpolation2D.Interpolate;
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0f)
        {
            StopMovement();
            return;
        }

        float deltaTime = GetPlayerDeltaTime();

        if (!GameStateManager.IsGameplayStarted ||
            IsGameOver)
        {
            SmoothStop(deltaTime);
            return;
        }

        /*
         * Dash sırasında PlayerDash, Rigidbody2D pozisyonunu
         * kendisi yönetir. İki scriptin aynı anda hareket
         * uygulamasını engelliyoruz.
         */
        if (playerDash != null &&
            playerDash.IsDashing)
        {
            currentVelocity = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float currentSpeed = GetCurrentSpeed();

        Vector2 targetVelocity =
            moveInput * currentSpeed;

        float accelerationRate =
            GetAdaptiveAccelerationRate();

        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            accelerationRate * deltaTime
        );

        if (currentVelocity.sqrMagnitude <= 0.0001f)
        {
            currentVelocity = Vector2.zero;
            return;
        }

        rb.MovePosition(
            rb.position +
            currentVelocity * deltaTime
        );
    }

    private float GetAdaptiveAccelerationRate()
    {
        float inputMagnitude = moveInput.magnitude;

        if (inputMagnitude <= 0.001f)
            return deceleration;

        float adaptiveAcceleration =
            acceleration *
            Mathf.Lerp(
                lowInputAccelerationMultiplier,
                highInputAccelerationMultiplier,
                inputMagnitude
            );

        if (currentVelocity.sqrMagnitude <= 0.0001f)
            return adaptiveAcceleration;

        Vector2 currentDirection =
            currentVelocity.normalized;

        Vector2 targetDirection =
            moveInput.normalized;

        float directionDot = Vector2.Dot(
            currentDirection,
            targetDirection
        );

        if (directionDot >= 0.75f)
            return adaptiveAcceleration;

        float turnAmount = Mathf.InverseLerp(
            0.75f,
            -1f,
            directionDot
        );

        float boostedTurnAcceleration =
            turnAcceleration *
            Mathf.Lerp(
                1f,
                sharpTurnBoost,
                turnAmount
            );

        return Mathf.Max(
            adaptiveAcceleration,
            boostedTurnAcceleration
        );
    }

    private float GetCurrentSpeed()
    {
        float currentSpeed = speed;

        if (coinCollector == null)
            return currentSpeed;

        bool hasConfiguredStages =
            comboSpeedStages != null &&
            comboSpeedStages.Length > 0;

        if (hasConfiguredStages)
        {
            float multiplier =
                GetComboSpeedMultiplier(
                    coinCollector.Combo
                );

            return multiplier > 0f
                ? currentSpeed * multiplier
                : currentSpeed;
        }

        /*
         * Eski sistem yalnızca comboSpeedStages boşsa
         * fallback olarak kullanılır.
         */
        if (coinCollector.Combo >= 3)
            currentSpeed += comboSpeedBonus;

        return currentSpeed;
    }

    private float GetComboSpeedMultiplier(
        int currentCombo
    )
    {
        if (currentCombo <= 1 ||
            comboSpeedStages == null ||
            comboSpeedStages.Length == 0)
        {
            return 0f;
        }

        float bestMultiplier = 0f;
        int bestCombo = 1;

        for (int i = 0;
             i < comboSpeedStages.Length;
             i++)
        {
            ComboSpeedStage stage =
                comboSpeedStages[i];

            if (stage == null)
                continue;

            if (stage.comboMultiplier <= bestCombo)
                continue;

            if (currentCombo <
                stage.comboMultiplier)
            {
                continue;
            }

            bestCombo =
                stage.comboMultiplier;

            bestMultiplier = Mathf.Max(
                1f,
                stage.playerSpeedMultiplier
            );
        }

        return bestMultiplier;
    }

    public void SetMoveInput(Vector2 input)
    {
        if (Time.timeScale == 0f ||
            !GameStateManager.IsGameplayStarted ||
            IsGameOver)
        {
            moveInput = Vector2.zero;
            return;
        }

        input = Vector2.ClampMagnitude(
            input,
            1f
        );

        if (input.magnitude < minInputToMove)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = input;

        LastMoveDirection =
            moveInput.normalized;

        UpdateFacing(moveInput.x);
    }

    private void SmoothStop(float deltaTime)
    {
        moveInput = Vector2.zero;

        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            Vector2.zero,
            deceleration * deltaTime
        );

        if (currentVelocity.sqrMagnitude <= 0.0001f)
            currentVelocity = Vector2.zero;

        rb.linearVelocity = Vector2.zero;
    }

    private void UpdateFacing(float horizontalInput)
    {
        if (horizontalInput > 0.05f &&
            facingDirection != 1)
        {
            facingDirection = 1;

            transform.localScale = new Vector3(
                Mathf.Abs(originalScale.x),
                originalScale.y,
                originalScale.z
            );
        }
        else if (horizontalInput < -0.05f &&
                 facingDirection != -1)
        {
            facingDirection = -1;

            transform.localScale = new Vector3(
                -Mathf.Abs(originalScale.x),
                originalScale.y,
                originalScale.z
            );
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

    public void GameOver(
        string deathCause = "UNKNOWN"
    )
    {
        if (IsGameOver)
            return;

        /*
         * State'i hemen kapatıyoruz. Böylece aynı frame
         * içerisinde ikinci bir çarpışma tekrar GameOver
         * başlatamaz.
         */
        IsGameOver = true;
        StopMovement();

        LastDeathInfo.Cause =
            string.IsNullOrWhiteSpace(deathCause)
                ? "UNKNOWN"
                : deathCause;

        VibrationManager.Instance?.VibrateHeavy();

        if (deathFade != null)
            deathFade.Play();

        int finalScore =
            coinCollector != null
                ? coinCollector.Score
                : 0;

        if (gameStateManager != null)
            gameStateManager.GameOver(finalScore);
    }

    private float GetPlayerDeltaTime()
    {
        /*
         * Global slow sırasında player'ın hızının
         * yavaşlamamasını sağlar.
         */
        if (Time.timeScale <= 0f)
            return Time.fixedDeltaTime;

        return Time.fixedDeltaTime /
               Time.timeScale;
    }

    private void OnValidate()
    {
        speed = Mathf.Max(0f, speed);
        comboSpeedBonus =
            Mathf.Max(0f, comboSpeedBonus);

        acceleration =
            Mathf.Max(0f, acceleration);

        deceleration =
            Mathf.Max(0f, deceleration);

        turnAcceleration =
            Mathf.Max(0f, turnAcceleration);
    }
}