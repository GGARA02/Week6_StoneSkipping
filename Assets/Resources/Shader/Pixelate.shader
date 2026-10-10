// PixelateFeature가 후처리 뒤 화면을 큰 픽셀 단위로 뭉개 그릴 때 쓰는 셰이더.
// 칸마다 가운데 한 점의 색으로 칸 전체를 채운다. _PixelSize는 한 칸의 화면 픽셀 수, _TargetSize는 화면 크기다.
Shader "Custom/Pixelate"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "Pixelate"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _PixelSize;
            float4 _TargetSize;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float cell = max(_PixelSize, 1.0);
                float2 pixel = (floor(input.texcoord * _TargetSize.xy / cell) + 0.5) * cell;
                // 화면 끝 칸이 화면 밖을 읽지 않게 막는다.
                float2 uv = min(pixel, _TargetSize.xy - 0.5) / _TargetSize.xy;
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
            }
            ENDHLSL
        }
    }
}
