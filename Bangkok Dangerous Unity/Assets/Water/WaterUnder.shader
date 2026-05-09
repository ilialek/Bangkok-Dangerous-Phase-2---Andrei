// HDRP forward unlit for submerged / seafloor geometry. Uses globals from Water.cs for
// extinction (Beer–Lambert style), sun-direction scattering, and projected caustics.
Shader "Water/UnderHDRP"
{
    Properties
    {
        _BaseColor ("Base albedo", Color) = (0.35, 0.32, 0.28, 1)
        [NoScaleOffset] _BaseMap ("Base map (optional)", 2D) = "white" {}
        _AbsorptionScale ("Extinction scale", Range(0.1, 4)) = 1
        _CausticsLocalScale ("Caustics UV scale on this object", Float) = 1
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
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float3 positionWS : TEXCOORD0;
        float3 normalWS : TEXCOORD1;
        float2 uv : TEXCOORD2;
    };

    TEXTURE2D(_BaseMap);
    SAMPLER(sampler_BaseMap);
    TEXTURE2D(_WaterCausticsTex);

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float _AbsorptionScale;
        float _CausticsLocalScale;
    CBUFFER_END

    float _Water_CameraUnderwater;
    float3 _Water_MainLightDir;
    float3 _Water_MainLightColor;
    float3 _Water_Absorption;
    float3 _Water_ScatterColor;
    float _Water_ScatterIntensity;
    float4 _Water_Caustics_ST;
    float _Water_CausticsIntensity;

    Varyings Vert(Attributes input)
    {
        Varyings o;
        o.positionWS = TransformObjectToWorld(input.positionOS);
        o.normalWS = TransformObjectToWorldNormal(input.normalOS);
        o.positionCS = TransformWorldToHClip(o.positionWS);
        o.uv = input.uv;
        return o;
    }

    float4 Frag(Varyings input) : SV_Target
    {
        float3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;

        // View path length through water (camera-relative): approximate as camera-to-surface
        // distance when underwater; above water this pass is usually swapped off, but we fade out.
        float camUnder = saturate(_Water_CameraUnderwater);
        float dist = distance(input.positionWS, _WorldSpaceCameraPos);

        // Beer–Lambert extinction: transmittance T = exp(-sigma * distance).
        float3 sigma = max(_Water_Absorption * _AbsorptionScale, 1e-4);
        float3 transmittance = exp(-sigma * dist * camUnder);

        // Forward scattering toward sun (isotropic phase approx).
        float3 L = normalize(_Water_MainLightDir);
        float3 N = normalize(input.normalWS);
        float wrap = saturate((dot(N, L) + 0.25) / 1.25);
        float3 inscatter = _Water_ScatterColor * _Water_ScatterIntensity * wrap * _Water_MainLightColor * (1.0 - transmittance) * camUnder;

        // Caustics: planar projection using world XZ + animated UV (matches surface driver).
        float t = _Time.y;
        float2 cuv = input.positionWS.xz * _CausticsLocalScale * _Water_Caustics_ST.xy + t * _Water_Caustics_ST.zw;
        float2 cuvF = frac(cuv);
        float c = SAMPLE_TEXTURE2D(_WaterCausticsTex, s_linear_clamp_sampler, cuvF).r;
        float caust = c * _Water_CausticsIntensity * saturate(dot(float3(0, 1, 0), L)) * camUnder;

        float3 lit = albedo * transmittance + inscatter + albedo * caust * 0.85;
        return float4(lit, _BaseColor.a);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
    FallBack Off
}
