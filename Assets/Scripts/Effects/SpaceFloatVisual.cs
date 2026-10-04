using UnityEngine;

/// <summary>Cosmetic drifting on a separate visual child. Movement and collider state stay on Player.</summary>
[DefaultExecutionOrder(160)]
public class SpaceFloatVisual : MonoBehaviour
{
    [Header("Float")]
    public float floatAmount = 0.08f;
    public float floatSpeed = 2f;

    [Header("Rotation")]
    public float rotationAmount = 3f;
    public float rotationSpeed = 1.5f;

    [Header("Random Offset")]
    public bool randomizeOffset = true;

    [Header("Natural Idle")]
    [Range(0f, 1.5f)] public float idleAmplitudeScale = 0.6f;
    [Range(0f, 1f)] public float sidewaysRatio = 0.45f;
    [Range(0f, 1f)] public float idleRotationScale = 0.55f;

    [Header("Movement / Dash")]
    [Range(0f, 1f)] public float movingFloatScale = 0.32f;
    [Range(0f, 1f)] public float movingRotationScale = 0.18f;
    [Range(0f, 1f)] public float dashFloatScale = 0.08f;
    [Min(0.1f)] public float transitionSpeed = 4f;
    [Min(0.1f)] public float positionResponse = 6f;

    private Vector3 startLocalPos;
    private Quaternion startLocalRot;
    private Vector2 currentOffset;
    private float currentAngle;
    private float motionBlend;
    private float dashBlend;
    private float clock;
    private float phase;
    private MovementVisualEffect movementDriver;
    private PlayerMovement playerMovement;
    private PlayerDash playerDash;
    private bool initialized;

    private void Awake()
    {
        startLocalPos = transform.localPosition;
        startLocalRot = transform.localRotation;
        playerMovement = GetComponentInParent<PlayerMovement>();
        playerDash = GetComponentInParent<PlayerDash>();
        phase = randomizeOffset ? Random.Range(0f, 100f) : 0f;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f) return;
        // Do not animate the same transform that owns Player's Rigidbody/collider.
        if (playerMovement != null && playerMovement.transform == transform) return;
        if (playerMovement != null && (playerMovement.IsGameOver || GameStateManager.IsGameplayEnded)) return;

        float dt = playerMovement != null ? Time.unscaledDeltaTime : Time.deltaTime;
        clock += dt;
        Vector2 velocity = Vector2.zero;
        bool dashing = false;
        float speedReference = 7f;
        if (playerMovement != null)
        {
            velocity = playerMovement.CurrentVelocity;
            speedReference = Mathf.Max(0.1f, playerMovement.CurrentMoveSpeed);
            dashing = playerDash != null && playerDash.IsDashing;
        }
        else if (movementDriver != null && movementDriver.isActiveAndEnabled)
            velocity = movementDriver.VisualVelocity;

        float targetMotion = dashing ? 1f : Mathf.Clamp01(velocity.magnitude / speedReference);
        float response = 1f - Mathf.Exp(-Mathf.Max(0.1f, transitionSpeed) * dt);
        motionBlend = Mathf.Lerp(motionBlend, targetMotion, response);
        dashBlend = Mathf.Lerp(dashBlend, dashing ? 1f : 0f, response * 0.9f);

        // Two incommensurate waves plus slow noise avoid an obvious repeating loop.
        float t = clock * Mathf.Max(0f, floatSpeed);
        float x = Mathf.Sin(t * 0.61f + phase) * 0.7f
            + Mathf.Sin(t * 0.29f + phase * 1.7f) * 0.3f;
        float y = Mathf.Sin(t + phase * 0.73f) * 0.68f
            + Mathf.Sin(t * 0.47f + phase + 1.2f) * 0.22f
            + (Mathf.PerlinNoise(phase + 13f, clock * 0.18f) * 2f - 1f) * 0.1f;
        float amplitude = floatAmount * idleAmplitudeScale
            * Mathf.Lerp(1f, movingFloatScale, motionBlend)
            * Mathf.Lerp(1f, dashFloatScale, dashBlend);
        Vector2 targetOffset = new Vector2(x * sidewaysRatio, y) * amplitude;
        float positionBlend = 1f - Mathf.Exp(-Mathf.Max(0.1f, positionResponse) * dt);
        currentOffset = Vector2.Lerp(currentOffset, targetOffset, positionBlend);

        float rotationTime = clock * Mathf.Max(0f, rotationSpeed);
        float driftRotation = (Mathf.Sin(rotationTime * 0.71f + phase + 0.6f) * 0.75f
            + Mathf.Sin(rotationTime * 0.33f + phase * 0.5f) * 0.25f)
            * rotationAmount * idleRotationScale
            * Mathf.Lerp(1f, movingRotationScale, motionBlend)
            * (1f - dashBlend);

        float tilt = movementDriver != null && movementDriver.isActiveAndEnabled
            ? movementDriver.tiltAmount : 8f;
        float tiltResponse = movementDriver != null && movementDriver.isActiveAndEnabled
            ? movementDriver.tiltSpeed : 10f;
        Vector2 direction = dashing && playerMovement != null
            ? playerMovement.LastMoveDirection : velocity.sqrMagnitude > 0.0001f
                ? velocity.normalized : Vector2.zero;
        // Parent flips horizontally when Player faces left; compensate local lean.
        float facingSign = transform.parent != null && transform.parent.lossyScale.x < 0f ? -1f : 1f;
        float lean = -direction.x * tilt * motionBlend * facingSign;
        float targetAngle = driftRotation + lean;
        float angleBlend = 1f - Mathf.Exp(-Mathf.Max(0.1f, tiltResponse) * dt);
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, angleBlend);
        transform.localPosition = startLocalPos + new Vector3(currentOffset.x, currentOffset.y, 0f);
        transform.localRotation = startLocalRot * Quaternion.Euler(0f, 0f, currentAngle);
    }

    public void RegisterMovementDriver(MovementVisualEffect driver) { movementDriver = driver; }
    public void UnregisterMovementDriver(MovementVisualEffect driver)
    {
        if (movementDriver == driver) movementDriver = null;
    }

    private void OnDisable()
    {
        if (!initialized) return;
        if (playerMovement != null && playerMovement.transform == transform) return;
        transform.localPosition = startLocalPos;
        transform.localRotation = startLocalRot;
        currentOffset = Vector2.zero;
        currentAngle = motionBlend = dashBlend = 0f;
    }
}
