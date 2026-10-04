#ifndef FATEFUL_SOLAR_SURFACE_INCLUDED
#define FATEFUL_SOLAR_SURFACE_INCLUDED
// Globals supplied by SolarAtmosphere. Zero strength leaves the original sprite unchanged.
float4 _FRSolarPosition;
half4 _FRSolarColor;
float _FRSolarSurfaceStrength;
half3 ApplySolarSurface(half3 baseRGB, float2 surfaceUV, float3 positionOS)
{
    if (_FRSolarSurfaceStrength <= 0.0001) return baseRGB;
    float2 p = (surfaceUV - 0.5) * 2.0;
    float radius2 = dot(p, p);
    float z = sqrt(saturate(1.0 - radius2));
    float3 normal = normalize(float3(p, max(0.08, z)));
    float3 sourceOS = TransformWorldToObject(_FRSolarPosition.xyz);
    float2 direction = sourceOS.xy - positionOS.xy;
    direction /= max(0.001, length(direction));
    float3 lightDirection = normalize(float3(direction, 0.4));
    float diffuse = saturate(dot(normal, lightDirection));
    float rim = pow(1.0 - z, 2.0) * diffuse;
    float amount = (diffuse * 0.32 + rim * 0.85) * _FRSolarSurfaceStrength;
    return baseRGB + _FRSolarColor.rgb * (baseRGB * 0.75 + 0.035) * amount;
}
#endif
