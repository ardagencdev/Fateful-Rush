using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class HunterEnemyFollow : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public PlayerMovement playerMovement;

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
    public float stunRotationSpeed = 320f;
    public float stunFlashAlpha = 0.45f;

    [Header("Collision")]
    public LayerMask wallLayer;
    public LayerMask obstacleLayer;

    [Header("Sound")]
    public AudioClip[] dashSounds;
    public AudioClip hitSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;

    [Range(0f, 1f)]
    public float hiddenAlpha = 0.15f;

    [Range(0f, 1f)]
    public float visibleAlpha = 1f;

    [Header("Visual Transitions")]
    public float revealDuration = 0.12f;
    public float hideDuration = 0.15f;

    private Rigidbody2D rb;
    private Collider2D col;
    private AudioSource audioSource;

    private PlayerArmor playerArmor;

    private Coroutine mainRoutine;
    private GameObject activeWarning;

    private bool isCharging;
    private bool isStunned;
    private bool stopped;
    private bool cloneAbsorbedCharge;

    private Vector2 chargeDirection;
    private Vector3 spawnScale;

    private AudioClip selectedDashSound;

    private int obstacleLayerIndex;
    private int wallLayerLowerIndex;
    private int wallLayerUpperIndex;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        spawnScale = transform.localScale;

        if (spawnScale == Vector3.zero)
            spawnScale = Vector3.one;

        rb.gravityScale = 0f;
        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;

        if (col != null)
            col.isTrigger = true;

        obstacleLayerIndex =
            LayerMask.NameToLayer("Obstacle");

        wallLayerLowerIndex =
            LayerMask.NameToLayer("wall");

        wallLayerUpperIndex =
            LayerMask.NameToLayer("Wall");
    }

    private void Start()
    {
        FindPlayerIfNeeded();
        SelectDashSound();

        EnterGhostModeImmediate();

        mainRoutine =
            StartCoroutine(HunterRoutine());
    }

    private void OnDisable()
    {
        DestroyWarningLine();
    }

    private void OnDestroy()
    {
        DestroyWarningLine();
    }

    private IEnumerator HunterRoutine()
    {
        while (!stopped)
        {
            FindPlayerIfNeeded();

            Transform currentTarget =
                GetCurrentTarget();

            if (currentTarget == null)
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

            if (stopped)
                yield break;

            yield return WarningRoutine();

            if (stopped)
                yield break;

            yield return ChargeRoutine();

            if (stopped)
                yield break;

            if (isStunned)
            {
                yield return StunRoutine();
            }
            else
            {
                yield return RecoveryRoutine();
            }
        }

        mainRoutine = null;
    }

    private Transform GetCurrentTarget()
    {
        if (VoidCloneAbility.ActiveCloneTarget != null)
            return VoidCloneAbility.ActiveCloneTarget;

        return player;
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

    private void SelectDashSound()
    {
        if (dashSounds == null ||
            dashSounds.Length == 0)
        {
            selectedDashSound = null;
            return;
        }

        selectedDashSound =
            dashSounds[
                Random.Range(0, dashSounds.Length)
            ];
    }

    private IEnumerator RepositionRoutine()
    {
        isCharging = false;
        isStunned = false;
        cloneAbsorbedCharge = false;

        yield return FadeToAlpha(
            hiddenAlpha,
            hideDuration
        );

        if (stopped)
            yield break;

        SetDangerous(false);

        Transform currentTarget =
            GetCurrentTarget();

        if (currentTarget == null)
            yield break;

        Vector2 startPosition =
            rb.position;

        Vector2 targetPosition =
            currentTarget.position;

        Vector2 destination =
            GetValidDashPosition(targetPosition);

        if (repositionTime <= 0f)
        {
            rb.position = destination;
            yield break;
        }

        float timer = 0f;

        while (timer < repositionTime)
        {
            if (IsGameOver())
            {
                StopHunter();
                yield break;
            }

            timer += Time.fixedDeltaTime;

            float t = Mathf.Clamp01(
                timer / repositionTime
            );

            float smoothT =
                Mathf.SmoothStep(0f, 1f, t);

            Vector2 nextPosition =
                Vector2.Lerp(
                    startPosition,
                    destination,
                    smoothT
                );

            nextPosition =
                ClampToCamera(nextPosition);

            rb.MovePosition(nextPosition);

            yield return new WaitForFixedUpdate();
        }

        rb.position = destination;
    }

    private IEnumerator WarningRoutine()
    {
        isCharging = false;
        cloneAbsorbedCharge = false;

        SetDangerous(false);

        yield return FadeToAlpha(
            visibleAlpha,
            revealDuration
        );

        if (stopped)
            yield break;

        Transform currentTarget =
            GetCurrentTarget();

        if (currentTarget == null)
            yield break;

        // Warning başladığı anda hedef konumu kilitlenir.
        Vector2 lockedTargetPosition =
            currentTarget.position;

        chargeDirection =
            lockedTargetPosition - rb.position;

        if (chargeDirection.sqrMagnitude <= 0.001f)
            chargeDirection = Vector2.right;
        else
            chargeDirection.Normalize();

        FlipSprite(chargeDirection);

        DestroyWarningLine();

        if (warningLinePrefab != null)
        {
            activeWarning =
                CreateWarningLine();
        }

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
        isStunned = false;
        cloneAbsorbedCharge = false;

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

            if (isStunned ||
                cloneAbsorbedCharge ||
                !isCharging)
            {
                yield break;
            }

            float moveDistance =
                chargeSpeed * Time.fixedDeltaTime;

            RaycastHit2D hit =
                Physics2D.CircleCast(
                    rb.position,
                    positionCheckRadius,
                    chargeDirection,
                    moveDistance,
                    wallLayer | obstacleLayer
                );

            if (hit.collider != null)
            {
                float safeDistance =
                    Mathf.Max(
                        hit.distance - 0.02f,
                        0f
                    );

                rb.MovePosition(
                    rb.position +
                    chargeDirection * safeDistance
                );

                StopChargeAndStun();
                yield break;
            }

            Vector2 nextPosition =
                rb.position +
                chargeDirection * moveDistance;

            rb.MovePosition(nextPosition);

            timer += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        isCharging = false;
        SetDangerous(false);
    }

    private IEnumerator RecoveryRoutine()
    {
        isCharging = false;
        SetDangerous(false);

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        yield return FadeToAlpha(
            hiddenAlpha,
            hideDuration
        );

        if (stopped)
            yield break;

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

        Color baseColor =
            spriteRenderer != null
                ? spriteRenderer.color
                : Color.white;

        float timer = 0f;

        while (timer < stunDuration)
        {
            if (IsGameOver())
            {
                StopHunter();
                yield break;
            }

            timer += Time.deltaTime;

            transform.Rotate(
                0f,
                0f,
                stunRotationSpeed * Time.deltaTime
            );

            if (spriteRenderer != null)
            {
                float flash =
                    Mathf.PingPong(
                        timer * 8f,
                        1f
                    );

                float alpha =
                    Mathf.Lerp(
                        stunFlashAlpha,
                        visibleAlpha,
                        flash
                    );

                spriteRenderer.color =
                    new Color(
                        baseColor.r,
                        baseColor.g,
                        baseColor.b,
                        alpha
                    );
            }

            yield return null;
        }

        transform.rotation =
            Quaternion.identity;

        isStunned = false;

        SetDangerous(false);

        yield return FadeToAlpha(
            hiddenAlpha,
            hideDuration
        );
    }

    private Vector2 GetValidDashPosition(
        Vector2 targetPosition
    )
    {
        if (CameraWorldBounds.Instance == null)
            return rb.position;

        for (int i = 0;
             i < maxPositionAttempts;
             i++)
        {
            Vector2 randomDirection =
                Random.insideUnitCircle;

            if (randomDirection.sqrMagnitude <=
                0.001f)
            {
                randomDirection =
                    Vector2.right;
            }
            else
            {
                randomDirection.Normalize();
            }

            Vector2 candidate =
                targetPosition +
                randomDirection * prepareDistance;

            candidate =
                ClampToCamera(candidate);

            if (!IsSafePreparationPosition(candidate))
                continue;

            if (!HasClearDashLine(
                    candidate,
                    targetPosition))
            {
                continue;
            }

            return candidate;
        }

        Vector2[] fallbackDirections =
        {
            Vector2.right,
            Vector2.left,
            Vector2.up,
            Vector2.down,

            new Vector2(1f, 1f).normalized,
            new Vector2(-1f, 1f).normalized,
            new Vector2(1f, -1f).normalized,
            new Vector2(-1f, -1f).normalized
        };

        for (int i = 0;
             i < fallbackDirections.Length;
             i++)
        {
            Vector2 candidate =
                targetPosition +
                fallbackDirections[i] *
                prepareDistance;

            candidate =
                ClampToCamera(candidate);

            if (!IsSafePreparationPosition(candidate))
                continue;

            if (!HasClearDashLine(
                    candidate,
                    targetPosition))
            {
                continue;
            }

            return candidate;
        }

        // Güvenli yeni nokta bulunamazsa obstacle içine
        // ışınlanmak yerine mevcut konum korunur.
        return ClampToCamera(rb.position);
    }

    private bool IsSafePreparationPosition(
        Vector2 position
    )
    {
        Collider2D hit =
            Physics2D.OverlapCircle(
                position,
                positionCheckRadius,
                wallLayer | obstacleLayer
            );

        return hit == null;
    }

    private bool HasClearDashLine(
        Vector2 from,
        Vector2 to
    )
    {
        Vector2 direction =
            to - from;

        float distance =
            direction.magnitude;

        if (distance <= 0.001f)
            return false;

        RaycastHit2D hit =
            Physics2D.CircleCast(
                from,
                positionCheckRadius,
                direction.normalized,
                distance,
                wallLayer | obstacleLayer
            );

        return hit.collider == null;
    }

    private Vector2 ClampToCamera(
        Vector2 position
    )
    {
        if (CameraWorldBounds.Instance == null)
            return position;

        float padding =
            Mathf.Max(
                positionCheckRadius,
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

    private GameObject CreateWarningLine()
    {
        Vector2 startPosition =
            rb.position;

        float maximumDashDistance =
            chargeSpeed * maxChargeTime;

        float warningDistance =
            GetRealChargeDistance(
                startPosition,
                chargeDirection,
                maximumDashDistance
            );

        GameObject line =
            Instantiate(
                warningLinePrefab,
                startPosition,
                Quaternion.identity
            );

        line.transform.right =
            chargeDirection;

        SpriteRenderer lineRenderer =
            line.GetComponent<SpriteRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.drawMode =
                SpriteDrawMode.Sliced;

            lineRenderer.size =
                new Vector2(
                    warningDistance,
                    warningLineWidth
                );
        }
        else
        {
            line.transform.localScale =
                new Vector3(
                    warningDistance,
                    warningLineWidth,
                    1f
                );
        }

        line.transform.position =
            startPosition +
            chargeDirection *
            (warningDistance * 0.5f);

        return line;
    }

    private float GetRealChargeDistance(
        Vector2 startPosition,
        Vector2 direction,
        float maximumDistance
    )
    {
        RaycastHit2D hit =
            Physics2D.CircleCast(
                startPosition,
                positionCheckRadius,
                direction,
                maximumDistance,
                wallLayer | obstacleLayer
            );

        if (hit.collider != null)
            return hit.distance;

        return maximumDistance;
    }

    private void DestroyWarningLine()
    {
        if (activeWarning == null)
            return;

        Destroy(activeWarning);
        activeWarning = null;
    }

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (!isCharging ||
            stopped ||
            other == null)
        {
            return;
        }

        if (IsVoidClone(other.gameObject))
        {
            HandleCloneHit();
            return;
        }

        GameObject playerObject =
            FindTaggedObjectInParents(
                other.gameObject,
                "Player"
            );

        if (playerObject != null)
        {
            HandlePlayerHit(playerObject);
            return;
        }

        if (IsWallOrObstacle(
                other.gameObject))
        {
            StopChargeAndStun();
        }
    }

    private bool IsVoidClone(GameObject other)
    {
        if (other == null)
            return false;

        if (other.GetComponent<VoidClone>() != null)
            return true;

        return
            other.GetComponentInParent<VoidClone>()
            != null;
    }

    private void HandleCloneHit()
    {
        if (!isCharging)
            return;

        cloneAbsorbedCharge = true;
        isCharging = false;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        DestroyWarningLine();
        SetDangerous(false);
    }

    private void HandlePlayerHit(
        GameObject playerObject
    )
    {
        if (!isCharging ||
            playerObject == null)
        {
            return;
        }

        if (playerArmor == null)
        {
            playerArmor =
                playerObject.GetComponent<PlayerArmor>();
        }

        if (playerMovement == null)
        {
            playerMovement =
                playerObject.GetComponent<PlayerMovement>();
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
            Destroy(gameObject);
            return;
        }

        if (playerMovement != null &&
            !playerMovement.IsGameOver)
        {
            playerMovement.GameOver("HUNTER");
        }
    }

    private void StopChargeAndStun()
    {
        if (!isCharging || isStunned)
            return;

        isCharging = false;
        isStunned = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        PlaySound(hitSound);
        SetDangerous(false);
    }

    private void EnterGhostModeImmediate()
    {
        SetDangerous(false);
        SetAlpha(hiddenAlpha);
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

    private IEnumerator FadeToAlpha(
        float targetAlpha,
        float duration
    )
    {
        if (spriteRenderer == null)
            yield break;

        Color startColor =
            spriteRenderer.color;

        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            if (stopped)
                yield break;

            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            Color color =
                spriteRenderer.color;

            color.r = startColor.r;
            color.g = startColor.g;
            color.b = startColor.b;

            color.a =
                Mathf.Lerp(
                    startColor.a,
                    targetAlpha,
                    smoothT
                );

            spriteRenderer.color = color;

            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private bool IsObstacle(GameObject obj)
    {
        if (obj == null)
            return false;

        Transform current =
            obj.transform;

        while (current != null)
        {
            GameObject currentObject =
                current.gameObject;

            if (currentObject.CompareTag(
                    "Obstacle"))
            {
                return true;
            }

            if (currentObject.layer ==
                obstacleLayerIndex)
            {
                return true;
            }

            if (((1 << currentObject.layer) &
                 obstacleLayer.value) != 0)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsWall(GameObject obj)
    {
        if (obj == null)
            return false;

        Transform current =
            obj.transform;

        while (current != null)
        {
            GameObject currentObject =
                current.gameObject;

            if (currentObject.CompareTag("Wall"))
                return true;

            int currentLayer =
                currentObject.layer;

            if (wallLayerLowerIndex != -1 &&
                currentLayer ==
                wallLayerLowerIndex)
            {
                return true;
            }

            if (wallLayerUpperIndex != -1 &&
                currentLayer ==
                wallLayerUpperIndex)
            {
                return true;
            }

            if (((1 << currentLayer) &
                 wallLayer.value) != 0)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsWallOrObstacle(
        GameObject obj
    )
    {
        return IsWall(obj) ||
               IsObstacle(obj);
    }

    private GameObject FindTaggedObjectInParents(
        GameObject obj,
        string targetTag
    )
    {
        if (obj == null)
            return null;

        Transform current =
            obj.transform;

        while (current != null)
        {
            if (current.CompareTag(targetTag))
                return current.gameObject;

            current = current.parent;
        }

        return null;
    }

    private bool IsGameOver()
    {
        return playerMovement != null &&
               playerMovement.IsGameOver;
    }

    private void StopHunter()
    {
        if (stopped)
            return;

        stopped = true;
        isCharging = false;
        isStunned = false;

        DestroyWarningLine();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        SetDangerous(false);

        if (audioSource != null)
            audioSource.Stop();

        enabled = false;
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null)
            return;

        Color color =
            spriteRenderer.color;

        color.a =
            Mathf.Clamp01(alpha);

        spriteRenderer.color = color;
    }

    private void FlipSprite(
        Vector2 direction
    )
    {
        Vector3 scale =
            transform.localScale;

        float absoluteX =
            Mathf.Abs(scale.x);

        if (absoluteX <= 0.001f)
            absoluteX = Mathf.Abs(spawnScale.x);

        if (direction.x > 0.01f)
            scale.x = absoluteX;
        else if (direction.x < -0.01f)
            scale.x = -absoluteX;

        transform.localScale = scale;
    }

    private void SetDangerous(bool state)
    {
        gameObject.tag =
            state ? "Enemy" : "Untagged";
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null ||
            audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(
            clip,
            soundVolume *
            SoundManager.SFXVolume
        );
    }
}