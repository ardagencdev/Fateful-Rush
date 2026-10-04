using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[DisallowMultipleComponent]
public sealed class AmbientTextGlitchSwapper : MonoBehaviour
{
    private const int RequiredTextCount = 2;
    [Header("References")]
    [Tooltip("ThreatUnknownText, NoReturnVectorText. Old three-reference arrays migrate automatically.")]
    [SerializeField] private MenuFloatingText[] floatingTexts = new MenuFloatingText[RequiredTextCount];
    [Header("Message Timing")]
    [Min(0.5f)] [SerializeField] private float swapIntervalMin = 10f;
    [Min(0.5f)] [SerializeField] private float swapIntervalMax = 20f;
    [Header("Shared Glitch")]
    [Range(0.04f, 0.25f)] [SerializeField] private float sharedGlitchDuration = 0.09f;
    [Range(0.1f, 0.9f)] [SerializeField] private float swapMoment = 0.5f;
    [Header("Reset")]
    [Tooltip("Reset to the equipped skin's first messages when disabled.")]
    [FormerlySerializedAs("restoreOriginalOrderOnDisable")]
    [SerializeField] private bool restoreOriginalTextsOnDisable = true;
    private readonly int[] indices = new int[RequiredTextCount];
    private readonly int[][] bags = new int[RequiredTextCount][];
    private readonly int[] bagPositions = new int[RequiredTextCount];
    private Coroutine routine;
    private string profile;
    private bool turkish, refreshRequested;

