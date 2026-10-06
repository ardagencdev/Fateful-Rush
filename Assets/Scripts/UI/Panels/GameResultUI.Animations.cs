using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Same Unity component. Inspector data and lifecycle entry points remain in GameResultUI.cs.
public partial class GameResultUI
{
    private enum ExitKind { Enemy, Obstacle, Bomb, Coin, Player, Pickup }
    private sealed class WorldExitItem
    {
        public Transform root;
        public ExitKind kind;
        public Vector3 position, scale;
        public float delay;
        public bool collectCoin;
        public readonly System.Collections.Generic.List<WorldExitRenderer> renderers =
            new System.Collections.Generic.List<WorldExitRenderer>();
        public readonly System.Collections.Generic.List<Behaviour> suspended =
            new System.Collections.Generic.List<Behaviour>();
    }
    private sealed class WorldExitRenderer
    {
        public Renderer renderer;
        public bool enabled;
        public Color color;
        public int colorId;
        public MaterialPropertyBlock originalBlock, block;
        public MeshFilter filter;
        public Mesh originalMesh, fadeMesh;
        public Color[] originalColors, fadeColors;
        public ParticleSystem particles;
        public ParticleSystem.Particle[] particleBuffer;
        public Color32[] particleColors;
        public int particleCount;
        public Color lineStart, lineEnd;
    }
    private readonly System.Collections.Generic.List<WorldExitItem> worldExitItems =
        new System.Collections.Generic.List<WorldExitItem>();
    private readonly System.Collections.Generic.List<SpriteRenderer> exitTrail =
        new System.Collections.Generic.List<SpriteRenderer>();
    private Coroutine worldExitRoutine;

    private static Transform FindExitRoot(Transform child, out ExitKind kind)
    {
        kind = ExitKind.Obstacle;
        for (Transform t = child; t != null; t = t.parent)
        {
            if (t.GetComponent<PlayerMovement>() != null) { kind = ExitKind.Player; return t; }
            if (t.GetComponent<Coin>() != null) { kind = ExitKind.Coin; return t; }
            if (t.GetComponent<SpaceBomb>() != null) { kind = ExitKind.Bomb; return t; }
            if (t.GetComponent<EnemyFollow>() != null || t.GetComponent<HunterEnemyFollow>() != null ||
                t.GetComponent<ProjectileEnemyFollow>() != null || t.GetComponent<BossEnemyFollow>() != null ||
                t.GetComponent<MiniBossFollow>() != null || t.GetComponent<BeaconEnemy>() != null ||
                t.GetComponent<EnemyProjectile>() != null || t.GetComponent<VoidClone>() != null ||
                t.GetComponent<BeaconPulseWave>() != null ||
                t.CompareTag("Enemy") || t.CompareTag("BeaconEnemy"))
            { kind = ExitKind.Enemy; return t; }
            if (t.GetComponent<ArmorPowerUp>() != null || t.GetComponent<SlowPowerUp>() != null)
            { kind = ExitKind.Pickup; return t; }
            if (t.CompareTag("Wall") || t.GetComponent<LaserWall>() != null)
            { kind = ExitKind.Obstacle; return t; }
        }
        return null;
    }

