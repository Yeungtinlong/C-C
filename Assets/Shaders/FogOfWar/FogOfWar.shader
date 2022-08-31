Shader "Custom/FogOfWar"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL

        Pass
        {
            //Pass0
            Name "Blur"
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct VertexInput
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 positionCS : SV_POSITION;
                float2 uv[9] : TEXCOORD0;
            };

            float _BlurRadius;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_TexelSize;
            CBUFFER_END

            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv[0] = input.uv + _MainTex_TexelSize.xy * float2(-1, -1) * _BlurRadius;
                output.uv[1] = input.uv + _MainTex_TexelSize.xy * float2(-1, 0) * _BlurRadius;
                output.uv[2] = input.uv + _MainTex_TexelSize.xy * float2(-1, 1) * _BlurRadius;
                output.uv[3] = input.uv + _MainTex_TexelSize.xy * float2(0, -1) * _BlurRadius;
                output.uv[4] = input.uv;
                output.uv[5] = input.uv + _MainTex_TexelSize.xy * float2(0, 1) * _BlurRadius;
                output.uv[6] = input.uv + _MainTex_TexelSize.xy * float2(1, -1) * _BlurRadius;
                output.uv[7] = input.uv + _MainTex_TexelSize.xy * float2(1, 0) * _BlurRadius;
                output.uv[8] = input.uv + _MainTex_TexelSize.xy * float2(1, 1) * _BlurRadius;

                return output;
            }

            float4 frag(VertexOutput input) : SV_Target
            {
                // const float G[9] = {
                //     1, 2, 1,
                //     2, 4, 2,
                //     1, 2, 1
                // };

                const float G[9] = {
                    0.0947416, 0.118318, 0.0947416,
                    0.118318, 0.147761, 0.118318,
                    0.0947416, 0.118318, 0.0947416
                };

                float sum = 0;
                for (int i = 0; i < 9; i++)
                {
                    float col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv[i]).r;
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
            #pragma vertex vert
            #pragma fragment frag

            struct VertexInput
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _FOWDarkness;

            TEXTURE2D(_BlurTexture);
            SAMPLER(sampler_BlurTexture);
            // TEXTURE2D(_LerpTexture);
            // SAMPLER(sampler_LerpTexture);

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
                // float LerpMask = 1 - SAMPLE_TEXTURE2D(_LerpTexture, sampler_LerpTexture, 1 - input.uv).r;
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
            #pragma vertex vert
            #pragma fragment frag

            struct VertexInput
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
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
            #pragma vertex vert
            #pragma fragment frag

            struct VertexInput
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_LastTexture);
            SAMPLER(sampler_LastTexture);

            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(VertexOutput input) : SV_Target
            {
                float curCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).r;
                float lastCol = SAMPLE_TEXTURE2D(_LastTexture, sampler_LastTexture, input.uv).r;
                return lerp(lastCol, curCol, 0.5);
            }
            ENDHLSL
        }
    }
}