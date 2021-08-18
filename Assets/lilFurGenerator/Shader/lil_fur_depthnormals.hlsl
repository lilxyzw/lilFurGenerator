#ifndef LIL_PASS_DEPTHNORMAL_INCLUDED
#define LIL_PASS_DEPTHNORMAL_INCLUDED

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
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 positionCS   : SV_POSITION;
    float3 normalWS     : TEXCOORD0;
    float2 uv           : TEXCOORD1;
    float2 uv4          : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

//------------------------------------------------------------------------------------------------------------------------------
// Shader
v2f vert(appdata input)
{
    v2f output;
    LIL_INITIALIZE_STRUCT(v2f, output);

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = lilOptMul(LIL_MATRIX_M, input.positionOS.xyz).xyz;
    CalcFur(positionWS, input.normalOS, input.tangentOS, input.color, input.uv4);
    output.positionCS = lilOptMul(LIL_MATRIX_VP, positionWS);
    output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
    output.uv4 = input.uv4;
    output.uv4.x *= _FurDensity;
    output.normalWS = lilTransformNormalOStoWS(input.normalOS);
    output.normalWS = NormalizeNormalPerVertex(output.normalWS);

    return output;
}

float4 frag(v2f input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float alpha = LIL_SAMPLE_2D(_MainTex, sampler_MainTex, input.uv).a;
    alpha *= saturate(LIL_SAMPLE_2D(_FurMap, sampler_linear_repeatU_clampV, input.uv4).a + fwidth(input.uv4.y) * 2.0);
    clip(alpha - lerp(_Cutoff, 1.0, _Cutoff));

    return float4(PackNormalOctRectEncode(TransformWorldToViewDir(input.normalWS, true)), 0.0, 0.0);
}

#endif