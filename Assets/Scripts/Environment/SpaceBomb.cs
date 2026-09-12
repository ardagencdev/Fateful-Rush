using System.Collections;
using UnityEngine;

public class SpaceBomb : MonoBehaviour
{
    [Header("Explosion")]
    public GameObject explosionEffectPrefab;
    public AudioClip explosionSound;

    [Header("Spawn Safety")]
    public float spawnSafeTime = 0.35f;

    [Header("Visibility Assist")]
    [Tooltip("Keeps the existing red bomb pulse and adds a fading expanding electromagnetic warning wave.")]
    public bool visibilityAssistEnabled = true;

    [Tooltip("Pale cyan/white so the warning stays readable even on red chaos backgrounds.")]
    public Color electromagneticColor = new Color(0.72f, 0.94f, 1f, 1f);

    [Tooltip("Bir pulse'un başlangıcından sonraki pulse'un başlangıcına kadar geçen süre.")]
    [Min(0.4f)]
    public float waveInterval = 1.45f;

    [Tooltip("Wave'in görünür şekilde büyüyüp kaybolma süresi. Interval'dan kısa tutulursa arada sakin bir boşluk oluşur.")]
    [Min(0.1f)]
    public float waveDuration = 0.58f;

    [Tooltip("Wave bombanın gerçek görsel kenarından bu kadar dışarıda başlar.")]
    [Min(0f)]
    public float waveStartPadding = 0.035f;

    [Tooltip("Bombanın görsel yarıçapına eklenecek dış büyüme mesafesi.")]
    [Min(0.1f)]
    public float waveTravelDistance = 1.75f;

    [Range(0f, 1f)]
    public float waveMaxAlpha = 0.34f;

    [Min(0.005f)]
    public float waveWidth = 0.07f;

    private bool triggered;
    private Collider2D bombCollider;
    private SpriteRenderer bombRenderer;

    private LineRenderer waveRenderer;
    private Material warningMaterial;
    private float visibilityTimer;
    private float runtimeWaveStartRadius;
    private float runtimeWaveEndRadius;

    private const int RingSegments = 48;

    private void Awake()
    {
        bombCollider = GetComponent<Collider2D>();
        bombRenderer = GetComponent<SpriteRenderer>();

        SetColliderEnabled(false);
        SetupVisibilityAssist();
    }

    private IEnumerator Start()
    {
        float safeTime = Mathf.Max(0f, spawnSafeTime);

        if (safeTime > 0f)
            yield return new WaitForSeconds(safeTime);

        if (!triggered)
            SetColliderEnabled(true);
    }

    private void Update()
    {
        if (!visibilityAssistEnabled ||
            triggered ||
            waveRenderer == null)
        {
            return;
        }

        UpdateVisibilityAssist();
    }

    private void SetupVisibilityAssist()
    {
        if (!visibilityAssistEnabled)
            return;

        Material sourceMaterial = bombRenderer != null
            ? bombRenderer.sharedMaterial
            : null;

        if (sourceMaterial != null)
        {
            warningMaterial = new Material(sourceMaterial);
        }
        else
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
                return;

            warningMaterial = new Material(shader);
        }

        warningMaterial.name = "SpaceBomb_RuntimeWarningMaterial";
        warningMaterial.hideFlags = HideFlags.HideAndDontSave;

        int sortingLayerId = bombRenderer != null
            ? bombRenderer.sortingLayerID
            : 0;

        int baseOrder = bombRenderer != null
            ? bombRenderer.sortingOrder
            : 0;

        waveRenderer = CreateRingRenderer(
            "ElectromagneticWave",
            sortingLayerId,
            baseOrder + 4,
            waveWidth
        );

        runtimeWaveStartRadius =
            GetBombVisualRadiusInLocalSpace() +
            Mathf.Max(0f, waveStartPadding);

        runtimeWaveEndRadius =
            runtimeWaveStartRadius +
            Mathf.Max(0.1f, waveTravelDistance);

        DrawRing(
            waveRenderer,
            runtimeWaveStartRadius
        );

