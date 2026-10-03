using System.Collections;
using UnityEngine;

// Dynamic AOE danger preview.
// CPU sadece 1D angular visibility texture'i belirli araliklarla raycast ile gunceller.
// Full-screen danger/safe cizimi, wave, gradient ve fade tamamen shader tarafinda yapilir.

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public partial class BossEnemyFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public float speed = 1.2f;

    [Tooltip("Boss normal chase durumundayken kullanilan minimal shake miktari.")]
    [Min(0f)] public float normalShakeAmount = 0.015f;

    [Tooltip("Hedef yonune donusun ne kadar yumusak olacagi.")]
    public float directionSmoothness = 7f;

    [Header("Collision")]
    public LayerMask solidLayers;
    public float castSkin = 0.05f;
    public float obstacleProbeDistance = 1f;
    [Range(0f, 1f)] public float obstacleOutwardBias = 0.3f;

    [Tooltip("Dogrudan hareket mumkun degilse kac farkli kayma acisi denenecek.")]
    [Range(1, 8)]
    public int slideDirectionAttempts = 4;

    [Header("Advanced Unstuck")]
    public LayerMask obstacleLayer;
    public float escapeCheckRadius = 1.2f;
    public float escapeSpeedMultiplier = 2.2f;

    [Header("Boss Route Planning")]
    [Tooltip("Bossun obstacle/corner problemlerini onceden gorebilmesi icin kullanilan uzun menzilli shape-cast mesafesi.")]
    [Min(1f)] public float routeLookAheadDistance = 3.6f;

    [Tooltip("Boss bir obstacle'in hangi tarafindan dolanacagina karar verdikten sonra bu kadar sure o tarafa sadik kalir. Ayni dar koridora tekrar tekrar donmesini engeller.")]
    [Min(0.1f)] public float routeCommitDuration = 1.15f;

    [Tooltip("Uzun menzilli rota adaylarinin kac farkli acida test edilecegi.")]
    [Range(4, 9)] public int routeAngleSamples = 7;

    [Tooltip("Boss collider'i ile ekran siniri arasinda birakilacak ekstra guvenlik boslugu.")]
    [Min(0f)] public float arenaEdgePadding = 0.10f;

    [Tooltip("Rota seciminde mevcut commit edilen tarafa verilen bonus. Yuksek deger sag-sol kararsizligini azaltir.")]
    [Range(0f, 1f)] public float routeSideCommitment = 0.35f;

    [Header("Spawn Effect")]
    [Tooltip("Diger enemyler gibi Boss da 0 scale'dan kendi boyutuna smooth sekilde gelir.")]
    [Min(0f)] public float spawnEffectDuration = 0.15f;

    [Header("Boss Global AOE")]
    public bool aoeEnabled = true;

    [Tooltip("Stalker absorption bittikten sonra ilk AOE charge baslamadan once beklenecek sure.")]
    [Min(0f)] public float firstAoeDelay = 1.5f;

    [Tooltip("Bir AOE bittikten sonra yeni AOE charge baslayana kadar Boss playeri kovalar.")]
    [Min(0f)] public float aoeCooldown = 5f;

    [Tooltip("Boss bu sure boyunca tamamen sabit kalir. Shake ve kirmizi danger preview 0'dan maksimuma dogru artar; sure bitince strike gerceklesir.")]
    [Min(3f)] public float aoeChargeDuration = 3f;

    [Tooltip("AOE patlamasina yaklasirken ulasilacak maksimum shake miktari.")]
    [Min(0f)] public float aoeMaxShakeAmount = 0.18f;

    [Tooltip("0 birakilirsa obstacleLayer cover icin kullanilir.")]
    public LayerMask aoeCoverLayers;

    [Header("Boss Power-Up")]
    [Tooltip("Tum Stalkerlar emildikten sonra Bossun kalici olarak buyuyecegi oran.")]
    [Min(1f)] public float powerUpScaleMultiplier = 1.12f;

    [Tooltip("Final buyumeden once hedef boyutun kac kere hizlica gorunup kaybolacagi.")]
    [Range(0, 4)] public int powerUpPreviewFlashes = 2;

    [Tooltip("Hedef boyut preview'inin ekranda kaldigi cok kisa sure.")]
    [Min(0.01f)] public float powerUpPreviewOnDuration = 0.055f;

    [Tooltip("Preview flashlari arasindaki cok kisa bosluk.")]
    [Min(0.01f)] public float powerUpPreviewOffDuration = 0.035f;

    [Tooltip("Preview ghost'un alpha degeri.")]
    [Range(0.05f, 1f)] public float powerUpPreviewAlpha = 0.55f;

    [Tooltip("Preview bittikten sonra Bossun asil boyutuna yumusak gecis suresi.")]
    [Min(0.01f)] public float powerUpGrowDuration = 0.12f;

    public AudioClip powerUpSfx;

    [Header("Boss AOE Danger Preview / SFX")]
    [Tooltip("AOE charge boyunca 0 alphadan bu maksimum renge dogru ilerler. Safe alanlar obstacle arkalarinda tamamen bos kalir.")]
    public Color dangerPreviewColor =
        new Color(1f, 0.025f, 0.025f, 0.46f);

    [Tooltip("AOE strike gerceklestigi anda Boss merkezinden disariya yayilan parlak shockwave'in suresi.")]
    [Min(0.05f)] public float dangerStrikeWaveDuration = 0.38f;

    [Tooltip("Strike shockwave halkasinin radial genisligi.")]
    [Range(0.03f, 0.30f)] public float dangerStrikeWaveWidth = 0.09f;

    [Tooltip("Strike shockwave'in normal red danger alanina gore ekstra parlaklik/alpha gucu.")]
    [Range(0f, 3f)] public float dangerStrikeWaveBoost = 1.55f;

    [Tooltip("AOE strike + shockwave bittikten sonra kirmizi alanin alpha ile tamamen kaybolma suresi. Bu fade bitene kadar Boss sabit kalir.")]
    [Min(0.05f)] public float dangerPreviewFadeOutDuration = 0.6f;

    [Tooltip("Boss sprite/collider kenari ile kirmizi dalganin baslangici arasindaki bosluk. Bossun ustune kirmizi binmesini engeller.")]
    [Min(0f)] public float dangerPreviewInnerPadding = 0.16f;

    [Tooltip("Boss cevresindeki kirmizi bolgenin alpha carpani. Ekran kenarina dogru alpha artar.")]
    [Range(0.05f, 1f)] public float dangerPreviewInnerAlphaMultiplier = 0.22f;

    [Tooltip("Dalga on cephesinin yumusaklik/genislik miktari.")]
    [Range(0.03f, 0.3f)] public float dangerPreviewWaveFrontWidth = 0.12f;

    [Tooltip("Disariya ilerleyen dalga on cephesinin ekstra parlaklik/alpha gucu.")]
    [Range(0f, 2f)] public float dangerPreviewWaveFrontBoost = 0.75f;

    [Tooltip("Boss cevresindeki kirmizinin ne kadar koyu baslayacagi. Ekran kenarina dogru normal renge doner.")]
    [Range(0.1f, 1f)] public float dangerPreviewInnerBrightness = 0.55f;

    [Tooltip("Merkezden disariya kac halka kullanilacagi. Yuksek deger dalga gradientini daha yumusak yapar.")]
    [Range(6, 24)] public int dangerPreviewRadialSegments = 14;

    [Tooltip("Obstacle golge kenarlarindaki pütürleri azaltmak icin kac smoothing pass uygulanacagi.")]
    [Range(0, 4)] public int dangerPreviewEdgeSmoothingPasses = 2;

    [Tooltip("Obstacle arkasi safe alanlarin acisal hassasiyeti. Kod en az 360 ray kullanir.")]
    [Range(360, 960)] public int dangerPreviewRayCount = 360;

    [Tooltip("Danger preview icin kullanilan shader. Bos birakilirsa FatefulRush/BossDangerPreview Shader.Find ile aranir.")]
    public Shader dangerPreviewShader;

    [Tooltip("Hareketli obstacle cover bilgisinin saniyede kac kez guncellenecegi. 12-20 arasi mobil icin ideal.")]
    [Range(5f, 30f)] public float dangerPreviewVisibilityRefreshRate = 15f;

    [Tooltip("Obstacle safe-area kenarlarinin dunya birimi cinsinden yumusaklik miktari.")]
    [Range(0.01f, 0.35f)] public float dangerPreviewCoverFeather = 0.08f;

    [Tooltip("Danger mesh'in SpriteRenderer'larin ustunde gorunmesi icin sorting order.")]
    public int dangerPreviewSortingOrder = 20000;

    public AudioClip aoeSfx;

    [Header("Split Settings")]
    public GameObject miniBossPrefab;
    public bool canSplit = true;
    public float miniBossSpeed = 2.5f;
    public float splitDelay = 0.8f;
    public float splitDistance = 1.2f;
    public float splitShakeAmount = 0.18f;
    public Color splitFlashColor =
        new Color(0.45f, 0f, 0f, 1f);
    public float flashSpeed = 0.08f;

    [Tooltip("Ikinci MiniBossun ilk AOE'si, birinciden bu kadar daha gec hazir olur.")]
    [Min(0f)] public float miniBossAoeStagger = 1.25f;

    [Header("Split Visual")]
    public float splitScaleMin = 0.92f;
    public float splitScaleMax = 1.08f;

    [Tooltip("Split bittiginde Boss kuculerek kaybolur.")]
    public float splitDisappearDuration = 0.12f;

    [Header("Stuck Fix")]
    public float stuckCheckTime = 0.5f;
    public float stuckDistance = 0.08f;
    public float unstuckDuration = 0.5f;
    public float unstuckSideForce = 1.5f;

    private PlayerMovement playerMovement;
    private PlayerArmor playerArmor;

    private Rigidbody2D rb;
    private Collider2D bossCollider;
    private SpriteRenderer spriteRenderer;

    private Vector2 lastPosition;
    private Vector2 smoothedDirection;

    private float stuckTimer;
    private float unstuckTimer;
    private int unstuckDirection = 1;

    private bool isSplitting;
    private bool stopped;
    private bool isSpawning;
    private Coroutine spawnRoutine;

    private AudioSource bossSfxSource;
    private float bossSfxBasePitch = 1f;
    private bool bossSfxFollowsGameTime;
    private bool bossSfxPausedByGame;
    private float bossSfxVolumeMultiplier = 1f;

    private Color originalColor;
    private Vector3 originalScale;
    private Vector3 currentScaleMagnitude;
    private float scaleSignY = 1f;
    private float scaleSignZ = 1f;
    private int facingSign = 1;

    private ContactFilter2D navigationFilter;

    private readonly RaycastHit2D[] castHits =
        new RaycastHit2D[8];

    private readonly RaycastHit2D[] avoidanceHits =
        new RaycastHit2D[12];

    private readonly RaycastHit2D[] routePlanningHits =
        new RaycastHit2D[16];

    private readonly Collider2D[] escapeHits =
        new Collider2D[16];

    private int committedRouteSide;
    private float routeCommitTimer;

    private bool absorptionStarted;
    private int pendingStalkerAbsorptions;
    private bool aoeUnlocked;
    private float aoeCooldownTimer;
    private bool isChargingAoe;
    private bool isAoeFadingOut;
    private float aoeChargeProgress;
    private Vector2 aoeChargeCenter;
    private Coroutine aoeRoutine;
    private Coroutine powerUpRoutine;
    private GameObject powerUpPreviewGhost;
    private GameObject dangerPreviewObject;

    public bool IsAoeUnlocked => aoeUnlocked;
    public bool IsChargingAoe => isChargingAoe;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        originalScale = transform.localScale;

        if (originalScale == Vector3.zero)
            originalScale = Vector3.one;

        facingSign = originalScale.x < 0f ? -1 : 1;
        scaleSignY = originalScale.y < 0f ? -1f : 1f;
        scaleSignZ = originalScale.z < 0f ? -1f : 1f;

        currentScaleMagnitude = new Vector3(
            Mathf.Max(0.0001f, Mathf.Abs(originalScale.x)),
            Mathf.Max(0.0001f, Mathf.Abs(originalScale.y)),
            Mathf.Max(0.0001f, Mathf.Abs(originalScale.z))
        );

        ApplyCurrentScaleMagnitude();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        CreateBossSfxSource();

        EnemyObstacleSteering2D.ConfigureAIMovementBody(rb, true);

        RefreshNavigationFilter();
    }

    private void Start()
    {
        FindPlayerIfNeeded();
        RefreshNavigationFilter();

        lastPosition = rb.position;

        unstuckDirection =
            Random.Range(0, 2) == 0 ? -1 : 1;

        committedRouteSide = unstuckDirection;
        routeCommitTimer = 0f;

        spawnRoutine = StartCoroutine(SpawnEffectRoutine());

    }

    private IEnumerator SpawnEffectRoutine()
    {
        isSpawning = true;

        Vector3 targetScale = new Vector3(
            currentScaleMagnitude.x * facingSign,
            currentScaleMagnitude.y * scaleSignY,
            currentScaleMagnitude.z * scaleSignZ
        );

        transform.localScale = Vector3.zero;

        float duration = Mathf.Max(0f, spawnEffectDuration);

        if (duration > 0f)
        {
            float timer = 0f;

            while (timer < duration)
            {
                if (stopped || isSplitting)
                {
                    spawnRoutine = null;
                    yield break;
                }

                timer += Time.deltaTime;

                float t = Mathf.Clamp01(
                    timer / duration
                );

                // EnemyFollow / ProjectileEnemyFollow ile ayni smooth-step giris.
                t = t * t * (3f - 2f * t);

                transform.localScale = Vector3.Lerp(
                    Vector3.zero,
                    targetScale,
                    t
                );

                yield return null;
            }
        }

        ApplyCurrentScaleMagnitude();

        isSpawning = false;
        spawnRoutine = null;

        ResetStuckCheck();

        if (!stopped && !isSplitting)
            BeginAbsorptionOfCurrentStalkers();
    }

    private void Update()
    {
        UpdateBossSfxState();

        if (stopped || isSplitting || isSpawning)
            return;

        FindPlayerIfNeeded();

        if (playerMovement != null &&
            playerMovement.IsGameOver)
        {
            // Strike sonrasi danger fade devam ederken Boss sabit kalir
            // ve preview smooth sekilde tamamen kaybolur.
            if (isAoeFadingOut)
                return;

            StopBoss();
            return;
        }

        if (!aoeEnabled ||
            !aoeUnlocked ||
            isChargingAoe)
        {
            return;
        }

        if (aoeCooldownTimer > 0f)
        {
            aoeCooldownTimer -= Time.deltaTime;
            return;
        }

        aoeRoutine = StartCoroutine(AoeStrikeRoutine());
    }

    private void FixedUpdate()
    {
        if (stopped || isSplitting || isSpawning)
            return;

        FindPlayerIfNeeded();

        if (rb == null || player == null)
            return;

        if (isAoeFadingOut)
        {
            // Fade bitene kadar Boss tam strike merkezinde kilitli kalir.
            RestoreAoeChargeCenter();
            return;
        }

        if (playerMovement != null &&
            playerMovement.IsGameOver)
        {
            StopBoss();
            return;
        }

        if (isChargingAoe)
        {
            ApplyAoeChargeShake();
            return;
        }

        MoveBoss();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (stopped || isSplitting || isSpawning)
            return;

        if (collision == null)
            return;

        GameObject playerObject =
            FindPlayerObjectInParents(collision.gameObject);

        if (playerObject == null)
            return;

        if (playerMovement == null)
            playerMovement = playerObject.GetComponent<PlayerMovement>();

        if (playerArmor == null)
            playerArmor = playerObject.GetComponent<PlayerArmor>();

        if (playerMovement == null ||
            playerMovement.IsGameOver)
        {
            return;
        }

        if (playerArmor != null && playerArmor.IsImmune)
            return;

        if (playerArmor != null && playerArmor.HasArmor)
        {
            playerArmor.BreakArmor();
            StatsManager.AddArmorEnemyKill();

            if (canSplit)
                StartCoroutine(SplitRoutine());
            else
                Destroy(gameObject);

            return;
        }

        StopBossMovement();
        playerMovement.GameOver("BOSS");
    }

    private GameObject FindPlayerObjectInParents(GameObject hitObject)
    {
        if (hitObject == null)
            return null;

        Transform current = hitObject.transform;

        while (current != null)
        {
            if (current.CompareTag("Player"))
                return current.gameObject;

            current = current.parent;
        }

        return null;
    }

    public void StopForGameEnd()
    {
        StopBoss();
    }

    private void StopBoss()
    {
        if (stopped)
            return;

        stopped = true;
        isSplitting = false;
        isSpawning = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (aoeRoutine != null)
        {
            StopCoroutine(aoeRoutine);
            aoeRoutine = null;
        }

        if (powerUpRoutine != null)
        {
            StopCoroutine(powerUpRoutine);
            powerUpRoutine = null;
        }

        StopBossSfx();
        DestroyPowerUpPreviewGhost();
        HideDangerPreview();

        if (isChargingAoe)
            RestoreAoeChargeCenter();

        isAoeFadingOut = false;
        isChargingAoe = false;
        GameAudioMixerController.SetBossDanger(this, false);
        aoeChargeProgress = 0f;

        StopBossMovement();

        if (bossCollider != null)
            bossCollider.enabled = false;

        enabled = false;
    }

    private void OnDisable()
    {
        if (!stopped)
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }

            if (aoeRoutine != null)
            {
                StopCoroutine(aoeRoutine);
                aoeRoutine = null;
            }

            if (powerUpRoutine != null)
            {
                StopCoroutine(powerUpRoutine);
                powerUpRoutine = null;
            }

            StopBossSfx();
            DestroyPowerUpPreviewGhost();
            HideDangerPreview();

            isSpawning = false;
            isAoeFadingOut = false;
            isChargingAoe = false;
            GameAudioMixerController.SetBossDanger(this, false);
            aoeChargeProgress = 0f;
        }
    }

    private void OnValidate()
    {
        speed = Mathf.Max(0f, speed);
        normalShakeAmount = Mathf.Max(0f, normalShakeAmount);
        spawnEffectDuration = Mathf.Max(0f, spawnEffectDuration);
        aoeCooldown = Mathf.Max(0f, aoeCooldown);
        firstAoeDelay = Mathf.Max(0f, firstAoeDelay);
        aoeChargeDuration = Mathf.Max(3f, aoeChargeDuration);
        aoeMaxShakeAmount = Mathf.Max(normalShakeAmount, aoeMaxShakeAmount);
        powerUpScaleMultiplier = Mathf.Max(1f, powerUpScaleMultiplier);
        powerUpPreviewFlashes = Mathf.Clamp(powerUpPreviewFlashes, 0, 4);
        powerUpPreviewOnDuration = Mathf.Max(0.01f, powerUpPreviewOnDuration);
        powerUpPreviewOffDuration = Mathf.Max(0.01f, powerUpPreviewOffDuration);
        powerUpGrowDuration = Mathf.Max(0.01f, powerUpGrowDuration);
        dangerStrikeWaveDuration =
            Mathf.Max(0.05f, dangerStrikeWaveDuration);
        dangerStrikeWaveWidth =
            Mathf.Clamp(dangerStrikeWaveWidth, 0.03f, 0.30f);
        dangerStrikeWaveBoost =
            Mathf.Clamp(dangerStrikeWaveBoost, 0f, 3f);
        dangerPreviewFadeOutDuration =
            Mathf.Max(0.05f, dangerPreviewFadeOutDuration);
        dangerPreviewInnerPadding = Mathf.Max(0f, dangerPreviewInnerPadding);
        dangerPreviewInnerAlphaMultiplier =
            Mathf.Clamp01(dangerPreviewInnerAlphaMultiplier);
        dangerPreviewWaveFrontWidth =
            Mathf.Clamp(dangerPreviewWaveFrontWidth, 0.03f, 0.3f);
        dangerPreviewWaveFrontBoost =
            Mathf.Max(0f, dangerPreviewWaveFrontBoost);
        dangerPreviewInnerBrightness =
            Mathf.Clamp(dangerPreviewInnerBrightness, 0.1f, 1f);
        dangerPreviewRadialSegments =
            Mathf.Clamp(dangerPreviewRadialSegments, 6, 24);
        dangerPreviewEdgeSmoothingPasses =
            Mathf.Clamp(dangerPreviewEdgeSmoothingPasses, 0, 4);
        dangerPreviewRayCount =
            Mathf.Clamp(dangerPreviewRayCount, 360, 960);
        dangerPreviewVisibilityRefreshRate =
            Mathf.Clamp(dangerPreviewVisibilityRefreshRate, 5f, 30f);
        dangerPreviewCoverFeather =
            Mathf.Clamp(dangerPreviewCoverFeather, 0.01f, 0.35f);
        routeLookAheadDistance =
            Mathf.Max(1f, routeLookAheadDistance);
        routeCommitDuration =
            Mathf.Max(0.1f, routeCommitDuration);
        routeAngleSamples =
            Mathf.Clamp(routeAngleSamples, 4, 9);
        arenaEdgePadding =
            Mathf.Max(0f, arenaEdgePadding);
        routeSideCommitment =
            Mathf.Clamp01(routeSideCommitment);
        slideDirectionAttempts = Mathf.Clamp(slideDirectionAttempts, 1, 8);
    }
}
