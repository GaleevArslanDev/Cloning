
Shader "Custom/ReactiveGlass/LensCube"
{
    Properties
    {
        _GlassTint("Glass Tint", Color) = (0.65, 0.85, 1, 1)
        _RimColor("Edge Color", Color) = (0.8, 0.95, 1, 1)
        [HDR] _LaserColor("Laser Color HDR", Color) = (0.2, 0.6, 1, 1)

        _GlassDensity("Glass Density", Range(0, 2)) = 0.45
        _FresnelPower("Edge Sharpness", Range(1, 8)) = 3
        _GlowRadius("Light Spread", Range(0.1, 1.5)) = 0.7
        _GlowIntensity("Glow Intensity", Range(0, 10)) = 4
        _LaserAmount("Laser Amount", Range(0, 1)) = 0

        _HitPointOS("Hit Point Local", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GlassTint;
                float4 _RimColor;
                float4 _LaserColor;
                float4 _HitPointOS;

                float _GlassDensity;
                float _FresnelPower;
                float _GlowRadius;
                float _GlowIntensity;
                float _LaserAmount;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionOS = v.positionOS.xyz;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 cameraOS = TransformWorldToObject(_WorldSpaceCameraPos);
                float3 ray = normalize(i.positionOS - cameraOS);

                float3 raySign = float3(
                    ray.x < 0 ? -1 : 1,
                    ray.y < 0 ? -1 : 1,
                    ray.z < 0 ? -1 : 1
                );

                float3 safeRay = raySign * max(abs(ray), 0.00001);

                float3 t0 = (-0.5 - i.positionOS) / safeRay;
                float3 t1 = (0.5 - i.positionOS) / safeRay;
                float3 farT = max(t0, t1);

                float thickness = max(
                    0, min(farT.x, min(farT.y, farT.z))
                );

                thickness = min(thickness, 2.0);

                float volume = 0;

                [unroll]
                for (int s = 0; s < 8; s++)
                {
                    float t = (s + 0.5) / 8.0;
                    float3 p = i.positionOS + ray * thickness * t;
                    float3 d = p - _HitPointOS.xyz;

                    float radius2 = max(
                        _GlowRadius * _GlowRadius, 0.0001
                    );

                    volume += exp(-dot(d, d) / radius2);
                }

                volume = volume / 8.0;
                volume *= saturate(thickness * 2.0);

                float3 N = normalize(i.normalWS);
                float3 V = normalize(_WorldSpaceCameraPos - i.positionWS);

                float fresnel = pow(
                    1.0 - saturate(dot(N, V)),
                    _FresnelPower
                );

                float absorption =
                    1.0 - exp(-thickness * _GlassDensity);

                float glow = volume * _LaserAmount;

                float alpha = saturate(
                    0.04 +
                    absorption * 0.28 +
                    fresnel * 0.32 +
                    glow * 0.24
                );

                float3 glassColor =
                    _GlassTint.rgb * (0.65 + absorption * 0.25) +
                    _RimColor.rgb * fresnel * 0.55;

                float3 emission =
                    _LaserColor.rgb * glow * _GlowIntensity;

                return half4(glassColor * alpha + emission, alpha);
            }
            ENDHLSL
        }
    }
}
