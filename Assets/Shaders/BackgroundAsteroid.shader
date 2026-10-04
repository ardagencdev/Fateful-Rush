Shader "FatefulRush/BackgroundAsteroid"
{
    Properties
    {
        [PerRendererData] _MainTex ("Asteroid Sprite", 2D) = "white" {}
        _Tint ("Tint", Color) = (0.45,0.45,0.45,1)
        _BackdropStyle ("Backdrop: desaturation, contrast, sun", Vector) = (0,1,1,0)
        _SurfaceBounds ("Sprite Bounds", Vector) = (0,0,1,1)
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
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/SolarSurfaceLighting.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float4 _SurfaceBounds;
                float4 _BackdropStyle;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float2 surfaceUV : TEXCOORD1; float3 positionOS : TEXCOORD2; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                output.positionOS = input.positionOS.xyz;
                output.surfaceUV = (input.positionOS.xy - _SurfaceBounds.xy) / max(float2(0.001, 0.001), _SurfaceBounds.zw) + 0.5;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half luminance = dot(tex.rgb, half3(0.2126, 0.7152, 0.0722));
                tex.rgb = lerp(tex.rgb, half3(luminance, luminance, luminance), saturate(_BackdropStyle.x));
                tex.rgb = lerp(half3(0.35, 0.35, 0.35), tex.rgb, saturate(_BackdropStyle.y));
                half4 color = tex * _Tint * input.color;
                half3 lit = ApplySolarSurface(color.rgb, input.surfaceUV, input.positionOS);
                color.rgb = lerp(color.rgb, lit, saturate(_BackdropStyle.z));
                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
