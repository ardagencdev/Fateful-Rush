using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UIHorizontalFlip : BaseMeshEffect
{
    [SerializeField]
    private bool flipped;

    public bool Flipped => flipped;

    public void SetFlipped(bool value)
    {
        if (flipped == value)
            return;

        flipped = value;

        if (graphic != null)
            graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || !flipped)
            return;

        List<UIVertex> vertices = new List<UIVertex>();
        vh.GetUIVertexStream(vertices);

        RectTransform rectTransform =
            transform as RectTransform;

        if (rectTransform == null)
            return;

        float centerX = rectTransform.rect.center.x;

        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex vertex = vertices[i];

            Vector3 position = vertex.position;
            position.x =
                centerX - (position.x - centerX);

            vertex.position = position;
            vertices[i] = vertex;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(vertices);
    }
}