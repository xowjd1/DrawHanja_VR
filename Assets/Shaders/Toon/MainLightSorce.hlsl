void CustomLight_File_float(out float3 Direction, out float3 Color)
{
    #ifdef SHADERGRAPH_PREVIEW
    Direction = float3(1, 1, 1);
    Color = float3(1, 1, 1);
    #else
    Light light = GetMainLight();
    Direction = light.direction;
    Color = light.color;
    #endif
}




void CustomLight_Shadow_float(float3 worldPos, out float ShadowAtten)
{

    #ifdef SHADERGRAPH_PREVIEW
    ShadowAtten = 1.0f;
    #else
    
    //Create shadow coordinate
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)

    half4 clipPos = TransformWorldToHClip(worldPos);

    half4 shadowCoord = ComputeScreenPos(clipPos);

    #else

    half4 shadowCoord = TransformWorldToShadowCoord(worldPos);

    #endif

    Light light = GetMainLight();

    //If there is no main light or the receiving shadow is off, the shape is removed.

    #if !defined(_MAIN_LIGHT_SHADOWS) || defined(_RECEIVE_SHADOWS_OFF)

    ShadowAtten = 1.0f;

    #else

    //Get ShadowAtten and create it

    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)

    ShadowAtten = SampleScreenSpaceShadowmap(shadowCoord);

    #else

    ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();

    half shadowStrength = GetMainLightShadowStrength();

    ShadowAtten = SampleShadowmap(
        shadowCoord,
        TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture),
        shadowSamplingData, shadowStrength, false);

    #endif

    #endif

    #endif

}