    private void StartWorldExit(bool won)
    {
        RestoreWorldExit();
        var roots = new System.Collections.Generic.Dictionary<Transform, WorldExitItem>();
        Camera camera = Camera.main;
        Renderer[] renderers = UnityFindCompat.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        bool expired = !won && string.Equals(displayedDeathCause, "TIME EXPIRED",
            System.StringComparison.OrdinalIgnoreCase);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || renderer.gameObject.scene != gameObject.scene) continue;
            Transform root = FindExitRoot(renderer.transform, out ExitKind kind);
            if (root == null || (kind == ExitKind.Player && !won && !expired)) continue;
            if (!roots.TryGetValue(root, out WorldExitItem item))
            {
                item = new WorldExitItem { root = root, kind = kind };
                // Prevent decorative LateUpdate scripts from overwriting the fade/pose.
                foreach (MonoBehaviour visual in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (visual == null || !visual.enabled) continue;
                    if (visual is ObstacleIdleAnimation || visual is ObstacleReadabilityAccent ||
                        visual is SpaceFloatVisual || visual is MovementVisualEffect ||
                        visual is EnemyDangerPreviewRuntime || visual is ShieldRotate)
                    { item.suspended.Add(visual); visual.enabled = false; }
                }
                item.position = root.position;
                item.scale = root.localScale;
                item.delay = (worldExitItems.Count % 5) * 0.025f;
                Vector3 viewport = camera != null ? camera.WorldToViewportPoint(root.position) : new Vector3(.5f,.5f,1f);
                Coin coin = kind == ExitKind.Coin ? root.GetComponent<Coin>() : null;
                item.collectCoin = won && coin != null && !coin.IsCollected && viewport.z > 0f &&
                    viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
                roots.Add(root, item);
                worldExitItems.Add(item);
            }
            WorldExitRenderer state = new WorldExitRenderer { renderer = renderer, enabled = renderer.enabled };
            if (renderer is SpriteRenderer sprite) state.color = sprite.color;
            else if (renderer is LineRenderer line)
            { state.lineStart = line.startColor; state.lineEnd = line.endColor; }
            else if (renderer is ParticleSystemRenderer)
            {
                state.particles = renderer.GetComponent<ParticleSystem>();
                if (state.particles != null)
                {
                    state.particleBuffer = new ParticleSystem.Particle[state.particles.main.maxParticles];
                    state.particleCount = state.particles.GetParticles(state.particleBuffer);
                    state.particleColors = new Color32[state.particleCount];
                    for (int i = 0; i < state.particleCount; i++) state.particleColors[i] = state.particleBuffer[i].startColor;
                }
            }
            else
            {
                Material material = renderer.sharedMaterial;
                foreach (string property in new[] { "_BaseColor", "_Color", "_TintColor" })
                    if (material != null && material.HasProperty(property))
                    { state.colorId = Shader.PropertyToID(property); state.color = material.GetColor(state.colorId); break; }
                if (state.colorId != 0)
                {
                    state.originalBlock = new MaterialPropertyBlock(); renderer.GetPropertyBlock(state.originalBlock);
                    state.block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(state.block);
                    if (state.block.HasColor(state.colorId)) state.color = state.block.GetColor(state.colorId);
                }
                else
                {
                    state.filter = renderer.GetComponent<MeshFilter>();
                    if (state.filter != null && state.filter.sharedMesh != null && state.filter.sharedMesh.isReadable)
                    {
                        state.originalMesh = state.filter.sharedMesh;
                        state.fadeMesh = Instantiate(state.originalMesh);
                        state.originalColors = state.originalMesh.colors;
                        if (state.originalColors.Length != state.originalMesh.vertexCount)
                        {
                            state.originalColors = new Color[state.originalMesh.vertexCount];
                            for (int i = 0; i < state.originalColors.Length; i++) state.originalColors[i] = Color.white;
                        }
                        state.fadeColors = new Color[state.originalColors.Length];
                        state.filter.sharedMesh = state.fadeMesh;
                    }
                }
            }
            item.renderers.Add(state);
        }
        worldExitRoutine = StartCoroutine(PlayWorldExit(won));
    }

    private void ApplyWorldExitOpacity(WorldExitItem item, float alpha, float light)
    {
        foreach (WorldExitRenderer state in item.renderers)
        {
            if (state.renderer == null) continue;
            if (state.renderer is SpriteRenderer sprite)
            {
                Color color = Color.Lerp(state.color, light >= 0f ? Color.white : Color.black, Mathf.Abs(light));
                color.a = state.color.a * alpha;
                sprite.color = color;
            }
            else if (state.renderer is LineRenderer line)
            {
                Color start = state.lineStart, end = state.lineEnd;
                start.a *= alpha; end.a *= alpha; line.startColor = start; line.endColor = end;
            }
            else if (state.particles != null)
            {
                for (int i = 0; i < state.particleCount; i++)
                {
                    Color32 color = state.particleColors[i]; color.a = (byte)(color.a * alpha);
                    state.particleBuffer[i].startColor = color;
                }
                state.particles.SetParticles(state.particleBuffer, state.particleCount);
            }
            else if (state.colorId != 0)
            {
                Color color = state.color; color.a *= alpha;
                state.block.SetColor(state.colorId, color); state.renderer.SetPropertyBlock(state.block);
            }
            else if (state.fadeMesh != null)
            {
                for (int i = 0; i < state.fadeColors.Length; i++)
                { Color color = state.originalColors[i]; color.a *= alpha; state.fadeColors[i] = color; }
                state.fadeMesh.colors = state.fadeColors;
            }
            if (alpha <= 0f) state.renderer.enabled = false;
        }
    }

    private IEnumerator PlayWorldExit(bool won)
    {
        WorldExitItem player = worldExitItems.Find(item => item.kind == ExitKind.Player);
        bool sweepCoins = won && player != null && worldExitItems.Exists(item => item.collectCoin);
        Vector3 destination = player != null ? player.position : Vector3.zero;
        float duration = Mathf.Max(0.1f, worldExitDuration);
        float elapsed = 0f;
        int soundStep = 0;
        string skinId = PlayerPrefs.GetString(PlayerSkinCatalog.SelectedSkinKey, "white");
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (sweepCoins && soundStep < 3 && t >= 0.12f + soundStep * 0.20f)
            { SoundManager.Instance?.PlayResultCoinSweep(skinId, soundStep); soundStep++; }
            foreach (WorldExitItem item in worldExitItems)
            {
                if (item.root == null) continue;
                float p = Mathf.Clamp01((t - item.delay) / 0.65f);
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, p);
                float light = 0f;
                if (item.collectCoin && player != null)
                {
                    float flight = Mathf.Clamp01((t - item.delay) / 0.43f);
                    float eased = Mathf.SmoothStep(0f, 1f, flight);
                    Vector3 midpoint = (item.position + destination) * 0.5f + Vector3.up * 0.35f;
                    float inverse = 1f - eased;
                    item.root.position = inverse * inverse * item.position +
                        2f * inverse * eased * midpoint + eased * eased * destination;
                    item.root.localScale = item.scale * Mathf.Lerp(1f, 0.15f, eased);
                    alpha = 1f - Mathf.SmoothStep(0.7f, 1f, flight);
                    light = 0.15f * Mathf.Sin(flight * Mathf.PI);
                }
                else if (item.kind == ExitKind.Player)
                {
                    float departure = Mathf.Clamp01((t - (sweepCoins ? 0.56f : 0.16f)) /
                        (sweepCoins ? 0.44f : 0.84f));
                    float eased = Mathf.SmoothStep(0f, 1f, departure);
                    alpha = 1f - eased;
                    item.root.localScale = item.scale * Mathf.Lerp(1f, won ? 0.72f : 0.9f, eased);
                    if (won)
                    {
                        item.root.position = item.position + Vector3.up * (1.7f * eased);
                        light = 0.22f * Mathf.Sin(Mathf.Clamp01(t / 0.7f) * Mathf.PI);
                        if (departure > 0.1f && exitTrail.Count < 3 && departure >= 0.15f + exitTrail.Count * 0.2f)
                            CreateExitTrail(item);
                    }
                }
                else if (item.kind == ExitKind.Enemy)
                    item.root.localScale = item.scale * Mathf.Lerp(1f, 0.82f, Mathf.SmoothStep(0f, 1f, p));
                else if (item.kind == ExitKind.Coin || item.kind == ExitKind.Pickup)
                { item.root.localScale = item.scale * Mathf.Lerp(1f, 0.6f, p); light = 0.12f * Mathf.Sin(p * Mathf.PI); }
                else if (item.kind == ExitKind.Bomb)
                {
                    alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.14f) / 0.55f));
                    light = -0.35f * Mathf.SmoothStep(0f, 0.2f, t);
                }
                ApplyWorldExitOpacity(item, alpha, light);
            }
            foreach (SpriteRenderer ghost in exitTrail)
            {
                if (ghost == null) continue;
                Color color = ghost.color; color.a = Mathf.MoveTowards(color.a, 0f, Time.unscaledDeltaTime * 0.65f);
                ghost.color = color;
            }
            yield return null;
        }
        foreach (WorldExitItem item in worldExitItems) ApplyWorldExitOpacity(item, 0f, 0f);
        foreach (SpriteRenderer ghost in exitTrail) if (ghost != null) Destroy(ghost.gameObject);
        exitTrail.Clear();
        worldExitRoutine = null;
    }

    private void CreateExitTrail(WorldExitItem player)
    {
        SpriteRenderer source = player.root != null ? player.root.GetComponent<SpriteRenderer>() : null;
        foreach (WorldExitRenderer state in player.renderers)
            if (state.renderer is SpriteRenderer sprite && sprite.sprite != null)
            {
                if (source == null || sprite.transform == player.root || sprite.gameObject.name == "Visual") source = sprite;
                if (sprite.transform == player.root || sprite.gameObject.name == "Visual") break;
            }
        if (source == null) return;
        GameObject trail = new GameObject("ResultDepartureTrail");
        trail.layer = source.gameObject.layer;
        trail.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        trail.transform.localScale = source.transform.lossyScale;
        SpriteRenderer ghost = trail.AddComponent<SpriteRenderer>();
        ghost.sprite = source.sprite; ghost.sharedMaterial = source.sharedMaterial;
        ghost.sortingLayerID = source.sortingLayerID; ghost.sortingOrder = source.sortingOrder - 1;
        ghost.color = new Color(source.color.r, source.color.g, source.color.b, 0.16f);
        exitTrail.Add(ghost);
    }

    private void RestoreWorldExit()
    {
        if (worldExitRoutine != null) { StopCoroutine(worldExitRoutine); worldExitRoutine = null; }
        foreach (SpriteRenderer ghost in exitTrail) if (ghost != null) Destroy(ghost.gameObject);
        exitTrail.Clear();
        foreach (WorldExitItem item in worldExitItems)
        {
            if (item.root != null) { item.root.position = item.position; item.root.localScale = item.scale; }
            foreach (WorldExitRenderer state in item.renderers)
            {
                if (state.renderer != null)
                {
                    state.renderer.enabled = state.enabled;
                    if (state.renderer is SpriteRenderer sprite) sprite.color = state.color;
                    else if (state.renderer is LineRenderer line) { line.startColor = state.lineStart; line.endColor = state.lineEnd; }
                    else if (state.originalBlock != null) state.renderer.SetPropertyBlock(state.originalBlock);
                }
                if (state.filter != null && state.fadeMesh != null && state.filter.sharedMesh == state.fadeMesh)
                    state.filter.sharedMesh = state.originalMesh;
                if (state.fadeMesh != null) Destroy(state.fadeMesh);
                if (state.particles != null)
                {
                    for (int i = 0; i < state.particleCount; i++) state.particleBuffer[i].startColor = state.particleColors[i];
                    state.particles.SetParticles(state.particleBuffer, state.particleCount);
                }
            }
            foreach (Behaviour visual in item.suspended) if (visual != null) visual.enabled = true;
        }
        worldExitItems.Clear();
    }

    private sealed class CinematicContentState
    {
        public CanvasGroup group;
        public float alpha;
        public float delay;
    }

    private readonly System.Collections.Generic.List<CinematicContentState> cinematicContentStates =
        new System.Collections.Generic.List<CinematicContentState>();

    private void RestoreCinematicContent()
    {
        foreach (CinematicContentState state in cinematicContentStates)
            if (state.group != null) state.group.alpha = state.alpha;
        cinematicContentStates.Clear();
    }

    private void PrepareCinematicContent(GameObject content)
    {
        RestoreCinematicContent();
        if (!cinematicResultIntro || content == null) return;
        foreach (Transform child in content.transform)
        {
            // Buttons already have their own delayed slide/fade routine.
            if (child.GetComponent<Button>() != null) continue;
            string itemName = child.name;
            bool heading = itemName == "WinTitleText" || itemName == "LoseTitleText" ||
                itemName == "MissionReportText" || itemName == "Mission Number Text" ||
                itemName == "MissionNumberText";
            // Reward groups own their internal fades; do not fight those routines.
            if (itemName == "NewBestTimeUI" || itemName == "SkinUnlockUI") continue;
            CanvasGroup group = child.GetComponent<CanvasGroup>();
            if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();
            cinematicContentStates.Add(new CinematicContentState
            {
                group = group,
                alpha = group.alpha,
                delay = heading ? 0f : 0.30f
            });
            group.alpha = 0f;
        }
    }

    private void PrepareResultIntroUI()
    {
        if (resultPanel == null)
            return;

        if (resultPanelCanvasGroup == null)
        {
            resultPanelCanvasGroup =
                resultPanel.GetComponent<CanvasGroup>();

            if (resultPanelCanvasGroup == null)
            {
                resultPanelCanvasGroup =
                    resultPanel.AddComponent<CanvasGroup>();
            }
        }

        CacheResultScales();
    }

    private void CacheResultScales()
    {
        if (resultScalesCached)
            return;

        if (winUI != null)
        {
            winUIRestScale =
                winUI.transform.localScale;

            if (winUIRestScale == Vector3.zero)
                winUIRestScale = Vector3.one;
        }

        if (loseUI != null)
        {
            loseUIRestScale =
                loseUI.transform.localScale;

            if (loseUIRestScale == Vector3.zero)
                loseUIRestScale = Vector3.one;
        }

        resultScalesCached = true;
    }

    private void StartResultIntro(bool won)
    {
        StopResultIntro();
        PrepareResultIntroUI();

        resultIntroRoutine =
            StartCoroutine(
                ResultIntroRoutine(won)
            );
    }

    private IEnumerator ResultIntroRoutine(bool won)
    {
        if (resultPanel == null)
        {
            resultIntroRoutine = null;
            yield break;
        }

        PrepareResultIntroUI();

        GameObject content =
            won ? winUI : loseUI;

        Vector3 restScale =
            won
                ? winUIRestScale
                : loseUIRestScale;

        float duration = cinematicResultIntro ? Mathf.Max(0.1f, cinematicResultDuration) :
            Mathf.Max(
                0.05f,
                resultIntroDuration
            );

        float startScaleFactor = cinematicResultIntro ? 0.985f :
            Mathf.Clamp(
                resultIntroStartScale,
                0.85f,
                1f
            );

        Vector3 startScale =
            restScale * startScaleFactor;

        if (resultPanelCanvasGroup != null)
        {
            resultPanelCanvasGroup.alpha = 0f;
            resultPanelCanvasGroup.interactable = false;
            resultPanelCanvasGroup.blocksRaycasts = false;
        }

        if (content != null)
        {
            content.transform.localScale =
                startScale;
        }

        PrepareCinematicContent(content);
        if (cinematicWorldExit) StartWorldExit(won);
        // Consume taps while the report is appearing, without enabling buttons.
        if (resultPanelCanvasGroup != null) resultPanelCanvasGroup.blocksRaycasts = true;
        yield return null; // Settle TMP/layout before showing the report.
        if (ResultRevealHold > 0f)
            yield return new WaitForSecondsRealtime(ResultRevealHold);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float eased =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            foreach (CinematicContentState state in cinematicContentStates)
            {
                if (state.group == null) continue;
                float local = Mathf.Clamp01((progress - state.delay) / (1f - state.delay));
                state.group.alpha = state.alpha * Mathf.SmoothStep(0f, 1f, local);
            }

            if (resultPanelCanvasGroup != null)
            {
                resultPanelCanvasGroup.alpha =
                    eased;
            }

            if (content != null)
            {
                content.transform.localScale =
                    Vector3.LerpUnclamped(
                        startScale,
                        restScale,
                        eased
                    );
            }

            yield return null;
        }

        if (resultPanelCanvasGroup != null)
        {
            resultPanelCanvasGroup.alpha = 1f;
            resultPanelCanvasGroup.interactable = true;
            resultPanelCanvasGroup.blocksRaycasts = true;
        }

        if (content != null)
        {
            content.transform.localScale =
                restScale;
        }

        RestoreCinematicContent();
        resultIntroRoutine = null;
    }

    private void StopResultIntro()
    {
        RestoreWorldExit();
        if (resultIntroRoutine == null)
            return;

        StopCoroutine(resultIntroRoutine);
        resultIntroRoutine = null;
        RestoreCinematicContent();
    }

    private void StartResultButtonsIntro(bool won)
    {
        StopResultButtonsIntro();

        // The report intro yields a frame for the normal canvas/layout pass.

        ResultButtonIntroState[] states = won
            ? new[]
            {
                PrepareResultButtonIntroState(
                    nextLevelButton,
                    nextLevelButtonIntroState
                ),
                PrepareResultButtonIntroState(
                    tryAgainButton,
                    tryAgainButtonIntroState
                ),
                PrepareResultButtonIntroState(
                    menuButton,
                    menuButtonIntroState
                )
            }
            : new[]
            {
                PrepareResultButtonIntroState(
                    tryAgainButton,
                    tryAgainButtonIntroState
                ),
                PrepareResultButtonIntroState(
                    menuButton,
                    menuButtonIntroState
                )
            };

        // Görünür olmayan butonları (ör. son level'daki Next Level)
        // animasyon sırasına hiç alma.
        int activeCount = 0;

        for (int i = 0; i < states.Length; i++)
        {
            ResultButtonIntroState state = states[i];

            if (state == null ||
                state.gameObject == null ||
                !state.gameObject.activeInHierarchy)
            {
                continue;
            }

            states[activeCount] = state;
            activeCount++;
        }

        if (activeCount == 0)
            return;

        ResultButtonIntroState[] activeStates =
            new ResultButtonIntroState[activeCount];

        for (int i = 0; i < activeCount; i++)
        {
            activeStates[i] = states[i];
            ApplyResultButtonHiddenState(activeStates[i]);
        }

        resultButtonsIntroRoutine =
            StartCoroutine(
                ResultButtonsIntroRoutine(activeStates)
            );
    }

    private ResultButtonIntroState PrepareResultButtonIntroState(
        GameObject buttonObject,
        ResultButtonIntroState state)
    {
        if (state == null)
            return null;

        if (buttonObject == null)
        {
            state.gameObject = null;
            state.rect = null;
            state.canvasGroup = null;
            state.button = null;
            state.cached = false;
            return state;
        }

        if (state.gameObject != buttonObject)
        {
            state.gameObject = buttonObject;
            state.rect =
                buttonObject.GetComponent<RectTransform>();

            state.canvasGroup =
                buttonObject.GetComponent<CanvasGroup>();

            if (state.canvasGroup == null)
            {
                state.canvasGroup =
                    buttonObject.AddComponent<CanvasGroup>();
            }

            state.button =
                buttonObject.GetComponent<Button>();

            if (state.button == null)
            {
                state.button =
                    buttonObject.GetComponentInChildren<Button>(true);
            }

            state.cached = false;
        }

        if (!state.cached && state.rect != null)
        {
            state.restPosition =
                state.rect.anchoredPosition;

            state.restScale =
                state.rect.localScale;

            if (state.restScale == Vector3.zero)
                state.restScale = Vector3.one;

            state.cached = true;
        }

        return state;
    }

    private void ApplyResultButtonHiddenState(
        ResultButtonIntroState state)
    {
        if (state == null ||
            state.gameObject == null ||
            !state.gameObject.activeInHierarchy)
        {
            return;
        }

        if (state.rect != null && state.cached)
        {
            state.rect.anchoredPosition =
                state.restPosition +
                Vector2.down *
                Mathf.Max(0f, resultButtonSlideDistance);

            state.rect.localScale =
                state.restScale *
                Mathf.Clamp(
                    resultButtonStartScale,
                    0.85f,
                    1f
                );
        }

        if (state.canvasGroup != null)
        {
            state.canvasGroup.alpha = 0f;
            state.canvasGroup.interactable = false;
            state.canvasGroup.blocksRaycasts = false;
        }

        if (state.button != null)
            state.button.interactable = false;
    }

    private IEnumerator ResultButtonsIntroRoutine(
        ResultButtonIntroState[] states)
    {
        if (states == null || states.Length == 0)
        {
            resultButtonsIntroRoutine = null;
            yield break;
        }

        float baseDelay =
            (cinematicResultIntro
                ? ResultRevealHold + Mathf.Max(0.1f, cinematicResultDuration)
                : Mathf.Max(0.05f, resultIntroDuration)) +
            Mathf.Max(0f, resultButtonStartDelay);

        if (baseDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                baseDelay
            );
        }

        float duration =
            Mathf.Max(
                0.05f,
                resultButtonAnimationDuration
            );

        float stagger =
            Mathf.Max(
                0f,
                resultButtonStagger
            );

        float totalDuration =
            duration +
            stagger *
            Mathf.Max(0, states.Length - 1);

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            for (int i = 0; i < states.Length; i++)
            {
                ResultButtonIntroState state = states[i];

                if (state == null ||
                    state.gameObject == null ||
                    !state.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float localElapsed =
                    elapsed - i * stagger;

                float progress =
                    Mathf.Clamp01(
                        localElapsed / duration
                    );

                float eased =
                    EaseOutCubic(progress);

                if (state.canvasGroup != null)
                {
                    state.canvasGroup.alpha =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            progress
                        );
                }

                if (state.rect != null && state.cached)
                {
                    Vector2 startPosition =
                        state.restPosition +
                        Vector2.down *
                        Mathf.Max(
                            0f,
                            resultButtonSlideDistance
                        );

                    Vector3 startScale =
                        state.restScale *
                        Mathf.Clamp(
                            resultButtonStartScale,
                            0.85f,
                            1f
                        );

                    state.rect.anchoredPosition =
                        Vector2.LerpUnclamped(
                            startPosition,
                            state.restPosition,
                            eased
                        );

                    state.rect.localScale =
                        Vector3.LerpUnclamped(
                            startScale,
                            state.restScale,
                            eased
                        );
                }

                bool finished =
                    progress >= 1f;

                if (state.canvasGroup != null)
                {
                    state.canvasGroup.interactable =
                        finished;

                    state.canvasGroup.blocksRaycasts =
                        finished;
                }

                if (state.button != null)
                {
                    state.button.interactable =
                        finished;
                }
            }

            yield return null;
        }

        for (int i = 0; i < states.Length; i++)
        {
            RestoreResultButtonState(states[i]);
        }

        resultButtonsIntroRoutine = null;
    }

    private static float EaseOutCubic(float value)
    {
        value = Mathf.Clamp01(value);
        float inverse = 1f - value;
        return 1f - inverse * inverse * inverse;
    }

    private void StopResultButtonsIntro()
    {
        if (resultButtonsIntroRoutine == null)
            return;

        StopCoroutine(resultButtonsIntroRoutine);
        resultButtonsIntroRoutine = null;
    }

    private void RestoreResultButtonsState()
    {
        RestoreResultButtonState(nextLevelButtonIntroState);
        RestoreResultButtonState(tryAgainButtonIntroState);
        RestoreResultButtonState(menuButtonIntroState);
    }

    private static void RestoreResultButtonState(
        ResultButtonIntroState state)
    {
        if (state == null || state.gameObject == null)
            return;

        if (state.rect != null && state.cached)
        {
            state.rect.anchoredPosition =
                state.restPosition;

            state.rect.localScale =
                state.restScale;
        }

        if (state.canvasGroup != null)
        {
            state.canvasGroup.alpha = 1f;
            state.canvasGroup.interactable = true;
            state.canvasGroup.blocksRaycasts = true;
        }

        if (state.button != null)
            state.button.interactable = true;
    }

    private void RestoreResultIntroState()
    {
        RestoreWorldExit();
        RestoreCinematicContent();
        PrepareResultIntroUI();

        if (resultPanelCanvasGroup != null)
        {
            resultPanelCanvasGroup.alpha = 1f;
            resultPanelCanvasGroup.interactable = true;
            resultPanelCanvasGroup.blocksRaycasts = true;
        }

        if (winUI != null)
        {
            winUI.transform.localScale =
                winUIRestScale;
        }

        if (loseUI != null)
        {
            loseUI.transform.localScale =
                loseUIRestScale;
        }
    }

    private void StartResultEdgeGlow(bool won)
    {
        HideResultEdgeGlowImmediate();

        if (resultEdgeGlow == null)
            return;

        Color glowColor = won ? winEdgeGlowColor : loseEdgeGlowColor;
        glowColor.a = 0f;

        PrepareResultEdgeGlowMaterial();
        SetResultEdgeGlowOpacity(glowColor, 0f);
        resultEdgeGlow.raycastTarget = false;
        resultEdgeGlow.gameObject.SetActive(true);
        resultEdgeGlowRoutine = StartCoroutine(AnimateResultEdgeGlow(glowColor));
    }

    private IEnumerator AnimateResultEdgeGlow(Color glowColor)
    {
        // Result panelinin kendi intro animasyonu bitsin, ardından ayrıca
        // ayarlanan süre kadar bekle. Bu sırada glow tamamen görünmez kalır.
        float initialDelay =
            Mathf.Max(0f, resultIntroDuration) +
            Mathf.Max(0f, edgeGlowStartDelay);

        if (initialDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                initialDelay
            );
        }

        if (resultEdgeGlow == null)
        {
            resultEdgeGlowRoutine = null;
            yield break;
        }

        float minAlpha =
            Mathf.Clamp01(
                Mathf.Min(
                    edgeGlowMinAlpha,
                    edgeGlowMaxAlpha
                )
            );

        float maxAlpha =
            Mathf.Clamp01(
                Mathf.Max(
                    edgeGlowMinAlpha,
                    edgeGlowMaxAlpha
                )
            );

        float fadeDuration = Mathf.Max(0.5f, edgeGlowFadeInDuration);
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(fadeElapsed / fadeDuration);
            float smoothT = t * t * t * (t * (t * 6f - 15f) + 10f);
            SetResultEdgeGlowOpacity(glowColor, maxAlpha * smoothT);
            yield return null;
        }
        SetResultEdgeGlowOpacity(glowColor, maxAlpha);

        float phase = 0f;
        float duration = Mathf.Max(0.25f, edgeGlowBreathDuration);
        while (resultEdgeGlow != null)
        {
            phase = Mathf.Repeat(phase + Time.unscaledDeltaTime / duration, 1f);
            // One sinusoid: flattening it again with SmootherStep concentrates
            // most changes into a short steep interval, which looks stepped.
            float pulse = (Mathf.Cos(phase * Mathf.PI * 2f) + 1f) * 0.5f;
            SetResultEdgeGlowOpacity(glowColor, Mathf.Lerp(minAlpha, maxAlpha, pulse));
            yield return null;
        }
        resultEdgeGlowRoutine = null;
    }

    private void PrepareResultEdgeGlowMaterial()
    {
        if (resultEdgeGlowMaterial == null)
        {
            Shader shader = Resources.Load<Shader>("ResultEdgeGlow/ResultEdgeGlow");
            if (shader == null || !shader.isSupported) return;
            resultEdgeGlowMaterial = new Material(shader) { name = "Result rounded edge glow" };
        }
        resultEdgeGlow.overrideSprite = null;
        resultEdgeGlow.sprite = null;
        resultEdgeGlow.type = Image.Type.Simple;
        resultEdgeGlow.preserveAspect = false;
        resultEdgeGlow.material = resultEdgeGlowMaterial;
        ResultEdgeGlowGeometry geometry = resultEdgeGlow.GetComponent<ResultEdgeGlowGeometry>();
        if (geometry == null) geometry = resultEdgeGlow.gameObject.AddComponent<ResultEdgeGlowGeometry>();
        geometry.SetShape(edgeGlowCornerRadius, edgeGlowSoftness);
        UpdateResultEdgeGlowShape();
    }

    private void UpdateResultEdgeGlowShape()
    {
        if (resultEdgeGlow == null || resultEdgeGlowMaterial == null) return;
        Rect rect = resultEdgeGlow.rectTransform.rect;
        float radius = Mathf.Clamp(edgeGlowCornerRadius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
        float softness = Mathf.Max(1f, edgeGlowSoftness);
        if (rect == resultEdgeGlowLastRect && radius == resultEdgeGlowLastRadius &&
            softness == resultEdgeGlowLastSoftness) return;
        resultEdgeGlowLastRect = rect;
        resultEdgeGlowLastRadius = radius;
        resultEdgeGlowLastSoftness = softness;
        resultEdgeGlowMaterial.SetVector(GlowBoundsId, new Vector4(rect.center.x, rect.center.y,
            rect.width * 0.5f, rect.height * 0.5f));
        resultEdgeGlowMaterial.SetFloat(GlowRadiusId, radius);
        resultEdgeGlowMaterial.SetFloat(GlowSoftnessId, softness);
        ResultEdgeGlowGeometry geometry = resultEdgeGlow.GetComponent<ResultEdgeGlowGeometry>();
        if (geometry != null) geometry.SetShape(radius, softness);
    }

    private void SetResultEdgeGlowOpacity(Color glowColor, float opacity)
    {
        if (resultEdgeGlow == null) return;
        if (resultEdgeGlowMaterial != null)
        {
            // Keep the UI vertex alpha constant at 1. Animate a float uniform
            // rather than the Graphic Color32 channel (only 15 steps at .06).
            glowColor.a = 1f;
            if (resultEdgeGlow.color != glowColor) resultEdgeGlow.color = glowColor;
            resultEdgeGlowMaterial.SetFloat(GlowOpacityId, Mathf.Clamp01(opacity));
            UpdateResultEdgeGlowShape();
        }
        else
        {
            glowColor.a = Mathf.Clamp01(opacity);
            resultEdgeGlow.color = glowColor;
        }
    }

    private void HideResultEdgeGlowImmediate()
    {
        if (resultEdgeGlowRoutine != null)
        {
            StopCoroutine(resultEdgeGlowRoutine);
            resultEdgeGlowRoutine = null;
        }

        if (resultEdgeGlow != null)
            resultEdgeGlow.gameObject.SetActive(false);
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.18f;
        float shifted = value - 1f;

        return 1f +
               (overshoot + 1f) *
               shifted *
               shifted *
               shifted +
               overshoot *
               shifted *
               shifted;
    }
}
