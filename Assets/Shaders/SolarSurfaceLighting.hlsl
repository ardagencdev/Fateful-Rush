#ifndef FATEFUL_SOLAR_SURFACE_INCLUDED
#define FATEFUL_SOLAR_SURFACE_INCLUDED
// Globals supplied by SolarAtmosphere. Zero strength leaves the original sprite unchanged.
float4 _FRSolarPosition;
half4 _FRSolarColor;
float _FRSolarSurfaceStrength;
float2 SolarSurfaceDelta(float3 positionOS)
{
    return TransformWorldToObject(_FRSolarPosition.xyz).xy - positionOS.xy;
}
half3 ApplySolarSurfaceDelta(half3 baseRGB, float2 surfaceUV, float2 direction)
{
    if (_FRSolarSurfaceStrength <= 0.0001) return baseRGB;
    float2 p = (surfaceUV - 0.5) * 2.0;
    float radius2 = dot(p, p);
    float z = sqrt(saturate(1.0 - radius2));
    float3 normal = normalize(float3(p, max(0.08, z)));
    direction /= max(0.001, length(direction));
    float3 lightDirection = normalize(float3(direction, 0.4));
    float diffuse = saturate(dot(normal, lightDirection));
    float rimBase = 1.0 - z;
    float rim = rimBase * rimBase * diffuse;
    float amount = (diffuse * 0.32 + rim * 0.85) * _FRSolarSurfaceStrength;
    return baseRGB + _FRSolarColor.rgb * (baseRGB * 0.75 + 0.035) * amount;
}
// Compatibility for any other existing shaders using the original signature.
half3 ApplySolarSurface(half3 baseRGB, float2 surfaceUV, float3 positionOS)
{
    return ApplySolarSurfaceDelta(baseRGB, surfaceUV, SolarSurfaceDelta(positionOS));
}
#endif
