Shader "FatefulRush/SolarAtmosphereBake"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend One Zero
        ZTest Always
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _FRSolarViewport;
            half4 _FRSolarColor;
            float _FRSolarAtmosphereStrength;
            float _FRSolarClock;
            float _FRSolarCenterProtection;
            float4 _SolarAxis;
            float4 _RayCenters0;
            float4 _RayCenters1;
            float _SolarBreathe;
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.uv = GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 metric = float2(_FRSolarViewport.z, 1.0);
                float2 d = (input.uv - _FRSolarViewport.xy) * metric;
                float2 axis = _SolarAxis.xy;
                float distanceToSource = max(0.001, length(d));
                float along = dot(d, axis);
                float across = d.x * axis.y - d.y * axis.x;
                float angular = across / max(0.07, along);
                // Six broad overlapping beams; slow drift, no random per-frame flicker.
                float rays = 0.0;
                [unroll] for (int i = 0; i < 6; i++)
                {
                    float seed = (float)i;
                    float center = i < 4 ? _RayCenters0[min(i, 3)] : _RayCenters1[max(0, i - 4)];
                    float width = 0.07 + 0.025 * sin(seed * 1.7 + 1.0);
                    float delta = (angular - center) / width;
                    rays += exp(-delta * delta) * (0.035 + 0.012 * sin(seed * 2.4));
                }
                float fan = exp(-angular * angular * 1.4) * smoothstep(0.0, 0.12, along);
                float glow = exp(-distanceToSource * distanceToSource * 15.0) * 0.85;
                float haze = exp(-distanceToSource * 1.8) * fan * 0.10;
                rays *= exp(-distanceToSource * 1.4) * fan;
                float centerMask = smoothstep(0.10, 0.60, length((input.uv - 0.5) * metric));
                float protect = lerp(1.0, 0.25 + 0.75 * centerMask, _FRSolarCenterProtection);
                float breathe = _SolarBreathe;
                float energy = (glow + haze + rays) * protect * breathe * _FRSolarAtmosphereStrength;
                half3 color = _FRSolarColor.rgb * energy;
                // A warm compact core, with the wide glow acting as bloom without a post-process.
                color += lerp(_FRSolarColor.rgb, half3(1.0, 0.94, 0.85), 0.25)
                    * exp(-distanceToSource * distanceToSource * 95.0) * _FRSolarAtmosphereStrength * 0.18;
                return half4(color, 0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
