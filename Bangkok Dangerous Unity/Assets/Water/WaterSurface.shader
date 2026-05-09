// HDRP 14+ (Unity 6 / HDRP 17 tested). Optically-inspired water surface: Schlick Fresnel,
// screen refraction with chromatic offsets, planar reflection RT, depth masking, dual normals,
// sun specular, cheap forward-scatter, depth absorption. See README_Water.md for setup.
Shader "Water/SurfaceHDRP"
{
    Properties
    {
        [Header(Surface colors)]
        _ShallowColor ("Shallow lit water", Color) = (0.05, 0.35, 0.42, 0.55)
        _DeepColor ("Deep transmitted tint", Color) = (0.01, 0.12, 0.22, 0.92)
        _AbsorptionCoeff ("Absorption coeff (Beer-Lambert)", Vector) = (0.35, 0.65, 0.9, 0)

        [Header(Normals)]
        _NormalMapA ("Normal map A", 2D) = "bump" {}
        _NormalMapB ("Normal map B", 2D) = "bump" {}
        _NormalScale ("Normal strength", Range(0, 2)) = 0.65
        _NormalScrollA ("Scroll A (XY) * time", Vector) = (0.03, 0.02, 0, 0)
        _NormalScrollB ("Scroll B (XY) * time", Vector) = (-0.02, 0.035, 0, 0)
        _NormalTilingA ("Tiling A", Float) = 2.5
        _NormalTilingB ("Tiling B", Float) = 3.2
        _NormalDistanceFadeStart ("Normal fade start (m)", Float) = 8
        _NormalDistanceFadeEnd ("Normal fade end (m)", Float) = 45

        [Header(Refraction and screen)]
        _RefractionStrength ("Refraction UV strength", Range(0, 0.08)) = 0.018
        _ChromaticAberration ("Chromatic separation", Range(0, 0.02)) = 0.0045
        _DepthMaskDistance ("Depth mask range (eye units)", Range(0.05, 30)) = 2.5
        _DepthMaskSoftness ("Depth mask softness", Range(0.01, 10)) = 0.35

        [Header(Reflection and Fresnel)]
        [NoScaleOffset] _PlanarReflectionTexture ("Planar reflection RT", 2D) = "black" {}
        _ReflectionStrength ("Reflection strength", Range(0, 2)) = 1.0
        _ReflectionMipBias ("Reflection mip bias", Range(-4, 4)) = 0
        _WaterIOR ("Water IOR (Fresnel)", Range(1.2, 1.45)) = 1.333
        _FresnelBias ("Fresnel bias (adds reflectivity)", Range(0, 0.5)) = 0.02

        [Header(Sun specular and scatter)]
        _SpecularPower ("Specular exponent", Range(16, 2048)) = 512
        _SpecularIntensity ("Specular intensity", Range(0, 8)) = 1.25
        _ScatterColor ("Forward scatter tint", Color) = (0.25, 0.55, 0.7, 1)
        _ScatterIntensity ("Scatter intensity", Range(0, 4)) = 0.85
        _ScatterWrap ("Scatter wrap", Range(0, 1)) = 0.35

        [Header(Caustics on surface)]
        _CausticsSurfaceScale ("Caustics world scale", Float) = 0.35
        _CausticsSurfaceSpeed ("Caustics scroll", Vector) = (0.04, 0.03, 0, 0)
        _CausticsSurfaceIntensity ("Caustics on water", Range(0, 2)) = 0.35

        [Header(Roughness)]
        _Roughness ("Microfacet roughness", Range(0.02, 1)) = 0.06
    }

    HLSLINCLUDE
    #pragma target 4.5
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

    struct Attributes
    {
        float3 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float4 tangentOS : TANGENT;
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float3 positionWS : TEXCOORD0;
        float3 normalWS : TEXCOORD1;
        float3 tangentWS : TEXCOORD2;
        float3 bitangentWS : TEXCOORD3;
        float2 uv : TEXCOORD4;
        float4 clipPos : TEXCOORD5;
    };

    TEXTURE2D(_NormalMapA);
    SAMPLER(sampler_NormalMapA);
    TEXTURE2D(_NormalMapB);
    SAMPLER(sampler_NormalMapB);
    TEXTURE2D(_PlanarReflectionTexture);
    SAMPLER(sampler_PlanarReflectionTexture);
    TEXTURE2D(_WaterCausticsTex);

    float4x4 _PlanarReflWorldToClip;

    CBUFFER_START(UnityPerMaterial)
        float4 _ShallowColor;
        float4 _DeepColor;
        float4 _AbsorptionCoeff;
        float _NormalScale;
        float4 _NormalScrollA;
        float4 _NormalScrollB;
        float _NormalTilingA;
        float _NormalTilingB;
        float _NormalDistanceFadeStart;
        float _NormalDistanceFadeEnd;
        float _RefractionStrength;
        float _ChromaticAberration;
        float _DepthMaskDistance;
        float _DepthMaskSoftness;
        float _ReflectionStrength;
        float _ReflectionMipBias;
        float _WaterIOR;
        float _FresnelBias;
        float _SpecularPower;
        float _SpecularIntensity;
        float4 _ScatterColor;
        float _ScatterIntensity;
        float _ScatterWrap;
        float _CausticsSurfaceScale;
        float4 _CausticsSurfaceSpeed;
        float _CausticsSurfaceIntensity;
        float _Roughness;
    CBUFFER_END

    // Globals from Water.cs (underwater + shared lighting / extinction)
    float _Water_CameraUnderwater;
    float3 _Water_MainLightDir;
    float3 _Water_MainLightColor;
    float3 _Water_Absorption;
    float3 _Water_ScatterColor;
    float _Water_ScatterIntensity;
    float4 _Water_Caustics_ST;
    float _Water_CausticsIntensity;

    float2 ScreenUVFromClip(float4 clip)
    {
        float2 ndc = clip.xy * rcp(max(abs(clip.w), 1e-6));
        float2 uv = ndc * 0.5 + 0.5;
#if defined(UNITY_UV_STARTS_AT_TOP)
        uv.y = 1.0 - uv.y;
#endif
        return saturate(uv);
    }

    float3 UnpackNormalRG(float4 t, float scale)
    {
        float3 n;
        n.xy = (t.xy * 2.0 - 1.0) * scale;
        n.z = sqrt(saturate(1.0 - dot(n.xy, n.xy)));
        return normalize(n);
    }

    // Schlick Fresnel for dielectric; R0 from IOR (air ~1, water ~1.33).
    float3 FresnelSchlick(float cosTheta, float3 F0)
    {
        float x = 1.0 - saturate(cosTheta);
        float x5 = x * x * x * x * x;
        return F0 + (1.0 - F0) * x5;
    }

    float3 SampleOpaquePyramid(float2 uv, float lodBias)
    {
        return SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, s_linear_clamp_sampler, uv, lodBias).rgb;
    }

    Varyings Vert(Attributes input)
    {
        Varyings o;
        float3 positionWS = TransformObjectToWorld(input.positionOS);
        o.positionWS = positionWS;
        float3 N = TransformObjectToWorldNormal(input.normalOS);
        float3 T = TransformObjectToWorldDir(input.tangentOS.xyz);
        float sgn = input.tangentOS.w * GetOddNegativeScale();
        float3 B = cross(N, T) * sgn;
        o.normalWS = N;
        o.tangentWS = T;
        o.bitangentWS = B;
        o.positionCS = TransformWorldToHClip(positionWS);
        o.uv = input.uv;
        o.clipPos = o.positionCS;
        return o;
    }

    float4 Frag(Varyings input) : SV_Target
    {
        float t = _Time.y;
        float distCam = distance(input.positionWS, _WorldSpaceCameraPos);

        // --- Dual scrolling normals, distance fade (reduces shimmer far away)
        float fade = saturate((_NormalDistanceFadeEnd - distCam) / max(_NormalDistanceFadeEnd - _NormalDistanceFadeStart, 0.001));
        float ns = _NormalScale * fade;

        float2 uva = input.positionWS.xz * _NormalTilingA + t * _NormalScrollA.xy;
        float2 uvb = input.positionWS.xz * _NormalTilingB + t * _NormalScrollB.xy;
        float3 tna = UnpackNormalRG(SAMPLE_TEXTURE2D(_NormalMapA, sampler_NormalMapA, uva), ns);
        float3 tnb = UnpackNormalRG(SAMPLE_TEXTURE2D(_NormalMapB, sampler_NormalMapB, uvb), ns);
        float3 tangentNormal = normalize(float3(tna.xy + tnb.xy, tna.z * tnb.z));

        float3x3 TBN = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
        float3 N = normalize(mul(tangentNormal, TBN));

        float3 V = normalize(_WorldSpaceCameraPos - input.positionWS);
        float NdotV = saturate(dot(N, V));

        // Sun / main light: direction from shaded point toward the light (Water.cs sets -light.forward).
        float3 Lsun = normalize(_Water_MainLightDir);
        float NdotL = saturate(dot(N, Lsun));
        float3 H = normalize(Lsun + V);
        float NdotH = saturate(dot(N, H));
        // Blinn-Phong lobe; roughness maps to wider highlight.
        float specPow = lerp(_SpecularPower, 32.0, saturate(_Roughness * 4.0));
        float3 spec = _Water_MainLightColor * _SpecularIntensity * pow(NdotH, specPow) * NdotL;

        // Cheap isotropic forward scatter in sun direction (not full SS).
        float wrap = saturate((dot(N, Lsun) + _ScatterWrap) / (1.0 + _ScatterWrap));
        float3 scatter = _ScatterColor.rgb * _ScatterIntensity * wrap * _Water_MainLightColor;

        // --- Fresnel (external reflection at air side of interface)
        float nAir = 1.0;
        float nW = max(_WaterIOR, 1.001);
        float R0 = pow((nAir - nW) / (nAir + nW), 2.0);
        float3 F0 = float3(R0, R0, R0);
        float3 F = saturate(FresnelSchlick(NdotV, F0) + float3(_FresnelBias, _FresnelBias, _FresnelBias));

        float2 screenUV = ScreenUVFromClip(input.clipPos);

        float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, s_linear_clamp_sampler, screenUV).r;
        float sceneEyeZ = LinearEyeDepth(rawDepth, _ZBufferParams);
        float waterEyeZ = LinearEyeDepth(input.clipPos.z * rcp(max(abs(input.clipPos.w), 1e-6)), _ZBufferParams);
        float depthBehind = max(0.0, sceneEyeZ - waterEyeZ);

        // Mask refraction when foreground is too close (reduces edge swimming).
        float depthMask = saturate((depthBehind - _DepthMaskSoftness) / max(_DepthMaskDistance, 1e-4));

        float2 refrOff = tangentNormal.xy * _RefractionStrength * depthMask;

        // Chromatic aberration: sample RGB with slightly different UV offsets along refr direction.
        float2 dir = normalize(refrOff + 1e-5);
        float ca = _ChromaticAberration;
        float3 refr;
        refr.r = SampleOpaquePyramid(screenUV + refrOff + dir * ca * 1.0, 0).r;
        refr.g = SampleOpaquePyramid(screenUV + refrOff + dir * ca * 0.15, 0).g;
        refr.b = SampleOpaquePyramid(screenUV + refrOff - dir * ca * 0.85, 0).b;

        // Beer–Lambert style tint through water column (uses per-material coeff * global).
        float3 absCoeff = max(_AbsorptionCoeff.rgb + _Water_Absorption * 0.25, 1e-4);
        float3 absorption = exp(-absCoeff * depthBehind);
        float3 volumeTint = lerp(_DeepColor.rgb, _ShallowColor.rgb, absorption);

        // Planar reflection (RT from mirrored camera; clip matrix set in C#).
        float4 reflClip = mul(_PlanarReflWorldToClip, float4(input.positionWS, 1.0));
        float2 reflUV = reflClip.xy * rcp(max(abs(reflClip.w), 1e-5)) * 0.5 + 0.5;
