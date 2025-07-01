Shader "Japanese/Aizome_URP"
{
    Properties
    {
        _Albedo("Albedo", 2D) = "white" {}
        _Normal("Normal", 2D) = "bump" {}
        _MetallicSmoothness("Metallic Smoothness", 2D) = "white" {}
        _AmbientOcclusion("Ambient Occlusion", 2D) = "white" {}
        _ColorA("Color A", Color) = (1,1,1,0)
        _ColorB("Color B", Color) = (0.05882353,0.2156863,0.427451,0)
        _ColorMask("Color Mask", 2D) = "white" {}
        [Toggle(_INVERTCOLORMASK_ON)] _InvertColorMask("Invert Color Mask", Float) = 0
        _Sign1("Sign 1", 2D) = "white" {}
        [Toggle(_INVERTSIGN1_ON)] _InvertSign1("Invert Sign 1", Float) = 0
        _Sign1size("Sign 1 size", Float) = 2.87
        _Sign1position("Sign 1 position", Vector) = (0.33,0,0,0)
        _Sing2("Sign 2", 2D) = "white" {}
        [Toggle(_INVERTSIGN2_ON)] _InvertSign2("Invert Sign 2", Float) = 0
        _Sign2size("Sign 2 size", Float) = 2.87
        _Sign2position("Sign 2 position", Vector) = (0.33,0,0,0)
        _Sing3("Sign 3", 2D) = "white" {}
        [Toggle(_INVERTSIGN3_ON)] _InvertSign3("Invert Sign 3", Float) = 0
        _Sign3size("Sign 3 size", Float) = 2.87
        _Sign3position("Sign 3 position", Vector) = (0.33,0,0,0)
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _INVERTCOLORMASK_ON
            #pragma multi_compile_fragment _ _INVERTSIGN1_ON
            #pragma multi_compile_fragment _ _INVERTSIGN2_ON
            #pragma multi_compile_fragment _ _INVERTSIGN3_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _Albedo, _Normal, _MetallicSmoothness, _AmbientOcclusion;
            sampler2D _ColorMask, _Sign1, _Sing2, _Sing3;

            float4 _Albedo_ST, _Normal_ST, _MetallicSmoothness_ST, _AmbientOcclusion_ST, _ColorMask_ST;

            float4 _ColorA, _ColorB;
            float _Sign1size, _Sign2size, _Sign3size;
            float2 _Sign1position, _Sign2position, _Sign3position;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float grayscale(float3 c) { return (c.r + c.g + c.b) / 3.0; }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                float2 uvNormal = uv * _Normal_ST.xy + _Normal_ST.zw;
                float3 normalTS = UnpackNormal(tex2D(_Normal, uvNormal));

                float2 uvAlbedo = uv * _Albedo_ST.xy + _Albedo_ST.zw;
                float4 baseColor = tex2D(_Albedo, uvAlbedo);

                float2 uvS1 = uv * _Sign1size + _Sign1position;
                float4 texS1 = tex2D(_Sign1, uvS1);
                #ifdef _INVERTSIGN1_ON
                    texS1 = texS1;
                #else
                    texS1 = 1.0 - texS1;
                #endif

                float2 uvS2 = uv * _Sign2size + _Sign2position;
                float4 texS2 = tex2D(_Sing2, uvS2);
                #ifdef _INVERTSIGN2_ON
                    texS2 = texS2;
                #else
                    texS2 = 1.0 - texS2;
                #endif

                float2 uvS3 = uv * _Sign3size + _Sign3position;
                float4 texS3 = tex2D(_Sing3, uvS3);
                #ifdef _INVERTSIGN3_ON
                    texS3 = texS3;
                #else
                    texS3 = 1.0 - texS3;
                #endif

                float grayscaleSigns = grayscale(saturate(texS1 + texS2 + texS3));

                float2 uvMask = uv * _ColorMask_ST.xy + _ColorMask_ST.zw;
                float4 maskTex = tex2D(_ColorMask, uvMask);
                #ifdef _INVERTCOLORMASK_ON
                    maskTex = maskTex;
                #else
                    maskTex = 1.0 - maskTex;
                #endif
                float grayscaleMask = grayscale(maskTex);

                float lerpValue = lerp(grayscaleSigns, grayscaleMask, maskTex.r);
                float4 resultColor = lerp(_ColorA, _ColorB, lerpValue);

                float2 uvMS = uv * _MetallicSmoothness_ST.xy + _MetallicSmoothness_ST.zw;
                float4 ms = tex2D(_MetallicSmoothness, uvMS);
                float metallic = ms.r;
                float smoothness = ms.a;

                float2 uvAO = uv * _AmbientOcclusion_ST.xy + _AmbientOcclusion_ST.zw;
                float occlusion = tex2D(_AmbientOcclusion, uvAO).r;

                float3 albedo = baseColor.rgb * resultColor.rgb;

                return float4(albedo, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}
