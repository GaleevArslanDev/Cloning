Shader "Cloning/Water Waves"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.05, 0.55, 0.75, 0.45)
        _FresnelColor("Edge Color", Color) = (0.3, 0.9, 1, 0.65)
        _WaveHeight("Wave Height", Range(0, 0.2)) = 0.025
        _WaveFrequency("Wave Frequency", Range(0.1, 8)) = 1.5
        _WaveSpeed("Wave Speed", Range(0, 5)) = 1
        _FresnelPower("Edge Power", Range(0.5, 8)) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _FresnelColor;
                float _WaveHeight;
                float _WaveFrequency;
                float _WaveSpeed;
                float _FresnelPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float time = _Time.y * _WaveSpeed;
                float firstAngle = positionWS.x * _WaveFrequency + time;
                float secondFrequency = _WaveFrequency * 1.37;
                float secondAngle = positionWS.z * secondFrequency - time * 0.8;
                float firstWave = sin(firstAngle);
                float secondWave = sin(secondAngle);

                positionWS.y += (firstWave + secondWave) * _WaveHeight;

                float xSlope = cos(firstAngle) * _WaveFrequency * _WaveHeight;
                float zSlope = cos(secondAngle) * secondFrequency * _WaveHeight;
                float3 normalWS = normalize(float3(-xSlope, 1, -zSlope));

                output.positionWS = positionWS;
                output.normalWS = normalWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirection = SafeNormalize(
                    GetCameraPositionWS() - input.positionWS
                );
                Light mainLight = GetMainLight();
                float lighting = saturate(dot(normalWS, mainLight.direction));
                float fresnel = pow(
                    1 - saturate(dot(normalWS, viewDirection)),
                    _FresnelPower
                );

                float3 color = _BaseColor.rgb *
                               (0.45 + lighting * 0.55) *
                               mainLight.color;
                color = lerp(
                    color,
                    _FresnelColor.rgb,
                    fresnel * _FresnelColor.a
                );

                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
