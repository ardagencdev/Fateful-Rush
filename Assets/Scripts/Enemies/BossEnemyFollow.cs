using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BossEnemyFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public float speed = 1.2f;

    [Tooltip("Bossun hareket ederken yaptığı görsel titreşim.")]
    public float shakeAmount = 0.04f;

    [Tooltip("Hedef yönüne dönüşün ne kadar yumuşak olacağı.")]
    public float directionSmoothness = 7f;

    [Header("Collision")]
    public LayerMask solidLayers;
    public float castSkin = 0.05f;

    [Tooltip("Doğrudan hareket mümkün değilse kaç farklı kayma açısı denenecek.")]
    [Range(1, 8)]
    public int slideDirectionAttempts = 4;

    [Header("Advanced Unstuck")]
    public LayerMask obstacleLayer;
    public float escapeCheckRadius = 1.2f;
    public float escapeSpeedMultiplier = 2.2f;

    [Header("Split Settings")]
    public GameObject miniBossPrefab;
    public bool canSplit = true;
    public float miniBossSpeed = 2.5f;
    public float splitDelay = 0.8f;
    public float splitDistance = 1.2f;
    public float splitShakeAmount = 0.18f;
    public Color splitFlashColor =
        new Color(0.45f, 0f, 0f, 1f);
    public float flashSpeed = 0.08f;

    [Header("Split Visual")]
    public float splitScaleMin = 0.92f;
    public float splitScaleMax = 1.08f;

    [Tooltip("Split bittiğinde Boss küçülerek kaybolur.")]
    public float splitDisappearDuration = 0.12f;

    [Header("Stuck Fix")]
    public float stuckCheckTime = 0.5f;
    public float stuckDistance = 0.08f;
    public float unstuckDuration = 0.5f;
    public float unstuckSideForce = 1.5f;

    private PlayerMovement playerMovement;
    private PlayerArmor playerArmor;

    private Rigidbody2D rb;
    private Collider2D bossCollider;
    private SpriteRenderer spriteRenderer;

    private Vector2 lastPosition;
    private Vector2 smoothedDirection;

    private float stuckTimer;
    private float unstuckTimer;

    private int unstuckDirection = 1;

    private bool isSplitting;
    private bool stopped;

    private Color originalColor;
    private Vector3 originalScale;

    private ContactFilter2D solidFilter;

    private readonly RaycastHit2D[] castHits =
        new RaycastHit2D[8];

    private readonly Collider2D[] escapeHits =
        new Collider2D[16];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<Collider2D>();

        spriteRenderer =
            GetComponentInChildren<SpriteRenderer>();

        originalScale = transform.localScale;

        if (originalScale == Vector3.zero)
            originalScale = Vector3.one;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;

        solidFilter = new ContactFilter2D();
        solidFilter.SetLayerMask(solidLayers);
        solidFilter.useLayerMask = true;
        solidFilter.useTriggers = false;
    }

    private void Start()
    {
        FindPlayerIfNeeded();

        lastPosition = rb.position;

        unstuckDirection =
            Random.Range(0, 2) == 0 ? -1 : 1;
    }

    private void FixedUpdate()
    {
        if (stopped || isSplitting)
            return;

        FindPlayerIfNeeded();

        if (rb == null || player == null)
            return;

        if (playerMovement != null &&
            playerMovement.IsGameOver)
        {
            StopBoss();
            return;
        }

        MoveBoss();
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

            if (playerArmor == null)
            {
                playerArmor =
                    player.GetComponent<PlayerArmor>();
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

        playerArmor =
            foundPlayer.GetComponent<PlayerArmor>();
    }

    private void MoveBoss()
    {
        Vector2 toPlayer =
            (Vector2)player.position - rb.position;

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

        Vector2 finalDirection =
            smoothedDirection;

        if (unstuckTimer > 0f)
        {
            unstuckTimer -=
                Time.fixedDeltaTime;

            Vector2 sideDirection =
                GetPerpendicularDirection(
                    smoothedDirection,
                    unstuckDirection
                );

            finalDirection =
                (
                    smoothedDirection +
                    sideDirection *
                    unstuckSideForce
                ).normalized;
        }

        bool moved =
            MoveWithCollision(finalDirection);

        HandleStuckCheck(moved);
    }

    private bool MoveWithCollision(
        Vector2 direction
    )
    {
        if (direction.sqrMagnitude <= 0.001f)
            return false;

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

        Vector2 intendedMovement =
            direction *
            speed *
            Time.fixedDeltaTime;

        Vector2 movement =
            intendedMovement +
            shakeOffset;

        if (CanMove(movement))
        {
            rb.MovePosition(
                rb.position + movement
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

            Vector2 firstSide =
                unstuckDirection > 0
                    ? leftDirection
                    : rightDirection;

            Vector2 secondSide =
                unstuckDirection > 0
                    ? rightDirection
                    : leftDirection;

            Vector2 firstDirection =
                Vector2.Lerp(
                    forwardDirection,
                    firstSide,
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
                    secondSide,
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

    private bool CanMove(Vector2 movement)
    {
        if (bossCollider == null)
            return true;

        if (movement.sqrMagnitude <= 0.001f)
            return true;

        int hitCount =
            bossCollider.Cast(
                movement.normalized,
                solidFilter,
                castHits,
                movement.magnitude +
                Mathf.Max(castSkin, 0f)
            );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider =
                castHits[i].collider;

            if (hitCollider == null)
                continue;

            if (hitCollider == bossCollider)
                continue;

            return false;
        }

        return true;
    }

    private void HandleStuckCheck(bool attemptedMove)
    {
        if (!attemptedMove)
        {
            stuckTimer +=
                Time.fixedDeltaTime;
        }
        else
        {
            stuckTimer +=
                Time.fixedDeltaTime;
        }

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
                Vector2 playerDirection =
                    player != null
                        ? ((Vector2)player.position -
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

        lastPosition = rb.position;
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
                hit == bossCollider)
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
                Mathf.Max(distance, 0.05f);

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

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        if (stopped || isSplitting)
            return;

        if (collision == null)
            return;

        GameObject playerObject =
            FindPlayerObjectInParents(
                collision.gameObject
            );

        if (playerObject == null)
            return;

        if (playerMovement == null)
        {
            playerMovement =
                playerObject.GetComponent<PlayerMovement>();
        }

        if (playerArmor == null)
        {
            playerArmor =
                playerObject.GetComponent<PlayerArmor>();
        }

        if (playerMovement == null ||
            playerMovement.IsGameOver)
        {
            return;
        }

        if (playerArmor != null &&
            playerArmor.IsImmune)
        {
            return;
        }

        if (playerArmor != null &&
            playerArmor.HasArmor)
        {
            playerArmor.BreakArmor();

            if (canSplit)
            {
                StartCoroutine(
                    SplitRoutine()
                );
            }
            else
            {
                Destroy(gameObject);
            }

            return;
        }

        StopBossMovement();

        playerMovement.GameOver("BOSS");
    }

    private GameObject FindPlayerObjectInParents(
        GameObject hitObject
    )
    {
        if (hitObject == null)
            return null;

        Transform current =
            hitObject.transform;

        while (current != null)
        {
            if (current.CompareTag("Player"))
                return current.gameObject;

            current = current.parent;
        }

        return null;
    }

    private IEnumerator SplitRoutine()
    {
        if (isSplitting || stopped)
            yield break;

        isSplitting = true;

        Vector3 splitCenter =
            transform.position;

        Vector3 splitStartScale =
            transform.localScale;

        Color splitStartColor =
            spriteRenderer != null
                ? spriteRenderer.color
                : originalColor;

        if (bossCollider != null)
            bossCollider.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;

            rb.angularVelocity = 0f;

            rb.bodyType =
                RigidbodyType2D.Kinematic;
        }

        float safeFlashSpeed =
            Mathf.Max(flashSpeed, 0.01f);

        float timer = 0f;

        while (timer < splitDelay)
        {
            if (playerMovement != null &&
                playerMovement.IsGameOver)
            {
                StopBoss();
                yield break;
            }

            timer += Time.deltaTime;

            Vector3 shakeOffset =
                new Vector3(
                    Random.Range(
                        -splitShakeAmount,
                        splitShakeAmount
                    ),
                    Random.Range(
                        -splitShakeAmount,
                        splitShakeAmount
                    ),
                    0f
                );

            transform.position =
                splitCenter +
                shakeOffset;

            if (spriteRenderer != null)
            {
                float flash =
                    Mathf.PingPong(
                        timer /
                        safeFlashSpeed,
                        1f
                    );

                spriteRenderer.color =
                    Color.Lerp(
                        splitStartColor,
                        splitFlashColor,
                        flash
                    );
            }

            float scaleJitter =
                Random.Range(
                    splitScaleMin,
                    splitScaleMax
                );

            transform.localScale =
                splitStartScale *
                scaleJitter;

            yield return null;
        }

        transform.position =
            splitCenter;

        transform.localScale =
            splitStartScale;

        if (spriteRenderer != null)
            spriteRenderer.color =
                splitStartColor;

        SpawnMiniBosses(splitCenter);

        if (splitDisappearDuration > 0f)
        {
            yield return
                SplitDisappearRoutine(
                    splitStartScale,
                    splitStartColor
                );
        }

        Destroy(gameObject);
    }

    private IEnumerator SplitDisappearRoutine(
        Vector3 startScale,
        Color startColor
    )
    {
        float timer = 0f;

        while (timer <
               splitDisappearDuration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    splitDisappearDuration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    Vector3.zero,
                    smoothT
                );

            if (spriteRenderer != null)
            {
                Color color =
                    startColor;

                color.a =
                    Mathf.Lerp(
                        startColor.a,
                        0f,
                        smoothT
                    );

                spriteRenderer.color =
                    color;
            }

            yield return null;
        }
    }

    private void SpawnMiniBosses(
        Vector2 bossPosition
    )
    {
        if (miniBossPrefab == null ||
            player == null)
        {
            return;
        }

        Vector2 playerDirection =
            (Vector2)player.position -
            bossPosition;

        if (playerDirection.sqrMagnitude <=
            0.001f)
        {
            playerDirection =
                Vector2.right;
        }
        else
        {
            playerDirection.Normalize();
        }

        Vector2 splitDirection =
            Mathf.Abs(playerDirection.x) >
            Mathf.Abs(playerDirection.y)
                ? Vector2.up
                : Vector2.right;

        Vector2 firstPosition =
            FindSafeMiniBossPosition(
                bossPosition +
                splitDirection *
                splitDistance,
                splitDirection
            );

        Vector2 secondPosition =
            FindSafeMiniBossPosition(
                bossPosition -
                splitDirection *
                splitDistance,
                -splitDirection
            );

        CreateMiniBoss(
    firstPosition,
    true
);

        CreateMiniBoss(
            secondPosition,
            false
        );
    }

    private Vector2 FindSafeMiniBossPosition(
        Vector2 desiredPosition,
        Vector2 searchDirection
    )
    {
        Vector2 clampedPosition =
            ClampToCameraBounds(
                desiredPosition
            );

        if (IsMiniBossPositionClear(
                clampedPosition))
        {
            return clampedPosition;
        }

        const int attempts = 8;

        for (int i = 1;
             i <= attempts;
             i++)
        {
            float distance =
                0.25f * i;

            Vector2 candidate =
                ClampToCameraBounds(
                    clampedPosition +
                    searchDirection.normalized *
                    distance
                );

            if (IsMiniBossPositionClear(
                    candidate))
            {
                return candidate;
            }

            Vector2 sideDirection =
                new Vector2(
                    -searchDirection.y,
                    searchDirection.x
                );

            candidate =
                ClampToCameraBounds(
                    clampedPosition +
                    sideDirection *
                    distance
                );

            if (IsMiniBossPositionClear(
                    candidate))
            {
                return candidate;
            }

            candidate =
                ClampToCameraBounds(
                    clampedPosition -
                    sideDirection *
                    distance
                );

            if (IsMiniBossPositionClear(
                    candidate))
            {
                return candidate;
            }
        }

        return clampedPosition;
    }

    private bool IsMiniBossPositionClear(
        Vector2 position
    )
    {
        Collider2D hit =
            Physics2D.OverlapCircle(
                position,
                0.35f,
                solidLayers
            );

        return hit == null;
    }

    private Vector2 ClampToCameraBounds(
        Vector2 position
    )
    {
        if (CameraWorldBounds.Instance == null)
            return position;

        float padding =
            Mathf.Max(
                splitDistance * 0.25f,
                0.7f
            );

        position.x =
            Mathf.Clamp(
                position.x,
                CameraWorldBounds.Instance.MinX +
                padding,
                CameraWorldBounds.Instance.MaxX -
                padding
            );

        position.y =
            Mathf.Clamp(
                position.y,
                CameraWorldBounds.Instance.MinY +
                padding,
                CameraWorldBounds.Instance.MaxY -
                padding
            );

        return position;
    }

    private void CreateMiniBoss(
    Vector2 spawnPosition,
    bool canTargetClone
)
    {
        GameObject miniBoss =
            Instantiate(
                miniBossPrefab,
                spawnPosition,
                Quaternion.identity
            );

        MiniBossFollow miniScript =
            miniBoss.GetComponent<MiniBossFollow>();

        if (miniScript == null)
            return;

        miniScript.player = player;

        miniScript.solidLayers =
            solidLayers;

        miniScript.obstacleLayer =
            obstacleLayer;

        miniScript.speed =
            miniBossSpeed;

        miniScript.canTargetClone =
            canTargetClone;
    }

    private void StopBossMovement()
    {
        speed = 0f;
        shakeAmount = 0f;

        if (rb == null)
            return;

        rb.linearVelocity =
            Vector2.zero;

        rb.angularVelocity = 0f;
    }

    private void StopBoss()
    {
        if (stopped)
            return;

        stopped = true;
        isSplitting = false;

        StopBossMovement();

        if (bossCollider != null)
            bossCollider.enabled = false;

        enabled = false;
    }
}