using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class VoidClone : MonoBehaviour
{
    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public float blinkSpeed = 10f;
    public float minAlpha = 0.35f;
    public float maxAlpha = 0.75f;

    [Header("Movement")]
    [Tooltip("Kept for prefab compatibility. The clone now always chooses a useful route, even if the player is standing still.")]
    public float minimumMovementSpeed = 0.1f;

    [Tooltip("Clone speed relative to the player's movement speed.")]
    [Range(0.5f, 1.5f)]
    public float speedMultiplier = 0.9f;

    public float fallbackAcceleration = 55f;
    public float fallbackTurnAcceleration = 90f;

    [Header("Natural Movement")]
    [Tooltip("How often the clone gently varies its route.")]
    public Vector2 directionChangeInterval = new Vector2(0.55f, 0.95f);

    [Tooltip("Maximum angle used for a natural route variation.")]
    [Range(0f, 60f)]
    public float maximumTurnAngle = 24f;

    [Tooltip("Small memory of the initial escape direction. Internally capped so it never becomes robotic.")]
    [Range(0f, 1f)]
    public float originalDirectionInfluence = 0.12f;

    [Header("Human-like Steering")]
    [Tooltip("Clone tries to stay at least this far from the real player so enemies targeting it do not line up through the player.")]
    [Min(0.5f)]
    public float playerSeparationRadius = 2.8f;

    [Range(0f, 3f)]
    public float playerSeparationStrength = 1.6f;

    [Tooltip("Viewport edge zone where the clone starts curving away before reaching the border.")]
    [Range(0.05f, 0.35f)]
    public float borderSoftZone = 0.16f;

    [Range(0f, 3f)]
    public float borderSteeringStrength = 1.45f;

    [Range(0f, 1f)]
    public float borderTangentAmount = 0.38f;

    [Tooltip("How quickly strategic steering affects the chosen route. Lower values are smoother.")]
    [Range(0.02f, 0.5f)]
    public float strategicSteeringResponse = 0.18f;

    [Header("Obstacle Avoidance - Same System As Stalker")]
    [Tooltip("Extra layers. Obstacle and Wall layers are added automatically.")]
    public LayerMask solidLayers;

    public float avoidanceLookAhead = 1.25f;

    [Range(2, 8)]
    public int obstacleAvoidanceAttempts = 5;

    [Range(0f, 1f)]
    public float obstacleOutwardBias = 0.25f;

    public float collisionSkin = 0.04f;

    [Header("Advanced Unstuck")]
    [Min(0.1f)]
    public float stuckCheckTime = 0.35f;

    [Min(0.001f)]
    public float stuckDistance = 0.06f;

    [Min(0.1f)]
    public float escapeCheckRadius = 1.1f;

    [Min(1f)]
    public float escapeSpeedMultiplier = 1.9f;

    private Rigidbody2D rb;
    private Collider2D cloneCollider;
    private Transform realPlayer;
    private Camera mainCamera;

    private Vector2 originalDirection;
    private Vector2 desiredDirection;
    private Vector2 currentVelocity;
    private Vector2 lastPosition;

    private float targetSpeed;
    private float acceleration;
    private float turnAcceleration;
    private float directionTimer;
    private float nextDirectionChange;
    private float stuckTimer;

    private bool cloneActive;
    private bool shouldMove;

    private Vector3 originalScale;
    private int obstacleAvoidanceSide = 1;
    private int borderTangentSide = 1;

    private ContactFilter2D navigationFilter;
    private readonly RaycastHit2D[] avoidanceHits = new RaycastHit2D[16];
    private readonly Collider2D[] escapeHits = new Collider2D[16];

    private GameObject clonedArmorVisual;

    public Vector2 VisualMoveDirection
    {
        get
        {
            if (!cloneActive ||
                !shouldMove ||
                currentVelocity.sqrMagnitude <= 0.0001f)
            {
                return Vector2.zero;
            }

            return currentVelocity.normalized;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cloneCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        originalScale = transform.localScale;

        if (originalScale == Vector3.zero)
            originalScale = Vector3.one;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.simulated = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        RebuildNavigationFilter();

        obstacleAvoidanceSide = Random.value < 0.5f ? -1 : 1;
        borderTangentSide = Random.value < 0.5f ? -1 : 1;
        lastPosition = rb.position;
    }

    private void RebuildNavigationFilter()
    {
        navigationFilter = new ContactFilter2D();
        navigationFilter.SetLayerMask(
            EnemyObstacleSteering2D.BuildNavigationMask(solidLayers)
        );
        navigationFilter.useLayerMask = true;
        navigationFilter.useTriggers = false;
    }

    public void SetSkin(Sprite skinSprite)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer != null && skinSprite != null)
            spriteRenderer.sprite = skinSprite;
    }

    public void CopyArmorVisual(PlayerArmor sourceArmor)
    {
        ClearClonedArmorVisual();

        if (sourceArmor == null ||
            !sourceArmor.HasArmor ||
            sourceArmor.ArmorVisualObject == null)
        {
            return;
        }

        GameObject sourceVisual = sourceArmor.ArmorVisualObject;

        clonedArmorVisual = Instantiate(
            sourceVisual,
            transform,
            false
        );

        clonedArmorVisual.name = "CloneArmorVisual";
        clonedArmorVisual.transform.localPosition = sourceVisual.transform.localPosition;
        clonedArmorVisual.transform.localRotation = sourceVisual.transform.localRotation;
        clonedArmorVisual.transform.localScale = sourceVisual.transform.localScale;

        Collider2D[] armorColliders =
            clonedArmorVisual.GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < armorColliders.Length; i++)
            armorColliders[i].enabled = false;

        Rigidbody2D[] armorBodies =
            clonedArmorVisual.GetComponentsInChildren<Rigidbody2D>(true);

        for (int i = 0; i < armorBodies.Length; i++)
            armorBodies[i].simulated = false;

        ShieldRotate[] armorRotators =
            clonedArmorVisual.GetComponentsInChildren<ShieldRotate>(true);

        for (int i = 0; i < armorRotators.Length; i++)
            armorRotators[i].ConfigureForClone(this);

        clonedArmorVisual.SetActive(true);
    }

    public void StartClone(float duration, PlayerMovement playerMovement)
    {
        StopAllCoroutines();

        Vector2 playerVelocity = Vector2.zero;
        Vector2 playerInput = Vector2.zero;
        float playerMoveSpeed = 0f;

        acceleration = fallbackAcceleration;
        turnAcceleration = fallbackTurnAcceleration;
        realPlayer = playerMovement != null
            ? playerMovement.transform
            : null;

        mainCamera = Camera.main;

        if (playerMovement != null)
        {
            playerVelocity = playerMovement.CurrentVelocity;
            playerInput = playerMovement.CurrentMoveInput;
            playerMoveSpeed = playerMovement.CurrentMoveSpeed;

            acceleration = Mathf.Max(0.01f, playerMovement.acceleration);
            turnAcceleration = Mathf.Max(0.01f, playerMovement.turnAcceleration);
        }

        Vector2 sourceDirection;

        if (playerVelocity.sqrMagnitude > 0.04f)
        {
            sourceDirection = playerVelocity.normalized;
        }
        else if (playerInput.sqrMagnitude > 0.04f)
        {
            sourceDirection = playerInput.normalized;
        }
        else
        {
            sourceDirection = Random.insideUnitCircle.normalized;

            if (sourceDirection.sqrMagnitude <= 0.001f)
                sourceDirection = Vector2.right;
        }

        // Start by splitting away from the player, then become independent.
        originalDirection = -sourceDirection;
        desiredDirection = RotateVector(
            originalDirection,
            Random.Range(-18f, 18f)
        ).normalized;

        float referenceSpeed = Mathf.Max(
            playerMoveSpeed * 0.82f,
            playerVelocity.magnitude * 0.9f,
            3.6f
        );

        targetSpeed = referenceSpeed * speedMultiplier;
        currentVelocity = desiredDirection * Mathf.Max(1.4f, targetSpeed * 0.55f);

        // A clone that stands still is a poor decoy. Even when spawned while
        // the player is idle it now chooses a natural route of its own.
        shouldMove = true;
        cloneActive = true;

        directionTimer = 0f;
        ScheduleNextDirectionChange();

        obstacleAvoidanceSide = Random.value < 0.5f ? -1 : 1;
        borderTangentSide = Random.value < 0.5f ? -1 : 1;
        stuckTimer = 0f;
        lastPosition = rb != null
            ? rb.position
            : (Vector2)transform.position;

        UpdateFacing(currentVelocity);
        StartCoroutine(CloneLifetimeRoutine(duration));
    }

    private void FixedUpdate()
    {
        if (!cloneActive ||
            !shouldMove ||
            rb == null ||
            cloneCollider == null ||
            Time.timeScale <= 0f)
        {
            return;
        }

        float delta = GetCloneDeltaTime();

        UpdateNaturalDirection(delta);
        ApplyHumanSteering();
        UpdateVelocity(delta);

        if (currentVelocity.sqrMagnitude <= 0.0001f)
        {
            HandleStuckCheck(delta);
            return;
        }

        float movementDistance = currentVelocity.magnitude * delta;

        Vector2 steeredDirection =
            EnemyObstacleSteering2D.GetSteeredDirection(
                cloneCollider,
                currentVelocity.normalized,
                desiredDirection,
                navigationFilter,
                avoidanceHits,
                avoidanceLookAhead,
                movementDistance,
                collisionSkin,
                obstacleAvoidanceAttempts,
                obstacleOutwardBias,
                ref obstacleAvoidanceSide
            );

        if (steeredDirection.sqrMagnitude > 0.001f)
        {
            float currentSpeed = currentVelocity.magnitude;

            currentVelocity = steeredDirection.normalized * currentSpeed;

            desiredDirection = Vector2.Lerp(
                desiredDirection,
                steeredDirection.normalized,
                0.58f
            ).normalized;

            rb.MovePosition(rb.position + currentVelocity * delta);
            UpdateFacing(currentVelocity);
        }
        else
        {
            TryImmediateEscape(delta);
        }

        HandleStuckCheck(delta);
    }

    private void UpdateNaturalDirection(float delta)
    {
        directionTimer += delta;

        if (directionTimer < nextDirectionChange)
            return;

        directionTimer = 0f;

        float randomAngle = Random.Range(
            -maximumTurnAngle,
            maximumTurnAngle
        );

        Vector2 naturalDirection = RotateVector(
            desiredDirection.sqrMagnitude > 0.001f
                ? desiredDirection
                : originalDirection,
            randomAngle
        ).normalized;

        float initialMemory = Mathf.Min(
            originalDirectionInfluence,
            0.12f
        );

        desiredDirection = Vector2.Lerp(
            naturalDirection,
            originalDirection,
            initialMemory
        ).normalized;

        ScheduleNextDirectionChange();
    }

    private void ApplyHumanSteering()
    {
        Vector2 steering = desiredDirection.sqrMagnitude > 0.001f
            ? desiredDirection.normalized
            : originalDirection;

        Vector2 playerAvoidance = GetPlayerSeparationSteering();
        Vector2 borderAvoidance = GetBorderSteering();

        steering += playerAvoidance;
        steering += borderAvoidance;

        if (steering.sqrMagnitude <= 0.001f)
            return;

        desiredDirection = Vector2.Lerp(
            desiredDirection,
            steering.normalized,
            strategicSteeringResponse
        ).normalized;
    }

    private Vector2 GetPlayerSeparationSteering()
    {
        if (realPlayer == null || playerSeparationStrength <= 0f)
            return Vector2.zero;

        Vector2 away = rb.position - (Vector2)realPlayer.position;
        float distance = away.magnitude;

        if (distance >= playerSeparationRadius)
            return Vector2.zero;

        if (distance <= 0.001f)
        {
            away = originalDirection.sqrMagnitude > 0.001f
                ? originalDirection
                : Vector2.right;
            distance = 0f;
        }

        float closeness = 1f - Mathf.Clamp01(
            distance / Mathf.Max(0.01f, playerSeparationRadius)
        );

        // Strong close-range push prevents the decoy from crossing directly
        // through the real player while enemies are aiming at the clone.
        float weight = playerSeparationStrength *
            Mathf.Lerp(closeness, closeness * closeness, 0.35f);

        return away.normalized * weight;
    }

    private Vector2 GetBorderSteering()
    {
        if (borderSteeringStrength <= 0f)
            return Vector2.zero;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return Vector2.zero;

        Vector3 viewport = mainCamera.WorldToViewportPoint(rb.position);
        float zone = Mathf.Clamp(borderSoftZone, 0.01f, 0.45f);

        Vector2 inward = Vector2.zero;
        float strongest = 0f;

        if (viewport.x < zone)
        {
            float w = 1f - Mathf.Clamp01(viewport.x / zone);
            inward.x += w;
            strongest = Mathf.Max(strongest, w);
        }
        else if (viewport.x > 1f - zone)
        {
            float w = 1f - Mathf.Clamp01((1f - viewport.x) / zone);
            inward.x -= w;
            strongest = Mathf.Max(strongest, w);
        }

        if (viewport.y < zone)
        {
            float w = 1f - Mathf.Clamp01(viewport.y / zone);
            inward.y += w;
            strongest = Mathf.Max(strongest, w);
        }
        else if (viewport.y > 1f - zone)
        {
            float w = 1f - Mathf.Clamp01((1f - viewport.y) / zone);
            inward.y -= w;
            strongest = Mathf.Max(strongest, w);
        }

        if (inward.sqrMagnitude <= 0.001f)
            return Vector2.zero;

        Vector2 inwardDirection = inward.normalized;
        Vector2 tangent = new Vector2(
            -inwardDirection.y,
            inwardDirection.x
        ) * borderTangentSide;

        // Curving along the edge first looks much more human than bouncing
        // straight back toward the centre of the screen.
        Vector2 curvedDirection = (
            inwardDirection + tangent * borderTangentAmount
        ).normalized;

        return curvedDirection *
            borderSteeringStrength *
            Mathf.Clamp01(strongest);
    }

    private void UpdateVelocity(float delta)
    {
        Vector2 targetVelocity = desiredDirection * targetSpeed;

        float angle = currentVelocity.sqrMagnitude > 0.001f
            ? Vector2.Angle(currentVelocity, targetVelocity)
            : 0f;

        float rate = angle > 35f
            ? turnAcceleration
            : acceleration;

        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            rate * delta
        );
    }

    private void TryImmediateEscape(float delta)
    {
        Vector2 escapeDirection = GetEscapeDirection();

        if (escapeDirection.sqrMagnitude <= 0.001f)
        {
            escapeDirection = GetBorderSteering();

            if (escapeDirection.sqrMagnitude <= 0.001f)
                escapeDirection = RotateVector(desiredDirection, 90f * obstacleAvoidanceSide);
        }

        if (escapeDirection.sqrMagnitude <= 0.001f)
            return;

        escapeDirection.Normalize();

        float escapeSpeed = Mathf.Max(targetSpeed, currentVelocity.magnitude);

        if (escapeSpeed <= 0.01f)
            return;

        currentVelocity = escapeDirection * escapeSpeed;
        desiredDirection = escapeDirection;

        rb.MovePosition(
            rb.position +
            escapeDirection *
            escapeSpeed *
            escapeSpeedMultiplier *
            delta
        );

        UpdateFacing(currentVelocity);
        ResetStuckCheck();
    }

    private void HandleStuckCheck(float delta)
    {
        stuckTimer += delta;

        if (stuckTimer < stuckCheckTime)
            return;

        float movedSqrDistance = (rb.position - lastPosition).sqrMagnitude;
        float stuckSqrDistance = stuckDistance * stuckDistance;

        if (movedSqrDistance < stuckSqrDistance)
        {
            Vector2 escapeDirection = GetEscapeDirection();

            if (escapeDirection.sqrMagnitude <= 0.001f)
            {
                escapeDirection = RotateVector(
                    desiredDirection.sqrMagnitude > 0.001f
                        ? desiredDirection
                        : Vector2.right,
                    Random.Range(75f, 130f) * obstacleAvoidanceSide
                ).normalized;
            }

            if (escapeDirection.sqrMagnitude > 0.001f)
            {
                float escapeSpeed = Mathf.Max(targetSpeed, 0.1f);

                currentVelocity = escapeDirection.normalized * escapeSpeed;
                desiredDirection = escapeDirection.normalized;

                rb.MovePosition(
                    rb.position +
                    desiredDirection *
                    escapeSpeed *
                    escapeSpeedMultiplier *
                    delta
                );

                obstacleAvoidanceSide *= -1;
                borderTangentSide *= -1;
            }
        }

        lastPosition = rb.position;
        stuckTimer = 0f;
    }

    private Vector2 GetEscapeDirection()
    {
        int hitCount = Physics2D.OverlapCircle(
            rb.position,
            escapeCheckRadius,
            navigationFilter,
            escapeHits
        );

        if (hitCount <= 0)
            return Vector2.zero;

        Vector2 escapeDirection = Vector2.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = escapeHits[i];

            if (hit == null || hit == cloneCollider)
                continue;

            if (hit.attachedRigidbody == rb)
                continue;

            Vector2 closestPoint = hit.ClosestPoint(rb.position);
            Vector2 awayFromObstacle = rb.position - closestPoint;

            if (awayFromObstacle.sqrMagnitude <= 0.001f)
            {
                awayFromObstacle = rb.position - (Vector2)hit.bounds.center;
            }

            if (awayFromObstacle.sqrMagnitude > 0.001f)
                escapeDirection += awayFromObstacle.normalized;
        }

        return escapeDirection.sqrMagnitude > 0.001f
            ? escapeDirection.normalized
            : Vector2.zero;
    }

    private void ResetStuckCheck()
    {
        stuckTimer = 0f;
        lastPosition = rb.position;
    }

    private void ScheduleNextDirectionChange()
    {
        float minimum = Mathf.Max(
            0.1f,
            Mathf.Min(directionChangeInterval.x, directionChangeInterval.y)
        );

        float maximum = Mathf.Max(
            minimum,
            Mathf.Max(directionChangeInterval.x, directionChangeInterval.y)
        );

        nextDirectionChange = Random.Range(minimum, maximum);
    }

    private static Vector2 RotateVector(Vector2 vector, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    private IEnumerator CloneLifetimeRoutine(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            UpdateVisual();
            yield return null;
        }

        StopMovement();
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null)
            return;

        Color color = spriteRenderer.color;

        color.a = Mathf.Lerp(
            minAlpha,
            maxAlpha,
            Mathf.PingPong(Time.time * blinkSpeed, 1f)
        );

        spriteRenderer.color = color;
    }

    private void UpdateFacing(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) <= 0.05f)
            return;

        Vector3 scale = transform.localScale;

        scale.x = direction.x > 0f
            ? Mathf.Abs(originalScale.x)
            : -Mathf.Abs(originalScale.x);

        transform.localScale = scale;
    }

    private float GetCloneDeltaTime()
    {
        if (Time.timeScale <= 0f)
            return Time.fixedDeltaTime;

        return Time.fixedDeltaTime / Time.timeScale;
    }

    private void StopMovement()
    {
        cloneActive = false;
        shouldMove = false;
        realPlayer = null;
        originalDirection = Vector2.zero;
        desiredDirection = Vector2.zero;
        currentVelocity = Vector2.zero;
        targetSpeed = 0f;
        directionTimer = 0f;
        nextDirectionChange = 0f;
        stuckTimer = 0f;
    }

    private void ClearClonedArmorVisual()
    {
        if (clonedArmorVisual == null)
            return;

        Destroy(clonedArmorVisual);
        clonedArmorVisual = null;
    }

    private void OnDisable()
    {
        StopMovement();
    }

    private void OnDestroy()
    {
        ClearClonedArmorVisual();
    }

    private void OnValidate()
    {
        minimumMovementSpeed = Mathf.Max(0f, minimumMovementSpeed);
        playerSeparationRadius = Mathf.Max(0.5f, playerSeparationRadius);
        avoidanceLookAhead = Mathf.Max(0.05f, avoidanceLookAhead);
        collisionSkin = Mathf.Max(0f, collisionSkin);
        stuckCheckTime = Mathf.Max(0.1f, stuckCheckTime);
        stuckDistance = Mathf.Max(0.001f, stuckDistance);
        escapeCheckRadius = Mathf.Max(0.1f, escapeCheckRadius);
        escapeSpeedMultiplier = Mathf.Max(1f, escapeSpeedMultiplier);

        if (Application.isPlaying)
            RebuildNavigationFilter();
    }
}
