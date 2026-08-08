using UnityEngine;

public enum CoinType
{
    Normal = 0,
    Gold = 1,
    Rare = 2
}

public class Coin : MonoBehaviour
{
    [Min(1)]
    public int value = 1;

    [SerializeField]
    private CoinType coinType = CoinType.Normal;

    private bool isCollected;
    private Collider2D[] cachedColliders;
    private Rigidbody2D[] cachedRigidbodies;

    public bool IsCollected => isCollected;
    public CoinType Type => coinType;

    private void Awake()
    {
        CachePhysics();
    }

    private void OnEnable()
    {
        isCollected = false;
        RestorePhysicsAndCollisions();
    }

    public void Configure(CoinType type, int coinValue)
    {
        coinType = type;
        value = Mathf.Max(1, coinValue);
    }

    /// <summary>
    /// Coin toplama işlemini yalnızca bir kez başlatır.
    /// Aynı fizik karesinde birden fazla trigger çağrısı gelse bile
    /// skorun ikinci kez eklenmesini engeller.
    /// </summary>
    public bool TryBeginCollection()
    {
        if (isCollected)
            return false;

        isCollected = true;
        SpawnAreaRegistry.Unregister(gameObject);
        DisablePhysicsAndCollisions();
        return true;
    }

    private void CachePhysics()
    {
        cachedColliders =
            GetComponentsInChildren<Collider2D>(true);

        cachedRigidbodies =
            GetComponentsInChildren<Rigidbody2D>(true);
    }

    private void DisablePhysicsAndCollisions()
    {
        if (cachedColliders == null || cachedRigidbodies == null)
            CachePhysics();

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            Collider2D collider = cachedColliders[i];
            if (collider != null)
                collider.enabled = false;
        }

        for (int i = 0; i < cachedRigidbodies.Length; i++)
        {
            Rigidbody2D body = cachedRigidbodies[i];
            if (body != null)
                body.simulated = false;
        }
    }

    private void RestorePhysicsAndCollisions()
    {
        if (cachedColliders == null || cachedRigidbodies == null)
            CachePhysics();

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            Collider2D collider = cachedColliders[i];
            if (collider != null)
                collider.enabled = true;
        }

        for (int i = 0; i < cachedRigidbodies.Length; i++)
        {
            Rigidbody2D body = cachedRigidbodies[i];
            if (body != null)
                body.simulated = true;
        }
    }

    private void OnDisable()
    {
        SpawnAreaRegistry.Unregister(gameObject);
    }
}
