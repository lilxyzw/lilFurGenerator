#ifndef LIL_MAIN_INCLUDED
#define LIL_MAIN_INCLUDED

//------------------------------------------------------------------------------------------------------------------------------
// Setting
#include "lil_fur_setting.hlsl"

//------------------------------------------------------------------------------------------------------------------------------
// Struct
struct appdata
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
    float2 uv4          : TEXCOORD4;
    float4 color        : COLOR;
    LIL_VERTEX_INPUT_LIGHTMAP_UV
    LIL_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float2 uv4          : TEXCOORD1;
    float3 positionWS   : TEXCOORD2;
    #if !defined(LIL_PASS_FORWARDADD)
        float3 normalWS     : TEXCOORD3;
    #endif
    LIL_LIGHTCOLOR_COORDS(4)
    LIL_LIGHTDIRECTION_COORDS(5)
    LIL_VERTEXLIGHT_COORDS(6)
    LIL_FOG_COORDS(7)
    LIL_SHADOW_COORDS(8)
    LIL_LIGHTMAP_COORDS(9)
    LIL_VERTEX_INPUT_INSTANCE_ID
    LIL_VERTEX_OUTPUT_STEREO
};

//------------------------------------------------------------------------------------------------------------------------------
// Shader
v2f vert(appdata input)
{
    v2f output;
    LIL_INITIALIZE_STRUCT(v2f, output);

    LIL_SETUP_INSTANCE_ID(input);
    LIL_TRANSFER_INSTANCE_ID(input, output);
    LIL_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionWS = lilOptMul(LIL_MATRIX_M, input.positionOS.xyz).xyz;
    CalcFur(output.positionWS, input.normalOS, input.tangentOS, input.color, input.uv4);
    output.positionCS = lilOptMul(LIL_MATRIX_VP, output.positionWS);
    output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
    output.uv4 = input.uv4;
    output.uv4.x *= _FurDensity;

    #if !defined(LIL_PASS_FORWARDADD)
        output.normalWS = lilTransformNormalOStoWS(input.normalOS);
    #endif

    //----------------------------------------------------------------------------------------------------------------------
    // Clipping Canceller
    #if defined(LIL_FEATURE_CLIPPING_CANCELLER)
        #if defined(UNITY_REVERSED_Z)
            // DirectX
            if(_UseClippingCanceller && output.positionCS.w < _ProjectionParams.y * 1.01 && output.positionCS.w > 0) output.positionCS.z = output.positionCS.z * 0.0001 + output.positionCS.w * 0.999;
        #else
            // OpenGL
            if(_UseClippingCanceller && output.positionCS.w < _ProjectionParams.y * 1.01 && output.positionCS.w > 0) output.positionCS.z = output.positionCS.z * 0.0001 - output.positionCS.w * 0.999;
        #endif
    #endif

    LIL_CALC_MAINLIGHT(output, output);
    LIL_TRANSFER_SHADOW(output, input.uv1, output);
    LIL_TRANSFER_FOG(output, output);
    LIL_TRANSFER_LIGHTMAPUV(input.uv1, output);
    LIL_CALC_VERTEXLIGHT(output, output);
    return output;
}

float4 frag(v2f input) : SV_Target
{
    LIL_SETUP_INSTANCE_ID(input);
    LIL_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    LIL_GET_MAINLIGHT(input, lightColor, lightDirection, attenuation);
    LIL_GET_VERTEXLIGHT(input, vertexLightColor);
    LIL_GET_ADDITIONALLIGHT(input.positionWS, additionalLightColor);
    #if !defined(LIL_PASS_FORWARDADD)
        #if defined(LIL_USE_LIGHTMAP)
            lightColor = max(lightColor, _LightMinLimit);
            lightColor = lerp(lightColor, 1.0, _AsUnlit);
        #endif
        #if defined(_ADDITIONAL_LIGHTS)
            float3 addLightColor = vertexLightColor + lerp(additionalLightColor, 0.0, _AsUnlit);
        #else
            float3 addLightColor = vertexLightColor;
        #endif
    #else
        lightColor = lerp(lightColor, 0.0, _AsUnlit);
    #endif

    //-------------------------------------------------------------------------------------------------------------------------
    // Main
    float4 col = LIL_SAMPLE_2D(_MainTex, sampler_MainTex, input.uv);

    //-------------------------------------------------------------------------------------------------------------------------
    // Alpha
    #if defined(LIL_TRANSPARENT)
        col.a = input.uv4.y > -0.5 ? col.a * saturate(LIL_SAMPLE_2D(_FurMap, sampler_linear_repeatU_clampV, input.uv4).a) : col.a;
        clip(col.a - _Cutoff);
    #else
        col.a = input.uv4.y > -0.5 ? col.a * saturate(LIL_SAMPLE_2D(_FurMap, sampler_linear_repeatU_clampV, input.uv4).a) : col.a;
        col.a = saturate((col.a - _Cutoff) / max(fwidth(col.a), 0.0001) + 0.5);
        clip(col.a);
    #endif

    //-------------------------------------------------------------------------------------------------------------------------
    // Lighting
    col.rgb = lerp(col.rgb * _FurAOColor.rgb, col.rgb, saturate(input.uv4.y) * _FurAO + 1.0 - _FurAO);
    float3 albedo = col.rgb;

    #if !defined(LIL_PASS_FORWARDADD)
        #if defined(LIL_FEATURE_SHADOW)
            float3 normalDirection = normalize(input.normalWS);
            float ln = saturate(dot(lightDirection,normalDirection)*0.5+0.5);

            // Shadow
            #if (defined(LIL_USE_SHADOW) || defined(LIL_LIGHTMODE_SHADOWMASK)) && defined(LIL_FEATURE_RECEIVE_SHADOW)
                if(_ShadowReceive) ln *= saturate(attenuation + distance(lightDirection, _MainLightPosition.xyz));
            #endif

            float lnB = ln;

            // Toon
            ln = lilTooning(ln, _ShadowBorder, _ShadowBlur);
            lnB = lilTooning(lnB, _ShadowBorder, _ShadowBlur, _ShadowBorderRange);

            col.rgb = lerp(albedo * _ShadowColor.rgb, col.rgb, ln);
            col.rgb = lerp(col.rgb, albedo, lnB * _ShadowBorderColor.rgb);

            col.rgb *= lightColor;
            col.rgb += albedo * addLightColor;
            col.rgb = min(col.rgb, albedo);
        #else
            col.rgb *= saturate(lightColor + addLightColor);
        #endif
    #else
        col.rgb *= lightColor;
    #endif

    //--------------------------------------------------------------------------------------------------------------------------
    // Distance Fade
    #if defined(LIL_FEATURE_DISTANCE_FADE)
        float depth = length(LIL_GET_VIEWDIR_WS(input.positionWS.xyz));
        float distFade = saturate((depth - _DistanceFade.x) / (_DistanceFade.y - _DistanceFade.x)) * _DistanceFade.z;
        #if defined(LIL_PASS_FORWARDADD)
            col.rgb = lerp(col.rgb, 0.0, distFade);
        #else
            col.rgb = lerp(col.rgb, _DistanceFadeColor.rgb, distFade);
        #endif
    #endif

    //-------------------------------------------------------------------------------------------------------------------------
    // Fog
    LIL_APPLY_FOG(col, input.fogCoord);

    return col;
}

#endif