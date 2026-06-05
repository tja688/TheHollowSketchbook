Shader "CardDungeon/RetroFakeLit"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _PrintMap ("Print Map", 2D) = "white" {}
        [NoScaleOffset] _PrintMask ("Print Mask", 2D) = "white" {}
        _PrintColor ("Print Color", Color) = (1,1,1,1)
        _PrintStrength ("Print Strength", Range(0,1)) = 0
        _PrintRotation90 ("Print Rotate 90", Float) = 0
        _LightWrap ("Light Wrap", Range(0,1)) = 0
        _ShadowColor ("Shadow Color", Color) = (0.08,0.065,0.05,1)
        _AmbientStrength ("Ambient Strength", Range(0,1)) = 0.08
        _SpecColor ("Spec Color", Color) = (0.28,0.22,0.16,1)
        _SpecStrength ("Spec Strength", Range(0,1)) = 0.06
        _SpecPower ("Spec Power", Range(4,96)) = 20
        _RampSteps ("Ramp Steps", Range(0,8)) = 4
        _RampStrength ("Ramp Strength", Range(0,1)) = 0.35
        _FogColor ("Fog Color", Color) = (0.01,0.008,0.006,1)
        _FogStart ("Fog Start", Float) = 2.0
        _FogEnd ("Fog End", Float) = 6.0
        _EmissionMap ("Emission Map", 2D) = "black" {}
        _EmissionColor ("Emission Color", Color) = (1,0.65,0.35,1)
        _EmissionStrength ("Emission Strength", Range(0,8)) = 0
        _UseRoundedClip ("Use Rounded Clip", Float) = 0
        _CardAspect ("Card Aspect", Float) = 0.714
        _CornerRadius ("Corner Radius", Range(0,0.25)) = 0.06
        _EdgeSoftness ("Edge Softness", Range(0.0005,0.02)) = 0.002
        _EdgeDarkenWidth ("Edge Darken Width", Range(0.001,0.12)) = 0.02
        _EdgeDarkenStrength ("Edge Darken Strength", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PrintMap); SAMPLER(sampler_PrintMap);
            TEXTURE2D(_PrintMask); SAMPLER(sampler_PrintMask);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _PrintMap_ST;
                float4 _EmissionMap_ST;
                half4 _BaseColor;
                half4 _PrintColor;
                half _PrintStrength;
                half _PrintRotation90;
                half _LightWrap;
                half4 _ShadowColor;
                half _AmbientStrength;
                half4 _SpecColor;
                half _SpecStrength;
                half _SpecPower;
                half _RampSteps;
                half _RampStrength;
                half4 _FogColor;
                float _FogStart;
                float _FogEnd;
                half4 _EmissionColor;
                half _EmissionStrength;
                half _UseRoundedClip;
                float _CardAspect;
                float _CornerRadius;
                float _EdgeSoftness;
                float _EdgeDarkenWidth;
                half _EdgeDarkenStrength;
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
                float2 maskUV : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.maskUV = input.uv;
                return output;
            }

            half QuantizeLight(half value)
            {
                half steps = max(_RampSteps, 1.0h);
                half ramped = floor(value * steps) / max(steps - 1.0h, 1.0h);
                return lerp(value, saturate(ramped), _RampStrength * step(1.5h, _RampSteps));
            }

            half3 AccumulateLight(half3 normalWS, float3 positionWS, half3 viewDirWS)
            {
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half wrapped = saturate(lerp(ndotl, ndotl * 0.5h + 0.5h, _LightWrap));
                half lit = QuantizeLight(wrapped) * mainLight.shadowAttenuation;
                half3 color = lerp(_ShadowColor.rgb, mainLight.color, lit);

                half3 halfDir = normalize(mainLight.direction + viewDirWS);
                half spec = pow(saturate(dot(normalWS, halfDir)), _SpecPower) * _SpecStrength * lit;
                color += _SpecColor.rgb * spec;

                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, positionWS);
                    half addNdotL = saturate(dot(normalWS, light.direction));
                    half addWrapped = saturate(lerp(addNdotL, addNdotL * 0.5h + 0.5h, _LightWrap));
                    half addLit = QuantizeLight(addWrapped) * light.distanceAttenuation * light.shadowAttenuation;
                    color += light.color * addLit;
                }
                #endif

                return color + _ShadowColor.rgb * _AmbientStrength;
            }

            float RoundedRectSDF(float2 uv, float aspect, float radius)
            {
                float safeAspect = max(aspect, 0.001);
                float2 halfSize = float2(safeAspect, 1.0) * 0.5;
                float safeRadius = max(0.0, min(radius, min(halfSize.x, halfSize.y) - 0.0001));
                float2 p = (uv - 0.5) * float2(safeAspect, 1.0);
                float2 q = abs(p) - (halfSize - safeRadius);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - safeRadius;
            }

            float2 RotatePrintUv90(float2 uv)
            {
                return float2(1.0 - uv.y, uv.x);
            }

            half GetPrintMask(float2 uv)
            {
                half4 maskSample = SAMPLE_TEXTURE2D(_PrintMask, sampler_PrintMask, uv);
                return saturate(max(max(maskSample.r, maskSample.g), maskSample.b) * maskSample.a);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float edgeMask = 0.0;
                if (_UseRoundedClip > 0.5h)
                {
                    float roundedDistance = RoundedRectSDF(input.maskUV, _CardAspect, _CornerRadius);
                    float clipSoftness = max(_EdgeSoftness, 0.0001);
                    float roundedMask = 1.0 - smoothstep(0.0, clipSoftness, roundedDistance);
                    clip(roundedMask - 0.001);
                    edgeMask = 1.0 - saturate(abs(roundedDistance) / max(_EdgeDarkenWidth, 0.0001));
                }

                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;

                float2 printSourceUV = _PrintRotation90 > 0.5h ? RotatePrintUv90(input.maskUV) : input.maskUV;
                float2 printUV = TRANSFORM_TEX(printSourceUV, _PrintMap);
                half4 printSample = SAMPLE_TEXTURE2D(_PrintMap, sampler_PrintMap, printUV);
                half printAlpha = saturate(printSample.a * GetPrintMask(printUV) * _PrintColor.a * _PrintStrength);
                albedo = lerp(albedo, printSample.rgb * _PrintColor.rgb, printAlpha);

                half3 lighting = AccumulateLight(normalWS, input.positionWS, viewDirWS);
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, TRANSFORM_TEX(input.uv, _EmissionMap)).rgb * _EmissionColor.rgb * _EmissionStrength;
                half3 color = albedo * lighting + emission;
                color *= 1.0h - edgeMask * _EdgeDarkenStrength;

                float distanceToCamera = distance(GetCameraPositionWS(), input.positionWS);
                float fog = saturate((distanceToCamera - _FogStart) / max(0.001, _FogEnd - _FogStart));
                color = lerp(color, _FogColor.rgb, fog);
                return half4(saturate(color), 1.0h);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 GetShadowPositionHClip(ShadowAttributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            ShadowVaryings ShadowPassVertex(ShadowAttributes input)
            {
                ShadowVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(ShadowVaryings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
