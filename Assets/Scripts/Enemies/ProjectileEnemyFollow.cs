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

    private Queue<GameObject> projectilePool = new Queue<GameObject>();
    private List<EnemyProjectile> ownedProjectiles = new List<EnemyProjectile>();

    private float movementOffset;
    private float sideMoveAmount;
    private float sideMoveSpeed;

    [Header("Stuck Fix")]
    public float stuckCheckTime = 0.5f;
    public float stuckDistance = 0.05f;
    public float unstuckDuration = 0.5f;
    public float unstuckSideForce = 1.5f;

    [Header("Sound")]
    public AudioClip fireSound;

    [Header("Spawn Effect")]
    public float spawnEffectDuration = 0.15f;

    private Vector3 spawnTargetScale;
    private bool isSpawning;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private PlayerMovement playerMovement;

    private float fireCooldown;
    private bool stopped;

    private Vector2 lastPosition;
    private float stuckTimer;
    private float unstuckTimer;
    private int unstuckDirection = 1;

    private readonly Collider2D[] escapeHits = new Collider2D[8];

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
        StartCoroutine(SpawnEffect());

        fireCooldown = Random.Range(fireRate * 0.5f, fireRate * 1.5f);

        movementOffset = Random.Range(0f, 100f);
        sideMoveAmount = Random.Range(-0.2f, 0.2f);
        sideMoveSpeed = Random.Range(1.5f, 3f);

        lastPosition = rb.position;
        unstuckDirection = Random.Range(0, 2) == 0 ? -1 : 1;

        FindPlayerIfNeeded();
        CreateProjectilePool();
    }

    private void CreateProjectilePool()
    {
        if (projectilePrefab == null) return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject projectile = Instantiate(projectilePrefab);
            projectile.SetActive(false);

            EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();

            if (projectileScript != null)
            {
                projectileScript.SetPoolOwner(this);

                if (!ownedProjectiles.Contains(projectileScript))
                    ownedProjectiles.Add(projectileScript);
            }

            projectilePool.Enqueue(projectile);
        }
    }

    public void ReturnProjectileToPool(GameObject projectile)
    {
        if (projectile == null) return;

        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();

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

        GameObject projectile = Instantiate(projectilePrefab);
        projectile.SetActive(false);

        EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();

        if (projectileScript != null)
        {
            projectileScript.SetPoolOwner(this);

            if (!ownedProjectiles.Contains(projectileScript))
                ownedProjectiles.Add(projectileScript);
        }

        return projectile;
    }

    private void FixedUpdate()
    {
        FindPlayerIfNeeded();

        if (isSpawning) return;
        if (stopped) return;

        if (player == null) return;

        if (playerMovement != null && playerMovement.IsGameOver)
        {
            StopEnemy();
            return;
        }

        HandleMovement();
        HandleStuckCheck();
        FlipSprite();
    }

    private void Update()
    {
        FindPlayerIfNeeded();

        if (isSpawning) return;
        if (stopped) return;
        if (player == null) return;

        if (playerMovement != null && playerMovement.IsGameOver) return;

        HandleAttack();
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

        if (foundPlayer == null)
            return;

        player = foundPlayer.transform;
        playerMovement = foundPlayer.GetComponent<PlayerMovement>();
    }

    private void HandleMovement()
    {
        Vector2 toPlayer = (Vector2)player.position - rb.position;

        if (toPlayer.sqrMagnitude <= 0.001f)
            return;

        Vector2 direction = toPlayer.normalized;
        float distance = toPlayer.magnitude;

        Vector2 waveSideDirection = new Vector2(-direction.y, direction.x);
        float wave = Mathf.Sin((Time.time + movementOffset) * sideMoveSpeed) * sideMoveAmount;
        direction = (direction + waveSideDirection * wave).normalized;

        if (unstuckTimer > 0f)
        {
            unstuckTimer -= Time.fixedDeltaTime;

            Vector2 sideDirection = new Vector2(-direction.y, direction.x) * unstuckDirection;
            Vector2 finalDirection = (direction + sideDirection * unstuckSideForce).normalized;

            Move(finalDirection);
            return;
        }

        if (distance > stoppingDistance)
        {
            float moveStep = Mathf.Min(moveSpeed * Time.fixedDeltaTime, distance - stoppingDistance);
            Move(direction, moveStep);
        }
        else if (distance < retreatDistance)
        {
            float moveStep = Mathf.Min(moveSpeed * Time.fixedDeltaTime, retreatDistance - distance);
            Move(-direction, moveStep);
        }
    }

    private void Move(Vector2 direction)
    {
        Move(direction, moveSpeed * Time.fixedDeltaTime);
    }

    private void Move(Vector2 direction, float distance)
    {
        if (direction.sqrMagnitude <= 0.001f) return;

        rb.MovePosition(rb.position + direction.normalized * distance);
    }

    private void HandleAttack()
    {
        fireCooldown -= Time.deltaTime;

        if (fireCooldown > 0f) return;

        if (CanSeePlayer())
            ShootProjectile();

        fireCooldown = fireRate;
    }

    private bool CanSeePlayer()
    {
        if (firePoint == null || player == null) return false;

        Vector2 direction = (player.position - firePoint.position).normalized;
        float distance = Vector2.Distance(firePoint.position, player.position);

        RaycastHit2D hit = Physics2D.Raycast(
            firePoint.position,
            direction,
            distance,
            obstacleLayer
        );

        return hit.collider == null;
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null || firePoint == null || player == null) return;

        GameObject projectile = GetProjectileFromPool();
        if (projectile == null) return;

        projectile.transform.position = firePoint.position;
        projectile.transform.rotation = Quaternion.identity;
        projectile.SetActive(true);

        Vector2 direction = (player.position - firePoint.position).normalized;

        EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();

        if (projectileScript != null)
        {
            projectileScript.SetPoolOwner(this);

            if (!ownedProjectiles.Contains(projectileScript))
                ownedProjectiles.Add(projectileScript);

            projectileScript.Launch(direction, projectileSpeed, playerMovement);
        }
        else
        {
            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();

            if (projectileRb != null)
                projectileRb.linearVelocity = direction * projectileSpeed;
        }

        if (fireSound != null && audioSource != null)
            audioSource.PlayOneShot(fireSound, SoundManager.SFXVolume);
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

            rb.MovePosition(rb.position + escapeDirection * moveSpeed * escapeSpeedMultiplier * Time.fixedDeltaTime);
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

    private void FlipSprite()
    {
        if (player == null || isSpawning) return;

        Vector2 direction = player.position - transform.position;
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

    private void StopEnemy()
    {
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
        for (int i = 0; i < ownedProjectiles.Count; i++)
        {
            EnemyProjectile projectile = ownedProjectiles[i];

            if (projectile != null && projectile.gameObject.activeSelf)
                projectile.ReturnToPool();
        }
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

    private void OnDestroy()
    {
        DisableActiveProjectiles();
    }
}