using UnityEngine;

/// <summary>Targets the separate visual child; shares motion with SpaceFloatVisual without competing writes.</summary>
[DefaultExecutionOrder(150)]
public class MovementVisualEffect : MonoBehaviour
{
    [Header("Visual")]
    public Transform visual;
    [Header("Movement Detection")]
    public float moveThreshold = 0.01f;
    [Header("Tilt")]
    public float tiltAmount = 8f;
    public float tiltSpeed = 10f;
    [Header("Float (fallback when SpaceFloatVisual is absent)")]
    public float floatAmount = 0.04f;
    public float floatSpeed = 4f;

    private Vector3 lastPosition;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private SpaceFloatVisual ambientFloat;
    private PlayerMovement playerMovement;
    private bool hasSeparateVisual;
    private bool initialized;
    public Vector2 VisualVelocity { get; private set; }

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        // Repair the existing prefab's Visual reference, which points at Player itself.
        if (visual == null || visual == transform)
        {
            SpaceFloatVisual[] floatVisuals = GetComponentsInChildren<SpaceFloatVisual>(true);
            for (int i = 0; i < floatVisuals.Length; i++)
                if (floatVisuals[i].transform != transform)
                {
                    visual = floatVisuals[i].transform;
                    break;
                }
            if (visual == null || visual == transform)
            {
                Transform namedVisual = transform.Find("Visual");
                if (namedVisual != null) visual = namedVisual;
            }
        }
        hasSeparateVisual = visual != null && visual != transform;
        lastPosition = transform.position;
        if (hasSeparateVisual)
        {
            originalLocalPosition = visual.localPosition;
            originalLocalRotation = visual.localRotation;
            ambientFloat = visual.GetComponent<SpaceFloatVisual>();
        }
        initialized = true;
        RegisterFloat();
    }

    private void OnEnable()
    {
        lastPosition = transform.position;
        VisualVelocity = Vector2.zero;
        if (initialized) RegisterFloat();
    }

    private void RegisterFloat()
    {
        if (ambientFloat != null) ambientFloat.RegisterMovementDriver(this);
    }

    private void LateUpdate()
    {
        if (!hasSeparateVisual) return;
        if (Time.timeScale <= 0f)
        {
            lastPosition = transform.position;
            VisualVelocity = Vector2.zero;
            return;
        }
        float dt = playerMovement != null ? Time.unscaledDeltaTime : Time.deltaTime;
        VisualVelocity = playerMovement != null ? playerMovement.CurrentVelocity
            : (Vector2)(transform.position - lastPosition) / Mathf.Max(dt, 0.0001f);
        lastPosition = transform.position;
        // A single script owns the final position/rotation when the float component is active.
        if (ambientFloat != null && ambientFloat.isActiveAndEnabled) return;
        if (playerMovement != null && (playerMovement.IsGameOver || GameStateManager.IsGameplayEnded)) return;

        bool moving = VisualVelocity.magnitude > moveThreshold;
        float facingSign = visual.parent != null && visual.parent.lossyScale.x < 0f ? -1f : 1f;
        float targetZ = moving ? -VisualVelocity.normalized.x * tiltAmount * facingSign : 0f;
        Quaternion targetRotation = originalLocalRotation * Quaternion.Euler(0f, 0f, targetZ);
        visual.localRotation = Quaternion.Slerp(visual.localRotation, targetRotation,
            1f - Mathf.Exp(-Mathf.Max(0.1f, tiltSpeed) * dt));
        visual.localPosition = originalLocalPosition
            + Vector3.up * (Mathf.Sin(Time.time * floatSpeed) * floatAmount);
    }

    private void OnDisable()
    {
        if (ambientFloat != null) ambientFloat.UnregisterMovementDriver(this);
        VisualVelocity = Vector2.zero;
    }
}
