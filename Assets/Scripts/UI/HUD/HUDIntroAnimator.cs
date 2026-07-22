using System.Collections;
using UnityEngine;

public class HUDIntroAnimator : MonoBehaviour
{
    public static bool HUDIntroFinished { get; private set; }

    [System.Serializable]
    public class HUDItem
    {
        public GameObject target;

        [Min(0f)]
        public float delay;
    }

    [Header("HUD Order")]
    public HUDItem[] hudItems;

    [Header("Animation")]
    [Min(0f)]
    public float popDuration = 0.18f;

    [Min(0f)]
    public float startScale;

    [Min(0f)]
    public float overshootScale = 1.15f;

    [Min(0f)]
    public float finalScale = 1f;

    private Coroutine activeRoutine;

    private void Awake()
    {
        HUDIntroFinished = false;
        HideInstant();
    }

    private void OnDisable()
    {
        StopActiveRoutine();
        HUDIntroFinished = false;
    }

    public void Play()
    {
        StopActiveRoutine();

        HUDIntroFinished = false;
        activeRoutine = StartCoroutine(PlayRoutine());
    }

    public IEnumerator PlayAndWait()
    {
        StopActiveRoutine();

        HUDIntroFinished = false;
        activeRoutine = StartCoroutine(PlayRoutine());

        yield return activeRoutine;
    }

    public void HideInstant()
    {
        StopActiveRoutine();

        HUDIntroFinished = false;

        if (hudItems == null)
            return;

        foreach (HUDItem item in hudItems)
        {
            if (item == null || item.target == null)
                continue;

            Transform targetTransform = item.target.transform;

            targetTransform.localScale =
                Vector3.one * finalScale;

            item.target.SetActive(false);
        }
    }

    private IEnumerator PlayRoutine()
    {
        PrepareItemsForIntro();

        if (hudItems == null || hudItems.Length == 0)
        {
            HUDIntroFinished = true;
            activeRoutine = null;
            yield break;
        }

        foreach (HUDItem item in hudItems)
        {
            if (item == null || item.target == null)
                continue;

            if (item.delay > 0f)
                yield return new WaitForSecondsRealtime(item.delay);

            yield return PopItem(item.target);
        }

        HUDIntroFinished = true;
        activeRoutine = null;
    }

    private IEnumerator PopItem(GameObject target)
    {
        if (target == null)
            yield break;

        Transform targetTransform =
            target.transform;

        /*
         * UIButtonEffect gibi componentlerin Awake sırasında
         * scale değerini 0 olarak kaydetmesini engeller.
         */
        targetTransform.localScale =
            Vector3.one * finalScale;

        target.SetActive(true);

        if (popDuration <= 0f)
        {
            targetTransform.localScale =
                Vector3.one * finalScale;

            yield break;
        }

        float elapsedTime = 0f;

        targetTransform.localScale =
            Vector3.one * startScale;

        while (elapsedTime < popDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime / popDuration
                );

            float scale =
                EvaluateScale(normalizedTime);

            targetTransform.localScale =
                Vector3.one * scale;

            yield return null;
        }

        targetTransform.localScale =
            Vector3.one * finalScale;
    }

    private float EvaluateScale(float normalizedTime)
    {
        const float overshootPoint = 0.65f;

        if (normalizedTime < overshootPoint)
        {
            float progress =
                normalizedTime / overshootPoint;

            return Mathf.Lerp(
                startScale,
                overshootScale,
                EaseOutBack(progress)
            );
        }

        float returnProgress =
            (normalizedTime - overshootPoint) /
            (1f - overshootPoint);

        return Mathf.Lerp(
            overshootScale,
            finalScale,
            returnProgress
        );
    }

    private void PrepareItemsForIntro()
    {
        if (hudItems == null)
            return;

        foreach (HUDItem item in hudItems)
        {
            if (item == null || item.target == null)
                continue;

            item.target.transform.localScale =
                Vector3.one * startScale;

            item.target.SetActive(false);
        }
    }

    private void StopActiveRoutine()
    {
        if (activeRoutine == null)
            return;

        StopCoroutine(activeRoutine);
        activeRoutine = null;
    }

    private static float EaseOutBack(float value)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        float shiftedValue = value - 1f;

        return 1f +
               c3 * shiftedValue * shiftedValue * shiftedValue +
               c1 * shiftedValue * shiftedValue;
    }

    private void OnValidate()
    {
        popDuration = Mathf.Max(0f, popDuration);
        startScale = Mathf.Max(0f, startScale);
        overshootScale = Mathf.Max(0f, overshootScale);
        finalScale = Mathf.Max(0f, finalScale);

        if (hudItems == null)
            return;

        foreach (HUDItem item in hudItems)
        {
            if (item != null)
                item.delay = Mathf.Max(0f, item.delay);
        }
    }
}