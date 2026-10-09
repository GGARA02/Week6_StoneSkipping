// 파낸 물 구멍의 윗면에 쓰는 마스크 셰이더. 색과 깊이는 쓰지 않고 스텐실 비트만 표시한다.
// 수면 셰이더(WaterV2, WaterV3)는 이 비트가 표시된 곳을 그리지 않아, 수면 아래에 둔 구멍 안쪽이 보이게 된다.
// 수면보다 먼저 그려야 하므로 불투명 큐 앞쪽에 두며, 깊이 프리패스에서도 같은 표시를 남긴다.
Shader "Custom/WaterHoleMask"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry-10" "RenderPipeline" = "UniversalPipeline" }
        ColorMask 0
        ZWrite Off

        Stencil
        {
            Ref 128
            WriteMask 128
            Comp Always
            Pass Replace
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        float4 MaskVert(float4 positionOS : POSITION) : SV_POSITION
        {
            return TransformObjectToHClip(positionOS.xyz);
        }

        half4 MaskFrag() : SV_Target
        {
            return 0;
        }
        ENDHLSL

        Pass
        {
            Name "Mask"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex MaskVert
            #pragma fragment MaskFrag
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            HLSLPROGRAM
            #pragma vertex MaskVert
            #pragma fragment MaskFrag
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            HLSLPROGRAM
            #pragma vertex MaskVert
            #pragma fragment MaskFrag
            ENDHLSL
        }
    }
}