    private void Reset() { EnsureReferences(); }
    private void Awake() { EnsureReferences(); }
    private void OnEnable()
    {
        EnsureReferences();
        if (!HasValidReferences()) return;
        ClaimTexts(true);
        PlayerSkinCatalog.SelectedSkinChanged += HandleSkinChanged;
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        profile = null;
        turkish = FatefulRushLocalization.IsTurkish;
        RefreshProfile();
        ApplyTexts();
        refreshRequested = false;
        routine = StartCoroutine(MessageLoop());
    }
    private void HandleSkinChanged() { refreshRequested = true; }
    private void HandleLocaleChanged(Locale locale)
    {
        turkish = locale != null && locale.Identifier.Code.StartsWith("tr", System.StringComparison.OrdinalIgnoreCase);
        refreshRequested = true;
    }
    private bool RefreshProfile()
    {
        string selected = AmbientSkinMessages.Normalize(PlayerPrefs.GetString(PlayerSkinCatalog.SelectedSkinKey, "white"));
        if (selected == profile) return false;
        profile = selected;
        for (int i = 0; i < RequiredTextCount; i++)
        {
            indices[i] = 0;
            bags[i] = null;
            bagPositions[i] = 0;
        }
        return true;
    }
    private IEnumerator MessageLoop()
    {
        while (isActiveAndEnabled && !AllReady(false)) yield return null;
        if (!isActiveAndEnabled) yield break;
        // Initialization may have completed since OnEnable. Enforce exclusive message ownership.
        ClaimTexts(true);
        ApplyTexts();
        float remaining = NextInterval();
        while (isActiveAndEnabled)
        {
            remaining -= Time.unscaledDeltaTime;
            if (refreshRequested || remaining <= 0f)
            {
                while (isActiveAndEnabled && !AllReady(true)) yield return null;
                if (!isActiveAndEnabled) yield break;
                bool advance = !refreshRequested;
                refreshRequested = false;
                yield return GlitchAndChange(advance);
                remaining = NextInterval();
            }
            yield return null;
        }
    }
    private float NextInterval()
    {
        float min = Mathf.Max(0.5f, swapIntervalMin);
        return Random.Range(min, Mathf.Max(min, swapIntervalMax));
    }
    private IEnumerator GlitchAndChange(bool advance)
    {
        float duration = Mathf.Max(0.04f, sharedGlitchDuration);
        float midpoint = duration * Mathf.Clamp(swapMoment, 0.1f, 0.9f);
        float elapsed = 0f;
        bool committed = false;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (!committed && elapsed >= midpoint)
            {
                Commit(advance);
                committed = true;
            }
            for (int i = 0; i < RequiredTextCount; i++) floatingTexts[i].ApplySharedGlitchFrame();
            yield return null;
        }
        if (!committed) Commit(advance);
        EndGlitches();
    }
    private void Commit(bool advance)
    {
        bool skinChanged = RefreshProfile();
        // Read the latest request at the midpoint, so a rapid equip never writes stale text.
        if (advance && !skinChanged && !refreshRequested)
            for (int i = 0; i < RequiredTextCount; i++) indices[i] = NextIndex(i);
        ApplyTexts();
    }
    private int NextIndex(int slot)
    {
        int count = AmbientSkinMessages.Count(profile, slot);
        if (count <= 1) return 0;
        if (bags[slot] == null || bagPositions[slot] >= count)
        {
            int[] bag = bags[slot];
            if (bag == null || bag.Length != count) bag = new int[count];
            for (int i = 0; i < count; i++) bag[i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int value = bag[i]; bag[i] = bag[j]; bag[j] = value;
            }
            if (bag[0] == indices[slot])
            {
                int value = bag[0]; bag[0] = bag[1]; bag[1] = value;
            }
            bags[slot] = bag;
            bagPositions[slot] = 0;
        }
        return bags[slot][bagPositions[slot]++];
    }
    private void ApplyTexts()
    {
        for (int i = 0; i < RequiredTextCount; i++)
            floatingTexts[i].SetStableText(AmbientSkinMessages.Get(profile, i, indices[i], turkish));
    }
    private bool AllReady(bool glitchReady)
    {
        if (!HasValidReferences()) return false;
        for (int i = 0; i < RequiredTextCount; i++)
            if (glitchReady ? !floatingTexts[i].IsReadyForSharedGlitch : !floatingTexts[i].IsInitialized) return false;
        return true;
    }
    private void ClaimTexts(bool owned)
    {
        if (floatingTexts == null) return;
        foreach (MenuFloatingText text in floatingTexts)
        {
            if (text == null) continue;
            text.SetExternalMessagesControlled(owned);
            text.SetSharedGlitchControlled(owned);
        }
    }
    private void EndGlitches()
    {
        if (floatingTexts == null) return;
        foreach (MenuFloatingText text in floatingTexts) if (text != null) text.EndSharedGlitch();
    }
    private void EnsureReferences()
    {
        // Scene data may still contain the former three-item array, with an
        // inactive or deleted SignalLostText. Match the two surviving labels
        // before validating; never wait for the unused third label to initialize.
        MenuFloatingText[] found = GetComponentsInChildren<MenuFloatingText>(true);
        MenuFloatingText threat = null, navigation = null;
        foreach (MenuFloatingText text in found)
        {
            if (string.Equals(text.name, "ThreatUnknownText", System.StringComparison.OrdinalIgnoreCase)) threat = text;
            if (string.Equals(text.name, "NoReturnVectorText", System.StringComparison.OrdinalIgnoreCase)) navigation = text;
        }
        if (threat != null && navigation != null && threat != navigation)
        {
            floatingTexts = new[] { threat, navigation };
            return;
        }
        // Preserve manually assigned references for renamed labels. Drop the
        // old SignalLostText reference and any deleted (null) reference.
        if (floatingTexts != null)
        {
            var kept = new System.Collections.Generic.List<MenuFloatingText>(RequiredTextCount);
            foreach (MenuFloatingText text in floatingTexts)
            {
                if (text == null || string.Equals(text.name, "SignalLostText", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!kept.Contains(text)) kept.Add(text);
            }
            if (kept.Count == RequiredTextCount) floatingTexts = kept.ToArray();
        }
        if (!HasValidReferences())
            Debug.LogWarning("AmbientTextGlitchSwapper: assign ThreatUnknownText and NoReturnVectorText only.", this);
    }
    private bool HasValidReferences()
    {
        if (floatingTexts == null || floatingTexts.Length != RequiredTextCount) return false;
        for (int i = 0; i < RequiredTextCount; i++)
        {
            if (floatingTexts[i] == null) return false;
            for (int j = i + 1; j < RequiredTextCount; j++) if (floatingTexts[i] == floatingTexts[j]) return false;
        }
        return true;
    }
    private void OnDisable()
    {
        PlayerSkinCatalog.SelectedSkinChanged -= HandleSkinChanged;
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        if (routine != null) { StopCoroutine(routine); routine = null; }
        EndGlitches();
        if (restoreOriginalTextsOnDisable && HasValidReferences())
        {
            RefreshProfile();
            for (int i = 0; i < RequiredTextCount; i++) indices[i] = 0;
            ApplyTexts();
        }
        ClaimTexts(false);
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        swapIntervalMin = Mathf.Max(0.5f, swapIntervalMin);
        swapIntervalMax = Mathf.Max(swapIntervalMin, swapIntervalMax);
    }
#endif
}