        // İlk karede parlak bir halka patlamasın.
        // İlk pulse UpdateVisibilityAssist tarafından kontrollü şekilde başlatılır.
        Color wave = electromagneticColor;
        wave.a = 0f;
        SetLineColor(waveRenderer, wave);
    }

    private LineRenderer CreateRingRenderer(
        string objectName,
        int sortingLayerId,
        int sortingOrder,
        float width)
    {
        GameObject ringObject = new GameObject(objectName);
        ringObject.transform.SetParent(transform, false);
        ringObject.layer = gameObject.layer;

        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = RingSegments;
        line.widthMultiplier = width;
        line.numCornerVertices = 2;
        line.numCapVertices = 0;
        line.alignment = LineAlignment.TransformZ;
        line.textureMode = LineTextureMode.Stretch;
        line.sharedMaterial = warningMaterial;
        line.sortingLayerID = sortingLayerId;
        line.sortingOrder = sortingOrder;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        return line;
    }

    private void UpdateVisibilityAssist()
    {
        float interval =
            Mathf.Max(0.4f, waveInterval);

        float duration =
            Mathf.Clamp(
                waveDuration,
                0.1f,
                interval
            );

        visibilityTimer += Time.deltaTime;

        float cycleTime =
            Mathf.Repeat(
                visibilityTimer,
                interval
            );

        // Pulse bittikten sonra kısa bir tamamen görünmez boşluk bırak.
        // Böylece efekt sürekli yanıp sönüyormuş gibi dikkat dağıtmaz.
        if (cycleTime >= duration)
        {
            Color hiddenColor = electromagneticColor;
            hiddenColor.a = 0f;
            SetLineColor(
                waveRenderer,
                hiddenColor
            );
            return;
        }

        float phase =
            Mathf.Clamp01(
                cycleTime / duration
            );

        float eased =
            1f -
            Mathf.Pow(
                1f - phase,
                2.2f
            );

        float radius =
            Mathf.Lerp(
                runtimeWaveStartRadius,
                runtimeWaveEndRadius,
                eased
            );

        DrawRing(
            waveRenderer,
            radius
        );

        Color waveColor =
            electromagneticColor;

        // İlk anda hızlıca görünür olur, büyürken sakin şekilde fade olur.
        float fade =
            Mathf.Pow(
                1f - phase,
                1.45f
            );

        float fadeIn =
            Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(
                    phase / 0.10f
                )
            );

        waveColor.a =
            waveMaxAlpha *
            fade *
            fadeIn;

        SetLineColor(
            waveRenderer,
            waveColor
        );
    }

    private float GetBombVisualRadiusInLocalSpace()
    {
        if (bombRenderer == null)
            return 0.55f;

        Bounds worldBounds =
            bombRenderer.bounds;

        Vector3 center =
            worldBounds.center;

        Vector3 rightPoint =
            center +
            Vector3.right *
            worldBounds.extents.x;

        Vector3 upPoint =
            center +
            Vector3.up *
            worldBounds.extents.y;

        Vector3 localCenter =
            transform.InverseTransformPoint(
                center
            );

        Vector3 localRight =
            transform.InverseTransformPoint(
                rightPoint
            );

        Vector3 localUp =
            transform.InverseTransformPoint(
                upPoint
            );

        float horizontalRadius =
            Vector2.Distance(
                localCenter,
                localRight
            );

        float verticalRadius =
            Vector2.Distance(
                localCenter,
                localUp
            );

        float radius =
            Mathf.Max(
                horizontalRadius,
                verticalRadius
            );

        return radius > 0.01f
            ? radius
            : 0.55f;
    }

    private static void DrawRing(LineRenderer line, float radius)
    {
        if (line == null)
            return;

        float safeRadius = Mathf.Max(0.01f, radius);

        for (int i = 0; i < RingSegments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / RingSegments;
            line.SetPosition(
                i,
                new Vector3(
                    Mathf.Cos(angle) * safeRadius,
                    Mathf.Sin(angle) * safeRadius,
                    0f
                )
            );
        }
    }

    private static void SetLineColor(LineRenderer line, Color color)
    {
        if (line == null)
            return;

        line.startColor = color;
        line.endColor = color;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTrigger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTrigger(other);
    }

    private void TryTrigger(Collider2D other)
    {
        if (triggered ||
            !GameStateManager.IsGameplayStarted ||
            GameStateManager.IsGameplayEnded)
        {
            return;
        }

        if (other == null || !other.CompareTag("Player"))
            return;

        triggered = true;
        SetColliderEnabled(false);

        try
        {
            StatsManager.AddSpaceBombTrigger();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
        }

        PlayerArmor armor =
            other.GetComponentInParent<PlayerArmor>();

        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        bool isImmune =
            armor != null && armor.IsImmune;

        bool willBreakArmor =
            !isImmune &&
            armor != null &&
            armor.HasArmor;

        bool lethalHit =
            !isImmune &&
            !willBreakArmor;

        try
        {
            Explode(lethalHit);
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "[SpaceBomb] Explosion feedback failed. Hit resolution continues.",
                this
            );
            Debug.LogException(exception, this);
        }

        if (isImmune)
            return;

        if (willBreakArmor)
        {
            armor.BreakArmor();
            return;
        }

        if (player != null)
        {
            player.GameOver("SPACE BOMB");
            return;
        }

        GameStateManager gameStateManager =
            FindAnyObjectByType<GameStateManager>(
                FindObjectsInactive.Include
            );

        if (gameStateManager != null)
            gameStateManager.GameOver(0, "SPACE BOMB");
    }

    private void Explode(bool persistThroughGameEnd)
    {
        CameraShake.Instance?.Shake(
            0.14f,
            0.10f
        );

        VibrationManager.Instance?.VibrateSpaceBomb();

        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                explosionEffectPrefab,
                transform.position,
                Quaternion.identity
            );

            if (persistThroughGameEnd)
                ConfigurePersistentExplosionVisual(effect);
        }

        if (explosionSound != null)
        {
            if (persistThroughGameEnd)
            {
                PlayPersistentExplosionSound();
            }
            else if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayCriticalSoundAtWorld(
                    explosionSound,
                    transform.position
                );
            }
            else
            {
                PlayMixerRoutedExplosionFallback();
            }
        }

        Destroy(gameObject);
    }

    private void ConfigurePersistentExplosionVisual(GameObject effect)
    {
        if (effect == null)
            return;

        ParticleSystem[] particleSystems =
            effect.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem system = particleSystems[i];

            if (system == null)
                continue;

            ParticleSystem.MainModule main = system.main;
            main.useUnscaledTime = true;
        }

        Animator[] animators =
            effect.GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
                animators[i].updateMode = AnimatorUpdateMode.UnscaledTime;
        }
    }

    private void PlayPersistentExplosionSound()
    {
        if (explosionSound == null)
            return;

        GameObject audioObject =
            new GameObject("SpaceBomb_LethalExplosionAudio");

        audioObject.transform.position = transform.position;

        GameEndPersistentAudio persistence =
            audioObject.AddComponent<GameEndPersistentAudio>();

        AudioSource source =
            audioObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.clip = explosionSound;
        source.volume = SoundManager.SFXVolume;
        source.pitch = 1f;
        source.ignoreListenerPause = false;

        SoundManager.ConfigureAsWorld3D(source);
        GameAudioMixerController.Route(
            source,
            GameAudioMixerController.AudioBus.CriticalSFX
        );

        source.Play();
        persistence.DestroyAfterRealtime(explosionSound.length + 0.25f);
    }

    private void PlayMixerRoutedExplosionFallback()
    {
        if (explosionSound == null)
            return;

        GameObject audioObject =
            new GameObject("SpaceBomb_ExplosionAudio");

        audioObject.transform.position = transform.position;

        AudioSource source =
            audioObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.clip = explosionSound;
        source.volume = SoundManager.SFXVolume;
        source.pitch = 1f;

        SoundManager.ConfigureAsWorld3D(source);
        GameAudioMixerController.Route(
            source,
            GameAudioMixerController.AudioBus.CriticalSFX
        );

        source.Play();

        Destroy(
            audioObject,
            explosionSound.length + 0.15f
        );
    }

    private void SetColliderEnabled(bool enabledState)
    {
        if (bombCollider != null)
            bombCollider.enabled = enabledState;
    }

    private void OnDestroy()
    {
        if (warningMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(warningMaterial);
            else
                DestroyImmediate(warningMaterial);
        }
    }

    private void OnValidate()
    {
        spawnSafeTime = Mathf.Max(0f, spawnSafeTime);
        waveInterval = Mathf.Max(0.4f, waveInterval);
        waveDuration = Mathf.Clamp(waveDuration, 0.1f, waveInterval);
        waveStartPadding = Mathf.Max(0f, waveStartPadding);
        waveTravelDistance = Mathf.Max(0.1f, waveTravelDistance);
        waveWidth = Mathf.Max(0.005f, waveWidth);
    }
}
