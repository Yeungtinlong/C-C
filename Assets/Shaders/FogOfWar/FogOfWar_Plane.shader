Shader "Custom/FogOfWar_Plane"
{
    Properties
    {
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
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
                "LightMode"="FogOfWarPlane"
            }
        }
    }
}