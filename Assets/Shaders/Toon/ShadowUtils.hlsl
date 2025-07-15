#ifndef CUSTOM_SHADOW_UTILS_INCLUDED
#define CUSTOM_SHADOW_UTILS_INCLUDED

#ifdef SHADERGRAPH_PREVIEW

void GetShadowFactor_float(float3 PositionWS, out float Result)
{
    Result = 1.0; // Preview에서는 항상 밝음
}

#else

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

void GetShadowFactor_float(float3 PositionWS, out float Result)
{
    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);
    Result = MainLightRealtimeShadow(shadowCoord);
}

#endif // SHADERGRAPH_PREVIEW

#endif // CUSTOM_SHADOW_UTILS_INCLUDED
