using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Lifetime")]
    public float lifeTime = 3f;

    [Header("Hit Settings")]
    public string gameOverName = "LASER BULLET";

    [Header("Collision Filter")]
    public LayerMask hitLayers;
    public bool useHitLayerFilter = false;

    private Rigidbody2D rb;
    private Collider2D col;

    private bool stopped;
    private Coroutine lifeRoutine;

    private ProjectileEnemyFollow poolOwner;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private void OnEnable()
    {
        stopped = false;

        if (col != null)
            col.enabled = true;
    }

    public void SetPoolOwner(ProjectileEnemyFollow owner)
    {
        poolOwner = owner;
    }

    public void Launch(Vector2 direction, float speed, PlayerMovement playerMov)
    {
        playerMovement = playerMov;
        stopped = false;

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
            if (playerMovement != null && playerMovement.IsGameOver)
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
        if (stopped) return;

        stopped = true;

        if (lifeRoutine != null)
        {
            StopCoroutine(lifeRoutine);
            lifeRoutine = null;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (col != null)
            col.enabled = false;

        if (poolOwner != null)
            poolOwner.ReturnProjectileToPool(gameObject);
        else
            gameObject.SetActive(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (stopped) return;

        HandleHit(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (stopped) return;

        if (ShouldIgnoreTrigger(other))
            return;

        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject other)
    {
        if (other == null) return;

        if (useHitLayerFilter && ((1 << other.layer) & hitLayers) == 0)
            return;

        if (other.CompareTag("Player"))
        {
            HandlePlayerHit(other);
            return;
        }

        if (ShouldReturnOnHit(other))
            ReturnToPool();
    }

    private void HandlePlayerHit(GameObject playerObj)
    {
        PlayerArmor armor = playerObj.GetComponent<PlayerArmor>();
        PlayerMovement player = playerObj.GetComponent<PlayerMovement>();

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

    private bool ShouldReturnOnHit(GameObject other)
    {
        if (other.CompareTag("Enemy"))
            return false;

        if (other.CompareTag("Player"))
            return true;

        if (other.CompareTag("Wall"))
            return true;

        if (other.CompareTag("Obstacle"))
            return true;

        if (useHitLayerFilter)
            return ((1 << other.layer) & hitLayers) != 0;

        return false;
    }

    private bool ShouldIgnoreTrigger(Collider2D other)
    {
        if (other == null) return true;

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

        if (other.isTrigger && !other.CompareTag("Player") && !other.CompareTag("Wall") && !other.CompareTag("Obstacle"))
            return true;

        return false;
    }
}