Shader "Hidden/CardDungeon/RetroPosterizeThreshold"
{
    Properties
    {
        _UserLut ("User LUT", 2D) = "white" {}
        _Contribution ("Contribution", Range(0,1)) = 0.85
        _Threshold ("Threshold", Range(0,1)) = 0.50
        _ThresholdSharpness ("Threshold Sharpness", Range(1,24)) = 12
        _LutStrength ("LUT Strength", Range(0,1)) = 1
        _CompareDebug ("Compare Debug", Range(0,1)) = 0
        _DebugMask ("Debug Mask", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "RetroPosterizeThreshold"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_UserLut);
            SAMPLER(sampler_UserLut);

            float _Contribution;
            float _Threshold;
            float _ThresholdSharpness;
            float _LutStrength;
            float _CompareDebug;
            float _DebugMask;

            float Luma(float3 c)
            {
                return dot(c, float3(0.299, 0.587, 0.114));
            }

            float3 SampleUserLut(float3 color, float luma)
            {
                return SAMPLE_TEXTURE2D(_UserLut, sampler_UserLut, float2(saturate(luma), 0.5)).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float3 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;

                float luma = Luma(source);
                float mask = saturate((_Threshold - luma) * _ThresholdSharpness) * _Contribution;
                float3 lutColor = SampleUserLut(source, luma);
                float3 result = lerp(source, lutColor, mask * _LutStrength);

                if (_DebugMask > 0.5)
                    return half4(mask.xxx, 1.0);

                if (_CompareDebug > 0.5)
                {
                    float divider = 1.0 - step(0.003, abs(uv.x - 0.5));
                    result = uv.x < 0.5 ? source : result;
                    result = lerp(result, float3(1.0, 0.82, 0.25), divider);
                }

                return half4(saturate(result), 1.0);
            }
            ENDHLSL
        }
    }
}
