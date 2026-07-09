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

    [Header("Collision")]
    public LayerMask solidLayers;
    public float castSkin = 0.05f;

    [Header("Advanced Unstuck")]
    public LayerMask obstacleLayer;
    public float escapeCheckRadius = 1.2f;
    public float escapeSpeedMultiplier = 2.2f;

    [Header("Stuck Fix")]
    public float stuckCheckTime = 0.5f;
    public float stuckDistance = 0.08f;
    public float unstuckDuration = 0.5f;
    public float unstuckSideForce = 1.5f;

    private float movementOffset;
    private float sideMoveAmount;
    private float sideMoveSpeed;

    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerMovement playerMovement;

    private Vector3 originalScale;
    private Vector2 lastPosition;

    private float stuckTimer;
    private float unstuckTimer;
    private int unstuckDirection = 1;

    private ContactFilter2D solidFilter;
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[4];
    private readonly Collider2D[] escapeHits = new Collider2D[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        originalScale = transform.localScale;

        solidFilter = new ContactFilter2D();
        solidFilter.SetLayerMask(solidLayers);
        solidFilter.useTriggers = false;
    }

    private void Start()
    {
        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.bodyType = RigidbodyType2D.Dynamic;

        movementOffset = Random.Range(0f, 100f);
        sideMoveAmount = Random.Range(-0.35f, 0.35f);
        sideMoveSpeed = Random.Range(1.5f, 3f);

        lastPosition = rb.position;
    }

    private void FixedUpdate()
    {
        if (player == null) return;
        if (playerMovement == null || playerMovement.IsGameOver) return;

        MoveMiniBoss();
    }

    private void MoveMiniBoss()
    {
        Vector2 direction = ((Vector2)player.position - rb.position).normalized;

        FlipSprite(direction);

        Vector2 waveSideDirection = new Vector2(-direction.y, direction.x);
        float wave = Mathf.Sin((Time.time + movementOffset) * sideMoveSpeed) * sideMoveAmount;
        direction = (direction + waveSideDirection * wave).normalized;

        if (unstuckTimer > 0f)
        {
            unstuckTimer -= Time.fixedDeltaTime;

            Vector2 sideDirection = new Vector2(-direction.y, direction.x) * unstuckDirection;
            Vector2 finalDirection = (direction + sideDirection * unstuckSideForce).normalized;

            MoveWithCollision(finalDirection);
            return;
        }

        MoveWithCollision(direction);
        HandleStuckCheck();
    }

    private void MoveWithCollision(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f) return;

        Vector2 shake = new Vector2(
            Random.Range(-shakeAmount, shakeAmount),
            Random.Range(-shakeAmount, shakeAmount)
        );

        Vector2 movement = direction * speed * Time.fixedDeltaTime + shake;

        if (CanMove(movement))
        {
            rb.MovePosition(rb.position + movement);
            return;
        }

        Vector2 sideDirection = new Vector2(-direction.y, direction.x) * unstuckDirection;
        Vector2 sideMovement = sideDirection.normalized * speed * Time.fixedDeltaTime;

        if (CanMove(sideMovement))
            rb.MovePosition(rb.position + sideMovement);
        else
            unstuckDirection *= -1;
    }

    private bool CanMove(Vector2 movement)
    {
        if (col == null) return true;
        if (movement.sqrMagnitude <= 0.001f) return true;

        int hitCount = col.Cast(
            movement.normalized,
            solidFilter,
            castHits,
            movement.magnitude + castSkin
        );

        return hitCount == 0;
    }

    private void HandleStuckCheck()
    {
        stuckTimer += Time.fixedDeltaTime;

        if (stuckTimer < stuckCheckTime) return;

        float movedDistanceSqr = ((Vector2)rb.position - lastPosition).sqrMagnitude;
        float stuckDistanceSqr = stuckDistance * stuckDistance;

        if (movedDistanceSqr < stuckDistanceSqr)
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
        ContactFilter2D obstacleFilter = new ContactFilter2D();
        obstacleFilter.SetLayerMask(obstacleLayer);
        obstacleFilter.useTriggers = true;

        int hitCount = Physics2D.OverlapCircle(
            rb.position,
            escapeCheckRadius,
            obstacleFilter,
            escapeHits
        );

        if (hitCount == 0)
            return Vector2.zero;

        Vector2 escapeDirection = Vector2.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = escapeHits[i];
            if (hit == null) continue;

            Vector2 awayFromObstacle = rb.position - (Vector2)hit.ClosestPoint(rb.position);

            if (awayFromObstacle.sqrMagnitude > 0.001f)
                escapeDirection += awayFromObstacle.normalized;
        }

        return escapeDirection.normalized;
    }

    private void FlipSprite(Vector2 direction)
    {
        if (direction.x > 0)
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
    }
}