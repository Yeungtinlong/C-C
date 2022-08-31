Shader "Custom/SSOutlineShader"
{
    Properties
    {
        [HideInInspector]_MainTex("Main Texture", 2D) = "white" {}
        [HideInInspector]_OcclusionTexture("OcclusionTexture Texture", 2D) = "white" {}
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL

        Pass
        {
            //Pass0
            Name "Get Mask"

            ZTest Always
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct VertexInput
            {
                float4 positionOS : POSITION;
            };

            struct VertexOutput
            {
                float4 positionCS : SV_POSITION;
            };

            VertexOutput vert(VertexInput i)
            {
                VertexOutput o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);

                return o;
            }

            float4 frag(VertexOutput i) : SV_TARGET
            {
                return 1;
            }
            ENDHLSL
        }

        Pass
        {
            // Pass1
            Name "Merge Occlusion Into Camera"

            ZTest Always
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            float _OcclusionUVScale;
            float4 _OcclusionColor;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_OcclusionTexture);
            SAMPLER(sampler_OcclusionTexture);

            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);

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

            VertexOutput vert(VertexInput i)
            {
                VertexOutput o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }

            float4 frag(VertexOutput i) : SV_TARGET
            {
                float4 originColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.uv).r;
                float occlusionMask = SAMPLE_TEXTURE2D(_OcclusionTexture, sampler_OcclusionTexture,
                                                       i.uv * _OcclusionUVScale).a;

                float4 occlusionAreaColor = mask * occlusionMask * _OcclusionColor;
                return float4(lerp(originColor.rgb, occlusionAreaColor.rgb, occlusionAreaColor.a), 1);
            }
            ENDHLSL
        }

        Pass
        {
            //Pass2
            Name "Get Edge"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct VertexInput
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float2 uv[9] : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            float2 _MainTex_TexelSize;
            CBUFFER_END

            float _SampleDistance;
            float4 _OutlineColor;
            float _HDRIntensity;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            VertexOutput vert(VertexInput i)
            {
                VertexOutput o;

                o.position = TransformObjectToHClip(i.position.xyz);

                o.uv[0] = i.uv + _MainTex_TexelSize * float2(-1, -1) * _SampleDistance;
                o.uv[1] = i.uv + _MainTex_TexelSize * float2(0, -1) * _SampleDistance;
                o.uv[2] = i.uv + _MainTex_TexelSize * float2(1, -1) * _SampleDistance;
                o.uv[3] = i.uv + _MainTex_TexelSize * float2(-1, 0) * _SampleDistance;
                o.uv[4] = i.uv + _MainTex_TexelSize * float2(0, 0) * _SampleDistance;
                o.uv[5] = i.uv + _MainTex_TexelSize * float2(1, 0) * _SampleDistance;
                o.uv[6] = i.uv + _MainTex_TexelSize * float2(-1, 1) * _SampleDistance;
                o.uv[7] = i.uv + _MainTex_TexelSize * float2(0, 1) * _SampleDistance;
                o.uv[8] = i.uv + _MainTex_TexelSize * float2(1, 1) * _SampleDistance;

                return o;
            }

            float4 frag(VertexOutput i) : SV_TARGET
            {
                const float Gx[9] = {
                    -1, 0, 1,
                    -2, 0, 2,
                    -1, 0, 1
                };
                const float Gy[9] = {
                    -1, -2, -1,
                    0, 0, 0,
                    1, 2, 1
                };

                float edgeX = 0;
                float edgeY = 0;
                for (int it = 0; it < 9; it++)
                {
                    float col = SAMPLE_DEPTH_TEXTURE(_MainTex, sampler_MainTex, i.uv[it]);
                    edgeX += col * Gx[it];
                    edgeY += col * Gy[it];
                }
                float edge = abs(edgeX) + abs(edgeY);
                float intensity = pow(2, _HDRIntensity);

                return saturate(edge) * _OutlineColor * intensity;
            }
            ENDHLSL
        }

        Pass
        {
            //Pass3
            Name "Merge Outline Into Camera"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct VertexInput
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_Edge);
            SAMPLER(sampler_Edge);

            VertexOutput vert(VertexInput i)
            {
                VertexOutput o;
                o.position = TransformObjectToHClip(i.position.xyz);
                o.uv = i.uv;
                return o;
            }

            float4 frag(VertexOutput i) : SV_TARGET
            {
                float4 cameraColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float4 outlineColor = SAMPLE_TEXTURE2D(_Edge, sampler_Edge, i.uv);
                return float4(lerp(cameraColor.rgb, outlineColor.rgb, outlineColor.a), 1);
            }
            ENDHLSL
        }
    }
}