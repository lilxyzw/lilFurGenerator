#ifndef LIL_PASS_META_INCLUDED
#define LIL_PASS_META_INCLUDED

//------------------------------------------------------------------------------------------------------------------------------
// Struct
struct appdata
{
    float4 positionOS   : POSITION;
    float2 uv           : TEXCOORD0;
    float2 uv1          : TEXCOORD1;
    float2 uv2          : TEXCOORD2;
};

struct v2f
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    #ifdef EDITOR_VISUALIZATION
        float2 vizUV        : TEXCOORD1;
        float4 lightCoord   : TEXCOORD2;
    #endif
};

//------------------------------------------------------------------------------------------------------------------------------
// Shader
v2f vert (appdata input)
{
    v2f output;
    LIL_INITIALIZE_STRUCT(v2f, output);

    LIL_TRANSFER_METAPASS(input,output);
    output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
    #ifdef EDITOR_VISUALIZATION
        if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
            output.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, input.uv, input.uv1, input.uv2, unity_EditorViz_Texture_ST);
        else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
        {
            output.vizUV = input.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
            output.lightCoord = mul(unity_EditorViz_WorldToLight, LIL_TRANSFORM_POS_OS_TO_WS(input.positionOS.xyz));
        }
    #endif

    return output;
}

float4 frag(v2f input) : SV_Target
{
    MetaInput metaInput;
    LIL_INITIALIZE_STRUCT(MetaInput, metaInput);

    metaInput.Albedo = LIL_SAMPLE_2D(_MainTex, sampler_MainTex, input.uv).rgb;

    #ifdef EDITOR_VISUALIZATION
        metaInput.VizUV = input.vizUV;
        metaInput.LightCoord = input.lightCoord;
    #endif

    return MetaFragment(metaInput);
}

#endif