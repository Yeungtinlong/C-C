Shader "Custom/FogOfWar_Blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags
        {
            "LightMode"="UniversalForward"
            "RenderPipeline"="UniversalRenderPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent+10"
        }
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL

        Pass
        {
            //Pass0
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
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

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_TexelSize;
            CBUFFER_END
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // TEXTURE2D(_CameraColorTexture);
            // SAMPLER(sampler_CameraColorTexture);

            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(VertexOutput input) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return col;
            }
            ENDHLSL
        }
    }
}