using UnityEngine;
using UnityEngine.UI;

/// <summary>Dashed HUD ring with a few brief sparks at the higher magnet stages.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ComboMagnetRingGraphic : MaskableGraphic
{
    private int stage;
    private int sparkFrame = -1;

    public void Configure(int combo, Color tint)
    {
        if (stage == combo && color == tint)
            return;

        stage = combo;
        color = tint;
        SetVerticesDirty();
    }

    public void UpdateSparks(float time)
    {
        if (stage < 5)
            return;

        // The ring rotates via its transform. Only rebuild the spark geometry at 12 Hz.
        int frame = Mathf.FloorToInt(time * 12f);
        if (frame == sparkFrame)
            return;

        sparkFrame = frame;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (stage < 4)
            return;

        Rect area = rectTransform.rect;
        float radius = Mathf.Max(5f, Mathf.Min(area.width, area.height) * 0.5f - 5f);
        bool stronger = stage >= 5;
        float thickness = stage >= 6 ? 1.85f : stronger ? 1.55f : 1.25f;
        int dashCount = stronger ? 12 : 8;
        float spacing = 360f / dashCount;

        for (int i = 0; i < dashCount; i++)
            AddArc(vh, radius, thickness, i * spacing, spacing * 0.44f, 3, stronger ? 0.94f : 0.88f);

        if (!stronger)
            return;

        // A faint continuous rim sits outside the brighter dashed ring.
        AddArc(vh, radius + 2.7f, 0.8f, 0f, 360f, 48, stage >= 6 ? 0.28f : 0.19f);
        AddSpark(vh, radius, 0, 12);
        AddSpark(vh, radius, 6, 15);

        if (stage >= 6)
            AddSpark(vh, radius, 10, 17);
    }

    private void AddArc(VertexHelper vh, float radius, float thickness,
        float startDegrees, float sweepDegrees, int steps, float alpha)
    {
        Color32 tint = WithAlpha(alpha);
        Vector2 previous = Direction(startDegrees);
        for (int i = 1; i <= steps; i++)
        {
            Vector2 next = Direction(startDegrees + sweepDegrees * i / steps);
            AddQuad(vh, previous * radius, next * radius,
                next * (radius - thickness), previous * (radius - thickness), tint);
            previous = next;
        }
    }

    private void AddSpark(VertexHelper vh, float radius, int offset, int period)
    {
        if (sparkFrame < 0)
            return;

        int frame = sparkFrame + offset;
        int age = frame % period;
        if (age >= 3)
            return;

        float angle = (frame / period) * 137.508f + offset * 31f;
        Vector2 outward = Direction(angle);
        Vector2 tangent = new Vector2(-outward.y, outward.x);
        Vector2 start = outward * (radius + 0.5f + age * 0.8f);
        Vector2 bend = start + outward * 1.8f + tangent * 0.65f;
        Vector2 end = bend + outward * 1.6f - tangent * 1.0f;
        Vector2 edge = tangent * 0.45f;
        Color32 tint = WithAlpha(0.92f - age * 0.25f);

        AddQuad(vh, start - edge, start + edge, bend + edge, bend - edge, tint);
        AddQuad(vh, bend - edge, bend + edge, end + edge, end - edge, tint);
    }

    private static Vector2 Direction(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b,
        Vector2 c, Vector2 d, Color32 tint)
    {
        int index = vh.currentVertCount;
        vh.AddVert(a, tint, Vector2.zero);
        vh.AddVert(b, tint, Vector2.zero);
        vh.AddVert(c, tint, Vector2.zero);
        vh.AddVert(d, tint, Vector2.zero);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }

    private Color32 WithAlpha(float alpha)
    {
        Color tint = color;
        tint.a *= alpha;
        return tint;
    }
}
