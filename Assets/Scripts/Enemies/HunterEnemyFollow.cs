using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class HunterEnemyFollow : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public PlayerMovement playerMovement;

    [Header("Stun Visual")]
    public float stunRotationSpeed = 320f;
    public float stunFlashAlpha = 0.45f;

    [Header("Positioning")]
    public float prepareDistance = 6f;
    public float repositionTime = 1.2f;
    public float recoveryTime = 0.25f;
    public float positionCheckRadius = 0.45f;
    public int maxPositionAttempts = 80;

    [Header("Warning")]
    public float warningDuration = 1f;
    public GameObject warningLinePrefab;
    public float warningLineWidth = 0.25f;

    [Header("Charge")]
    public float chargeSpeed = 15f;
    public float maxChargeTime = 0.35f;

    [Header("Stun")]
    public float stunDuration = 1f;

    [Header("Collision")]
    public LayerMask wallLayer;
    public LayerMask obstacleLayer;

    [Header("Sound")]
    public AudioClip[] dashSounds;
    public AudioClip hitSound;
    public float soundVolume = 1f;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public float hiddenAlpha = 0.15f;
    public float visibleAlpha = 1f;

    private Rigidbody2D rb;
    private Collider2D col;
    private AudioSource audioSource;
    private PlayerArmor playerArmor;

    private Coroutine mainRoutine;
    private GameObject activeWarning;

    private bool isCharging;
    private bool isStunned;
    private bool stopped;

    private Vector2 chargeDirection;
    private Vector3 spawnScale;
    private AudioClip selectedDashSound;

    private int obstacleLayerIndex;
    private int wallLayerIndex;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        spawnScale = transform.localScale;

        if (spawnScale == Vector3.zero)
            spawnScale = Vector3.one;

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;

        if (col != null)
            col.isTrigger = true;

        obstacleLayerIndex = LayerMask.NameToLayer("Obstacle");
        wallLayerIndex = LayerMask.NameToLayer("Wall");
    }

    private void Start()
    {
        FindPlayerIfNeeded();

        if (dashSounds != null && dashSounds.Length > 0)
            selectedDashSound = dashSounds[Random.Range(0, dashSounds.Length)];

        EnterGhostMode();

        mainRoutine = StartCoroutine(HunterRoutine());
    }

    private IEnumerator HunterRoutine()
    {
        while (!stopped)
        {
            FindPlayerIfNeeded();

            if (player == null)
            {
                yield return null;
                continue;
            }

            if (IsGameOver())
            {
                StopHunter();
                yield break;
            }

            yield return RepositionRoutine();
            yield return WarningRoutine();
            yield return ChargeRoutine();
            yield return RecoveryRoutine();
        }
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
        {
            if (playerMovement == null)
                playerMovement = player.GetComponent<PlayerMovement>();

            if (playerArmor == null)
                playerArmor = player.GetComponent<PlayerArmor>();

            return;
        }

        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");

        if (foundPlayer == null)
            return;

        player = foundPlayer.transform;
        playerMovement = foundPlayer.GetComponent<PlayerMovement>();
        playerArmor = foundPlayer.GetComponent<PlayerArmor>();
    }

    private IEnumerator RepositionRoutine()
    {
        isCharging = false;
        isStunned = false;

        EnterGhostMode();

        Vector2 startPos = rb.position;
        Vector2 targetPos = GetValidDashPosition();

        float timer = 0f;

        while (timer < repositionTime)
        {
            if (IsGameOver())
            {
                StopHunter();
                yield break;
            }

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / repositionTime);

            rb.MovePosition(Vector2.Lerp(startPos, targetPos, t));

            yield return null;
        }

        rb.MovePosition(targetPos);
    }

    private IEnumerator WarningRoutine()
    {
        isCharging = false;

        EnterWarningMode();

        if (player == null)
            yield break;

        Vector2 lockedPlayerPos = player.position;
        chargeDirection = (lockedPlayerPos - rb.position).normalized;

        if (chargeDirection.sqrMagnitude <= 0.001f)
            chargeDirection = Vector2.right;

        FlipSprite(chargeDirection);

        if (warningLinePrefab != null)
            activeWarning = CreateWarningLine();

        float timer = 0f;

        while (timer < warningDuration)
        {
            if (IsGameOver())
            {
                DestroyWarningLine();
                StopHunter();
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        DestroyWarningLine();
    }

    private IEnumerator ChargeRoutine()
    {
        isCharging = true;
        EnterChargeMode();

        PlaySound(selectedDashSound);

        float timer = 0f;

        while (timer < maxChargeTime)
        {
            if (IsGameOver())
            {
                StopHunter();
                yield break;
            }

            if (isStunned)
                yield break;

            float moveDistance = chargeSpeed * Time.fixedDeltaTime;

            RaycastHit2D hit = Physics2D.CircleCast(
                rb.position,
                positionCheckRadius,
                chargeDirection,
                moveDistance,
                wallLayer | obstacleLayer
            );

            if (hit.collider != null)
            {
                rb.MovePosition(rb.position + chargeDirection * Mathf.Max(hit.distance - 0.02f, 0f));
                StopChargeAndStun();
                yield break;
            }

            rb.MovePosition(rb.position + chargeDirection * moveDistance);

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isCharging = false;
        EnterGhostMode();
    }

    private IEnumerator RecoveryRoutine()
    {
        EnterGhostMode();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        float timer = 0f;

        while (timer < recoveryTime)
        {
            if (IsGameOver())
            {
                StopHunter();
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator StunRoutine()
    {
        isCharging = false;
        isStunned = true;

        DestroyWarningLine();
        EnterStunMode();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Color baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        float timer = 0f;

        while (timer < stunDuration)
        {
            if (IsGameOver())
            {
                StopHunter();
                yield break;
            }

            timer += Time.deltaTime;

            transform.Rotate(0f, 0f, stunRotationSpeed * Time.deltaTime);

            if (spriteRenderer != null)
            {
                float flash = Mathf.PingPong(timer * 8f, 1f);
                float alpha = Mathf.Lerp(stunFlashAlpha, visibleAlpha, flash);
                spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }

            yield return null;
        }

        transform.rotation = Quaternion.identity;

        isStunned = false;

        EnterGhostMode();

        if (!stopped)
            mainRoutine = StartCoroutine(HunterRoutine());
    }

    private Vector2 GetValidDashPosition()
    {
        if (CameraWorldBounds.Instance == null || player == null)
            return rb.position;

        for (int i = 0; i < maxPositionAttempts; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;

            if (randomDir.sqrMagnitude <= 0.001f)
                randomDir = Vector2.right;

            Vector2 candidate = (Vector2)player.position + randomDir * prepareDistance;
            candidate = ClampToCamera(candidate);

            if (!IsPositionOutsideObstacles(candidate))
                continue;

            if (!HasClearDashLine(candidate, player.position))
                continue;

            return candidate;
        }

        Vector2[] directions =
        {
            Vector2.right,
            Vector2.left,
            Vector2.up,
            Vector2.down
        };

        foreach (Vector2 dir in directions)
        {
            Vector2 candidate = ClampToCamera((Vector2)player.position + dir * prepareDistance);

            if (IsPositionOutsideObstacles(candidate) && HasClearDashLine(candidate, player.position))
                return candidate;
        }

        return ClampToCamera((Vector2)player.position + Vector2.right * prepareDistance);
    }

    private bool IsPositionOutsideObstacles(Vector2 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, positionCheckRadius, obstacleLayer);
        return hit == null;
    }

    private bool HasClearDashLine(Vector2 from, Vector2 to)
    {
        Vector2 direction = to - from;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return false;

        RaycastHit2D hit = Physics2D.CircleCast(
            from,
            positionCheckRadius,
            direction.normalized,
            distance,
            obstacleLayer | wallLayer
        );

        return hit.collider == null;
    }

    private Vector2 ClampToCamera(Vector2 pos)
    {
        if (CameraWorldBounds.Instance == null)
            return pos;

        pos.x = Mathf.Clamp(pos.x, CameraWorldBounds.Instance.MinX + 0.7f, CameraWorldBounds.Instance.MaxX - 0.7f);
        pos.y = Mathf.Clamp(pos.y, CameraWorldBounds.Instance.MinY + 0.7f, CameraWorldBounds.Instance.MaxY - 0.7f);

        return pos;
    }

    private GameObject CreateWarningLine()
    {
        Vector2 startPos = rb.position;

        float maxDashDistance = chargeSpeed * maxChargeTime;
        float warningDistance = GetRealChargeDistance(startPos, chargeDirection, maxDashDistance);

        GameObject line = Instantiate(warningLinePrefab, startPos, Quaternion.identity);
        line.transform.right = chargeDirection;

        SpriteRenderer lineRenderer = line.GetComponent<SpriteRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.drawMode = SpriteDrawMode.Sliced;
            lineRenderer.size = new Vector2(warningDistance, warningLineWidth);
        }
        else
        {
            line.transform.localScale = new Vector3(warningDistance, warningLineWidth, 1f);
        }

        line.transform.position = startPos + chargeDirection * (warningDistance * 0.5f);

        return line;
    }

    private float GetRealChargeDistance(Vector2 startPos, Vector2 direction, float maxDistance)
    {
        LayerMask hitMask = wallLayer | obstacleLayer;

        RaycastHit2D hit = Physics2D.CircleCast(
            startPos,
            positionCheckRadius,
            direction,
            maxDistance,
            hitMask
        );

        if (hit.collider != null)
            return hit.distance;

        return maxDistance;
    }

    private void DestroyWarningLine()
    {
        if (activeWarning != null)
        {
            Destroy(activeWarning);
            activeWarning = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isCharging)
            return;

        if (other.CompareTag("Player"))
        {
            HandlePlayerHit(other.gameObject);
            return;
        }

        if (IsWallOrObstacle(other.gameObject))
            StopChargeAndStun();
    }

    private void HandlePlayerHit(GameObject playerObj)
    {
        if (!isCharging)
            return;

        if (playerArmor == null)
            playerArmor = playerObj.GetComponent<PlayerArmor>();

        if (playerMovement == null)
            playerMovement = playerObj.GetComponent<PlayerMovement>();

        if (playerArmor != null && playerArmor.IsImmune)
            return;

        if (playerArmor != null && playerArmor.HasArmor)
        {
            playerArmor.BreakArmor();
            Destroy(gameObject);
            return;
        }

        if (playerMovement != null && !playerMovement.IsGameOver)
            playerMovement.GameOver("HUNTER");
    }

    private void StopChargeAndStun()
    {
        if (isStunned)
            return;

        PlaySound(hitSound);

        if (mainRoutine != null)
        {
            StopCoroutine(mainRoutine);
            mainRoutine = null;
        }

        isCharging = false;
        mainRoutine = StartCoroutine(StunRoutine());
    }

    private void EnterGhostMode()
    {
        SetDangerous(false);
        SetAlpha(hiddenAlpha);
    }

    private void EnterWarningMode()
    {
        SetDangerous(false);
        SetAlpha(visibleAlpha);
    }

    private void EnterChargeMode()
    {
        SetDangerous(true);
        SetAlpha(visibleAlpha);
    }

    private void EnterStunMode()
    {
        SetDangerous(false);
        SetAlpha(stunFlashAlpha);
    }

    private bool IsObstacle(GameObject obj)
    {
        return obj.layer == obstacleLayerIndex ||
               obj.CompareTag("Obstacle") ||
               ((1 << obj.layer) & obstacleLayer) != 0;
    }

    private bool IsWall(GameObject obj)
    {
        return obj.layer == wallLayerIndex ||
               obj.CompareTag("Wall") ||
               ((1 << obj.layer) & wallLayer) != 0;
    }

    private bool IsWallOrObstacle(GameObject obj)
    {
        return IsWall(obj) || IsObstacle(obj);
    }

    private bool IsGameOver()
    {
        return playerMovement != null && playerMovement.IsGameOver;
    }

    private void StopHunter()
    {
        stopped = true;
        isCharging = false;
        isStunned = false;

        DestroyWarningLine();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        SetDangerous(false);
        enabled = false;
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null)
            return;

        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
    }

    private void FlipSprite(Vector2 direction)
    {
        Vector3 scale = transform.localScale;

        float absX = Mathf.Abs(scale.x);

        if (absX <= 0.001f)
            absX = Mathf.Abs(spawnScale.x);

        if (direction.x > 0.01f)
            scale.x = absX;
        else if (direction.x < -0.01f)
            scale.x = -absX;

        transform.localScale = scale;
    }

    private void SetDangerous(bool state)
    {
        gameObject.tag = state ? "Enemy" : "Untagged";
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip, soundVolume * SoundManager.SFXVolume);
    }
}