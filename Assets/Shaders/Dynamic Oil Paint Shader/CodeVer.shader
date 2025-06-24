// URP Unlit-style Shader with custom Painterly lighting logic
// (Unity 2022+ with URP, Shader Model 4.5+)

Shader "URP/PainterlyLighting"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)

        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1

        _ShadingGradient ("Shading Gradient", 2D) = "white" {}
        _PainterlyGuide ("Painterly Guide", 2D) = "white" {}
        _PainterlySmoothness ("Painterly Smoothness", Range(0,1)) = 0.1

        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "PainterlyLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NormalMap;
            float _NormalStrength;
            sampler2D _ShadingGradient;
            sampler2D _PainterlyGuide;
            float _PainterlySmoothness;
            float4 _Color;
            float _Smoothness;
            float _Metallic;
            float4 _SpecularColor;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                float3 worldPos = mul(unity_ObjectToWorld, IN.positionOS).xyz;
                OUT.worldPos = worldPos;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.tangentWS = normalize(TransformObjectToWorldDir(IN.tangentOS.xyz));
                OUT.bitangentWS = cross(OUT.normalWS, OUT.tangentWS) * IN.tangentOS.w;
                OUT.viewDirWS = normalize(_WorldSpaceCameraPos - worldPos);
                return OUT;
            }

            float3 UnpackNormalCustom(float4 normalTex, float strength)
            {
                float3 normal = UnpackNormal(normalTex);
                normal.xy *= strength;
                return normalize(normal);
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float3 T = normalize(IN.tangentWS);
                float3 B = normalize(IN.bitangentWS);
                float3 N = normalize(IN.normalWS);
                float3x3 TBN = float3x3(T, B, N);

                float3 normalTS = UnpackNormalCustom(tex2D(_NormalMap, IN.uv), _NormalStrength);
                float3 normalWS = normalize(mul(TBN, normalTS));

                float3 albedo = tex2D(_MainTex, IN.uv).rgb * _Color.rgb;
                float guide = tex2D(_PainterlyGuide, IN.uv).r;

                // Manually retrieve main light (URP simplified path)
                float3 lightDir = normalize(_MainLightPosition.xyz);
                float3 lightColor = _MainLightColor.rgb;
                float atten = 1.0; // basic, no shadowing for now

                float nDotL = saturate(dot(normalWS, lightDir) + 0.2);
                float diff = smoothstep(guide - _PainterlySmoothness, guide + _PainterlySmoothness, nDotL);
                float3 diffColor = tex2D(_ShadingGradient, float2(diff, 0.5)).rgb;

                float3 reflectDir = reflect(-lightDir, normalWS);
                float vDotR = dot(IN.viewDirWS, reflectDir);
                float specThreshold = guide + _Smoothness;
                float specStrength = smoothstep(specThreshold - _PainterlySmoothness, specThreshold + _PainterlySmoothness, vDotR);

                float3 specular = _SpecularColor.rgb * lightColor * specStrength * _Smoothness;
                float3 color = (albedo * diffColor + specular) * atten;

                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
