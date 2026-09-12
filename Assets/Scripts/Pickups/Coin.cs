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

    private const float Combo4MagnetRadius = 1.25f;
    private const float Combo4MagnetMaxSpeed = 5.5f;

    private bool isCollected;
    private Collider2D[] cachedColliders;
    private Rigidbody2D[] cachedRigidbodies;
    private Vector3 magnetVelocity;
    private bool wasMagnetAffected;

    public bool IsCollected => isCollected;
    public CoinType Type => coinType;
    public bool WasMagnetAffected => wasMagnetAffected;

    private void Awake()
    {
        CachePhysics();
    }

    private void OnEnable()
    {
        isCollected = false;
        magnetVelocity = Vector3.zero;
        wasMagnetAffected = false;
        RestorePhysicsAndCollisions();
    }

    private void Update()
    {
        if (isCollected)
            return;

        PlayerCoinCollector collector =
            PlayerCoinCollector.Instance;

        if (collector == null)
        {
            magnetVelocity = Vector3.zero;
            return;
        }

        bool hasMagnet = collector.TryGetComboMagnetSettings(
            transform.position,
            out float maxSpeed,
            out float smoothTime
        );

        if (!hasMagnet)
        {
            hasMagnet = TryGetCombo4MagnetSettings(
                collector,
                transform.position,
                out maxSpeed,
                out smoothTime
            );
        }

        if (!hasMagnet)
        {
            magnetVelocity = Vector3.zero;
            return;
        }

        wasMagnetAffected = true;

        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = collector.transform.position;
        targetPosition.z = currentPosition.z;

        transform.position = Vector3.SmoothDamp(
            currentPosition,
            targetPosition,
            ref magnetVelocity,
            smoothTime,
            maxSpeed,
            Time.deltaTime
        );
    }

    private static bool TryGetCombo4MagnetSettings(
        PlayerCoinCollector collector,
        Vector3 coinPosition,
        out float maxSpeed,
        out float smoothTime)
    {
        maxSpeed = 0f;
        smoothTime = collector != null
            ? Mathf.Max(0.04f, collector.comboMagnetSmoothTime)
            : 0.16f;

        if (collector == null ||
            !collector.comboMagnetEnabled ||
            !collector.comboEnabled ||
            collector.Combo != 4 ||
            GameStateManager.IsGameplayEnded ||
            !GameStateManager.IsGameplayStarted)
        {
            return false;
        }

        float distance = Vector2.Distance(
            collector.transform.position,
            coinPosition
        );

        if (distance > Combo4MagnetRadius)
            return false;

        float normalized = 1f - Mathf.Clamp01(
            distance / Combo4MagnetRadius
        );

        float edgeFactor = Mathf.Clamp01(
            collector.comboMagnetEdgeSpeedFactor
        );

        float speedFactor = Mathf.SmoothStep(
            edgeFactor,
            1f,
            normalized
        );

        maxSpeed = Mathf.Max(
            0.1f,
            Combo4MagnetMaxSpeed * speedFactor
        );
        return true;
    }

    public void Configure(CoinType type, int coinValue)
    {
        coinType = type;
        value = Mathf.Max(1, coinValue);
    }

    public bool TryBeginCollection()
    {
        if (isCollected)
            return false;

        isCollected = true;
        magnetVelocity = Vector3.zero;
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
        magnetVelocity = Vector3.zero;
        SpawnAreaRegistry.Unregister(gameObject);
    }
}
