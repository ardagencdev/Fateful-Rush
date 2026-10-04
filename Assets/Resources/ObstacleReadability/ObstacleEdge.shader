Shader "FatefulRush/ObstacleCollisionAccent"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _EdgeColor ("Edge Color", Color) = (1,0.68,0.28,0.75)
        _EdgeWidthPixels ("Screen Pixel Width", Float) = 1.4
        _SpriteUVRect ("Atlas UV Rect", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _EdgeColor;
                float _EdgeWidthPixels;
                float4 _SpriteUVRect;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half AlphaAt(float2 uv)
            {
                if (any(uv < _SpriteUVRect.xy) || any(uv > _SpriteUVRect.zw)) return 0;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 dx = ddx(input.uv) * _EdgeWidthPixels;
                float2 dy = ddy(input.uv) * _EdgeWidthPixels;
                half alpha = AlphaAt(input.uv);
                half neighbor = min(min(AlphaAt(input.uv + dx), AlphaAt(input.uv - dx)),
                    min(AlphaAt(input.uv + dy), AlphaAt(input.uv - dy)));
                half edge = saturate((alpha - neighbor) * 3.0);
                return half4(_EdgeColor.rgb, edge * alpha * _EdgeColor.a * input.color.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
