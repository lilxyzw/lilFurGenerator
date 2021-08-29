#ifndef LIL_INPUT_INCLUDED
#define LIL_INPUT_INCLUDED

TEXTURE2D(_MainTex);
TEXTURE2D(_FurMap);
SAMPLER(sampler_MainTex);
SAMPLER(sampler_linear_repeatU_clampV);

CBUFFER_START(UnityPerMaterial)
    float4  _MainTex_ST;
    float4  _ShadowColor;
    float4  _ShadowBorderColor;
    float4  _DistanceFade;
    float4  _DistanceFadeColor;
    float4  _FurAOColor;
    #if defined(LIL_FUR_HQ)
        float4  _FurWindFreq1;
        float4  _FurWindMove1;
        float4  _FurWindFreq2;
        float4  _FurWindMove2;
    #endif
    float   _AsUnlit;
    float   _Cutoff;
    float   _VertexLightStrength;
    float   _LightMinLimit;
    float   _ShadowBorder;
    float   _ShadowBlur;
    float   _ShadowBorderRange;
    float   _FurLength;
    float   _FurGravity;
    float   _FurAO;
    float   _FurDensity;
    float   _FurSoftness;
    #if defined(LIL_FUR_HQ)
        float   _FurTouchStrength;
    #endif
    uint    _UseClippingCanceller;
    uint    _ShadowReceive;
CBUFFER_END

#endif