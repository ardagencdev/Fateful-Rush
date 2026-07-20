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
    [Tooltip("Player hızı bunun altındaysa clone sabit kalır.")]
    public float minimumMovementSpeed = 0.1f;

    [Tooltip("Clone hızının player hızına oranı.")]
    [Range(0.5f, 1.5f)]
    public float speedMultiplier = 0.9f;

    [Tooltip("Player ayarları alınamazsa kullanılacak hızlanma.")]
    public float fallbackAcceleration = 55f;

    [Tooltip("Player ayarları alınamazsa kullanılacak dönüş hızlanması.")]
    public float fallbackTurnAcceleration = 90f;

    [Header("Natural Movement")]
    [Tooltip("Clone'un sakin bir şekilde yeni rota seçme aralığı.")]
    public Vector2 directionChangeInterval = new Vector2(0.75f, 1.25f);

    [Tooltip("Doğal rota değişimindeki maksimum açı.")]
    [Range(0f, 60f)]
    public float maximumTurnAngle = 18f;

    [Tooltip("Clone'un ilk kaçış yönünü ne kadar koruyacağı.")]
    [Range(0f, 1f)]
    public float originalDirectionInfluence = 0.35f;

    [Header("Collision Avoidance")]
    [Tooltip("Wall ve Obstacle layerlarını seç.")]
    public LayerMask solidLayers;

    [Tooltip("Clone'un duvarı kaç birim önceden fark edeceği.")]
    public float avoidanceLookAhead = 1.25f;

    [Tooltip("Duvar algılandığında yana dönüşün gücü.")]
    [Range(0f, 1f)]
    public float avoidanceStrength = 0.85f;

    [Tooltip("Collider ile yüzey arasında bırakılacak mesafe.")]
    public float collisionSkin = 0.04f;

    [Tooltip("Tek FixedUpdate içinde kaç kayma denemesi yapılacağı.")]
    [Range(1, 4)]
    public int movementIterations = 2;

    private Rigidbody2D rb;
    private Collider2D cloneCollider;

    private Vector2 originalDirection;
    private Vector2 desiredDirection;
    private Vector2 currentVelocity;

    private float targetSpeed;
    private float acceleration;
    private float turnAcceleration;
    private float directionTimer;
    private float nextDirectionChange;

    private bool cloneActive;
    private bool shouldMove;

    private Vector3 originalScale;

    private readonly RaycastHit2D[] castResults = new RaycastHit2D[16];
    private ContactFilter2D collisionFilter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cloneCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        originalScale = transform.localScale;

        if (originalScale == Vector3.zero)
            originalScale = Vector3.one;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.simulated = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        collisionFilter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = true
        };

        collisionFilter.SetLayerMask(solidLayers);
    }

    public void StartClone(float duration, PlayerMovement playerMovement)
    {
        StopAllCoroutines();

        Vector2 playerVelocity = Vector2.zero;
        Vector2 playerInput = Vector2.zero;
        float playerMoveSpeed = 0f;

        acceleration = fallbackAcceleration;
        turnAcceleration = fallbackTurnAcceleration;

        if (playerMovement != null)
        {
            playerVelocity = playerMovement.CurrentVelocity;
            playerInput = playerMovement.CurrentMoveInput;
            playerMoveSpeed = playerMovement.CurrentMoveSpeed;

            acceleration = Mathf.Max(0.01f, playerMovement.acceleration);
            turnAcceleration = Mathf.Max(0.01f, playerMovement.turnAcceleration);
        }

        if (playerVelocity.sqrMagnitude < 0.01f && playerInput.sqrMagnitude > 0.01f)
            playerVelocity = playerInput.normalized * playerMoveSpeed;

        float playerSpeed = playerVelocity.magnitude;
        shouldMove = playerSpeed >= minimumMovementSpeed;

        if (shouldMove)
        {
            originalDirection = -playerVelocity.normalized;
            desiredDirection = originalDirection;

            targetSpeed = Mathf.Max(playerSpeed, playerMoveSpeed * 0.7f) * speedMultiplier;

            // Player gibi bir anda tam hıza fırlamasın.
            currentVelocity = originalDirection * Mathf.Min(playerSpeed, targetSpeed) * 0.55f;

            directionTimer = 0f;
            ScheduleNextDirectionChange();
        }
        else
        {
            originalDirection = Vector2.zero;
            desiredDirection = Vector2.zero;
            currentVelocity = Vector2.zero;
            targetSpeed = 0f;
        }

        cloneActive = true;
        UpdateFacing(currentVelocity);
        StartCoroutine(CloneLifetimeRoutine(duration));
    }

    private void FixedUpdate()
    {
        if (!cloneActive || !shouldMove || rb == null || cloneCollider == null || Time.timeScale <= 0f)
            return;

        float delta = GetCloneDeltaTime();

        UpdateNaturalDirection(delta);
        ApplyObstacleAvoidance();
        UpdateVelocity(delta);

        if (currentVelocity.sqrMagnitude <= 0.0001f)
            return;

        MoveWithSliding(currentVelocity * delta);
        UpdateFacing(currentVelocity);
    }

    private void UpdateNaturalDirection(float delta)
    {
        directionTimer += delta;

        if (directionTimer < nextDirectionChange)
            return;

        directionTimer = 0f;

        float randomAngle = Random.Range(-maximumTurnAngle, maximumTurnAngle);
        Vector2 naturalDirection = RotateVector(desiredDirection, randomAngle).normalized;

        desiredDirection = Vector2.Lerp(
            naturalDirection,
            originalDirection,
            originalDirectionInfluence
        ).normalized;

        ScheduleNextDirectionChange();
    }

    private void ApplyObstacleAvoidance()
    {
        Vector2 moveDirection = currentVelocity.sqrMagnitude > 0.001f
            ? currentVelocity.normalized
            : desiredDirection;

        if (moveDirection.sqrMagnitude <= 0.001f)
            return;

        int hitCount = cloneCollider.Cast(
            moveDirection,
            collisionFilter,
            castResults,
            avoidanceLookAhead
        );

        RaycastHit2D? closestHit = FindClosestValidHit(hitCount);

        if (!closestHit.HasValue)
            return;

        RaycastHit2D hit = closestHit.Value;

        Vector2 tangentA = new Vector2(-hit.normal.y, hit.normal.x);
        Vector2 tangentB = -tangentA;

        // Mevcut rotaya en yakın olan duvar paralelini seç.
        Vector2 chosenTangent = Vector2.Dot(tangentA, moveDirection) >=
                                Vector2.Dot(tangentB, moveDirection)
            ? tangentA
            : tangentB;

        float proximity = 1f - Mathf.Clamp01(hit.distance / Mathf.Max(avoidanceLookAhead, 0.01f));
        float steerAmount = Mathf.Clamp01(avoidanceStrength * (0.35f + proximity));

        // Bir miktar yüzey normalini de ekleyerek duvardan uzaklaşmasını sağla.
        Vector2 avoidanceDirection = (chosenTangent + hit.normal * 0.35f).normalized;

        desiredDirection = Vector2.Lerp(
            desiredDirection,
            avoidanceDirection,
            steerAmount
        ).normalized;
    }

    private void UpdateVelocity(float delta)
    {
        Vector2 targetVelocity = desiredDirection * targetSpeed;

        float angle = currentVelocity.sqrMagnitude > 0.001f
            ? Vector2.Angle(currentVelocity, targetVelocity)
            : 0f;

        float rate = angle > 35f ? turnAcceleration : acceleration;

        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            rate * delta
        );
    }

    private void MoveWithSliding(Vector2 displacement)
    {
        Vector2 remaining = displacement;
        Vector2 position = rb.position;

        int iterations = Mathf.Max(1, movementIterations);

        for (int i = 0; i < iterations; i++)
        {
            float distance = remaining.magnitude;

            if (distance <= 0.0001f)
                break;

            Vector2 direction = remaining / distance;

            int hitCount = cloneCollider.Cast(
                direction,
                collisionFilter,
                castResults,
                distance + collisionSkin
            );

            RaycastHit2D? closestHit = FindClosestValidHit(hitCount);

            if (!closestHit.HasValue)
            {
                position += remaining;
                remaining = Vector2.zero;
                break;
            }

            RaycastHit2D hit = closestHit.Value;
            float safeDistance = Mathf.Max(0f, hit.distance - collisionSkin);

            position += direction * safeDistance;

            float unusedDistance = Mathf.Max(0f, distance - safeDistance);
            Vector2 slideDirection = Vector2.Perpendicular(hit.normal);

            if (Vector2.Dot(slideDirection, direction) < 0f)
                slideDirection = -slideDirection;

            remaining = slideDirection * unusedDistance;

            // Top gibi geri sekmek yerine hızın duvara giren kısmını sil.
            currentVelocity -= Vector2.Dot(currentVelocity, hit.normal) * hit.normal;

            if (currentVelocity.sqrMagnitude > 0.001f)
                desiredDirection = currentVelocity.normalized;
            else
                desiredDirection = (slideDirection + hit.normal * 0.25f).normalized;
        }

        rb.MovePosition(position);
    }

    private RaycastHit2D? FindClosestValidHit(int hitCount)
    {
        bool foundHit = false;
        RaycastHit2D closestHit = default;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = castResults[i];

            if (hit.collider == null || hit.collider == cloneCollider)
                continue;

            if (!foundHit || hit.distance < closestHit.distance)
            {
                closestHit = hit;
                foundHit = true;
            }
        }

        return foundHit ? closestHit : null;
    }

    private void ScheduleNextDirectionChange()
    {
        float minimum = Mathf.Min(directionChangeInterval.x, directionChangeInterval.y);
        float maximum = Mathf.Max(directionChangeInterval.x, directionChangeInterval.y);
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
        originalDirection = Vector2.zero;
        desiredDirection = Vector2.zero;
        currentVelocity = Vector2.zero;
        targetSpeed = 0f;
        directionTimer = 0f;
        nextDirectionChange = 0f;
    }

    private void OnDisable()
    {
        StopMovement();
    }
}