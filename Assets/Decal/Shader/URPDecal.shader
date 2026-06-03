Shader "Custom/URPDecal"
{
    Properties
    {
        [HDR]_MainCol("Color",Color) = (1,1,1,1)
        _MainTex("Tex",2D)="white"{}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        
        Pass
        {
            Name "Pass"
            Tags 
            { 
                // LightMode: <None>
            }
            
            // Render State
            Blend One One
            Cull Back
            ZTest LEqual
            ZWrite On
            // ColorMask: <None>
            
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            
            // Pragmas
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma multi_compile_fog

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"

            
            CBUFFER_START(UnityPerMaterial)
            half4 _MainCol;
            CBUFFER_END
            TEXTURE2D (_MainTex);SAMPLER (samplestate_linear_clamp);
            TEXTURE2D (_CameraDepthTexture);SAMPLER(sampler_CameraDepthTexture);

            
            // Generated Type: Attributes
            
            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv0 : TEXCOORD0;
            };
            
            // Generated Type: Varyings
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float3 positionVS : TEXCOORD1;
                float fogCoord  : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                
                float3 positionWS = TransformObjectToWorld(v.positionOS);
                o.positionVS = TransformWorldToView(positionWS);
                o.positionCS = TransformWViewToHClip(o.positionVS);
                o.fogCoord = ComputeFogFactor(o.positionCS.z);

                o.uv0 = v.uv0;
                return o;
            }

            half4 frag(Varyings i) : SV_TARGET 
            {    
                

                
                float2 screenUV = i.positionCS.xy/_ScreenParams.xy;
                float var_Depth = SAMPLE_TEXTURE2D(_CameraDepthTexture,sampler_CameraDepthTexture,screenUV);
                
                float Depth = LinearEyeDepth(var_Depth,_ZBufferParams);
                
                float3 DepthXYZ = 1.0;
                DepthXYZ.z = Depth;
                DepthXYZ.xy = i.positionVS.xy * Depth / - i.positionVS.z;
                
                float3 DepthXYZ_WS = mul(unity_CameraToWorld,float4(DepthXYZ,1.0));
                float3 DepthXYZ_OS = TransformWorldToObject(DepthXYZ_WS);
                
                float2 DepthOSUV = float2(DepthXYZ_OS.x,DepthXYZ_OS.z);


                float4 var_MainTex = SAMPLE_TEXTURE2D(_MainTex,samplestate_linear_clamp,saturate(DepthOSUV + 0.5));

                float4 finalCol = var_MainTex * _MainCol;
                
                finalCol.rgb = MixFog(finalCol.rgb,i.fogCoord);
                //finalCol *= saturate(lerp(0.0,1.0,i.fogCoord));

                return finalCol;
            }

            
            ENDHLSL
        }
        
        
    }
    FallBack "Hidden/Shader Graph/FallbackError"
}
