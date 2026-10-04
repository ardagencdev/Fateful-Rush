Shader "FatefulRush/HomePlanetTheme"
{
    Properties
    {
        [PerRendererData] _MainTex ("Planet Sprite", 2D) = "white" {}
        _Tint ("Tint", Color) = (0.65,0.65,0.65,1)
        _ThemeFlashColor ("Theme Flash", Color) = (1,0.85,0.4,1)
        _ThemeFlashAmount ("Flash Amount", Float) = 0
        _ThemeSweep ("Surface Sweep", Float) = -1
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
                half4 _ThemeFlashColor;
                float _ThemeFlashAmount;
                float _ThemeSweep;
                float4 _SurfaceBounds;
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
                half4 color = tex * _Tint * input.color;
                color.rgb = ApplySolarSurface(color.rgb, input.surfaceUV, input.positionOS);
                float band = 1.0 - smoothstep(0.02, 0.16, abs(input.surfaceUV.x - _ThemeSweep));
                float flash = saturate(_ThemeFlashAmount * (0.65 + band * 0.65));
                color.rgb = lerp(color.rgb, _ThemeFlashColor.rgb, flash);
                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
