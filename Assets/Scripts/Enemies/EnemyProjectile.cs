using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Lifetime")]
    [Min(1f)]
    public float lifeTime = 10f;

    [Header("Smooth Environment Impact")]
    [Min(0.05f)]
    public float impactDisappearDuration = 0.2f;

    public bool shrinkOnImpact = true;
    public bool fadeOnImpact = true;

    [Header("Environment Hit Sound")]
    public AudioClip environmentHitSound;

    [Range(0f, 1f)]
    public float environmentHitSoundVolume = 1f;

    private AudioSource audioSource;

    [Header("Hit Settings")]
    public string gameOverName = "LASER BULLET";

    [Header("Collision Filter")]
    public LayerMask hitLayers;
    public bool useHitLayerFilter = false;

    private Rigidbody2D rb;
    private Collider2D col;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    private Vector3 originalScale;

    private bool stopped;
    private bool disappearing;

    private Coroutine lifeRoutine;
    private Coroutine disappearRoutine;

    private ProjectileEnemyFollow poolOwner;
    private PlayerMovement playerMovement;

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

        spriteRenderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        originalColors =
            new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;

        originalScale = transform.localScale;

        rb.gravityScale = 0f;
        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;

        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private void OnEnable()
    {
        stopped = false;
        disappearing = false;

        transform.localScale = originalScale;

        ResetSpriteColors();

        if (col != null)
            col.enabled = true;
    }

    public void SetPoolOwner(ProjectileEnemyFollow owner)
    {
        poolOwner = owner;
    }

    public void Launch(
        Vector2 direction,
        float speed,
        PlayerMovement playerMov
    )
    {
        playerMovement = playerMov;

        stopped = false;
        disappearing = false;

        transform.localScale = originalScale;
        ResetSpriteColors();

        if (disappearRoutine != null)
        {
            StopCoroutine(disappearRoutine);
            disappearRoutine = null;
        }

        if (col != null)
            col.enabled = true;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = direction.normalized * speed;
        rb.angularVelocity = 0f;

        if (lifeRoutine != null)
            StopCoroutine(lifeRoutine);

        lifeRoutine = StartCoroutine(LifeRoutine());
    }

    private IEnumerator LifeRoutine()
    {
        float timer = 0f;

        while (timer < lifeTime)
        {
            if (playerMovement != null &&
                playerMovement.IsGameOver)
            {
                ReturnToPool();
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        ReturnToPool();
    }

    public void ReturnToPool()
    {
        if (stopped)
            return;

        stopped = true;

        disappearing = false;

        if (lifeRoutine != null)
        {
            StopCoroutine(lifeRoutine);
            lifeRoutine = null;
        }

        if (disappearRoutine != null)
        {
            StopCoroutine(disappearRoutine);
            disappearRoutine = null;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (col != null)
            col.enabled = false;

        transform.localScale = originalScale;
        ResetSpriteColors();

        if (poolOwner != null)
            poolOwner.ReturnProjectileToPool(gameObject);
        else
            gameObject.SetActive(false);
    }

    private void StartSmoothDisappear()
    {
        if (stopped || disappearing)
            return;

        disappearing = true;

        PlayEnvironmentHitSound();

        if (lifeRoutine != null)
        {
            StopCoroutine(lifeRoutine);
            lifeRoutine = null;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Mermi artık başka bir şeye çarpmasın.
        if (col != null)
            col.enabled = false;

        // Inspector'da yanlışlıkla 0 olsa bile efekt direkt bitmesin.
        float safeDisappearDuration =
            Mathf.Max(impactDisappearDuration, 0.05f);

        if (disappearRoutine != null)
            StopCoroutine(disappearRoutine);

        disappearRoutine = StartCoroutine(
            SmoothDisappearRoutine(safeDisappearDuration)
        );
    }

    private IEnumerator SmoothDisappearRoutine(float duration)
    {
        Vector3 startScale = transform.localScale;

        Color[] startColors =
            new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                startColors[i] = spriteRenderers[i].color;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);

            // Biraz daha doğal küçülme.
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (shrinkOnImpact)
            {
                transform.localScale = Vector3.Lerp(
                    startScale,
                    Vector3.zero,
                    smoothT
                );
            }

            if (fadeOnImpact)
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] == null)
                        continue;

                    Color color = startColors[i];
                    color.a = Mathf.Lerp(
                        startColors[i].a,
                        0f,
                        smoothT
                    );

                    spriteRenderers[i].color = color;
                }
            }

            yield return null;
        }

        // ReturnToPool içerisinde görüntü ve scale resetlenecek.
        disappearing = false;
        disappearRoutine = null;

        ReturnToPool();
    }

    private void ResetSpriteColors()
    {
        if (spriteRenderers == null ||
            originalColors == null)
        {
            return;
        }

        int count = Mathf.Min(
            spriteRenderers.Length,
            originalColors.Length
        );

        for (int i = 0; i < count; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = originalColors[i];
        }
    }

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        if (stopped || disappearing)
            return;

        HandleHit(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (stopped || disappearing)
            return;

        if (IsVoidClone(other.gameObject))
        {
            ReturnToPool();
            return;
        }

        if (ShouldIgnoreTrigger(other))
            return;

        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject other)
    {
        if (other == null)
            return;

        if (IsVoidClone(other))
        {
            ReturnToPool();
            return;
        }

        if (other.CompareTag("Player"))
        {
            HandlePlayerHit(other);
            return;
        }

        // Wall veya Obstacle, collider child objede olsa bile
        // önce smooth kaybolma çalışır.
        if (IsEnvironmentHit(other))
        {
            StartSmoothDisappear();
            return;
        }

        if (useHitLayerFilter &&
            ((1 << other.layer) & hitLayers) == 0)
        {
            return;
        }

        if (ShouldReturnOnHit(other))
            ReturnToPool();
    }

    private bool IsVoidClone(GameObject other)
    {
        if (other == null)
            return false;

        if (other.GetComponent<VoidClone>() != null)
            return true;

        return other.GetComponentInParent<VoidClone>() != null;
    }

    private void HandlePlayerHit(GameObject playerObj)
    {
        PlayerArmor armor =
            playerObj.GetComponent<PlayerArmor>();

        PlayerMovement player =
            playerObj.GetComponent<PlayerMovement>();

        if (armor != null && armor.IsImmune)
        {
            ReturnToPool();
            return;
        }

        if (armor != null && armor.HasArmor)
        {
            armor.BreakArmor();
            ReturnToPool();
            return;
        }

        if (player != null && !player.IsGameOver)
            player.GameOver(gameOverName);

        ReturnToPool();
    }

    private bool IsEnvironmentHit(GameObject other)
    {
        if (other == null)
            return false;

        int wallLayerLower = LayerMask.NameToLayer("wall");
        int wallLayerUpper = LayerMask.NameToLayer("Wall");
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");

        Transform current = other.transform;

        while (current != null)
        {
            GameObject currentObject = current.gameObject;

            // Tag kontrolü
            if (currentObject.CompareTag("Wall") ||
                currentObject.CompareTag("Obstacle"))
            {
                return true;
            }

            // Layer kontrolü
            int currentLayer = currentObject.layer;

            if ((wallLayerLower != -1 &&
                 currentLayer == wallLayerLower) ||
                (wallLayerUpper != -1 &&
                 currentLayer == wallLayerUpper) ||
                (obstacleLayer != -1 &&
                 currentLayer == obstacleLayer))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool ShouldReturnOnHit(GameObject other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Enemy"))
            return false;

        if (other.CompareTag("Player"))
            return true;

        // Wall ve Obstacle burada ReturnToPool yapmıyor.
        // Onları IsEnvironmentHit yakalayıp smooth şekilde kapatıyor.
        if (IsEnvironmentHit(other))
            return false;

        if (useHitLayerFilter)
        {
            return ((1 << other.layer) & hitLayers) != 0;
        }

        return false;
    }

    private bool ShouldIgnoreTrigger(Collider2D other)
    {
        if (other == null)
            return true;

        if (IsVoidClone(other.gameObject))
            return false;

        if (other.GetComponent<BeaconPulseWave>() != null)
            return true;

        if (other.GetComponentInParent<BeaconPulseWave>() != null)
            return true;

        if (other.CompareTag("Enemy"))
            return true;

        if (other.CompareTag("Coin"))
            return true;

        if (other.CompareTag("PowerUp"))
            return true;

        // Wall veya obstacle tag/layer/parent üzerinden bulunduysa
        // trigger olsa bile ignore etme.
        if (IsEnvironmentHit(other.gameObject))
            return false;

        if (other.CompareTag("Player"))
            return false;

        if (other.isTrigger)
            return true;

        return false;
    }

    private void PlayEnvironmentHitSound()
    {
        if (environmentHitSound == null ||
            audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(
            environmentHitSound,
            SoundManager.SFXVolume *
            environmentHitSoundVolume
        );
    }
}