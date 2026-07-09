using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDTransparency : MonoBehaviour
{
    [Range(0f, 1f)]
    public float alpha = 0.7f;

    [Header("Ignore")]
    public Graphic[] ignoreGraphics;

    private void Start()
    {
        ApplyTransparency();
    }

    public void ApplyTransparency()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

        foreach (Graphic g in graphics)
        {
            bool ignored = false;

            foreach (Graphic ignore in ignoreGraphics)
            {
                if (g == ignore)
                {
                    ignored = true;
                    break;
                }
            }

            if (ignored) continue;

            Color c = g.color;
            c.a = alpha;
            g.color = c;
        }
    }
}