Shader "Custom/OrderRingShader"
{
    Properties
    {
        [HideInInspector]_Color ("Ring Color", Color) = (1,1,1,1)
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
            float4 _Color;
            CBUFFER_END

            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float4 frag(VertexOutput input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}