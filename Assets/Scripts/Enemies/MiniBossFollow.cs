using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MiniBossFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public float speed = 2.5f;
    public float shakeAmount = 0.03f;

    [Tooltip("Hedef yönündeki değişimlerin ne kadar yumuşak olacağı.")]
    public float directionSmoothness = 8f;

    [Header("Wave Movement")]
    public float minSideMoveAmount = 0.18f;
    public float maxSideMoveAmount = 0.35f;
    public float minSideMoveSpeed = 1.5f;
    public float maxSideMoveSpeed = 3f;

    [Header("Collision")]
    public LayerMask solidLayers;
    public float castSkin = 0.05f;

    [Tooltip("Düz yol kapalıysa kaç farklı kayma açısı denenecek.")]
    [Range(1, 8)]
    public int slideDirectionAttempts = 4;

    [Header("Advanced Unstuck")]
    public LayerMask obstacleLayer;
    public float escapeCheckRadius = 1.2f;
    public float escapeSpeedMultiplier = 2.2f;

    [Header("Stuck Fix")]
    public float stuckCheckTime = 0.5f;
    public float stuckDistance = 0.08f;
    public float unstuckDuration = 0.5f;
    public float unstuckSideForce = 1.5f;

    [Header("Clone Targeting")]
    [Tooltip("Bu Mini Boss, Void Clone tarafından hedef olarak seçilebilir mi?")]
    public bool canTargetClone;

    private float movementOffset;
    private float sideMoveAmount;
    private float sideMoveSpeed;

    private Rigidbody2D rb;
    private Collider2D col;

    private PlayerMovement playerMovement;

    private Vector3 originalScale;
    private Vector2 lastPosition;
    private Vector2 smoothedDirection;

    private float stuckTimer;
    private float unstuckTimer;

    private int unstuckDirection = 1;

    private bool stopped;

    private ContactFilter2D solidFilter;

    private readonly RaycastHit2D[] castHits =
        new RaycastHit2D[8];

    private readonly Collider2D[] escapeHits =
        new Collider2D[16];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        originalScale = transform.localScale;

        if (originalScale == Vector3.zero)
            originalScale = Vector3.one;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;

        rb.bodyType =
            RigidbodyType2D.Dynamic;

        solidFilter =
            new ContactFilter2D();

        solidFilter.SetLayerMask(
            solidLayers
        );

        solidFilter.useLayerMask = true;
        solidFilter.useTriggers = false;
    }

    private void Start()
    {
        FindPlayerIfNeeded();

        movementOffset =
            Random.Range(0f, 100f);

        float sideMagnitude =
            Random.Range(
                minSideMoveAmount,
                maxSideMoveAmount
            );

        sideMoveAmount =
            Random.value < 0.5f
                ? -sideMagnitude
                : sideMagnitude;

        sideMoveSpeed =
            Random.Range(
                minSideMoveSpeed,
                maxSideMoveSpeed
            );

        unstuckDirection =
            Random.Range(0, 2) == 0
                ? -1
                : 1;

        lastPosition = rb.position;
    }

    private void FixedUpdate()
    {
        if (stopped)
            return;

        FindPlayerIfNeeded();

        if (rb == null || player == null)
            return;

        if (playerMovement != null &&
            playerMovement.IsGameOver)
        {
            StopMiniBoss();
            return;
        }

        MoveMiniBoss();
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
        {
            if (playerMovement == null)
            {
                playerMovement =
                    player.GetComponent<PlayerMovement>();
            }

            return;
        }

        GameObject foundPlayer =
            GameObject.FindGameObjectWithTag("Player");

        if (foundPlayer == null)
            return;

        player = foundPlayer.transform;

        playerMovement =
            foundPlayer.GetComponent<PlayerMovement>();
    }

    private Transform GetCurrentTarget()
    {
        if (canTargetClone &&
            VoidCloneAbility.ActiveCloneTarget != null)
        {
            return VoidCloneAbility.ActiveCloneTarget;
        }

        return player;
    }

    private void MoveMiniBoss()
    {
        Transform currentTarget = GetCurrentTarget();

        if (currentTarget == null)
        {
            ResetStuckCheck();
            return;
        }

        Vector2 toPlayer =
            (Vector2)currentTarget.position -
            rb.position;

        if (toPlayer.sqrMagnitude <= 0.001f)
        {
            ResetStuckCheck();
            return;
        }

        Vector2 targetDirection =
            toPlayer.normalized;

        smoothedDirection =
            Vector2.Lerp(
                smoothedDirection == Vector2.zero
                    ? targetDirection
                    : smoothedDirection,
                targetDirection,
                directionSmoothness *
                Time.fixedDeltaTime
            ).normalized;

        FlipSprite(smoothedDirection);

        Vector2 waveSideDirection =
            GetPerpendicularDirection(
                smoothedDirection,
                1
            );

        float wave =
            Mathf.Sin(
                (Time.time +
                 movementOffset) *
                sideMoveSpeed
            ) *
            sideMoveAmount;

        Vector2 movementDirection =
            (
                smoothedDirection +
                waveSideDirection *
                wave
            ).normalized;

        if (unstuckTimer > 0f)
        {
            unstuckTimer -=
                Time.fixedDeltaTime;

            Vector2 escapeSideDirection =
                GetPerpendicularDirection(
                    movementDirection,
                    unstuckDirection
                );

            movementDirection =
                (
                    movementDirection +
                    escapeSideDirection *
                    unstuckSideForce
                ).normalized;
        }

        bool moved =
            MoveWithCollision(
                movementDirection
            );

        HandleStuckCheck(moved);
    }

    private bool MoveWithCollision(
        Vector2 direction
    )
    {
        if (direction.sqrMagnitude <= 0.001f)
            return false;

        Vector2 intendedMovement =
            direction *
            speed *
            Time.fixedDeltaTime;

        Vector2 shakeOffset =
            new Vector2(
                Random.Range(
                    -shakeAmount,
                    shakeAmount
                ),
                Random.Range(
                    -shakeAmount,
                    shakeAmount
                )
            );

        Vector2 finalMovement =
            intendedMovement +
            shakeOffset;

        if (CanMove(finalMovement))
        {
            rb.MovePosition(
                rb.position +
                finalMovement
            );

            return true;
        }

        if (TrySlideAroundObstacle(
                direction,
                intendedMovement.magnitude))
        {
            return true;
        }

        unstuckDirection *= -1;

        return false;
    }

    private bool TrySlideAroundObstacle(
        Vector2 forwardDirection,
        float movementDistance
    )
    {
        if (movementDistance <= 0f)
            return false;

        Vector2 leftDirection =
            GetPerpendicularDirection(
                forwardDirection,
                1
            );

        Vector2 rightDirection =
            GetPerpendicularDirection(
                forwardDirection,
                -1
            );

        for (int attempt = 0;
             attempt < slideDirectionAttempts;
             attempt++)
        {
            float blend =
                (attempt + 1f) /
                slideDirectionAttempts;

            Vector2 preferredSide =
                unstuckDirection > 0
                    ? leftDirection
                    : rightDirection;

            Vector2 oppositeSide =
                unstuckDirection > 0
                    ? rightDirection
                    : leftDirection;

            Vector2 firstDirection =
                Vector2.Lerp(
                    forwardDirection,
                    preferredSide,
                    blend
                ).normalized;

            Vector2 firstMovement =
                firstDirection *
                movementDistance;

            if (CanMove(firstMovement))
            {
                rb.MovePosition(
                    rb.position +
                    firstMovement
                );

                return true;
            }

            Vector2 secondDirection =
                Vector2.Lerp(
                    forwardDirection,
                    oppositeSide,
                    blend
                ).normalized;

            Vector2 secondMovement =
                secondDirection *
                movementDistance;

            if (CanMove(secondMovement))
            {
                rb.MovePosition(
                    rb.position +
                    secondMovement
                );

                unstuckDirection *= -1;

                return true;
            }
        }

        return false;
    }

    private bool CanMove(
        Vector2 movement
    )
    {
        if (col == null)
            return true;

        if (movement.sqrMagnitude <= 0.001f)
            return true;

        int hitCount =
            col.Cast(
                movement.normalized,
                solidFilter,
                castHits,
                movement.magnitude +
                Mathf.Max(castSkin, 0f)
            );

        for (int i = 0;
             i < hitCount;
             i++)
        {
            Collider2D hitCollider =
                castHits[i].collider;

            if (hitCollider == null)
                continue;

            if (hitCollider == col)
                continue;

            return false;
        }

        return true;
    }

    private void HandleStuckCheck(
        bool attemptedMove
    )
    {
        stuckTimer +=
            Time.fixedDeltaTime;

        if (stuckTimer < stuckCheckTime)
            return;

        float movedDistanceSqr =
            (rb.position - lastPosition)
            .sqrMagnitude;

        float requiredDistanceSqr =
            stuckDistance *
            stuckDistance;

        if (movedDistanceSqr <
            requiredDistanceSqr)
        {
            Vector2 escapeDirection =
                GetEscapeDirection();

            if (escapeDirection.sqrMagnitude <=
                0.001f)
            {
                Transform currentTarget =
    GetCurrentTarget();

                Vector2 playerDirection =
                    currentTarget != null
                        ? ((Vector2)currentTarget.position -
                           rb.position).normalized
                        : Vector2.right;

                Vector2 sideDirection =
                    GetPerpendicularDirection(
                        playerDirection,
                        unstuckDirection
                    );

                escapeDirection =
                    (
                        sideDirection +
                        Random.insideUnitCircle *
                        0.35f
                    ).normalized;
            }

            Vector2 escapeMovement =
                escapeDirection *
                speed *
                escapeSpeedMultiplier *
                Time.fixedDeltaTime;

            if (CanMove(escapeMovement))
            {
                rb.MovePosition(
                    rb.position +
                    escapeMovement
                );
            }
            else
            {
                unstuckDirection *= -1;
            }

            unstuckTimer =
                unstuckDuration;
        }

        lastPosition =
            rb.position;

        stuckTimer = 0f;
    }

    private void ResetStuckCheck()
    {
        stuckTimer = 0f;
        lastPosition = rb.position;
    }

    private Vector2 GetEscapeDirection()
    {
        ContactFilter2D obstacleFilter =
            new ContactFilter2D();

        obstacleFilter.SetLayerMask(
            obstacleLayer |
            solidLayers
        );

        obstacleFilter.useLayerMask = true;
        obstacleFilter.useTriggers = true;

        int hitCount =
            Physics2D.OverlapCircle(
                rb.position,
                escapeCheckRadius,
                obstacleFilter,
                escapeHits
            );

        if (hitCount <= 0)
            return Vector2.zero;

        Vector2 escapeDirection =
            Vector2.zero;

        int validHitCount = 0;

        for (int i = 0;
             i < hitCount;
             i++)
        {
            Collider2D hit =
                escapeHits[i];

            if (hit == null ||
                hit == col)
            {
                continue;
            }

            Vector2 closestPoint =
                hit.ClosestPoint(
                    rb.position
                );

            Vector2 awayFromObstacle =
                rb.position -
                closestPoint;

            if (awayFromObstacle.sqrMagnitude <=
                0.001f)
            {
                awayFromObstacle =
                    rb.position -
                    (Vector2)hit.bounds.center;
            }

            if (awayFromObstacle.sqrMagnitude <=
                0.001f)
            {
                continue;
            }

            float distance =
                awayFromObstacle.magnitude;

            float weight =
                1f /
                Mathf.Max(
                    distance,
                    0.05f
                );

            escapeDirection +=
                awayFromObstacle.normalized *
                weight;

            validHitCount++;
        }

        if (validHitCount <= 0 ||
            escapeDirection.sqrMagnitude <=
            0.001f)
        {
            return Vector2.zero;
        }

        return escapeDirection.normalized;
    }

    private Vector2 GetPerpendicularDirection(
        Vector2 direction,
        int side
    )
    {
        return new Vector2(
            -direction.y,
            direction.x
        ) * side;
    }

    private void FlipSprite(
        Vector2 direction
    )
    {
        if (Mathf.Abs(direction.x) <=
            0.01f)
        {
            return;
        }

        Vector3 scale =
            transform.localScale;

        float absoluteX =
            Mathf.Abs(originalScale.x);

        scale.x =
            direction.x > 0f
                ? absoluteX
                : -absoluteX;

        transform.localScale = scale;
    }

    private void StopMiniBoss()
    {
        if (stopped)
            return;

        stopped = true;

        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;

            rb.angularVelocity = 0f;
        }

        enabled = false;
    }
}