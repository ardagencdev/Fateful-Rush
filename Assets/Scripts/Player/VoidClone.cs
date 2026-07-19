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

    [Tooltip("Clone'un yön değişim yumuşaklığı.")]
    public float turnSpeed = 5f;

    [Header("Natural Movement")]
    [Tooltip("Clone'un kaç saniyede bir yeni yön seçebileceği.")]
    public Vector2 directionChangeInterval =
        new Vector2(0.3f, 0.65f);

    [Tooltip("Her yön seçiminde yapabileceği maksimum dönüş.")]
    [Range(0f, 90f)]
    public float maximumTurnAngle = 30f;

    [Tooltip("İlk ters rotadan tamamen kopmasını engeller.")]
    [Range(0f, 1f)]
    public float originalDirectionInfluence = 0.25f;

    [Header("Collision")]
    [Tooltip("Wall ve Obstacle layerlarını seç.")]
    public LayerMask solidLayers;

    [Tooltip("Collider ile yüzey arasında bırakılacak mesafe.")]
    public float collisionSkin = 0.04f;

    [Tooltip("Duvara çarptıktan sonra yeni yönüne eklenecek rastgele açı.")]
    [Range(0f, 45f)]
    public float bounceRandomAngle = 15f;

    private Rigidbody2D rb;
    private Collider2D cloneCollider;

    private Vector2 originalOppositeDirection;
    private Vector2 desiredDirection;
    private Vector2 currentDirection;

    private float movementSpeed;
    private float directionTimer;
    private float nextDirectionChange;

    private bool cloneActive;
    private bool shouldMove;

    private Vector3 originalScale;

    private readonly RaycastHit2D[] castResults =
        new RaycastHit2D[16];

    private ContactFilter2D collisionFilter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cloneCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        originalScale = transform.localScale;

        if (originalScale == Vector3.zero)
            originalScale = Vector3.one;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.simulated = true;

        rb.constraints =
            RigidbodyConstraints2D.FreezeRotation;

        rb.interpolation =
            RigidbodyInterpolation2D.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;

        collisionFilter = new ContactFilter2D
        {
            useLayerMask = true,

            // Wall collider Is Trigger olsa bile algılansın.
            useTriggers = true
        };

        collisionFilter.SetLayerMask(solidLayers);
    }

    public void StartClone(
        float duration,
        Vector2 playerVelocity
    )
    {
        StopAllCoroutines();

        float playerSpeed =
            playerVelocity.magnitude;

        shouldMove =
            playerSpeed >= minimumMovementSpeed;

        if (shouldMove)
        {
            originalOppositeDirection =
                -playerVelocity.normalized;

            currentDirection =
                originalOppositeDirection;

            desiredDirection =
                originalOppositeDirection;

            movementSpeed =
                playerSpeed * speedMultiplier;

            ScheduleNextDirectionChange();
        }
        else
        {
            originalOppositeDirection =
                Vector2.zero;

            currentDirection =
                Vector2.zero;

            desiredDirection =
                Vector2.zero;

            movementSpeed = 0f;
        }

        cloneActive = true;

        UpdateFacing(currentDirection);

        StartCoroutine(
            CloneLifetimeRoutine(duration)
        );
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

        UpdateNaturalDirection();

        currentDirection =
            Vector2.Lerp(
                currentDirection,
                desiredDirection,
                turnSpeed * Time.fixedDeltaTime
            ).normalized;

        if (currentDirection.sqrMagnitude <= 0.001f)
            return;

        float moveDistance =
            movementSpeed * Time.fixedDeltaTime;

        MoveSafely(
            currentDirection,
            moveDistance
        );

        UpdateFacing(currentDirection);
    }

    private void UpdateNaturalDirection()
    {
        directionTimer +=
            Time.fixedDeltaTime;

        if (directionTimer <
            nextDirectionChange)
        {
            return;
        }

        directionTimer = 0f;

        float randomAngle =
            Random.Range(
                -maximumTurnAngle,
                maximumTurnAngle
            );

        Vector2 turnedDirection =
            RotateVector(
                currentDirection,
                randomAngle
            );

        /*
         * Clone rastgele yön değiştirir fakat sürekli
         * ilk ters rotaya çekilmez. Yalnızca küçük bir
         * başlangıç yönü etkisi korunur.
         */
        desiredDirection =
            Vector2.Lerp(
                turnedDirection,
                originalOppositeDirection,
                originalDirectionInfluence
            ).normalized;

        ScheduleNextDirectionChange();
    }

    private void ScheduleNextDirectionChange()
    {
        float minimum =
            Mathf.Min(
                directionChangeInterval.x,
                directionChangeInterval.y
            );

        float maximum =
            Mathf.Max(
                directionChangeInterval.x,
                directionChangeInterval.y
            );

        nextDirectionChange =
            Random.Range(
                minimum,
                maximum
            );
    }

    private void MoveSafely(
        Vector2 direction,
        float distance
    )
    {
        int hitCount =
            cloneCollider.Cast(
                direction,
                collisionFilter,
                castResults,
                distance + collisionSkin
            );

        RaycastHit2D? closestHit =
            FindClosestValidHit(hitCount);

        if (!closestHit.HasValue)
        {
            rb.MovePosition(
                rb.position +
                direction * distance
            );

            return;
        }

        RaycastHit2D hit =
            closestHit.Value;

        float safeDistance =
            Mathf.Max(
                0f,
                hit.distance - collisionSkin
            );

        if (safeDistance > 0f)
        {
            rb.MovePosition(
                rb.position +
                direction * safeDistance
            );
        }
        else
        {
            /*
             * Collider yüzeye aşırı yakınsa clone'u
             * yüzeyden çok az dışarı iter.
             */
            rb.MovePosition(
                rb.position +
                hit.normal * collisionSkin
            );
        }

        BounceFromSurface(
            hit.normal
        );
    }

    private RaycastHit2D? FindClosestValidHit(
        int hitCount
    )
    {
        bool foundHit = false;
        RaycastHit2D closestHit = default;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit =
                castResults[i];

            if (hit.collider == null)
                continue;

            if (hit.collider == cloneCollider)
                continue;

            if (!foundHit ||
                hit.distance < closestHit.distance)
            {
                closestHit = hit;
                foundHit = true;
            }
        }

        return foundHit
            ? closestHit
            : null;
    }

    private void BounceFromSurface(
        Vector2 surfaceNormal
    )
    {
        Vector2 reflectedDirection =
            Vector2.Reflect(
                currentDirection,
                surfaceNormal
            ).normalized;

        float randomAngle =
            Random.Range(
                -bounceRandomAngle,
                bounceRandomAngle
            );

        reflectedDirection =
            RotateVector(
                reflectedDirection,
                randomAngle
            ).normalized;

        /*
         * Yansıyan yön hâlâ duvarın içine bakıyorsa
         * normal yönüne doğru düzelt.
         */
        if (Vector2.Dot(
                reflectedDirection,
                surfaceNormal
            ) <= 0.05f)
        {
            reflectedDirection =
                Vector2.Lerp(
                    reflectedDirection,
                    surfaceNormal,
                    0.5f
                ).normalized;
        }

        currentDirection =
            reflectedDirection;

        desiredDirection =
            reflectedDirection;

        /*
         * Duvara çarpınca bundan sonraki doğal hareket
         * yeni rotanın çevresinde devam etsin.
         */
        originalOppositeDirection =
            Vector2.Lerp(
                originalOppositeDirection,
                reflectedDirection,
                0.65f
            ).normalized;

        directionTimer = 0f;
        ScheduleNextDirectionChange();
    }

    private static Vector2 RotateVector(
        Vector2 vector,
        float angle
    )
    {
        float radians =
            angle * Mathf.Deg2Rad;

        float sin =
            Mathf.Sin(radians);

        float cos =
            Mathf.Cos(radians);

        return new Vector2(
            vector.x * cos -
            vector.y * sin,

            vector.x * sin +
            vector.y * cos
        );
    }

    private IEnumerator CloneLifetimeRoutine(
        float duration
    )
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

        Color color =
            spriteRenderer.color;

        color.a =
            Mathf.Lerp(
                minAlpha,
                maxAlpha,
                Mathf.PingPong(
                    Time.time * blinkSpeed,
                    1f
                )
            );

        spriteRenderer.color =
            color;
    }

    private void UpdateFacing(
        Vector2 direction
    )
    {
        if (Mathf.Abs(direction.x) <= 0.05f)
            return;

        Vector3 scale =
            transform.localScale;

        scale.x =
            direction.x > 0f
                ? Mathf.Abs(originalScale.x)
                : -Mathf.Abs(originalScale.x);

        transform.localScale =
            scale;
    }

    private void StopMovement()
    {
        cloneActive = false;
        shouldMove = false;

        originalOppositeDirection =
            Vector2.zero;

        desiredDirection =
            Vector2.zero;

        currentDirection =
            Vector2.zero;

        movementSpeed = 0f;
        directionTimer = 0f;
        nextDirectionChange = 0f;
    }

    private void OnDisable()
    {
        StopMovement();
    }
}