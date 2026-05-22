Shader "SyntyStudios/CustomCharacter"
{
    Properties
    {
        _Color_Primary("Color_Primary", Color) = (0.2431373,0.4196079,0.6196079,0)
        _Color_Secondary("Color_Secondary", Color) = (0.8196079,0.6431373,0.2980392,0)
        _Color_Leather_Primary("Color_Leather_Primary", Color) = (0.282353,0.2078432,0.1647059,0)
        _Color_Metal_Primary("Color_Metal_Primary", Color) = (0.5960785,0.6117647,0.627451,0)
        _Color_Leather_Secondary("Color_Leather_Secondary", Color) = (0.372549,0.3294118,0.2784314,0)
        _Color_Metal_Dark("Color_Metal_Dark", Color) = (0.1764706,0.1960784,0.2156863,0)
        _Color_Metal_Secondary("Color_Metal_Secondary", Color) = (0.345098,0.3764706,0.3960785,0)
        _Color_Hair("Color_Hair", Color) = (0.2627451,0.2117647,0.1333333,0)
        _Color_Skin("Color_Skin", Color) = (1,0.8000001,0.682353,1)
        _Color_Stubble("Color_Stubble", Color) = (0.8039216,0.7019608,0.6313726,1)
        _Color_Scar("Color_Scar", Color) = (0.9294118,0.6862745,0.5921569,1)
        _Color_BodyArt("Color_BodyArt", Color) = (0.2283196,0.5822246,0.7573529,1)
        _Color_Eyes("Color_Eyes", Color) = (0.2283196,0.5822246,0.7573529,1)
        _Texture("Texture", 2D) = "white" {}
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0
        _Emission("Emission", Range(0, 1)) = 0
        _BodyArt_Amount("BodyArt_Amount", Range(0, 1)) = 0
        [HideInInspector]_Mask_02("Mask_02", 2D) = "white" {}
        [HideInInspector]_Mask_05("Mask_05", 2D) = "white" {}
        [HideInInspector]_Mask_03("Mask_03", 2D) = "white" {}
        [HideInInspector]_Mask_04("Mask_04", 2D) = "white" {}
        [HideInInspector]_Mask_01("Mask_01", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Texture); SAMPLER(sampler_Texture);
            TEXTURE2D(_Mask_01); SAMPLER(sampler_Mask_01);
            TEXTURE2D(_Mask_02); SAMPLER(sampler_Mask_02);
            TEXTURE2D(_Mask_03); SAMPLER(sampler_Mask_03);
            TEXTURE2D(_Mask_04); SAMPLER(sampler_Mask_04);
            TEXTURE2D(_Mask_05); SAMPLER(sampler_Mask_05);

            CBUFFER_START(UnityPerMaterial)
                float4 _Texture_ST;
                float4 _Mask_01_ST;
                float4 _Mask_02_ST;
                float4 _Mask_03_ST;
                float4 _Mask_04_ST;
                float4 _Mask_05_ST;
                half4 _Color_Primary;
                half4 _Color_Secondary;
                half4 _Color_Leather_Primary;
                half4 _Color_Metal_Primary;
                half4 _Color_Leather_Secondary;
                half4 _Color_Metal_Dark;
                half4 _Color_Metal_Secondary;
                half4 _Color_Hair;
                half4 _Color_Skin;
                half4 _Color_Stubble;
                half4 _Color_Scar;
                half4 _Color_BodyArt;
                half4 _Color_Eyes;
                half _Metallic;
                half _Smoothness;
                half _Emission;
                half _BodyArt_Amount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half3 ApplyCharacterMasks(float2 uv)
            {
                half4 baseTexture = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, TRANSFORM_TEX(uv, _Texture));
                half4 mask01 = SAMPLE_TEXTURE2D(_Mask_01, sampler_Mask_01, TRANSFORM_TEX(uv, _Mask_01));
                half4 mask02 = SAMPLE_TEXTURE2D(_Mask_02, sampler_Mask_02, TRANSFORM_TEX(uv, _Mask_02));
                half4 mask03 = SAMPLE_TEXTURE2D(_Mask_03, sampler_Mask_03, TRANSFORM_TEX(uv, _Mask_03));
                half4 mask04 = SAMPLE_TEXTURE2D(_Mask_04, sampler_Mask_04, TRANSFORM_TEX(uv, _Mask_04));
                half4 mask05 = SAMPLE_TEXTURE2D(_Mask_05, sampler_Mask_05, TRANSFORM_TEX(uv, _Mask_05));

                half4 color = lerp(baseTexture, _Color_Primary, step(mask01.r, 0.5h));
                color = lerp(color, _Color_Secondary, step(mask01.g, 0.5h));
                color = lerp(color, _Color_Leather_Primary, step(mask04.r, 0.5h));
                color = lerp(color, _Color_Leather_Secondary, step(mask04.g, 0.5h));
                color = lerp(color, _Color_Metal_Primary, step(mask02.r, 0.5h));
                color = lerp(color, _Color_Metal_Secondary, step(mask02.g, 0.5h));
                color = lerp(color, _Color_Metal_Dark, step(mask02.b, 0.5h));
                color = lerp(color, _Color_Hair, step(mask04.b, 0.5h));
                color = lerp(color, _Color_Skin, step(mask03.r, 0.5h));
                color = lerp(color, _Color_Stubble, step(mask03.b, 0.5h));
                color = lerp(color, _Color_Scar, step(mask03.g, 0.5h));
                color = lerp(_Color_Eyes, color, mask05.r);

                half bodyArtMask = lerp(mask01.b, 1.0h, 1.0h - _BodyArt_Amount);
                color = lerp(_Color_BodyArt, color, bodyArtMask);
                return color.rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 albedo = ApplyCharacterMasks(input.uv);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = mainLight.color * (ndotl * mainLight.shadowAttenuation) + half3(0.25h, 0.25h, 0.25h);

                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    lighting += light.color * saturate(dot(normalWS, light.direction)) * light.distanceAttenuation * light.shadowAttenuation;
                }
                #endif

                half3 color = albedo * lighting + albedo * _Emission;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
