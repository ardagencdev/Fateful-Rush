using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public partial class ProjectileEnemyFollow : MonoBehaviour
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

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;
    public float obstacleProbeDistance = 0.8f;
    [Range(2, 8)] public int obstacleAvoidanceAttempts = 5;
    [Range(0f, 1f)] public float obstacleOutwardBias = 0.3f;

    [Header("Advanced Unstuck")]
    public float escapeCheckRadius = 1.2f;
    public float escapeSpeedMultiplier = 2.2f;

    [Header("Attack")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 1.5f;
    public float projectileSpeed = 6f;

    [Header("Shot Recoil")]
    [Min(0f)] public float shotRecoilDistance = 0.035f;
    [Min(0f)] public float shotRecoilPause = 0.02f;
    [Min(1f)] public float finalShotRecoilMultiplier = 1.12f;

    [Header("Arena Bounds")]
    [SerializeField, Min(0f)]
    private float arenaEdgePadding = 0.03f;

    [Header("Attack Timing Desync")]
    [SerializeField, Range(0f, 0.30f)]
    private float fireIntervalJitter = 0.10f;

    [SerializeField, Range(0f, 0.20f)]
    private float reloadDurationJitter = 0.08f;

    [SerializeField, Min(0f)]
    private float initialFireDelayMin = 0.15f;

    [SerializeField, Min(0f)]
    private float initialFireDelayMax = 0.85f;

    [SerializeField, Min(0f)]
    private float postReloadFireDelayMin = 0.12f;

    [SerializeField, Min(0f)]
    private float postReloadFireDelayMax = 0.38f;

    [Header("Final Burst Shot")]
    [Tooltip("Danger 1-2 use 3 shots per burst, so their final shot fires this many projectiles.")]
    [Min(1)] public int lowDangerFinalShotProjectileCount = 2;
    [Tooltip("Danger 3-5 use 4+ shots per burst, so their final shot fires this many projectiles.")]
    [Min(1)] public int highDangerFinalShotProjectileCount = 3;
    [Tooltip("Burst sizes at or above this value use the high-danger final-shot projectile count.")]
    [Min(1)] public int highDangerBurstThreshold = 4;
    [Range(0f, 20f)] public float finalShotAngleOffset = 6f;

    [Header("Burst / Reload")]
    [Min(1)] public int shotsPerBurst = 3;
    [Min(0.05f)] public float reloadDuration = 3f;
    [Min(0f)] public float reloadRetreatDistance = 9f;
    [Min(0.1f)] public float reloadMoveSpeedMultiplier = 1.4f;

    [Header("Reload Feedback")]
    public AudioClip reloadSound;
    [Range(0.5f, 1f)] public float reloadVolumeMinMultiplier = 0.82f;
    [Range(0.5f, 1f)] public float reloadVolumeMaxMultiplier = 1f;
    [Range(0.9f, 1.1f)] public float reloadPitchMin = 0.97f;
    [Range(0.9f, 1.1f)] public float reloadPitchMax = 1.03f;
    [Min(0.01f)] public float reloadSfxFadeOutDuration = 0.22f;
    [Range(0.05f, 1f)] public float reloadBlinkMinAlpha = 0.2f;
    [Min(0.1f)] public float reloadBlinkFrequency = 4.5f;

    [Header("Projectile Pool")]
    public int poolSize = 12;

    [Header("Stuck Fix")]
    public float stuckCheckTime = 0.5f;
    public float stuckDistance = 0.05f;
    public float unstuckDuration = 0.4f;
    public float unstuckSideForce = 1.8f;

    [Header("Sound")]
    public AudioClip fireSound;

    [Header("SFX Variation")]
    [SerializeField, Range(0f, 0.08f)]
    private float firePitchJitter = 0.015f;

    [SerializeField, Range(0f, 0.08f)]
    private float fireVolumeJitter = 0.01f;

    [Header("Spawn Effect")]
    public float spawnEffectDuration = 0.15f;

    [Header("Near Miss")]
    [SerializeField]
    private bool enableNearMiss = true;

    [Tooltip("Surface-to-surface distance that arms a projectile-enemy body near miss.")]
    [SerializeField, Min(0.05f)]
    private float nearMissDistance = 0.80f;

    [Tooltip("Enemy must separate this much after the closest point before the near miss fires.")]
    [SerializeField, Min(0f)]
    private float nearMissReleaseDistance = 0.10f;

    private readonly List<EnemyProjectile> ownedProjectiles =
        new List<EnemyProjectile>();

    private readonly RaycastHit2D[] avoidanceHits =
        new RaycastHit2D[12];

    private readonly Collider2D[] escapeHits =
        new Collider2D[16];

    private readonly Collider2D[] separationHits =
        new Collider2D[16];

    private Rigidbody2D rb;
    private Collider2D col;
    private Rigidbody2D targetRigidbody;
    private AudioSource audioSource;
    private AudioSource reloadAudioSource;
    private PlayerMovement playerMovement;
    private SpriteRenderer[] reloadRenderers;
    private float[] reloadRendererBaseAlphas;

    private Vector3 spawnTargetScale;
    private Vector2 lastPosition;

    private float fireCooldown;
    private float reloadTimer;
    private float reloadVisualTime;
    private float activeReloadDuration;
    private float activeReloadSfxVolume;
    private float shotRecoilPauseTimer;
    private float perEnemyFireCadenceMultiplier = 1f;
    private float perEnemyReloadCadenceMultiplier = 1f;
    private Vector2 pendingShotRecoil;
    private int shotsFiredInBurst;

    private float movementOffset;
    private float sideMoveAmount;
    private float sideMoveSpeed;

    private float stuckTimer;
    private float unstuckTimer;

    private float strafeDirectionTimer;
    private int strafeDirection = 1;
    private int unstuckDirection = 1;
    private int obstacleAvoidanceSide = 1;

    private bool isSpawning;
    private bool isReloading;
    private bool stopped;
    private bool attemptedMovementThisFrame;

    public bool IsReloading => isReloading;

    private Transform cachedCurrentTarget;
    private ContactFilter2D navigationFilter;

    private Collider2D nearMissPlayerCollider;
    private bool nearMissArmed;
    private bool nearMissTriggered;
    private bool nearMissTouchedPlayer;
    private float nearMissClosestDistance = float.PositiveInfinity;
    private Vector3 nearMissClosestPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        EnemyObstacleSteering2D.ConfigureAIMovementBody(rb, true);

        navigationFilter = new ContactFilter2D();
        navigationFilter.SetLayerMask(
            EnemyObstacleSteering2D.BuildNavigationMask(obstacleLayer)
        );
        navigationFilter.useLayerMask = true;
        navigationFilter.useTriggers = false;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        SoundManager.ConfigureAsWorld3D(audioSource);
        // User SFX volume is applied once through AudioSource.volume.
        // Keeping the initial source gain at 1 prevents accidental double scaling.
        audioSource.volume = 1f;

        reloadAudioSource = gameObject.AddComponent<AudioSource>();
        reloadAudioSource.playOnAwake = false;
        reloadAudioSource.loop = false;
        SoundManager.ConfigureAsWorld3D(reloadAudioSource);
        reloadAudioSource.volume = 1f;

        CacheReloadRenderers();
    }

    private void Start()
    {
        spawnTargetScale = transform.localScale;

        if (spawnTargetScale == Vector3.zero)
            spawnTargetScale = Vector3.one;

        transform.localScale = Vector3.zero;

        if (shotsPerBurst <= 0)
            shotsPerBurst = 3;

        if (reloadDuration <= 0f)
            reloadDuration = 3f;

        if (reloadRetreatDistance <= 0f)
            reloadRetreatDistance = Mathf.Max(stoppingDistance, 9f);
        else
            reloadRetreatDistance = Mathf.Max(stoppingDistance, reloadRetreatDistance);

        if (reloadMoveSpeedMultiplier <= 0f)
            reloadMoveSpeedMultiplier = 1.4f;

        if (reloadBlinkMinAlpha <= 0f)
            reloadBlinkMinAlpha = 0.2f;

        reloadBlinkMinAlpha = Mathf.Clamp(reloadBlinkMinAlpha, 0.05f, 1f);

        if (reloadBlinkFrequency <= 0f)
            reloadBlinkFrequency = 4.5f;

        // Keep recoil intentionally subtle even if an older prefab serialized
        // the previous, much stronger values.
        shotRecoilDistance = Mathf.Clamp(shotRecoilDistance, 0f, 0.05f);
        shotRecoilPause = Mathf.Clamp(shotRecoilPause, 0f, 0.03f);
        finalShotRecoilMultiplier = Mathf.Clamp(finalShotRecoilMultiplier, 1f, 1.18f);
        lowDangerFinalShotProjectileCount = Mathf.Max(1, lowDangerFinalShotProjectileCount);
        highDangerFinalShotProjectileCount = Mathf.Max(1, highDangerFinalShotProjectileCount);
        highDangerBurstThreshold = Mathf.Max(1, highDangerBurstThreshold);
        finalShotAngleOffset = Mathf.Clamp(finalShotAngleOffset, 0f, 20f);
        arenaEdgePadding = Mathf.Max(0f, arenaEdgePadding);

        fireIntervalJitter = Mathf.Clamp(fireIntervalJitter, 0f, 0.30f);
        reloadDurationJitter = Mathf.Clamp(reloadDurationJitter, 0f, 0.20f);
        initialFireDelayMin = Mathf.Max(0f, initialFireDelayMin);
        initialFireDelayMax = Mathf.Max(initialFireDelayMin, initialFireDelayMax);
        postReloadFireDelayMin = Mathf.Max(0f, postReloadFireDelayMin);
        postReloadFireDelayMax = Mathf.Max(postReloadFireDelayMin, postReloadFireDelayMax);

        perEnemyFireCadenceMultiplier = Random.Range(0.94f, 1.06f);
        perEnemyReloadCadenceMultiplier = Random.Range(0.95f, 1.05f);

        reloadVolumeMinMultiplier = Mathf.Clamp(reloadVolumeMinMultiplier, 0.5f, 1f);
        reloadVolumeMaxMultiplier = Mathf.Clamp(reloadVolumeMaxMultiplier, reloadVolumeMinMultiplier, 1f);
        reloadPitchMin = Mathf.Clamp(reloadPitchMin, 0.9f, 1.1f);
        reloadPitchMax = Mathf.Clamp(reloadPitchMax, reloadPitchMin, 1.1f);
        reloadSfxFadeOutDuration = Mathf.Max(0.01f, reloadSfxFadeOutDuration);

        fireCooldown =
            Random.Range(initialFireDelayMin, initialFireDelayMax) +
            GetNextFireInterval() * Random.Range(0.10f, 0.55f);

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

        obstacleAvoidanceSide = unstuckDirection;

        ResetStrafeTimer();

        lastPosition = rb.position;

        FindPlayerIfNeeded();
        RuntimeObjectPool.Prewarm(
            projectilePrefab,
            Mathf.Max(1, poolSize)
        );

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
        EnforceArenaBounds();

        TrackNearMiss();

        if (ApplyPendingShotRecoil())
        {
            EnforceArenaBounds();
            FlipSprite(currentTarget);
            return;
        }

        if (shotRecoilPauseTimer > 0f)
        {
            shotRecoilPauseTimer -= Time.fixedDeltaTime;
            ResetStuckCheck();
            FlipSprite(currentTarget);
            return;
        }

        HandleStrafeDirectionTimer();
        HandleMovement(currentTarget);
        HandleStuckCheck(currentTarget);
        EnforceArenaBounds();
        FlipSprite(currentTarget);
    }

    private void Update()
    {
        FindPlayerIfNeeded();

        if (isSpawning || stopped)
            return;

        UpdateReloadState();

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

    private void StopEnemy()
    {
        if (stopped)
            return;

        stopped = true;
        isReloading = false;
        reloadTimer = 0f;
        reloadVisualTime = 0f;
        activeReloadDuration = 0f;
        shotRecoilPauseTimer = 0f;
        pendingShotRecoil = Vector2.zero;

        RestoreReloadVisuals();
        StopReloadSound();
        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (audioSource != null)
            audioSource.Stop();

        DisableActiveProjectiles();

        enabled = false;
    }

    private IEnumerator SpawnEffect()
    {
        isSpawning = true;

        if (spawnEffectDuration <= 0f)
        {
            transform.localScale =
                spawnTargetScale;

            isSpawning = false;
            EnforceArenaBounds();
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

        EnforceArenaBounds();
        ResetStuckCheck();
    }

    private void OnDestroy()
    {
        RestoreReloadVisuals();
        StopReloadSound();
        DisableActiveProjectiles();
    }

    private void OnValidate()
    {
        shotRecoilDistance = Mathf.Clamp(shotRecoilDistance, 0f, 0.05f);
        shotRecoilPause = Mathf.Clamp(shotRecoilPause, 0f, 0.03f);
        finalShotRecoilMultiplier = Mathf.Clamp(finalShotRecoilMultiplier, 1f, 1.18f);
        lowDangerFinalShotProjectileCount = Mathf.Max(1, lowDangerFinalShotProjectileCount);
        highDangerFinalShotProjectileCount = Mathf.Max(1, highDangerFinalShotProjectileCount);
        highDangerBurstThreshold = Mathf.Max(1, highDangerBurstThreshold);
        finalShotAngleOffset = Mathf.Clamp(finalShotAngleOffset, 0f, 20f);
        arenaEdgePadding = Mathf.Max(0f, arenaEdgePadding);

        fireIntervalJitter = Mathf.Clamp(fireIntervalJitter, 0f, 0.30f);
        reloadDurationJitter = Mathf.Clamp(reloadDurationJitter, 0f, 0.20f);
        initialFireDelayMin = Mathf.Max(0f, initialFireDelayMin);
        initialFireDelayMax = Mathf.Max(initialFireDelayMin, initialFireDelayMax);
        postReloadFireDelayMin = Mathf.Max(0f, postReloadFireDelayMin);
        postReloadFireDelayMax = Mathf.Max(postReloadFireDelayMin, postReloadFireDelayMax);

        shotsPerBurst = Mathf.Max(1, shotsPerBurst);
        reloadDuration = Mathf.Max(0.05f, reloadDuration);
        reloadMoveSpeedMultiplier = Mathf.Max(0.1f, reloadMoveSpeedMultiplier);
        reloadBlinkMinAlpha = Mathf.Clamp(reloadBlinkMinAlpha, 0.05f, 1f);
        reloadBlinkFrequency = Mathf.Max(0.1f, reloadBlinkFrequency);
    }
}
