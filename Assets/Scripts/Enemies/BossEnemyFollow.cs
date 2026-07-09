using UnityEngine;
using System.Collections;

public class BossEnemyFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public float speed = 1.2f;
    public float shakeAmount = 0.04f;

    [Header("Collision")]
    public LayerMask solidLayers;
    public float castSkin = 0.05f;

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
    public Color splitFlashColor = new Color(0.45f, 0f, 0f, 1f);
    public float flashSpeed = 0.08f;

    [Header("Stuck Fix")]
    public float stuckCheckTime = 0.5f;
    public float stuckDistance = 0.08f;
    public float unstuckDuration = 0.5f;
    public float unstuckSideForce = 1.5f;

    private PlayerMovement playerMovement;
    private Rigidbody2D rb;
    private Collider2D bossCollider;
    private SpriteRenderer sr;

    private Vector2 lastPosition;
    private float stuckTimer;
    private float unstuckTimer;
    private int unstuckDirection = 1;

    private bool isSplitting;
    private Color originalColor;
    private Vector3 originalScale;

    private ContactFilter2D solidFilter;
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[4];
    private readonly Collider2D[] escapeHits = new Collider2D[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        originalScale = transform.localScale;

        if (sr != null)
            originalColor = sr.color;

        solidFilter = new ContactFilter2D();
        solidFilter.SetLayerMask(solidLayers);
        solidFilter.useTriggers = false;
    }

    private void Start()
    {
        if (rb != null)
            lastPosition = rb.position;

        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement>();
    }

    private void FixedUpdate()
    {
        if (isSplitting) return;
        if (rb == null || player == null) return;
        if (playerMovement == null || playerMovement.IsGameOver) return;

        MoveBoss();
    }

    private void MoveBoss()
    {
        Vector2 direction = ((Vector2)player.position - rb.position).normalized;

        FlipSprite(direction);

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
        if (bossCollider == null) return true;
        if (movement.sqrMagnitude <= 0.001f) return true;

        int hitCount = bossCollider.Cast(
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isSplitting) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        if (playerMovement == null || playerMovement.IsGameOver) return;

        PlayerArmor armor = collision.gameObject.GetComponent<PlayerArmor>();

        if (armor != null && armor.IsImmune)
            return;

        if (armor != null && armor.HasArmor)
        {
            armor.BreakArmor();

            if (canSplit)
                StartCoroutine(SplitRoutine());
            else
                Destroy(gameObject);

            return;
        }

        speed = 0f;
        shakeAmount = 0f;
        playerMovement.GameOver("BOSS");
    }

    private IEnumerator SplitRoutine()
    {
        isSplitting = true;

        Vector3 splitCenter = transform.position;

        if (bossCollider != null)
            bossCollider.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        float timer = 0f;

        while (timer < splitDelay)
        {
            timer += Time.deltaTime;

            Vector3 shakeOffset = new Vector3(
                Random.Range(-splitShakeAmount, splitShakeAmount),
                Random.Range(-splitShakeAmount, splitShakeAmount),
                0f
            );

            transform.position = splitCenter + shakeOffset;

            if (sr != null)
            {
                float flash = Mathf.PingPong(timer / flashSpeed, 1f);
                sr.color = Color.Lerp(originalColor, splitFlashColor, flash);
            }

            float scaleJitter = Random.Range(0.92f, 1.08f);
            transform.localScale = originalScale * scaleJitter;

            yield return null;
        }

        transform.position = splitCenter;
        transform.localScale = originalScale;

        SpawnMiniBosses(splitCenter);
        Destroy(gameObject);
    }

    private void SpawnMiniBosses(Vector2 bossPos)
    {
        if (miniBossPrefab == null || player == null) return;

        Vector2 playerDir = ((Vector2)player.position - bossPos).normalized;

        Vector2 splitDirection = Mathf.Abs(playerDir.x) > Mathf.Abs(playerDir.y)
            ? Vector2.up
            : Vector2.right;

        CreateMiniBoss(ClampToCameraBounds(bossPos + splitDirection * splitDistance));
        CreateMiniBoss(ClampToCameraBounds(bossPos - splitDirection * splitDistance));
    }

    private Vector2 ClampToCameraBounds(Vector2 pos)
    {
        if (CameraWorldBounds.Instance == null)
            return pos;

        float padding = 0.7f;

        pos.x = Mathf.Clamp(pos.x, CameraWorldBounds.Instance.MinX + padding, CameraWorldBounds.Instance.MaxX - padding);
        pos.y = Mathf.Clamp(pos.y, CameraWorldBounds.Instance.MinY + padding, CameraWorldBounds.Instance.MaxY - padding);

        return pos;
    }

    private void CreateMiniBoss(Vector2 spawnPos)
    {
        GameObject miniBoss = Instantiate(miniBossPrefab, spawnPos, Quaternion.identity);

        MiniBossFollow miniScript = miniBoss.GetComponent<MiniBossFollow>();

        if (miniScript != null)
        {
            miniScript.player = player;
            miniScript.solidLayers = solidLayers;
            miniScript.speed = miniBossSpeed;
        }
    }
}