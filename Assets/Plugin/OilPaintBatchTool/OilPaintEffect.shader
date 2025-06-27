Shader "Hidden/OilPaintEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Radius ("Radius", Float) = 2
        _TexelSize ("TexelSize", Vector) = (0.001, 0.001, 0, 0)
        _SubSampleLevel ("SubSampleLevel", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float _Radius;
            float _SubSampleLevel;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float3 OilPaint(float2 UV)
            {
                int Radius = (int)_Radius;
                float radiusF = float(Radius);
                float n = (radiusF + 1.0) * (radiusF + 1.0);

                // 서브샘플 개수 결정
                int subSampleCount = 1;
                if (_SubSampleLevel >= 2.0) subSampleCount = 9;
                else if (_SubSampleLevel >= 1.0) subSampleCount = 4;

                float totalSamples = n * subSampleCount;

                float3 mean[4] = {float3(0,0,0), float3(0,0,0), float3(0,0,0), float3(0,0,0)};
                float3 sigma[4] = {float3(0,0,0), float3(0,0,0), float3(0,0,0), float3(0,0,0)};
                float2 start[4] = {float2(-Radius, -Radius), float2(-Radius, 0), float2(0, -Radius), float2(0, 0)};
                float2 subOffsets[9] = {
                    float2(0.5, 0.5), // 기본 중심
                    float2(0.25, 0.25), float2(0.75, 0.25), float2(0.25, 0.75), float2(0.75, 0.75),
                    float2(0.166, 0.166), float2(0.833, 0.166), float2(0.166, 0.833), float2(0.833, 0.833)
                };

                for (int k = 0; k < 4; k++)
                {
                    for (int i = 0; i <= Radius; i++)
                    {
                        for (int j = 0; j <= Radius; j++)
                        {
                            float2 pos = float2(i, j) + start[k];
                            for (int s = 0; s < subSampleCount; s++)
                            {
                                float2 sampleUV = UV + (pos + subOffsets[s]) * _MainTex_TexelSize.xy;
                                float3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV).rgb;
                                mean[k] += col;
                                sigma[k] += col * col;
                            }
                        }
                    }
                }

                float minSigma = 1e10;
                float3 result = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UV).rgb;

                for (int l = 0; l < 4; l++)
                {
                    float3 m = mean[l] / totalSamples;
                    float3 s = sigma[l] / totalSamples - m * m;
                    float var = s.r + s.g + s.b;
                    if (var < minSigma)
                    {
                        minSigma = var;
                        result = m;
                    }
                }

                return result;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 color = OilPaint(IN.uv);
                return float4(color, 1.0);
            }

            ENDHLSL
        }
    }
}
