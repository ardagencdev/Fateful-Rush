Shader "FatefulRush/ComboElectric"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                // A narrow bright core and soft edges; no texture or bloom required.
                half edge = abs(input.uv.y * 2.0 - 1.0);
                half softness = 1.0 - smoothstep(0.25, 1.0, edge);
                half core = 1.0 - smoothstep(0.0, 0.35, edge);
                return half4(lerp(input.color.rgb, half3(1,1,1), core * 0.35),
                    input.color.a * softness);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
