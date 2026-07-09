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
    public float closeRangeSpeedMultiplier = 0.75f;

    private float movementOffset;
    private float sideMoveAmount;
    private float sideMoveSpeed;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;

    private Vector3 spawnTargetScale;
    private Vector2 lastPosition;

    private bool isSpawning;

    private float stuckTimer;
    private float unstuckTimer;
    private int unstuckDirection = 1;

    private readonly Collider2D[] escapeHits = new Collider2D[8];

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
        unstuckTimer = Random.Range(0f, 0.2f);

        FindPlayerIfNeeded();

        movementOffset = Random.Range(0f, 100f);
        sideMoveAmount = Random.Range(-0.35f, 0.35f);
        sideMoveSpeed = Random.Range(1.5f, 3f);

        lastPosition = rb.position;

        StartCoroutine(SpawnEffect());
    }

    private void FixedUpdate()
    {
        FindPlayerIfNeeded();

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

        FollowPlayer();
        IncreaseSpeed();
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
        {
            if (playerMovement == null)
                playerMovement = player.GetComponent<PlayerMovement>();

            return;
        }

        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");

        if (foundPlayer == null) return;

        player = foundPlayer.transform;
        playerMovement = foundPlayer.GetComponent<PlayerMovement>();
    }

    private IEnumerator SpawnEffect()
    {
        isSpawning = true;

        float time = 0f;

        while (time < spawnEffectDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / spawnEffectDuration);
            transform.localScale = Vector3.Lerp(Vector3.zero, spawnTargetScale, t);

            yield return null;
        }

        transform.localScale = spawnTargetScale;
        isSpawning = false;
    }

    private void FollowPlayer()
    {
        Vector2 toPlayer = (Vector2)player.position - rb.position;

        if (toPlayer.sqrMagnitude <= 0.001f)
            return;

        Vector2 direction = toPlayer.normalized;

        Vector2 waveSideDirection = new Vector2(-direction.y, direction.x);
        float wave = Mathf.Sin((Time.time + movementOffset) * sideMoveSpeed) * sideMoveAmount;
        direction = (direction + waveSideDirection * wave).normalized;

        if (unstuckTimer > 0f)
        {
            unstuckTimer -= Time.fixedDeltaTime;

            Vector2 sideDirection = new Vector2(-direction.y, direction.x) * unstuckDirection;
            Vector2 finalDirection = (direction + sideDirection * unstuckSideForce).normalized;

            Move(finalDirection, toPlayer.magnitude);
            FlipSprite(direction);
            return;
        }

        Move(direction, toPlayer.magnitude);
        HandleStuckCheck();
        FlipSprite(direction);
    }

    private void Move(Vector2 direction, float distanceToPlayer)
    {
        float finalSpeed = speed;

        if (distanceToPlayer < closeRangeDistance)
            finalSpeed *= closeRangeSpeedMultiplier;

        rb.MovePosition(rb.position + direction * finalSpeed * Time.fixedDeltaTime);
    }

    private void HandleStuckCheck()
    {
        stuckTimer += Time.fixedDeltaTime;

        if (stuckTimer < stuckCheckTime) return;

        float movedSqrDistance = (rb.position - lastPosition).sqrMagnitude;
        float stuckSqrDistance = stuckDistance * stuckDistance;

        if (movedSqrDistance < stuckSqrDistance)
        {
            Vector2 escapeDirection = GetEscapeDirection();

            if (escapeDirection == Vector2.zero)
            {
                unstuckDirection *= -1;
                escapeDirection = new Vector2(unstuckDirection, Random.Range(-0.5f, 0.5f)).normalized;
            }

            rb.MovePosition(rb.position + escapeDirection * speed * escapeSpeedMultiplier * Time.fixedDeltaTime);

            unstuckTimer = unstuckDuration;
        }

        lastPosition = rb.position;
        stuckTimer = 0f;
    }

    private Vector2 GetEscapeDirection()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(obstacleLayer);
        filter.useTriggers = false;

        int hitCount = Physics2D.OverlapCircle(rb.position, escapeCheckRadius, filter, escapeHits);

        if (hitCount == 0)
            return Vector2.zero;

        Vector2 escapeDirection = Vector2.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = escapeHits[i];
            if (hit == null) continue;

            Vector2 closestPoint = hit.ClosestPoint(rb.position);
            Vector2 awayFromObstacle = rb.position - closestPoint;

            if (awayFromObstacle.sqrMagnitude > 0.001f)
                escapeDirection += awayFromObstacle.normalized;
        }

        return escapeDirection.normalized;
    }

    private void FlipSprite(Vector2 direction)
    {
        if (isSpawning) return;

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