// Simple lit surface for the road, its ground and the roadside props: a base texture lit by the main light and ambient, with fog,
// whose geometry bends over the horizon (see CurveWorld.hlsl). No normal map, metallic or specular: the art is flat
// hand-painted colour, and the look comes from the light, the sky and the bend.
Shader "WrongLine/CurvedLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        // A car's body paint: where the mask is white the base colour is multiplied by the paint colour, so one texture
        // serves every colour of car (the colour is set per car, the mask never changes).
        [Toggle(_PAINTMASK_ON)] _PaintMaskOn("Paint Mask", Float) = 0
        _PaintMask("Paint Mask Texture", 2D) = "black" {}
        _PaintColor("Paint Color", Color) = (1, 1, 1, 1)

        // Lane lines: a strip 0.15 m wide is under a pixel far down the road, and a sub-pixel strip is drawn in broken bits.
        // With the toggle on, the vertices of a line move apart sideways, away from the line's centre (a multiple of the
        // spacing), until the strip is at least the minimum number of pixels wide at its distance. The depth offset keeps a
        // line, which sits 2 cm above the asphalt, in front of it where the depth buffer cannot tell 2 cm apart.
        [Toggle(_LINEWIDEN_ON)] _LineWiden("Widen Thin Lines", Float) = 0
        _LineWidth("Line Width", Float) = 0.15
        _LineSpacing("Line Spacing", Float) = 3
        _LineMinPixels("Line Minimum Pixels", Float) = 1.5
        // The ground beside the road: the texture is mapped from above in world space (pieces that overlap on a bend draw the
        // same picture, so the overlap cannot be seen), and tinted with the biome colour, which fades across a plane at a biome
        // change. The colours, the plane and the origin's texture offset are globals set by BiomeBlendDriver.
        [Toggle(_GROUNDBLEND_ON)] _GroundBlend("Ground Biome Blend", Float) = 0

        // Night lights. Emission: the map (UV as the base map) times the colour, added after the lighting, scaled by one of the
        // global light levels (street lamps, headlights: _WL_LightLevels, set by LightsDriver) that the level mask picks, so
        // a lamp's glass and a car's lights come on and off with the day without the material ever being written to.
        // Lamp pool: the road and the ground take a fake pool of light under the nearest street lamps (_WL_LampPools).
        [Toggle(_EMISSION)] _EmissionOn("Emission", Float) = 0
        _EmissionMap("Emission Map", 2D) = "black" {}
        _EmissionColor("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionLevelMask("Emission Level Mask (x street lamps, y headlights)", Vector) = (1, 0, 0, 0)
        [Toggle(_LAMPPOOL_ON)] _LampPool("Lamp Pools Of Light", Float) = 0
        [HideInInspector] _OffsetFactor("Offset Factor", Float) = 0
        [HideInInspector] _OffsetUnits("Offset Units", Float) = 0

        // Glass: alpha blend (the base colour's alpha is the opacity). Opaque is One / Zero with depth writing on.
        [HideInInspector] _SrcBlend("Src Blend", Float) = 1
        [HideInInspector] _DstBlend("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1
        // 2 = back faces culled (default); 0 = both faces drawn (a car's body, so the inside of the cabin is seen through the glass).
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "UniversalMaterialType" = "SimpleLit" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "CurveWorld.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_PaintMask);
        SAMPLER(sampler_PaintMask);
        TEXTURE2D(_EmissionMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half4 _PaintColor;
            half4 _EmissionColor;
            half4 _EmissionLevelMask;
            float _LineWidth;
            float _LineSpacing;
            float _LineMinPixels;
        CBUFFER_END

        // Globals (BiomeBlendDriver): the two ground colours, the plane where one fades into the other (xyz a point on it,
        // w the fade length in metres), the plane's normal (along the road), and the texture offset of the scene origin.
        float4 _WL_BiomeColorA;
        float4 _WL_BiomeColorB;
        float4 _WL_BiomePlane;
        float4 _WL_BiomeDir;
        float4 _WL_GroundUV;

        // Globals (LightsDriver): how lit the street lamps (x) and the headlights (y) are, 0..1; the heads of the nearest street
        // lamps in scene space (w 1 = in use); the pool's radius, vertical reach, strength (the lamp level already in it) and
        // the number of lamps; its colour. WL_LampPool is the same formula as WrongLine.World.LampPoolFalloff: change one, change the other.
        float4 _WL_LightLevels;
        float4 _WL_LampPools[8];
        float4 _WL_LampPoolParams;
        half4 _WL_LampPoolColor;
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            Cull [_Cull]
            ZWrite [_ZWrite]
            Offset [_OffsetFactor], [_OffsetUnits]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local _LINEWIDEN_ON
            #pragma shader_feature_local _GROUNDBLEND_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _PAINTMASK_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _LAMPPOOL_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                #if defined(_GROUNDBLEND_ON)
                    half3 tint : TEXCOORD4;
                #endif
                float3 flatWS : TEXCOORD5; // Before the bend: the lights stand in the flat scene.
            };

            // A street lamp's pool of light on a surface: LampPoolFalloff.Weight, summed over the lamps in use.
            half3 WL_LampPool(float3 flatWS)
            {
                half pool = 0;
                int count = (int)_WL_LampPoolParams.w;
                [loop] for (int i = 0; i < count; i++)
                {
                    float4 head = _WL_LampPools[i];
                    float across = saturate(1.0 - length(flatWS.xz - head.xz) / _WL_LampPoolParams.x);
                    float drop = head.y - flatWS.y;
                    float window = saturate(drop) * saturate(_WL_LampPoolParams.y - drop);
                    pool += across * across * window * head.w;
                }

                return pool * _WL_LampPoolParams.z * _WL_LampPoolColor.rgb;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz;
                #if defined(_LINEWIDEN_ON)
                    // One pixel across at this distance, in metres; the strip must cover _LineMinPixels of them.
                    float cameraDistance = length(TransformObjectToWorld(positionOS) - _WorldSpaceCameraPos);
                    float pixel = cameraDistance * 2.0 / (unity_CameraProjection._m11 * _ScreenParams.y);
                    float extra = min(max(0.0, _LineMinPixels * pixel - _LineWidth) * 0.5, 1.0);
                    float lineCentre = round(positionOS.x / _LineSpacing) * _LineSpacing;
                    positionOS.x += positionOS.x >= lineCentre ? extra : -extra;
                #endif
                float3 flatWS = TransformObjectToWorld(positionOS);
                float3 positionWS = WL_BendWorldPosition(flatWS);
                output.positionWS = positionWS;
                output.flatWS = flatWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_GROUNDBLEND_ON)
                    output.uv = (flatWS.xz + _WL_GroundUV.xy) * _BaseMap_ST.xy + _BaseMap_ST.zw;
                    float along = dot(flatWS - _WL_BiomePlane.xyz, _WL_BiomeDir.xyz);
                    float fade = smoothstep(-0.5 * _WL_BiomePlane.w, 0.5 * _WL_BiomePlane.w, along);
                    // Outside a run nothing sets the globals (w stays 0): untinted, so the ground is not black in the Editor.
                    output.tint = _WL_BiomePlane.w > 0.0 ? lerp(_WL_BiomeColorA.rgb, _WL_BiomeColorB.rgb, fade) : half3(1, 1, 1);
                #else
                    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                #endif
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                #if defined(_GROUNDBLEND_ON)
                    albedo.rgb *= input.tint;
                #endif
                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif
                // Seen from behind (a double-sided surface), the normal faces the other way.
                #if !defined(_GROUNDBLEND_ON)
                    // The ground is the exception: where it folds inside a tight bend, its back faces must light as its front.
                    input.normalWS = IS_FRONT_VFACE(facing, input.normalWS, -input.normalWS);
                #endif
                #if defined(_PAINTMASK_ON)
                    half paint = SAMPLE_TEXTURE2D(_PaintMask, sampler_PaintMask, input.uv).r;
                    albedo.rgb = lerp(albedo.rgb, albedo.rgb * _PaintColor.rgb, paint);
                #endif
                half3 normalWS = normalize(input.normalWS);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 diffuse = LightingLambert(mainLight.color, mainLight.direction, normalWS)
                    * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half3 ambient = SampleSH(normalWS);

                half3 light = diffuse + ambient;

                #if defined(_ADDITIONAL_LIGHTS)
                    // The car's lights: real lights, no shadows. They sit low and shine nearly along the road, where plain Lambert
                    // would give almost nothing (cos ~ 0.07), so the cosine is wrapped (half Lambert): the beam reads on the asphalt.
                    // The cluster loop reads the screen position.
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.flatWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light extra = GetAdditionalLight(lightIndex, input.flatWS);
                        light += extra.color * (saturate(dot(normalWS, extra.direction) * 0.5 + 0.5) * extra.distanceAttenuation);
                    LIGHT_LOOP_END
                #endif

                #if defined(_LAMPPOOL_ON)
                    light += WL_LampPool(input.flatWS);
                #endif

                half3 color = albedo.rgb * light;
                #if defined(_EMISSION)
                    color += SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, input.uv).rgb * _EmissionColor.rgb * saturate(dot(_WL_LightLevels, _EmissionLevelMask));
                #endif
                color = MixFog(color, input.fogFactor);
                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = WL_BendWorldPosition(TransformObjectToWorld(input.positionOS.xyz));
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
                output.positionCS = positionCS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformWorldToHClip(WL_BendWorldPosition(TransformObjectToWorld(input.positionOS.xyz)));
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half Frag(Varyings input) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a - _Cutoff);
                #endif
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
