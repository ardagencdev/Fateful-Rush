using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ProjectileEnemyFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float stoppingDistance = 7f;
    public float retreatDistance = 4f;

    [Header("Combat Strafe")]
    public bool strafeEnabled = true;
    public float strafeSpeedMultiplier = 0.65f;
    public float strafeDirectionChangeMinTime = 1.5f;
    public float strafeDirectionChangeMaxTime = 3f;
    public float strafeDistanceTolerance = 0.6f;

    [Header("Predictive Aim")]
    public bool predictiveAimEnabled = true;
    public float predictionTime = 0.3f;
    public float maxPredictionDistance = 2f;
    public float predictionDistanceThreshold = 2.5f;

    [Header("Enemy Separation")]
    public bool separationEnabled = true;
    public LayerMask enemyLayer;
    public float separationRadius = 0.9f;
    public float separationStrength = 0.5f;

    [Header("Movement Wave")]
    public float minSideMoveAmount = 0.05f;
    public float maxSideMoveAmount = 0.18f;
    public float minSideMoveSpeed = 1.5f;
    public float maxSideMoveSpeed = 3f;

    [Header("Advanced Unstuck")]
    public LayerMask obstacleLayer;
    public float escapeCheckRadius = 1.2f;
    public float escapeSpeedMultiplier = 2.2f;

    [Header("Attack")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 1.5f;
    public float projectileSpeed = 6f;

    [Header("Projectile Pool")]
    public int poolSize = 12;

    [Header("Stuck Fix")]
    public float stuckCheckTime = 0.5f;
    public float stuckDistance = 0.05f;
    public float unstuckDuration = 0.4f;
    public float unstuckSideForce = 1.8f;

    [Header("Sound")]
    public AudioClip fireSound;

    [Header("Spawn Effect")]
    public float spawnEffectDuration = 0.15f;

    private readonly Queue<GameObject> projectilePool =
        new Queue<GameObject>();

    private readonly List<EnemyProjectile> ownedProjectiles =
        new List<EnemyProjectile>();

    private readonly Collider2D[] escapeHits =
        new Collider2D[16];

    private readonly Collider2D[] separationHits =
        new Collider2D[16];

    private Rigidbody2D rb;
    private Rigidbody2D targetRigidbody;
    private AudioSource audioSource;
    private PlayerMovement playerMovement;

    private Vector3 spawnTargetScale;
    private Vector2 lastPosition;

    private float fireCooldown;

    private float movementOffset;
    private float sideMoveAmount;
    private float sideMoveSpeed;

    private float stuckTimer;
    private float unstuckTimer;

    private float strafeDirectionTimer;
    private int strafeDirection = 1;
    private int unstuckDirection = 1;

    private bool isSpawning;
    private bool stopped;
    private bool attemptedMovementThisFrame;

    private Transform cachedCurrentTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = SoundManager.SFXVolume;
    }

    private void Start()
    {
        spawnTargetScale = transform.localScale;

        if (spawnTargetScale == Vector3.zero)
            spawnTargetScale = Vector3.one;

        transform.localScale = Vector3.zero;

        fireCooldown = Random.Range(
            fireRate * 0.5f,
            fireRate * 1.5f
        );

        movementOffset = Random.Range(0f, 100f);

        sideMoveAmount = Random.Range(
            minSideMoveAmount,
            maxSideMoveAmount
        );

        if (Random.value < 0.5f)
            sideMoveAmount *= -1f;

        sideMoveSpeed = Random.Range(
            minSideMoveSpeed,
            maxSideMoveSpeed
        );

        strafeDirection =
            Random.Range(0, 2) == 0 ? -1 : 1;

        unstuckDirection =
            Random.Range(0, 2) == 0 ? -1 : 1;

        ResetStrafeTimer();

        lastPosition = rb.position;

        FindPlayerIfNeeded();
        CreateProjectilePool();

        StartCoroutine(SpawnEffect());
    }

    private void FixedUpdate()
    {
        FindPlayerIfNeeded();

        if (isSpawning || stopped)
            return;

        Transform currentTarget = GetCurrentTarget();

        if (currentTarget == null)
        {
            StopMovementOnly();
            return;
        }

        UpdateCachedTarget(currentTarget);

        if (playerMovement != null &&
            playerMovement.IsGameOver)
        {
            StopEnemy();
            return;
        }

        attemptedMovementThisFrame = false;

        HandleStrafeDirectionTimer();
        HandleMovement(currentTarget);
        HandleStuckCheck(currentTarget);
        FlipSprite(currentTarget);
    }

    private void Update()
    {
        FindPlayerIfNeeded();

        if (isSpawning || stopped)
            return;

        Transform currentTarget = GetCurrentTarget();

        if (currentTarget == null)
            return;

        UpdateCachedTarget(currentTarget);

        if (playerMovement != null &&
            playerMovement.IsGameOver)
        {
            return;
        }

        HandleAttack(currentTarget);
    }

    private Transform GetCurrentTarget()
    {
        if (VoidCloneAbility.ActiveCloneTarget != null)
            return VoidCloneAbility.ActiveCloneTarget;

        return player;
    }

    private void UpdateCachedTarget(Transform currentTarget)
    {
        if (cachedCurrentTarget == currentTarget)
            return;

        cachedCurrentTarget = currentTarget;

        targetRigidbody = currentTarget != null
            ? currentTarget.GetComponent<Rigidbody2D>()
            : null;
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
        {
            if (playerMovement == null)
                playerMovement =
                    player.GetComponent<PlayerMovement>();

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

    private void CreateProjectilePool()
    {
        if (projectilePrefab == null)
            return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject projectile =
                Instantiate(projectilePrefab);

            projectile.SetActive(false);

            RegisterProjectile(projectile);
            projectilePool.Enqueue(projectile);
        }
    }

    private void RegisterProjectile(GameObject projectile)
    {
        if (projectile == null)
            return;

        EnemyProjectile projectileScript =
            projectile.GetComponent<EnemyProjectile>();

        if (projectileScript == null)
            return;

        projectileScript.SetPoolOwner(this);

        if (!ownedProjectiles.Contains(projectileScript))
            ownedProjectiles.Add(projectileScript);
    }

    public void ReturnProjectileToPool(GameObject projectile)
    {
        if (projectile == null)
            return;

        Rigidbody2D projectileRb =
            projectile.GetComponent<Rigidbody2D>();

        if (projectileRb != null)
        {
            projectileRb.linearVelocity = Vector2.zero;
            projectileRb.angularVelocity = 0f;
        }

        projectile.SetActive(false);

        if (!projectilePool.Contains(projectile))
            projectilePool.Enqueue(projectile);
    }

    private GameObject GetProjectileFromPool()
    {
        if (projectilePool.Count > 0)
            return projectilePool.Dequeue();

        if (projectilePrefab == null)
            return null;

        GameObject projectile =
            Instantiate(projectilePrefab);

        projectile.SetActive(false);

        RegisterProjectile(projectile);

        return projectile;
    }

    private void HandleMovement(Transform currentTarget)
    {
        Vector2 targetPosition =
            currentTarget.position;

        Vector2 toTarget =
            targetPosition - rb.position;

        if (toTarget.sqrMagnitude <= 0.001f)
        {
            ResetStuckCheck();
            return;
        }

        float distance = toTarget.magnitude;
        Vector2 targetDirection = toTarget.normalized;

        Vector2 desiredDirection = Vector2.zero;
        float speedMultiplier = 1f;

        if (distance > stoppingDistance)
        {
            desiredDirection = targetDirection;
        }
        else if (distance < retreatDistance)
        {
            desiredDirection = -targetDirection;
        }
        else if (strafeEnabled)
        {
            desiredDirection =
                GetStrafeDirection(
                    targetDirection,
                    distance
                );

            speedMultiplier = strafeSpeedMultiplier;
        }
        else
        {
            ResetStuckCheck();
            return;
        }

        Vector2 waveDirection =
            GetWaveDirection(targetDirection);

        Vector2 separationDirection =
            GetSeparationDirection();

        Vector2 finalDirection =
            desiredDirection +
            waveDirection +
            separationDirection * separationStrength;

        if (finalDirection.sqrMagnitude <= 0.001f)
            finalDirection = desiredDirection;

        finalDirection.Normalize();

        if (unstuckTimer > 0f)
        {
            unstuckTimer -= Time.fixedDeltaTime;

            Vector2 sideDirection =
                new Vector2(
                    -targetDirection.y,
                    targetDirection.x
                ) * unstuckDirection;

            finalDirection =
                (
                    finalDirection +
                    sideDirection * unstuckSideForce
                ).normalized;
        }

        float movementDistance =
            moveSpeed *
            speedMultiplier *
            Time.fixedDeltaTime;

        if (distance > stoppingDistance)
        {
            float excessDistance =
                distance - stoppingDistance;

            movementDistance =
                Mathf.Min(
                    movementDistance,
                    excessDistance
                );
        }
        else if (distance < retreatDistance)
        {
            float missingDistance =
                retreatDistance - distance;

            movementDistance =
                Mathf.Min(
                    movementDistance,
                    missingDistance
                );
        }

        Move(finalDirection, movementDistance);
    }

    private Vector2 GetStrafeDirection(
        Vector2 targetDirection,
        float distance
    )
    {
        Vector2 sideDirection =
            new Vector2(
                -targetDirection.y,
                targetDirection.x
            ) * strafeDirection;

        float idealDistance =
            (stoppingDistance + retreatDistance) * 0.5f;

        float distanceDifference =
            distance - idealDistance;

        Vector2 distanceCorrection =
            Vector2.zero;

        if (Mathf.Abs(distanceDifference) >
            strafeDistanceTolerance)
        {
            float correctionStrength =
                Mathf.InverseLerp(
                    strafeDistanceTolerance,
                    Mathf.Max(
                        stoppingDistance - retreatDistance,
                        strafeDistanceTolerance + 0.01f
                    ),
                    Mathf.Abs(distanceDifference)
                );

            if (distanceDifference > 0f)
            {
                distanceCorrection =
                    targetDirection * correctionStrength;
            }
            else
            {
                distanceCorrection =
                    -targetDirection * correctionStrength;
            }
        }

        Vector2 finalDirection =
            sideDirection + distanceCorrection;

        if (finalDirection.sqrMagnitude <= 0.001f)
            return sideDirection;

        return finalDirection.normalized;
    }

    private Vector2 GetWaveDirection(
        Vector2 targetDirection
    )
    {
        Vector2 sideDirection =
            new Vector2(
                -targetDirection.y,
                targetDirection.x
            );

        float wave =
            Mathf.Sin(
                (Time.time + movementOffset) *
                sideMoveSpeed
            );

        return sideDirection *
               wave *
               sideMoveAmount;
    }

    private Vector2 GetSeparationDirection()
    {
        if (!separationEnabled)
            return Vector2.zero;

        if (separationRadius <= 0f)
            return Vector2.zero;

        ContactFilter2D filter =
            new ContactFilter2D();

        filter.SetLayerMask(enemyLayer);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        int hitCount =
            Physics2D.OverlapCircle(
                rb.position,
                separationRadius,
                filter,
                separationHits
            );

        if (hitCount <= 0)
            return Vector2.zero;

        Vector2 separationDirection =
            Vector2.zero;

        int validCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit =
                separationHits[i];

            if (hit == null)
                continue;

            if (hit.attachedRigidbody == rb)
                continue;

            ProjectileEnemyFollow other =
                hit.GetComponentInParent<
                    ProjectileEnemyFollow
                >();

            if (other == null || other == this)
                continue;

            Vector2 awayDirection =
                rb.position -
                (Vector2)other.transform.position;

            float sqrDistance =
                awayDirection.sqrMagnitude;

            if (sqrDistance <= 0.001f)
            {
                awayDirection =
                    Random.insideUnitCircle.normalized;

                sqrDistance = 0.001f;
            }

            float distance =
                Mathf.Sqrt(sqrDistance);

            float proximityStrength =
                1f -
                Mathf.Clamp01(
                    distance / separationRadius
                );

            separationDirection +=
                awayDirection.normalized *
                proximityStrength;

            validCount++;
        }

        if (validCount <= 0)
            return Vector2.zero;

        separationDirection /= validCount;

        return Vector2.ClampMagnitude(
            separationDirection,
            1f
        );
    }

    private void Move(
        Vector2 direction,
        float distance
    )
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        if (distance <= 0f)
            return;

        attemptedMovementThisFrame = true;

        rb.MovePosition(
            rb.position +
            direction.normalized * distance
        );
    }

    private void HandleStrafeDirectionTimer()
    {
        if (!strafeEnabled)
            return;

        strafeDirectionTimer -=
            Time.fixedDeltaTime;

        if (strafeDirectionTimer > 0f)
            return;

        strafeDirection *= -1;
        ResetStrafeTimer();
    }

    private void ResetStrafeTimer()
    {
        strafeDirectionTimer =
            Random.Range(
                strafeDirectionChangeMinTime,
                strafeDirectionChangeMaxTime
            );
    }

    private void HandleAttack(
        Transform currentTarget
    )
    {
        fireCooldown -= Time.deltaTime;

        if (fireCooldown > 0f)
            return;

        if (!CanSeeTarget(currentTarget))
        {
            fireCooldown = Mathf.Min(
                fireRate * 0.2f,
                0.25f
            );

            return;
        }

        ShootProjectile(currentTarget);
        fireCooldown = fireRate;
    }

    private bool CanSeeTarget(
        Transform currentTarget
    )
    {
        if (firePoint == null ||
            currentTarget == null)
        {
            return false;
        }

        Vector2 targetPosition =
            GetAimPosition(currentTarget);

        Vector2 direction =
            targetPosition -
            (Vector2)firePoint.position;

        float distance =
            direction.magnitude;

        if (distance <= 0.001f)
            return true;

        RaycastHit2D hit =
            Physics2D.Raycast(
                firePoint.position,
                direction.normalized,
                distance,
                obstacleLayer
            );

        return hit.collider == null;
    }

    private Vector2 GetAimPosition(
        Transform currentTarget
    )
    {
        Vector2 targetPosition =
            currentTarget.position;

        if (!predictiveAimEnabled)
            return targetPosition;

        if (targetRigidbody == null)
            return targetPosition;

        float distance =
            Vector2.Distance(
                firePoint != null
                    ? firePoint.position
                    : transform.position,
                targetPosition
            );

        if (distance <
            predictionDistanceThreshold)
        {
            return targetPosition;
        }

        Vector2 predictionOffset =
            targetRigidbody.linearVelocity *
            predictionTime;

        predictionOffset =
            Vector2.ClampMagnitude(
                predictionOffset,
                maxPredictionDistance
            );

        return targetPosition +
               predictionOffset;
    }

    private void ShootProjectile(
        Transform currentTarget
    )
    {
        if (projectilePrefab == null ||
            firePoint == null ||
            currentTarget == null)
        {
            return;
        }

        GameObject projectile =
            GetProjectileFromPool();

        if (projectile == null)
            return;

        projectile.transform.position =
            firePoint.position;

        projectile.transform.rotation =
            Quaternion.identity;

        projectile.SetActive(true);

        Vector2 aimPosition =
            GetAimPosition(currentTarget);

        Vector2 direction =
            aimPosition -
            (Vector2)firePoint.position;

        if (direction.sqrMagnitude <= 0.001f)
        {
            ReturnProjectileToPool(projectile);
            return;
        }

        direction.Normalize();

        EnemyProjectile projectileScript =
            projectile.GetComponent<
                EnemyProjectile
            >();

        if (projectileScript != null)
        {
            projectileScript.SetPoolOwner(this);

            if (!ownedProjectiles.Contains(
                    projectileScript
                ))
            {
                ownedProjectiles.Add(
                    projectileScript
                );
            }

            projectileScript.Launch(
                direction,
                projectileSpeed,
                playerMovement
            );
        }
        else
        {
            Rigidbody2D projectileRb =
                projectile.GetComponent<
                    Rigidbody2D
                >();

            if (projectileRb != null)
            {
                projectileRb.linearVelocity =
                    direction * projectileSpeed;
            }
        }

        if (fireSound != null &&
            audioSource != null)
        {
            audioSource.PlayOneShot(
                fireSound,
                SoundManager.SFXVolume
            );
        }
    }

    private void HandleStuckCheck(
        Transform currentTarget
    )
    {
        if (!attemptedMovementThisFrame)
        {
            ResetStuckCheck();
            return;
        }

        if (currentTarget == null)
        {
            ResetStuckCheck();
            return;
        }

        stuckTimer += Time.fixedDeltaTime;

        if (stuckTimer < stuckCheckTime)
            return;

        float movedSqrDistance =
            (rb.position - lastPosition)
            .sqrMagnitude;

        float stuckSqrDistance =
            stuckDistance * stuckDistance;

        if (movedSqrDistance <
            stuckSqrDistance)
        {
            Vector2 escapeDirection =
                GetEscapeDirection();

            if (escapeDirection != Vector2.zero)
            {
                rb.MovePosition(
                    rb.position +
                    escapeDirection *
                    moveSpeed *
                    escapeSpeedMultiplier *
                    Time.fixedDeltaTime
                );

                unstuckDirection =
                    Random.Range(0, 2) == 0
                        ? -1
                        : 1;

                unstuckTimer =
                    unstuckDuration;
            }
        }

        lastPosition = rb.position;
        stuckTimer = 0f;
    }

    private Vector2 GetEscapeDirection()
    {
        ContactFilter2D filter =
            new ContactFilter2D();

        filter.SetLayerMask(obstacleLayer);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        int hitCount =
            Physics2D.OverlapCircle(
                rb.position,
                escapeCheckRadius,
                filter,
                escapeHits
            );

        if (hitCount <= 0)
            return Vector2.zero;

        Vector2 escapeDirection =
            Vector2.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit =
                escapeHits[i];

            if (hit == null)
                continue;

            Vector2 closestPoint =
                hit.ClosestPoint(rb.position);

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

            if (awayFromObstacle.sqrMagnitude >
                0.001f)
            {
                escapeDirection +=
                    awayFromObstacle.normalized;
            }
        }

        if (escapeDirection.sqrMagnitude <=
            0.001f)
        {
            return Vector2.zero;
        }

        return escapeDirection.normalized;
    }

    private void ResetStuckCheck()
    {
        stuckTimer = 0f;
        lastPosition = rb.position;
    }

    private void FlipSprite(
        Transform currentTarget
    )
    {
        if (currentTarget == null ||
            isSpawning)
        {
            return;
        }

        Vector2 direction =
            currentTarget.position -
            transform.position;

        Vector3 scale =
            transform.localScale;

        float absX =
            Mathf.Abs(scale.x);

        if (absX <= 0.001f)
        {
            absX =
                Mathf.Abs(
                    spawnTargetScale.x
                );
        }

        if (direction.x > 0.01f)
            scale.x = absX;
        else if (direction.x < -0.01f)
            scale.x = -absX;

        transform.localScale = scale;
    }

    private void StopMovementOnly()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void StopEnemy()
    {
        if (stopped)
            return;

        stopped = true;

        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (audioSource != null)
            audioSource.Stop();

        DisableActiveProjectiles();

        enabled = false;
    }

    private void DisableActiveProjectiles()
    {
        for (int i = 0;
             i < ownedProjectiles.Count;
             i++)
        {
            EnemyProjectile projectile =
                ownedProjectiles[i];

            if (projectile != null &&
                projectile.gameObject.activeSelf)
            {
                projectile.ReturnToPool();
            }
        }
    }

    private IEnumerator SpawnEffect()
    {
        isSpawning = true;

        if (spawnEffectDuration <= 0f)
        {
            transform.localScale =
                spawnTargetScale;

            isSpawning = false;
            yield break;
        }

        float time = 0f;

        while (time <
               spawnEffectDuration)
        {
            time += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    time /
                    spawnEffectDuration
                );

            t = t * t *
                (3f - 2f * t);

            transform.localScale =
                Vector3.Lerp(
                    Vector3.zero,
                    spawnTargetScale,
                    t
                );

            yield return null;
        }

        transform.localScale =
            spawnTargetScale;

        isSpawning = false;

        ResetStuckCheck();
    }

    private void OnDestroy()
    {
        DisableActiveProjectiles();
    }
}