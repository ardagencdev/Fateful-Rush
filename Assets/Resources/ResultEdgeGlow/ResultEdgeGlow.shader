Shader "FatefulRush/UI/RoundedResultEdgeGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowOpacity ("Opacity", Range(0,1)) = 0
        _GlowCornerRadius ("Corner radius", Float) = 28
        _GlowSoftness ("Inward falloff", Float) = 32
        _GlowBounds ("Center and half size", Vector) = (0,0,960,540)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 localPosition : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            float4 _Color;
            float4 _ClipRect;
            float4 _GlowBounds;
            float _GlowOpacity;
            float _GlowCornerRadius;
            float _GlowSoftness;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.localPosition = v.vertex.xy;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float2 halfSize = max(_GlowBounds.zw, 0.001);
                float radius = clamp(_GlowCornerRadius, 0.0, min(halfSize.x, halfSize.y));
                float2 q = abs(i.localPosition - _GlowBounds.xy) - halfSize + radius;
                float distanceToEdge = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
                float aa = max(fwidth(distanceToEdge), 0.5);
                float inside = 1.0 - smoothstep(-aa, aa, distanceToEdge);
                float falloff = exp2(-max(0.0, -distanceToEdge) / max(1.0, _GlowSoftness) * 3.0);
                float opacity = saturate(_GlowOpacity) * falloff * inside;
                // Subtle spatial dithering keeps very faint gradients from
                // collapsing into visibly separate 8-bit brightness bands.
                float noise = frac(52.9829189 * frac(dot(i.vertex.xy, float2(0.06711056, 0.00583715)))) - 0.5;
                opacity = _GlowOpacity > 0.0 ? max(0.0, opacity + noise * falloff * inside / 255.0) : 0.0;
                float4 color = float4(i.color.rgb, opacity * i.color.a);
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.localPosition, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
