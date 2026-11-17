// The sky: a flat three-colour gradient (zenith, horizon, ground) with a soft-edged sun or moon disk. All colours are shader
// globals written by DayNightDriver, so the one sky material serves the whole day and is never written to at runtime.
Shader "WrongLine/SkyGradient"
{
    Properties
    {
        _HorizonBand("Horizon Band", Range(0.05, 1)) = 0.35
        _SunSize("Sun Size", Range(0.005, 0.2)) = 0.035
        _SunSoftness("Sun Softness", Range(0.001, 0.1)) = 0.012
    }

    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" "RenderPipeline" = "UniversalPipeline" "PreviewType" = "Skybox" }

        Pass
        {
            Name "Sky"
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _WL_SkyZenith;
            float4 _WL_SkyHorizon;
            float4 _WL_SkyGround;
            float4 _WL_SunDirection;
            float4 _WL_SunColor;

            // How far below the horizon (sine of the angle) the sky keeps the horizon colour, written by CurveWorldDriver: the
            // end of the bent road appears there, fogged to the horizon colour, and must not stand out against the ground blend.
            float _WL_SkyHorizonHold;

            CBUFFER_START(UnityPerMaterial)
                float _HorizonBand;
                float _SunSize;
                float _SunSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float height = direction.y;

                // Above the horizon the colour moves from the horizon colour to the zenith colour, quickly at first
                // (the band), then slowly. Below it the horizon colour holds down to the road end, then blends into the ground colour.
                float up = saturate(height / _HorizonBand);
                float3 sky = lerp(_WL_SkyHorizon.rgb, _WL_SkyZenith.rgb, smoothstep(0.0, 1.0, up));
                float down = saturate((-height - _WL_SkyHorizonHold) / _HorizonBand);
                float3 ground = lerp(_WL_SkyHorizon.rgb, _WL_SkyGround.rgb, smoothstep(0.0, 1.0, down));
                float3 color = height >= 0.0 ? sky : ground;

                // The sun, or the moon at night: a flat disk with a soft edge and a faint halo, the colour of the light.
                float angle = acos(clamp(dot(direction, normalize(_WL_SunDirection.xyz)), -1.0, 1.0));
                float disk = 1.0 - smoothstep(_SunSize, _SunSize + _SunSoftness, angle);
                float halo = (1.0 - smoothstep(_SunSize, _SunSize * 4.0, angle)) * 0.25;
                color = lerp(color, _WL_SunColor.rgb, saturate(disk + halo));

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
