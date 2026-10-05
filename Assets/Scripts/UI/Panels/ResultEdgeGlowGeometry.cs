using UnityEngine;
using UnityEngine.UI;

// Runtime mesh modifier attached to the existing Image. No Inspector setup.
// Retains the same rounded-distance shader and smooth float opacity, but avoids
// shading the practically invisible center of a full-screen quad.
public sealed class ResultEdgeGlowGeometry : BaseMeshEffect
{
    float radius = 28f, softness = 32f;
    public void SetShape(float cornerRadius, float falloff)
    {
        if (radius == cornerRadius && softness == falloff) return;
        radius = cornerRadius;
        softness = falloff;
        if (graphic != null) graphic.SetVerticesDirty();
    }
    public override void ModifyMesh(VertexHelper mesh)
    {
        if (!IsActive() || mesh.currentVertCount == 0 || graphic == null) return;
        UIVertex original = default;
        mesh.PopulateUIVertex(ref original, 0);
        Rect rect = graphic.GetPixelAdjustedRect();
        float thickness = Mathf.Clamp(Mathf.Max(radius + 2f, Mathf.Max(1f, softness) * 4f),
            1f, Mathf.Max(1f, Mathf.Min(rect.width, rect.height) * .5f));
        float left = rect.xMin, right = rect.xMax, bottom = rect.yMin, top = rect.yMax;
        mesh.Clear();
        AddBand(mesh, original.color, left, top - thickness, right, top);
        AddBand(mesh, original.color, left, bottom, right, bottom + thickness);
        if (top - thickness > bottom + thickness)
        {
            AddBand(mesh, original.color, left, bottom + thickness, left + thickness, top - thickness);
            AddBand(mesh, original.color, right - thickness, bottom + thickness, right, top - thickness);
        }
    }
    static void AddBand(VertexHelper mesh, Color32 color, float x0, float y0, float x1, float y1)
    {
        int start = mesh.currentVertCount;
        UIVertex v = UIVertex.simpleVert;
        v.color = color;
        v.position = new Vector3(x0, y0); mesh.AddVert(v);
        v.position = new Vector3(x0, y1); mesh.AddVert(v);
        v.position = new Vector3(x1, y1); mesh.AddVert(v);
        v.position = new Vector3(x1, y0); mesh.AddVert(v);
        mesh.AddTriangle(start, start + 1, start + 2);
        mesh.AddTriangle(start + 2, start + 3, start);
    }
}
