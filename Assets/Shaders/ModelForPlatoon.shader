Shader "Custom/ModelForPlatoon"
{
    Properties
    {
        _ColorA ("Color", Color) = (1,1,1,1)
        _ColorB ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL

        Pass
        {
            Tags
            {
                "LightMode"="UniversalForward"
                "RenderPipeline"="UniversalRenderPipeline"
                "RenderType"="Transparent"
                "Queue"="Transparent+10"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct VertexInput
            {
                float4 positionOS : POSITION;
            };

            struct VertexOutput
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _ColorA;
            float4 _ColorB;
            CBUFFER_END

            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float4 frag(VertexOutput input) : SV_Target
            {
                return lerp(_ColorA, _ColorB, sin(_Time.y * 5) * 0.5 + 0.5);
            }
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode"="ShadowCaster"
            }
        }
    }
}