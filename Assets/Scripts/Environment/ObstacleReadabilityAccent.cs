using UnityEngine;

/// <summary>Steady visual-only inner contour for collidable obstacles spawned by ObstacleSpawner.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(400)]
public sealed class ObstacleReadabilityAccent : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_EdgeColor");
    private static readonly int WidthId = Shader.PropertyToID("_EdgeWidthPixels");
    private static readonly int RectId = Shader.PropertyToID("_SpriteUVRect");
    private static Material sharedEdgeMaterial;
    private SpriteRenderer[] sources, edges;
    private Sprite[] cachedSprites;
    private Vector4[] uvRects;
    private MaterialPropertyBlock properties;
    private bool[] edgePropertiesValid;
    private Color lastEdgeColor;
    private float lastWidthPixels;
    private Color edgeColor = new Color(1f, 0.68f, 0.28f, 0.75f);
    private float widthPixels = 1.4f;

    public void Configure(Color color, float width)
    {
        edgeColor = color;
        widthPixels = Mathf.Clamp(width, 0.5f, 3f);
    }
    private void Start()
    {
        if (sharedEdgeMaterial == null)
            sharedEdgeMaterial = Resources.Load<Material>("ObstacleReadability/ObstacleEdge");
        if (sharedEdgeMaterial == null)
        {
            Debug.LogWarning("ObstacleReadabilityAccent: supplied Resources material is missing.", this);
            enabled = false;
            return;
        }
        sources = GetComponentsInChildren<SpriteRenderer>(true);
        edges = new SpriteRenderer[sources.Length];
        cachedSprites = new Sprite[sources.Length];
        uvRects = new Vector4[sources.Length];
        properties = new MaterialPropertyBlock();
        edgePropertiesValid = new bool[sources.Length];
        for (int i = 0; i < sources.Length; i++)
        {
            GameObject child = new GameObject("ObstacleCollisionAccent");
            child.layer = sources[i].gameObject.layer;
            child.transform.SetParent(sources[i].transform, false);
            SpriteRenderer edge = child.AddComponent<SpriteRenderer>();
            edge.sharedMaterial = sharedEdgeMaterial;
            edge.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            edge.receiveShadows = false;
            edge.enabled = false;
            edges[i] = edge;
        }
        SyncEdges();
    }
    private void LateUpdate() { SyncEdges(); }
    private void SyncEdges()
    {
        if (sources == null || properties == null) return;
        bool styleChanged = !lastEdgeColor.Equals(edgeColor) || lastWidthPixels != widthPixels;
        lastEdgeColor = edgeColor;
        lastWidthPixels = widthPixels;
        for (int i = 0; i < sources.Length; i++)
        {
            if (styleChanged) edgePropertiesValid[i] = false;
            SpriteRenderer source = sources[i], edge = edges[i];
            if (source == null || edge == null) continue;
            bool visible = source.enabled && source.sprite != null && source.color.a > 0.001f;
            if (edge.enabled != visible) edge.enabled = visible;
            if (!edge.enabled) continue;
            if (cachedSprites[i] != source.sprite)
            {
                cachedSprites[i] = source.sprite;
                edgePropertiesValid[i] = false;
                Vector2[] uv = source.sprite.uv;
                Vector2 min = new Vector2(1f, 1f), max = Vector2.zero;
                for (int j = 0; j < uv.Length; j++) { min = Vector2.Min(min, uv[j]); max = Vector2.Max(max, uv[j]); }
                uvRects[i] = new Vector4(min.x, min.y, max.x, max.y);
            }
            edge.sprite = source.sprite;
            edge.flipX = source.flipX;
            edge.flipY = source.flipY;
            edge.drawMode = source.drawMode;
            edge.size = source.size;
            edge.maskInteraction = source.maskInteraction;
            edge.sortingLayerID = source.sortingLayerID;
            edge.sortingOrder = source.sortingOrder + 1;
            edge.color = new Color(1f, 1f, 1f, source.color.a);
            if (!edgePropertiesValid[i])
            {
                properties.SetColor(ColorId, edgeColor);
                properties.SetFloat(WidthId, widthPixels);
                properties.SetVector(RectId, uvRects[i]);
                edge.SetPropertyBlock(properties);
                edgePropertiesValid[i] = true;
            }
        }
    }
    private void OnDisable()
    {
        if (edges == null) return;
        foreach (SpriteRenderer edge in edges) if (edge != null) edge.enabled = false;
    }
}
