using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public float speed = 5f;
    public float maxSpeed = 7f;
    public float speedIncreaseRate = 0.1f;
    public float minStartSpeed = 1.5f;
    public float maxStartSpeed = 2.5f;

    [Header("Predictive Pursuit")]
    public bool predictionEnabled = true;
    public float predictionDistanceThreshold = 2.5f;
    public float predictionTime = 0.25f;
    public float maxPredictionDistance = 1.5f;

    [Header("Wave Movement")]
    public float minSideMoveAmount = 0.1f;
    public float maxSideMoveAmount = 0.35f;
    public float minSideMoveSpeed = 1.5f;
    public float maxSideMoveSpeed = 3f;
    public float waveFadeDistance = 2.5f;

    [Header("Enemy Separation")]
    public bool separationEnabled = true;
    public LayerMask enemyLayer;
    public float separationRadius = 0.75f;
    public float separationStrength = 0.65f;

    [Header("Advanced Unstuck")]
    public LayerMask obstacleLayer;
    public float escapeCheckRadius = 1.2f;
    public float escapeSpeedMultiplier = 2.2f;

    [Header("Spawn Effect")]
    public float spawnEffectDuration = 0.15f;

    [Header("Stuck Fix")]
    public float stuckCheckTime = 0.5f;
    public float stuckDistance = 0.08f;
    public float unstuckDuration = 0.4f;
    public float unstuckSideForce = 1.5f;

    [Header("Close Range Smoothing")]
    public float closeRangeDistance = 0.8f;
    [Range(0.5f, 1f)]
    public float closeRangeSpeedMultiplier = 0.9f;

    private float movementOffset;
    private float sideMoveAmount;
    private float sideMoveSpeed;

    private Rigidbody2D rb;
    private Rigidbody2D targetRigidbody;
    private PlayerMovement playerMovement;
    private Transform originalPlayerTarget;

    private Vector3 spawnTargetScale;
    private Vector2 lastPosition;

    private bool isSpawning;

    private float stuckTimer;
    private float unstuckTimer;
    private int unstuckDirection = 1;

    private readonly Collider2D[] escapeHits = new Collider2D[16];
    private readonly Collider2D[] separationHits = new Collider2D[16];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        spawnTargetScale = transform.localScale;

        if (spawnTargetScale == Vector3.zero)
            spawnTargetScale = Vector3.one;

        transform.localScale = Vector3.zero;

        speed = Random.Range(minStartSpeed, maxStartSpeed);

        unstuckDirection = Random.Range(0, 2) == 0 ? -1 : 1;
        unstuckTimer = 0f;

        FindPlayerIfNeeded();

        originalPlayerTarget = player;

        movementOffset = Random.Range(0f, 100f);
        sideMoveAmount = Random.Range(minSideMoveAmount, maxSideMoveAmount);

        if (Random.value < 0.5f)
            sideMoveAmount *= -1f;

        sideMoveSpeed = Random.Range(minSideMoveSpeed, maxSideMoveSpeed);

        lastPosition = rb.position;

        StartCoroutine(SpawnEffect());
    }

    private void FixedUpdate()
    {
        FindPlayerIfNeeded();
        UpdateTarget();

        if (player == null)
        {
            StopEnemy();
            return;
        }

        if (playerMovement != null && playerMovement.IsGameOver)
        {
            StopEnemy();
            return;
        }

        if (isSpawning)
        {
            StopEnemy();
            return;
        }

        FollowTarget();
        IncreaseSpeed();
    }

    private void UpdateTarget()
    {
        Transform desiredTarget = VoidCloneAbility.ActiveCloneTarget != null
            ? VoidCloneAbility.ActiveCloneTarget
            : originalPlayerTarget;

        if (player == desiredTarget)
            return;

        player = desiredTarget;
        targetRigidbody = player != null
            ? player.GetComponent<Rigidbody2D>()
            : null;
    }

    private void FindPlayerIfNeeded()
    {
        if (originalPlayerTarget != null)
            return;

        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");

        if (foundPlayer == null)
            return;

        originalPlayerTarget = foundPlayer.transform;
        player = originalPlayerTarget;

        playerMovement = foundPlayer.GetComponent<PlayerMovement>();
        targetRigidbody = foundPlayer.GetComponent<Rigidbody2D>();
    }

    private IEnumerator SpawnEffect()
    {
        isSpawning = true;

        if (spawnEffectDuration <= 0f)
        {
            transform.localScale = spawnTargetScale;
            isSpawning = false;
            yield break;
        }

        float time = 0f;

        while (time < spawnEffectDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / spawnEffectDuration);

            // Hafif yumuşak spawn eğrisi.
            t = t * t * (3f - 2f * t);

            transform.localScale = Vector3.Lerp(
                Vector3.zero,
                spawnTargetScale,
                t
            );

            yield return null;
        }

        transform.localScale = spawnTargetScale;
        isSpawning = false;

        lastPosition = rb.position;
        stuckTimer = 0f;
    }

    private void FollowTarget()
    {
        Vector2 targetPosition = GetTargetPosition();
        Vector2 toTarget = targetPosition - rb.position;

        float distanceToTarget = Vector2.Distance(
            rb.position,
            player.position
        );

        if (toTarget.sqrMagnitude <= 0.001f)
        {
            ResetStuckCheck();
            return;
        }

        Vector2 targetDirection = toTarget.normalized;
        Vector2 finalDirection = targetDirection;

        finalDirection += GetWaveDirection(
            targetDirection,
            distanceToTarget
        );

        finalDirection += GetSeparationDirection()
            * separationStrength;

        if (finalDirection.sqrMagnitude <= 0.001f)
            finalDirection = targetDirection;
        else
            finalDirection.Normalize();

        if (unstuckTimer > 0f)
        {
            unstuckTimer -= Time.fixedDeltaTime;

            Vector2 sideDirection = new Vector2(
                -targetDirection.y,
                targetDirection.x
            ) * unstuckDirection;

            finalDirection = (
                finalDirection +
                sideDirection * unstuckSideForce
            ).normalized;
        }

        Move(finalDirection, distanceToTarget);
        HandleStuckCheck(distanceToTarget);
        FlipSprite(finalDirection);
    }

    private Vector2 GetTargetPosition()
    {
        Vector2 targetPosition = player.position;

        if (!predictionEnabled)
            return targetPosition;

        if (player != originalPlayerTarget)
            return targetPosition;

        if (targetRigidbody == null)
            return targetPosition;

        float distance = Vector2.Distance(
            rb.position,
            targetPosition
        );

        if (distance < predictionDistanceThreshold)
            return targetPosition;

        Vector2 targetVelocity = targetRigidbody.linearVelocity;

        Vector2 predictionOffset =
            targetVelocity * predictionTime;

        predictionOffset = Vector2.ClampMagnitude(
            predictionOffset,
            maxPredictionDistance
        );

        return targetPosition + predictionOffset;
    }

    private Vector2 GetWaveDirection(
        Vector2 targetDirection,
        float distanceToTarget
    )
    {
        if (waveFadeDistance <= 0f)
            return Vector2.zero;

        float waveStrength = Mathf.InverseLerp(
            closeRangeDistance,
            waveFadeDistance,
            distanceToTarget
        );

        if (waveStrength <= 0f)
            return Vector2.zero;

        Vector2 sideDirection = new Vector2(
            -targetDirection.y,
            targetDirection.x
        );

        float wave = Mathf.Sin(
            (Time.time + movementOffset) * sideMoveSpeed
        );

        return sideDirection
            * wave
            * sideMoveAmount
            * waveStrength;
    }

    private Vector2 GetSeparationDirection()
    {
        if (!separationEnabled)
            return Vector2.zero;

        if (separationRadius <= 0f)
            return Vector2.zero;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        int hitCount = Physics2D.OverlapCircle(
            rb.position,
            separationRadius,
            filter,
            separationHits
        );

        if (hitCount <= 0)
            return Vector2.zero;

        Vector2 separationDirection = Vector2.zero;
        int validEnemyCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = separationHits[i];

            if (hit == null)
                continue;

            if (hit.attachedRigidbody == rb)
                continue;

            EnemyFollow otherEnemy =
                hit.GetComponentInParent<EnemyFollow>();

            if (otherEnemy == null || otherEnemy == this)
                continue;

            Vector2 awayDirection =
                rb.position -
                (Vector2)otherEnemy.transform.position;

            float sqrDistance = awayDirection.sqrMagnitude;

            if (sqrDistance <= 0.001f)
            {
                awayDirection = Random.insideUnitCircle.normalized;
                sqrDistance = 0.001f;
            }

            float distance = Mathf.Sqrt(sqrDistance);

            float proximityStrength = 1f -
                Mathf.Clamp01(distance / separationRadius);

            separationDirection +=
                awayDirection.normalized * proximityStrength;

            validEnemyCount++;
        }

        if (validEnemyCount <= 0)
            return Vector2.zero;

        separationDirection /= validEnemyCount;

        return Vector2.ClampMagnitude(
            separationDirection,
            1f
        );
    }

    private void Move(
        Vector2 direction,
        float distanceToTarget
    )
    {
        float finalSpeed = speed;

        if (distanceToTarget < closeRangeDistance)
        {
            float closeRangeT = Mathf.InverseLerp(
                0f,
                closeRangeDistance,
                distanceToTarget
            );

            float speedMultiplier = Mathf.Lerp(
                closeRangeSpeedMultiplier,
                1f,
                closeRangeT
            );

            finalSpeed *= speedMultiplier;
        }

        rb.MovePosition(
            rb.position +
            direction *
            finalSpeed *
            Time.fixedDeltaTime
        );
    }

    private void HandleStuckCheck(float distanceToTarget)
    {
        if (distanceToTarget <= closeRangeDistance)
        {
            ResetStuckCheck();
            return;
        }

        stuckTimer += Time.fixedDeltaTime;

        if (stuckTimer < stuckCheckTime)
            return;

        float movedSqrDistance =
            (rb.position - lastPosition).sqrMagnitude;

        float stuckSqrDistance =
            stuckDistance * stuckDistance;

        if (movedSqrDistance < stuckSqrDistance)
        {
            Vector2 escapeDirection = GetEscapeDirection();

            if (escapeDirection != Vector2.zero)
            {
                rb.MovePosition(
                    rb.position +
                    escapeDirection *
                    speed *
                    escapeSpeedMultiplier *
                    Time.fixedDeltaTime
                );

                unstuckDirection =
                    Random.Range(0, 2) == 0 ? -1 : 1;

                unstuckTimer = unstuckDuration;
            }
        }

        lastPosition = rb.position;
        stuckTimer = 0f;
    }

    private Vector2 GetEscapeDirection()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(obstacleLayer);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        int hitCount = Physics2D.OverlapCircle(
            rb.position,
            escapeCheckRadius,
            filter,
            escapeHits
        );

        if (hitCount <= 0)
            return Vector2.zero;

        Vector2 escapeDirection = Vector2.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = escapeHits[i];

            if (hit == null)
                continue;

            Vector2 closestPoint =
                hit.ClosestPoint(rb.position);

            Vector2 awayFromObstacle =
                rb.position - closestPoint;

            if (awayFromObstacle.sqrMagnitude <= 0.001f)
            {
                awayFromObstacle =
                    rb.position -
                    (Vector2)hit.bounds.center;
            }

            if (awayFromObstacle.sqrMagnitude > 0.001f)
            {
                escapeDirection +=
                    awayFromObstacle.normalized;
            }
        }

        if (escapeDirection.sqrMagnitude <= 0.001f)
            return Vector2.zero;

        return escapeDirection.normalized;
    }

    private void ResetStuckCheck()
    {
        stuckTimer = 0f;
        lastPosition = rb.position;
    }

    private void FlipSprite(Vector2 direction)
    {
        if (isSpawning)
            return;

        Vector3 scale = transform.localScale;
        float absX = Mathf.Abs(scale.x);

        if (absX <= 0.001f)
            absX = Mathf.Abs(spawnTargetScale.x);

        if (direction.x > 0.01f)
            scale.x = absX;
        else if (direction.x < -0.01f)
            scale.x = -absX;

        transform.localScale = scale;
    }

    private void IncreaseSpeed()
    {
        speed += speedIncreaseRate * Time.fixedDeltaTime;
        speed = Mathf.Clamp(speed, 0f, maxSpeed);
    }

    private void StopEnemy()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }
}