#if defined(UNITY_UV_STARTS_AT_TOP)
        reflUV.y = 1.0 - reflUV.y;
#endif
        reflUV = saturate(reflUV);
        float3 reflCol = SAMPLE_TEXTURE2D_LOD(_PlanarReflectionTexture, sampler_PlanarReflectionTexture, reflUV, _ReflectionMipBias).rgb;
        reflCol *= _ReflectionStrength;

        // Combine reflection / refraction by Fresnel (energy split approximation).
        float3 baseCol = F * reflCol + (1.0 - F) * (refr * volumeTint);

        // Projected caustics (world XZ) modulated by surface normal — fake light focusing.
        float2 cuv = input.positionWS.xz * _CausticsSurfaceScale + t * _CausticsSurfaceSpeed.xy;
        float2 cuvCaust = frac(cuv * _Water_Caustics_ST.xy + _Water_Caustics_ST.zw);
        float c = SAMPLE_TEXTURE2D(_WaterCausticsTex, s_linear_clamp_sampler, cuvCaust).r;
        float caust = c * _CausticsSurfaceIntensity * _Water_CausticsIntensity * NdotL;
        baseCol += caust * _Water_MainLightColor;

        baseCol += spec + scatter * (1.0 - NdotV);

        // When camera is underwater, push tint toward deep color (no post stack required).
        float under = saturate(_Water_CameraUnderwater);
        baseCol = lerp(baseCol, _DeepColor.rgb, under * 0.55);

        float alpha = saturate(_ShallowColor.a + NdotV * 0.15);
        return float4(baseCol, alpha);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }
            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
    FallBack Off
}
