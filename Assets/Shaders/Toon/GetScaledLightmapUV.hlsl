void GetScaledLightmapUV_float(float2 UV2, out float2 Out)
{
    Out = UV2 * unity_LightmapST.xy + unity_LightmapST.zw;
}
