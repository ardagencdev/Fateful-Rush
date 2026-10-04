using System.Collections.Generic;
using UnityEngine;

/// <summary>Updates owned background property blocks only when values change.</summary>
internal sealed class BackgroundSpritePropertyCache
{
    private struct State
    {
        public Color tint;
        public Vector4 style, bounds;
    }
    private static readonly int TintId = Shader.PropertyToID("_Tint");
    private static readonly int StyleId = Shader.PropertyToID("_BackdropStyle");
    private static readonly int BoundsId = Shader.PropertyToID("_SurfaceBounds");
    private readonly Dictionary<SpriteRenderer, State> states = new Dictionary<SpriteRenderer, State>(3);
    private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

    public void Apply(SpriteRenderer renderer, Color tint, Vector4 style, Bounds bounds)
    {
        Vector4 vector = new Vector4(bounds.center.x, bounds.center.y, bounds.size.x, bounds.size.y);
        if (states.TryGetValue(renderer, out State previous) &&
            previous.tint.Equals(tint) && previous.style.Equals(style) && previous.bounds.Equals(vector))
            return;
        block.SetColor(TintId, tint);
        block.SetVector(StyleId, style);
        block.SetVector(BoundsId, vector);
        renderer.SetPropertyBlock(block);
        states[renderer] = new State { tint = tint, style = style, bounds = vector };
    }
}
