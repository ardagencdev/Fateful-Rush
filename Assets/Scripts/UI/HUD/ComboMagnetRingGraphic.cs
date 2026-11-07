using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight HUD ring built from UI vertices (no textures, particles or Update).
/// The parent controls rotation and opacity. This draws only when combo >= x4.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ComboMagnetRingGraphic : MaskableGraphic
{
    private int stage;

    public void Configure(int combo, Color tint)
    {
        int newStage = Mathf.Clamp(combo, 0, 6);
        bool changed = stage != newStage || color != tint;
        stage = newStage;
        if (changed)
        {
            color = tint;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (stage < 4)
            return;

        Rect area = rectTransform.rect;
        float rx = Mathf.Max(5f, area.width * 0.5f - 3f);
        float ry = Mathf.Max(5f, area.height * 0.5f - 3f);
        float thickness = stage == 4 ? 1.35f : stage == 5 ? 1.75f : 2.15f;

        // Broken circular arcs suggest a magnetic field without covering the text.
        AddArc(vh, rx, ry, thickness, 20f, 86f, 12, 0.75f);
        AddArc(vh, rx, ry, thickness, 198f, 83f, 12, 0.70f);

        if (stage >= 5)
        {
            AddArc(vh, rx, ry, thickness, 121f, 48f, 7, 0.60f);
            AddArc(vh, rx - 4.5f, ry - 4.5f, 0.95f, 292f, 42f, 7, 0.42f);
        }

        if (stage >= 6)
        {
            AddArc(vh, rx - 5.5f, ry - 5.5f, 1.05f, 45f, 78f, 10, 0.56f);
            AddArc(vh, rx - 5.5f, ry - 5.5f, 1.05f, 216f, 72f, 10, 0.50f);
            AddTick(vh, rx, ry, 14f, 3.8f, 0.75f);
            AddTick(vh, rx, ry, 111f, 3.2f, 0.67f);
            AddTick(vh, rx, ry, 196f, 3.5f, 0.80f);
            AddTick(vh, rx, ry, 291f, 3.2f, 0.67f);
        }
    }

    private void AddArc(VertexHelper vh, float rx, float ry, float thick,
        float startDegrees, float arcDegrees, int steps, float alpha)
    {
        for (int i = 0; i < steps; i++)
        {
            float a = (startDegrees + arcDegrees * i / steps) * Mathf.Deg2Rad;
            float b = (startDegrees + arcDegrees * (i + 1) / steps) * Mathf.Deg2Rad;
            float edge0 = Mathf.Sin(Mathf.PI * i / steps);
            float edge1 = Mathf.Sin(Mathf.PI * (i + 1) / steps);
            // Gentle taper on each arc's ends.
            Color32 ca = WithAlpha(alpha * (0.24f + 0.76f * edge0));
            Color32 cb = WithAlpha(alpha * (0.24f + 0.76f * edge1));
            Vector2 aOut = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
            Vector2 bOut = new Vector2(Mathf.Cos(b) * rx, Mathf.Sin(b) * ry);
            Vector2 aIn = new Vector2(Mathf.Cos(a) * (rx - thick), Mathf.Sin(a) * (ry - thick));
            Vector2 bIn = new Vector2(Mathf.Cos(b) * (rx - thick), Mathf.Sin(b) * (ry - thick));
            int n = vh.currentVertCount;
            vh.AddVert(aOut, ca, Vector2.zero);
            vh.AddVert(bOut, cb, Vector2.zero);
            vh.AddVert(bIn, cb, Vector2.zero);
            vh.AddVert(aIn, ca, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n, n + 2, n + 3);
        }
    }

    private void AddTick(VertexHelper vh, float rx, float ry, float angle,
        float length, float alpha)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector2 normal = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        Vector2 center = new Vector2(normal.x * rx, normal.y * ry);
        Vector2 tangent = new Vector2(-normal.y, normal.x) * 0.65f;
        Vector2 radial = normal * (length * 0.5f);
        Color32 c = WithAlpha(alpha);
        int n = vh.currentVertCount;
        vh.AddVert(center - radial - tangent, c, Vector2.zero);
        vh.AddVert(center - radial + tangent, c, Vector2.zero);
        vh.AddVert(center + radial + tangent, c, Vector2.zero);
        vh.AddVert(center + radial - tangent, c, Vector2.zero);
        vh.AddTriangle(n, n + 1, n + 2);
        vh.AddTriangle(n, n + 2, n + 3);
    }

    private Color32 WithAlpha(float alpha)
    {
        Color c = color;
        c.a *= Mathf.Clamp01(alpha);
        return c;
    }
}
