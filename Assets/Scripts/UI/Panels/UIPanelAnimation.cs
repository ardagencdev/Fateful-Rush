using UnityEngine;
using System.Collections;

public class UIPanelAnimation : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private float startScale = 0.8f;

    private CanvasGroup canvasGroup;
    private Coroutine animationRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(AnimatePanel());
    }

    private IEnumerator AnimatePanel()
    {
        float time = 0f;

        Vector3 fromScale = Vector3.one * startScale;
        Vector3 toScale = Vector3.one;

        transform.localScale = fromScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            transform.localScale = Vector3.Lerp(fromScale, toScale, t);

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        transform.localScale = toScale;
        animationRoutine = null;
    }
}