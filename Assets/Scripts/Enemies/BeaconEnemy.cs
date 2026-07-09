using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BeaconEnemy : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public PlayerMovement playerMovement;
    public SoundManager soundManager;

    [Header("Activation")]
    public float activationDelay = 3f;
    public GameObject activationWavePrefab;
    public GameObject loopWavePrefab;
    public float pulseInterval = 1f;

    [Header("Targeting")]
    public float retargetInterval = 0.5f;
    public float targetStopDistance = 1.5f;

    [Header("Spawner Buff Settings")]
    public float buffDuration = 15f;
    public float buffSizeMultiplier = 1.25f;
    public float projectileMoveMultiplier = 1.2f;
    public float projectileShotMultiplier = 1.25f;
    public float projectileFireMultiplier = 1.25f;
    public float hunterRepositionMultiplier = 0.8f;
    public float hunterWarningMultiplier = 0.8f;
    public float hunterChargeMultiplier = 1.25f;
    public float hunterStunMultiplier = 0.8f;

    [Header("Buff Values (Normal)")]
    public float normalSpeedMultiplier = 1.3f;
    public float normalMaxSpeedMultiplier = 1.2f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float safeDistanceFromPlayer = 5f;
    public float wanderStrength = 0.6f;
    public float boundsPadding = 0.6f;

    [Header("Collision")]
    public LayerMask solidLayers;
    public float castSkin = 0.05f;

    [Header("Spawn Effect")]
    public float spawnEffectDuration = 0.15f;

    [Header("Stuck Fix")]
    public float stuckCheckTime = 0.5f;
    public float stuckDistance = 0.06f;
    public float unstuckDuration = 0.45f;
    public float unstuckSideForce = 1.5f;
    public float escapeCheckRadius = 1.2f;
    public float escapeSpeedMultiplier = 2.2f;

    [Header("Optimization")]
    public float targetCacheRefreshInterval = 1.5f;

    private Rigidbody2D rb;
    private Collider2D col;
    private Transform currentTarget;

    private Vector3 targetScale;
    private Vector2 wanderDir;
    private Vector2 lastPosition;

    private bool active;
    private bool dead;
    private bool isSpawning;

    private float retargetTimer;
    private float targetCacheTimer;
    private float stuckTimer;
    private float unstuckTimer;
    private int unstuckDirection = 1;

    private EnemyBuffTarget[] cachedTargets = new EnemyBuffTarget[0];

    private ContactFilter2D solidFilter;
    private ContactFilter2D escapeFilter;

    private readonly RaycastHit2D[] castHits = new RaycastHit2D[4];
    private readonly Collider2D[] escapeHits = new Collider2D[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        solidFilter = new ContactFilter2D();
        solidFilter.SetLayerMask(solidLayers);
        solidFilter.useTriggers = false;

        escapeFilter = new ContactFilter2D();
        escapeFilter.SetLayerMask(solidLayers);
        escapeFilter.useTriggers = true;

        targetScale = transform.localScale;
        transform.localScale = Vector3.zero;

        wanderDir = Random.insideUnitCircle.normalized;
        if (wanderDir.sqrMagnitude <= 0.001f)
            wanderDir = Vector2.right;

        unstuckDirection = Random.Range(0, 2) == 0 ? -1 : 1;
    }

    private void Start()
    {
        if (player == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();

            if (pm != null)
            {
                player = pm.transform;
                playerMovement = pm;
            }
        }

        if (soundManager == null)
            soundManager = FindAnyObjectByType<SoundManager>();

        lastPosition = rb.position;

        RefreshTargetCache();

        StartCoroutine(SpawnEffect());
        StartCoroutine(ActivationRoutine());
    }

    private void FixedUpdate()
    {
        if (dead) return;
        if (isSpawning) return;
        if (playerMovement != null && playerMovement.IsGameOver) return;
        if (player == null) return;

        HandleTargetCache();
        HandleRetarget();
        MoveLogic();
        HandleStuckCheck();
    }

    private IEnumerator SpawnEffect()
    {
        isSpawning = true;

        float time = 0f;

        while (time < spawnEffectDuration)
        {
            time += Time.deltaTime;
            float t = time / spawnEffectDuration;

            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);

            yield return null;
        }

        transform.localScale = targetScale;
        isSpawning = false;
    }

    private IEnumerator ActivationRoutine()
    {
        yield return new WaitForSeconds(activationDelay);

        if (dead) yield break;

        active = true;

        if (soundManager != null)
            soundManager.PlayBeaconActivationWaveSound();

        if (activationWavePrefab != null)
        {
            GameObject wave = Instantiate(activationWavePrefab, transform.position, Quaternion.identity);
            BeaconPulseWave pulse = wave.GetComponent<BeaconPulseWave>();

            if (pulse != null)
                pulse.Initialize(this, false);
        }

        StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        while (active && !dead)
        {
            yield return new WaitForSeconds(pulseInterval);

            if (dead) yield break;
            if (loopWavePrefab == null) continue;

            if (soundManager != null)
                soundManager.PlayBeaconLoopWaveSound();

            GameObject wave = Instantiate(loopWavePrefab, transform.position, Quaternion.identity);
            BeaconPulseWave pulse = wave.GetComponent<BeaconPulseWave>();

            if (pulse != null)
                pulse.Initialize(this, true);
        }
    }

    private void HandleTargetCache()
    {
        targetCacheTimer -= Time.fixedDeltaTime;

        if (targetCacheTimer > 0f) return;

        targetCacheTimer = targetCacheRefreshInterval;
        RefreshTargetCache();
    }

    private void RefreshTargetCache()
    {
        cachedTargets = FindObjectsByType<EnemyBuffTarget>(FindObjectsInactive.Exclude);
    }

    private void HandleRetarget()
    {
        retargetTimer -= Time.fixedDeltaTime;

        if (retargetTimer > 0f) return;

        retargetTimer = retargetInterval;
        PickTarget();
    }

    private void PickTarget()
    {
        currentTarget = null;

        float bestSqrDistance = Mathf.Infinity;
        Vector2 beaconPos = rb.position;

        for (int i = 0; i < cachedTargets.Length; i++)
        {
            EnemyBuffTarget target = cachedTargets[i];

            if (target == null) continue;
            if (target.IsBuffed) continue;
            if (!target.CanReceiveBeaconBuff) continue;

            float sqrDistance = ((Vector2)target.transform.position - beaconPos).sqrMagnitude;

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                currentTarget = target.transform;
            }
        }
    }

    private void MoveLogic()
    {
        Vector2 pos = rb.position;
        Vector2 dir;

        float playerSqrDistance = ((Vector2)player.position - pos).sqrMagnitude;
        float safeSqrDistance = safeDistanceFromPlayer * safeDistanceFromPlayer;

        if (playerSqrDistance < safeSqrDistance)
        {
            dir = (pos - (Vector2)player.position).normalized;
        }
        else if (unstuckTimer > 0f)
        {
            unstuckTimer -= Time.fixedDeltaTime;

            Vector2 baseDir = currentTarget != null
                ? ((Vector2)currentTarget.position - pos).normalized
                : wanderDir.normalized;

            Vector2 sideDir = new Vector2(-baseDir.y, baseDir.x) * unstuckDirection;
            dir = (baseDir + sideDir * unstuckSideForce).normalized;
        }
        else if (currentTarget != null)
        {
            float targetSqrDistance = ((Vector2)currentTarget.position - pos).sqrMagnitude;
            float stopSqrDistance = targetStopDistance * targetStopDistance;

            dir = targetSqrDistance > stopSqrDistance
                ? ((Vector2)currentTarget.position - pos).normalized
                : Vector2.zero;
        }
        else
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;

            if (randomDir.sqrMagnitude <= 0.001f)
                randomDir = wanderDir;

            wanderDir = Vector2.Lerp(wanderDir, randomDir, wanderStrength * Time.fixedDeltaTime);
            dir = wanderDir.normalized;
        }

        MoveWithCollision(dir);
    }

    private void MoveWithCollision(Vector2 dir)
    {
        if (dir.sqrMagnitude <= 0.001f) return;

        Vector2 pos = rb.position;
        Vector2 nextPos = pos + dir.normalized * moveSpeed * Time.fixedDeltaTime;
        nextPos = ClampToBounds(nextPos);

        Vector2 movement = nextPos - pos;

        if (CanMove(movement))
        {
            rb.MovePosition(nextPos);
            return;
        }

        Vector2 sideDir = new Vector2(-dir.y, dir.x) * unstuckDirection;
        Vector2 sideMove = sideDir.normalized * moveSpeed * Time.fixedDeltaTime;

        if (CanMove(sideMove))
        {
            rb.MovePosition(pos + sideMove);
            return;
        }

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

            Vector2 movement = escapeDirection * moveSpeed * escapeSpeedMultiplier * Time.fixedDeltaTime;

            if (CanMove(movement))
                rb.MovePosition(rb.position + movement);

            unstuckTimer = unstuckDuration;
        }

        lastPosition = rb.position;
        stuckTimer = 0f;
    }

    private Vector2 GetEscapeDirection()
    {
        int hitCount = Physics2D.OverlapCircle(
            rb.position,
            escapeCheckRadius,
            escapeFilter,
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

    private Vector2 ClampToBounds(Vector2 pos)
    {
        if (CameraWorldBounds.Instance == null) return pos;

        pos.x = Mathf.Clamp(pos.x, CameraWorldBounds.Instance.MinX + boundsPadding, CameraWorldBounds.Instance.MaxX - boundsPadding);
        pos.y = Mathf.Clamp(pos.y, CameraWorldBounds.Instance.MinY + boundsPadding, CameraWorldBounds.Instance.MaxY - boundsPadding);

        return pos;
    }

    public void ApplyBuffToTarget(GameObject targetObject)
    {
        if (targetObject == null) return;

        EnemyBuffTarget target = targetObject.GetComponent<EnemyBuffTarget>();

        if (target == null)
            target = targetObject.GetComponentInParent<EnemyBuffTarget>();

        if (target == null)
            target = targetObject.AddComponent<EnemyBuffTarget>();

        if (!target.CanReceiveBeaconBuff) return;
        if (target.IsBuffed) return;

        target.buffDuration = buffDuration;

        target.ApplyBeaconBuff(
            buffSizeMultiplier,
            normalSpeedMultiplier,
            normalMaxSpeedMultiplier,
            projectileMoveMultiplier,
            projectileShotMultiplier,
            projectileFireMultiplier,
            hunterRepositionMultiplier,
            hunterWarningMultiplier,
            hunterChargeMultiplier,
            hunterStunMultiplier
        );
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDieFromDash(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDieFromDash(other.gameObject);
    }

    private void TryDieFromDash(GameObject other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerDash dash = other.GetComponent<PlayerDash>();

        if (dash != null && dash.IsDashing)
            Die();
    }

    private void Die()
    {
        if (dead) return;

        dead = true;
        active = false;

        if (soundManager != null)
            soundManager.PlayBeaconDeathSound();

        Destroy(gameObject);
    }
}