Shader "Custom/FogOfWar"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            //Pass0
            Name "Blur"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            // #pragma vertex vert
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#pragma vertex Vert
#pragma fragment frag

            float _BlurRadius;
            TEXTURE2D(_SrcTex);
            SAMPLER(sampler_SrcTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _SrcTex_TexelSize;
            CBUFFER_END

            float4 frag(Varyings input) : SV_Target
            {
                float2 uvs[9];
                uvs[0] = input.texcoord + _SrcTex_TexelSize.xy * float2(-1, -1) * _BlurRadius;
                uvs[1] = input.texcoord + _SrcTex_TexelSize.xy * float2(-1, 0) * _BlurRadius;
                uvs[2] = input.texcoord + _SrcTex_TexelSize.xy * float2(-1, 1) * _BlurRadius;
                uvs[3] = input.texcoord + _SrcTex_TexelSize.xy * float2(0, -1) * _BlurRadius;
                uvs[4] = input.texcoord;
                uvs[5] = input.texcoord + _SrcTex_TexelSize.xy * float2(0, 1) * _BlurRadius;
                uvs[6] = input.texcoord + _SrcTex_TexelSize.xy * float2(1, -1) * _BlurRadius;
                uvs[7] = input.texcoord + _SrcTex_TexelSize.xy * float2(1, 0) * _BlurRadius;
                uvs[8] = input.texcoord + _SrcTex_TexelSize.xy * float2(1, 1) * _BlurRadius;

                const float G[9] = {
                    0.0947416, 0.118318, 0.0947416,
                    0.118318, 0.147761, 0.118318,
                    0.0947416, 0.118318, 0.0947416
                };

                float sum = 0;
                for (int i = 0; i < 9; i++) {
                    // float col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv[i]).r;
                    float col = SAMPLE_TEXTURE2D(_SrcTex, sampler_SrcTex, uvs[i]).r;
                    sum += col * G[i];
                }
                // float FogMask = sum / 16;
                return float4(sum, 0, 0, 0);
            }
            ENDHLSL
        }

        Pass
        {
            //Pass1
            Name "Fog Of War"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex vert
            #pragma fragment frag

            struct VertexInput {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _FOWDarkness;

            TEXTURE2D(_BlurTexture);
            SAMPLER(sampler_BlurTexture);
            
            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(VertexOutput input) : SV_Target
            {
                float FogMask = 1 - SAMPLE_TEXTURE2D(_BlurTexture, sampler_BlurTexture, 1 - input.uv).r;
                return float4(0, 0, 0, _FOWDarkness) * FogMask;
            }
            ENDHLSL
        }

        Pass
        {
            //Pass2
            Name "Blit R Channel"

            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#pragma vertex vert
#pragma fragment frag

            struct VertexInput {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(VertexOutput input) : SV_Target
            {
                // return 1;
                float col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).r;
                return float4(col, 0, 0, 0);
            }
            ENDHLSL
        }

        Pass
        {
            //Pass3
            Name "Lerp Last and Current"

            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            // #pragma vertex vert
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#pragma vertex Vert
#pragma fragment frag
            
            TEXTURE2D(_SrcTex);
            SAMPLER(sampler_SrcTex);
            TEXTURE2D(_LastTexture);
            SAMPLER(sampler_LastTexture);

            float4 frag(Varyings input) : SV_Target
            {
                float curCol = SAMPLE_TEXTURE2D(_SrcTex, sampler_SrcTex, input.texcoord).r;
                float lastCol = SAMPLE_TEXTURE2D(_LastTexture, sampler_LastTexture, input.texcoord).r;
                return lerp(lastCol, curCol, 0.5);
            }
            ENDHLSL
        }
    }
}