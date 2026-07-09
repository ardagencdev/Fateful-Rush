using System.Collections;
using UnityEngine;

public class HUDIntroAnimator : MonoBehaviour
{
    public static bool HUDIntroFinished { get; private set; }

    [System.Serializable]
    public class HUDItem
    {
        public GameObject target;
        public float delay = 0f;
    }

    [Header("HUD Order")]
    public HUDItem[] hudItems;

    [Header("Animation")]
    public float popDuration = 0.18f;
    public float startScale = 0f;
    public float overshootScale = 1.15f;
    public float finalScale = 1f;

    private Coroutine routine;

    private void Awake()
    {
        HUDIntroFinished = false;
        HideInstant();
    }

    public void Play()
    {
        if (routine != null)
            StopCoroutine(routine);

        HUDIntroFinished = false;
        routine = StartCoroutine(PlayRoutine());
    }

    public IEnumerator PlayAndWait()
    {
        if (routine != null)
            StopCoroutine(routine);

        HUDIntroFinished = false;
        yield return PlayRoutine();

        routine = null;
    }

    public void HideInstant()
    {
        HUDIntroFinished = false;

        if (hudItems == null) return;

        foreach (HUDItem item in hudItems)
        {
            if (item == null || item.target == null) continue;

            item.target.SetActive(false);
            item.target.transform.localScale = Vector3.one * finalScale;
        }
    }

    private IEnumerator PlayRoutine()
    {
        HideInstant();

        foreach (HUDItem item in hudItems)
        {
            if (item == null || item.target == null) continue;

            if (item.delay > 0f)
                yield return new WaitForSecondsRealtime(item.delay);

            yield return PopItem(item.target);
        }

        HUDIntroFinished = true;
    }

    private IEnumerator PopItem(GameObject target)
    {
        target.SetActive(true);

        Transform tr = target.transform;
        float timer = 0f;

        tr.localScale = Vector3.one * startScale;

        while (timer < popDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / popDuration);

            float scale;

            if (t < 0.65f)
            {
                float p = t / 0.65f;
                scale = Mathf.Lerp(startScale, overshootScale, EaseOutBack(p));
            }
            else
            {
                float p = (t - 0.65f) / 0.35f;
                scale = Mathf.Lerp(overshootScale, finalScale, p);
            }

            tr.localScale = Vector3.one * scale;
            yield return null;
        }

        tr.localScale = Vector3.one * finalScale;
    }

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}