// Refined version of the Oil Painting effect for Shader Graph
// Matches original Unlit shader result as closely as possible

void OilPaintEffect_float(
    float2 UV,
    int Radius,
    float2 TexelSize,
    UnityTexture2D MainTex,
    UnitySamplerState MainTex_sampler,
    out float4 OutColor
)
{
    float3 mean[4] = {
        float3(0, 0, 0),
        float3(0, 0, 0),
        float3(0, 0, 0),
        float3(0, 0, 0)
    };

    float3 sigma[4] = {
        float3(0, 0, 0),
        float3(0, 0, 0),
        float3(0, 0, 0),
        float3(0, 0, 0)
    };

    float2 start[4] = {
        float2(-Radius, -Radius),
        float2(-Radius, 0),
        float2(0, -Radius),
        float2(0, 0)
    };

    float2 pos;
    float3 col;
    float radiusF = float(Radius);
    float n = (radiusF + 1.0) * (radiusF + 1.0);

    // Loop through 4 quadrants
    for (int k = 0; k < 4; k++) {
        for (int i = 0; i <= Radius; i++) {
            for (int j = 0; j <= Radius; j++) {
                pos = float2(i, j) + start[k];
                float2 offsetUV = UV + pos * TexelSize;
                col = MainTex.SampleLevel(MainTex_sampler, offsetUV, 0).rgb;
                mean[k] += col;
                sigma[k] += col * col;
            }
        }
    }

    float sigma2;
    float3 result = MainTex.SampleLevel(MainTex_sampler, UV, 0).rgb;
    float min = 1e10;

    for (int l = 0; l < 4; l++) {
        mean[l] /= n;
        sigma[l] = abs(sigma[l] / n - mean[l] * mean[l]);
        sigma2 = sigma[l].r + sigma[l].g + sigma[l].b;

        if (sigma2 < min) {
            min = sigma2;
            result = mean[l];
        }
    }

    OutColor = float4(result, 1.0);
}
