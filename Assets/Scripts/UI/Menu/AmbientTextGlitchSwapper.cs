using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AmbientTextGlitchSwapper : MonoBehaviour
{
    private const int RequiredTextCount = 3;

    [Header("References")]
    [Tooltip("SignalLostText, ThreatUnknownText ve NoReturnVectorText sırasıyla atanmalı.")]
    [SerializeField] private MenuFloatingText[] floatingTexts =
        new MenuFloatingText[RequiredTextCount];

    [Header("Swap Timing")]
    [Min(0.5f)]
    [SerializeField] private float swapIntervalMin = 10f;

    [Min(0.5f)]
    [SerializeField] private float swapIntervalMax = 20f;

    [Header("Shared Glitch")]
    [Range(0.04f, 0.25f)]
    [SerializeField] private float sharedGlitchDuration = 0.09f;

    [Range(0.1f, 0.9f)]
    [SerializeField] private float swapMoment = 0.5f;

    [Header("Reset")]
    [Tooltip("Main menu kapanınca yazıları kendi başlangıç sıralarına döndürür.")]
    [SerializeField] private bool restoreOriginalOrderOnDisable = true;

    // Each slot is stored in the common parent's local space. This avoids the
    // anchor-dependent anchoredPosition bug that moved texts off-screen.
    private readonly Vector3[] originalSlots = new Vector3[RequiredTextCount];
    private readonly int[] currentSlotByText = new int[RequiredTextCount];
    private readonly int[] nextSlotByText = new int[RequiredTextCount];

    private Coroutine swapRoutine;
    private bool slotsCaptured;

    private void Reset()
    {
        AutoAssignChildren();
    }

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        EnsureReferences();

        if (!HasValidReferences())
            return;

        if (swapRoutine != null)
            StopCoroutine(swapRoutine);

        swapRoutine = StartCoroutine(SwapLoop());
    }

    private IEnumerator SwapLoop()
    {
        while (isActiveAndEnabled && !AllTextsInitialized())
            yield return null;

        if (!isActiveAndEnabled)
            yield break;

        if (!ShareSameParent())
        {
            Debug.LogError(
                $"{nameof(AmbientTextGlitchSwapper)}: The three texts must be " +
                "children of the same parent RectTransform.",
                this
            );
            yield break;
        }

        SetSharedControl(true);

        if (!slotsCaptured)
            CaptureOriginalSlots();

        while (isActiveAndEnabled)
        {
            float minimum = Mathf.Max(0.5f, swapIntervalMin);
            float maximum = Mathf.Max(minimum, swapIntervalMax);

            yield return new WaitForSecondsRealtime(Random.Range(minimum, maximum));

            while (isActiveAndEnabled && !AllTextsReadyForGlitch())
                yield return null;

            if (!isActiveAndEnabled)
                yield break;

            yield return PlayGlitchAndSwap();
        }
    }

    private IEnumerator PlayGlitchAndSwap()
    {
        BuildRandomDerangement();

        float duration = Mathf.Max(0.04f, sharedGlitchDuration);
        float switchTime = duration * Mathf.Clamp(swapMoment, 0.1f, 0.9f);
        float elapsed = 0f;
        bool positionsChanged = false;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            for (int i = 0; i < RequiredTextCount; i++)
                floatingTexts[i].ApplySharedGlitchFrame();

            if (!positionsChanged && elapsed >= switchTime)
            {
                ApplyNextSlotAssignment();
                positionsChanged = true;
            }

            yield return null;
        }

        if (!positionsChanged)
            ApplyNextSlotAssignment();

        EndAllGlitches();
    }

    private void BuildRandomDerangement()
    {
        // With three texts there are exactly two arrangements where every text
        // moves to a different slot. Pick one randomly, so no text stays still
        // and no two texts can ever share a slot.
        bool rotateForward = Random.value < 0.5f;

        for (int i = 0; i < RequiredTextCount; i++)
        {
            int currentSlot = currentSlotByText[i];

            nextSlotByText[i] = rotateForward
                ? (currentSlot + 1) % RequiredTextCount
                : (currentSlot + RequiredTextCount - 1) % RequiredTextCount;
        }
    }

    private void ApplyNextSlotAssignment()
    {
        for (int i = 0; i < RequiredTextCount; i++)
        {
            int slotIndex = nextSlotByText[i];
            floatingTexts[i].SetBaseLocalPosition(originalSlots[slotIndex]);
            currentSlotByText[i] = slotIndex;
        }
    }

    private void CaptureOriginalSlots()
    {
        for (int i = 0; i < RequiredTextCount; i++)
        {
            originalSlots[i] = floatingTexts[i].BaseLocalPosition;
            currentSlotByText[i] = i;
            nextSlotByText[i] = i;
        }

        slotsCaptured = true;
    }

    private bool AllTextsInitialized()
    {
        if (!HasValidReferences())
            return false;

        for (int i = 0; i < RequiredTextCount; i++)
        {
            if (!floatingTexts[i].IsInitialized)
                return false;
        }

        return true;
    }

    private bool AllTextsReadyForGlitch()
    {
        for (int i = 0; i < RequiredTextCount; i++)
        {
            if (!floatingTexts[i].IsReadyForSharedGlitch)
                return false;
        }

        return true;
    }

    private bool ShareSameParent()
    {
        Transform parent = floatingTexts[0].TargetParent;

        if (parent == null)
            return false;

        for (int i = 1; i < RequiredTextCount; i++)
        {
            if (floatingTexts[i].TargetParent != parent)
                return false;
        }

        return true;
    }

    private void SetSharedControl(bool controlled)
    {
        if (floatingTexts == null)
            return;

        for (int i = 0; i < floatingTexts.Length; i++)
        {
            if (floatingTexts[i] != null)
                floatingTexts[i].SetSharedGlitchControlled(controlled);
        }
    }

    private void EndAllGlitches()
    {
        if (floatingTexts == null)
            return;

        for (int i = 0; i < floatingTexts.Length; i++)
        {
            if (floatingTexts[i] != null)
                floatingTexts[i].EndSharedGlitch();
        }
    }

    private void RestoreOriginalOrder()
    {
        if (!slotsCaptured || !HasValidReferences())
            return;

        for (int i = 0; i < RequiredTextCount; i++)
        {
            floatingTexts[i].SetBaseLocalPosition(originalSlots[i]);
            currentSlotByText[i] = i;
            nextSlotByText[i] = i;
        }
    }

    private void EnsureReferences()
    {
        if (HasValidReferences())
            return;

        AutoAssignChildren();

        if (!HasValidReferences())
        {
            Debug.LogWarning(
                $"{nameof(AmbientTextGlitchSwapper)} requires exactly three " +
                $"{nameof(MenuFloatingText)} references.",
                this
            );
        }
    }

    private void AutoAssignChildren()
    {
        MenuFloatingText[] foundTexts =
            GetComponentsInChildren<MenuFloatingText>(true);

        if (foundTexts.Length != RequiredTextCount)
            return;

        floatingTexts = foundTexts;
    }

    private bool HasValidReferences()
    {
        if (floatingTexts == null || floatingTexts.Length != RequiredTextCount)
            return false;

        for (int i = 0; i < RequiredTextCount; i++)
        {
            if (floatingTexts[i] == null)
                return false;

            for (int j = i + 1; j < RequiredTextCount; j++)
            {
                if (floatingTexts[i] == floatingTexts[j])
                    return false;
            }
        }

        return true;
    }

    private void OnDisable()
    {
        if (swapRoutine != null)
        {
            StopCoroutine(swapRoutine);
            swapRoutine = null;
        }

        EndAllGlitches();

        if (restoreOriginalOrderOnDisable)
            RestoreOriginalOrder();

        SetSharedControl(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        swapIntervalMin = Mathf.Max(0.5f, swapIntervalMin);
        swapIntervalMax = Mathf.Max(swapIntervalMin, swapIntervalMax);
        sharedGlitchDuration = Mathf.Max(0.04f, sharedGlitchDuration);
        swapMoment = Mathf.Clamp(swapMoment, 0.1f, 0.9f);
    }
#endif
}
