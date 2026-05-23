Shader "Hidden/CardDungeon/RetroComposite"
{
    Properties
    {
        _VirtualWidth ("Virtual Width", Float) = 480
        _VirtualHeight ("Virtual Height", Float) = 270
        _Pixelate ("Pixelate", Range(0,1)) = 1
        _PosterizeLevels ("Posterize Levels", Range(2,16)) = 6
        _PosterizeStrength ("Posterize Strength", Range(0,1)) = 0.78
        _PaletteStrength ("Palette Strength", Range(0,1)) = 0.62
        _PaletteDarkThreshold ("Palette Dark Threshold", Range(0,1)) = 0.56
        _DitherStrength ("Dither Strength", Range(0,1)) = 0.075
        _BlackCrush ("Black Crush", Range(0,0.5)) = 0.16
        _Contrast ("Contrast", Range(0.5,2.0)) = 1.22
        _Saturation ("Saturation", Range(0,2.0)) = 0.78
        _VignetteStrength ("Vignette Strength", Range(0,1)) = 0.62
        _VignetteRadius ("Vignette Radius", Range(0,1)) = 0.60
        _ScanlineStrength ("Scanline Strength", Range(0,1)) = 0.08
        _ChromaticAberration ("Chromatic Aberration", Range(0,4)) = 0.65
        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.04
        _CrtCurvature ("CRT Curvature", Range(0,0.25)) = 0.075
        _CrtEdgeSoftness ("CRT Edge Softness", Range(0,0.25)) = 0.055
        _CrtGlowBleed ("CRT Glow Bleed", Range(0,1)) = 0.32
        _HorizontalJitter ("Horizontal Jitter", Range(0,4)) = 0.45
        _WarmTint ("Warm Tint", Color) = (1.05, 0.88, 0.62, 1)
        _ColdTint ("Cold Shadow Tint", Color) = (0.12, 0.34, 0.30, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "RetroComposite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _VirtualWidth;
            float _VirtualHeight;
            float _Pixelate;
            float _PosterizeLevels;
            float _PosterizeStrength;
            float _PaletteStrength;
            float _PaletteDarkThreshold;
            float _DitherStrength;
            float _BlackCrush;
            float _Contrast;
            float _Saturation;
            float _VignetteStrength;
            float _VignetteRadius;
            float _ScanlineStrength;
            float _ChromaticAberration;
            float _NoiseStrength;
            float _CrtCurvature;
            float _CrtEdgeSoftness;
            float _CrtGlowBleed;
            float _HorizontalJitter;
            float4 _WarmTint;
            float4 _ColdTint;

            float Luma(float3 c)
            {
                return dot(c, float3(0.2126, 0.7152, 0.0722));
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Bayer4(float2 pixel)
            {
                int x = (int)pixel.x & 3;
                int y = (int)pixel.y & 3;
                int index = x + y * 4;
                float v = 0.0;
                if (index == 0) v = 0;
                if (index == 1) v = 8;
                if (index == 2) v = 2;
                if (index == 3) v = 10;
                if (index == 4) v = 12;
                if (index == 5) v = 4;
                if (index == 6) v = 14;
                if (index == 7) v = 6;
                if (index == 8) v = 3;
                if (index == 9) v = 11;
                if (index == 10) v = 1;
                if (index == 11) v = 9;
                if (index == 12) v = 15;
                if (index == 13) v = 7;
                if (index == 14) v = 13;
                if (index == 15) v = 5;
                return (v + 0.5) / 16.0;
            }

            float3 ClosestRetroPalette(float3 c)
            {
                float3 p0 = float3(0.012, 0.010, 0.008);
                float3 p1 = float3(0.050, 0.033, 0.023);
                float3 p2 = float3(0.140, 0.070, 0.032);
                float3 p3 = float3(0.320, 0.145, 0.050);
                float3 p4 = float3(0.670, 0.390, 0.150);
                float3 p5 = float3(0.760, 0.620, 0.360);
                float3 p6 = float3(0.055, 0.180, 0.160);
                float3 p7 = float3(0.030, 0.540, 0.480);
                float3 p8 = float3(0.300, 0.025, 0.020);
                float3 p9 = float3(0.850, 0.100, 0.035);

                float bestD = 999.0;
                float3 best = c;
                float d;
                d = dot(c - p0, c - p0); if (d < bestD) { bestD = d; best = p0; }
                d = dot(c - p1, c - p1); if (d < bestD) { bestD = d; best = p1; }
                d = dot(c - p2, c - p2); if (d < bestD) { bestD = d; best = p2; }
                d = dot(c - p3, c - p3); if (d < bestD) { bestD = d; best = p3; }
                d = dot(c - p4, c - p4); if (d < bestD) { bestD = d; best = p4; }
                d = dot(c - p5, c - p5); if (d < bestD) { bestD = d; best = p5; }
                d = dot(c - p6, c - p6); if (d < bestD) { bestD = d; best = p6; }
                d = dot(c - p7, c - p7); if (d < bestD) { bestD = d; best = p7; }
                d = dot(c - p8, c - p8); if (d < bestD) { bestD = d; best = p8; }
                d = dot(c - p9, c - p9); if (d < bestD) { bestD = d; best = p9; }
                return best;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 center = uv - 0.5;
                float2 warpedCenter = center;
                float radius2 = dot(center, center);
                warpedCenter *= 1.0 + radius2 * _CrtCurvature * 2.4;
                uv = warpedCenter + 0.5;

                float edgeMask = smoothstep(0.0, _CrtEdgeSoftness, uv.x) * smoothstep(0.0, _CrtEdgeSoftness, uv.y) *
                    smoothstep(0.0, _CrtEdgeSoftness, 1.0 - uv.x) * smoothstep(0.0, _CrtEdgeSoftness, 1.0 - uv.y);
                float2 virtualRes = float2(_VirtualWidth, _VirtualHeight);
                float2 pixel = floor(uv * virtualRes);
                float jitter = (Hash21(float2(pixel.y, floor(_Time.y * 18.0))) - 0.5) * _HorizontalJitter / _VirtualWidth;
                uv.x += jitter;
                pixel = floor(uv * virtualRes);
                float2 pixelUv = (pixel + 0.5) / virtualRes;
                float2 sampleUv = lerp(uv, pixelUv, _Pixelate);

                float ca = _ChromaticAberration / max(_VirtualWidth, _VirtualHeight);
                float2 caOffset = center * ca * 4.0;

                float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, sampleUv + caOffset).r;
                float g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, sampleUv).g;
                float b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, sampleUv - caOffset).b;
                float3 col = float3(r, g, b);

                float3 bleed = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, sampleUv + float2(1.25 / _VirtualWidth, 0)).rgb;
                col += max(bleed - col, 0.0) * _CrtGlowBleed;

                float n = Hash21(pixel + _Time.y * 37.0) - 0.5;
                float lum0 = Luma(col);
                col += n * _NoiseStrength * (1.0 - lum0);
                col = saturate((col - _BlackCrush) / max(0.0001, 1.0 - _BlackCrush));
                col = saturate((col - 0.5) * _Contrast + 0.5);

                float lum = Luma(col);
                col = lerp(lum.xxx, col, _Saturation);

                float shadowMask = saturate(1.0 - lum * 2.0);
                float highlightMask = saturate((lum - 0.35) * 1.6);
                col = lerp(col, col * _ColdTint.rgb, shadowMask * 0.45);
                col = lerp(col, col * _WarmTint.rgb, highlightMask * 0.35);

                float dither = Bayer4(pixel) - 0.5;
                col += dither * _DitherStrength;

                float l = max(Luma(col), 0.0001);
                float q = round(l * _PosterizeLevels) / _PosterizeLevels;
                float3 poster = col * (q / l);
                col = lerp(col, poster, _PosterizeStrength);

                float lumAfter = Luma(col);
                float paletteMask = saturate((_PaletteDarkThreshold - lumAfter) / max(0.0001, _PaletteDarkThreshold));
                col = lerp(col, ClosestRetroPalette(col), paletteMask * _PaletteStrength);

                float scan = (fmod(pixel.y, 2.0) < 1.0) ? 1.0 : (1.0 - _ScanlineStrength);
                float fineScan = 0.90 + 0.10 * sin((uv.y * _ScreenParams.y) * 3.14159265);
                col *= scan * fineScan;

                float dist = length(center);
                float vig = smoothstep(_VignetteRadius, 0.98, dist);
                col *= lerp(1.0, 0.25, vig * _VignetteStrength);
                col *= edgeMask;

                return half4(saturate(col), 1.0);
            }
            ENDHLSL
        }
    }
}
