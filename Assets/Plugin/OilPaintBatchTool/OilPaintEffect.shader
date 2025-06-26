Shader "Hidden/OilPaintEffect"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Radius("Radius", Float) = 2
        _TexelSize("TexelSize", Vector) = (0.001, 0.001, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Radius;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 SampleOil(float2 uv)
            {
                int radius = int(_Radius);
                float n = (radius + 1) * (radius + 1);

                float3 mean[4];
                float3 sigma[4];

                for (int i = 0; i < 4; i++) {
                    mean[i] = float3(0,0,0);
                    sigma[i] = float3(0,0,0);
                }

                float2 offset[4] = {
                    float2(-radius, -radius),
                    float2(-radius, 0),
                    float2(0, -radius),
                    float2(0, 0)
                };

                for (int q = 0; q < 4; q++) {
                    for (int i = 0; i <= radius; i++) {
                        for (int j = 0; j <= radius; j++) {
                            float2 sampleUV = uv + (offset[q] + float2(i, j)) * _MainTex_TexelSize.xy;
                            float3 col = tex2D(_MainTex, sampleUV).rgb;
                            mean[q] += col;
                            sigma[q] += col * col;
                        }
                    }
                }

                float minVar = 1e9;
                float3 result = tex2D(_MainTex, uv).rgb;

                for (int k = 0; k < 4; k++) {
                    float3 m = mean[k] / n;
                    float3 s = sigma[k] / n - m * m;
                    float v = s.r + s.g + s.b;
                    if (v < minVar) {
                        minVar = v;
                        result = m;
                    }
                }

                return result;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return float4(SampleOil(i.uv), 1.0);
            }
            ENDCG
        }
    }
}
