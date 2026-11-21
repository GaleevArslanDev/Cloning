Shader "Custom/MirrorGlassURP"
{
    Properties
    {
        [Header(Main Settings)]
        _Reflectivity ("Зеркальность (Reflectivity)", Range(0, 1)) = 0.7
        _GlassOpacity ("Стекольность / Плотность (Glass Opacity)", Range(0, 1)) = 0.15
        _GlassTint ("Оттенок стекла (Glass Tint)", Color) = (0.88, 0.97, 0.94, 1)

        [Header(Warped Mirror Distortion)]
        _Distortion ("Сила кривизны / Искажения", Range(0, 0.5)) = 0.08
        _DistortionScale ("Частота волн неровности", Range(1, 30)) = 6.0

        [Header(Multi Layer Glass)]
        _MultiGlassLayers ("Слои стекла (Двоение отражения)", Range(0, 0.15)) = 0.03
        _FresnelPower ("Эффект Френеля (Зеркало под углом)", Range(0.5, 10)) = 2.5
        _Smoothness ("Гладкость (Резкость отражения)", Range(0, 1)) = 0.98

        [Header(Fog and Dust)]
        _DirtAmount ("Запотевание / Иней", Range(0, 1)) = 0.0
        _DirtColor ("Цвет запотевания", Color) = (1, 1, 1, 1)

        [NoScaleOffset] _CustomReflectionCube ("Reflection Cubemap (Auto)", CUBE) = "" {}
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
            };

            TEXTURECUBE(_CustomReflectionCube);
            SAMPLER(sampler_CustomReflectionCube);

            CBUFFER_START(UnityPerMaterial)
                float _Reflectivity;
                float _GlassOpacity;
                float4 _GlassTint;
                float _Distortion;
                float _DistortionScale;
                float _MultiGlassLayers;
                float _FresnelPower;
                float _Smoothness;
                float _DirtAmount;
                float4 _DirtColor;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.screenPos = ComputeScreenPos(posInputs.positionCS);
                output.uv = input.uv;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 baseNormalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                float3 p = input.positionOS * _DistortionScale;
                float3 waveOffset = float3(
                    sin(p.y * 1.3 + cos(p.z * 1.1)),
                    cos(p.x * 1.2 + sin(p.z * 1.4)),
                    sin(p.x * 1.1 + cos(p.y * 1.3))
                ) * _Distortion;

                float3 normalWS = normalize(baseNormalWS + waveOffset);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 viewWarp = mul((float3x3)UNITY_MATRIX_V, waveOffset).xy * 0.5;
                float3 sceneColor = SampleSceneColor(saturate(screenUV + viewWarp)).rgb * _GlassTint.rgb;

                float3 reflectVector = reflect(-viewDirWS, normalWS);
                float mip = (1.0 - _Smoothness) * 6.0;

                float3 reflLayer1 = SAMPLE_TEXTURECUBE_LOD(_CustomReflectionCube, sampler_CustomReflectionCube, reflectVector, mip).rgb;
                float3 reflLayer2 = SAMPLE_TEXTURECUBE_LOD(_CustomReflectionCube, sampler_CustomReflectionCube, normalize(reflectVector + float3(_MultiGlassLayers, _MultiGlassLayers * 0.5, 0)), mip).rgb;
                float3 reflLayer3 = SAMPLE_TEXTURECUBE_LOD(_CustomReflectionCube, sampler_CustomReflectionCube, normalize(reflectVector + float3(_MultiGlassLayers * 2.0, _MultiGlassLayers, 0)), mip).rgb;

                float3 reflectionColor = reflLayer1;
                if (_MultiGlassLayers > 0.0001)
                {
                    reflectionColor = reflLayer1 * 0.65 + reflLayer2 * 0.25 + reflLayer3 * 0.15;
                }

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                float finalReflectivity = saturate(_Reflectivity + fresnel * (1.0 - _Reflectivity));

                float3 combinedColor = lerp(sceneColor, reflectionColor, finalReflectivity);
                combinedColor = lerp(combinedColor, _GlassTint.rgb, _GlassOpacity);

                if (_DirtAmount > 0.001)
                {
                    float n = ValueNoise(input.uv * 8.0) * 0.6 + ValueNoise(input.uv * 20.0) * 0.4;
                    float fogMask = saturate((n - (1.0 - _DirtAmount)) * 2.5);
                    combinedColor = lerp(combinedColor, _DirtColor.rgb, fogMask * _DirtAmount);
                }

                float alpha = saturate(_GlassOpacity + finalReflectivity * 0.85 + _DirtAmount * 0.5);
                return half4(combinedColor, alpha);
            }
            ENDHLSL
        }
    }
}