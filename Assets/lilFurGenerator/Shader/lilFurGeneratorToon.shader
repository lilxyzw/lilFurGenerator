Shader "_lil/FurGeneratorToon"
{
    Properties
    {
        //----------------------------------------------------------------------------------------------------------------------
        // Base
                        _Cutoff                     ("Alpha Cutoff", Range(0,1)) = 0.5
                        _AsUnlit                    ("As Unlit", Range(0, 1)) = 0
                        _VertexLightStrength        ("Vertex Light Strength", Range(0,1)) = 1
                        _LightMinLimit              ("Light Min Limit", Range(0,1)) = 0
        [lilToggle]     _UseClippingCanceller       ("Clipping Canceller", Int) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // Main
                        _MainTex                    ("Texture", 2D) = "white" {}

        //----------------------------------------------------------------------------------------------------------------------
        // Shadow
                        _ShadowColor                ("Shadow Color", Color) = (0.7,0.75,0.85,1.0)
                        _ShadowBorder               ("Border", Range(0, 1)) = 0.5
                        _ShadowBlur                 ("Blur", Range(0, 1)) = 0.1
                        _ShadowBorderColor          ("Border Color", Color) = (1,0,0,1)
                        _ShadowBorderRange          ("Border Range", Range(0, 1)) = 0
        [lilToggle]     _ShadowReceive              ("Receive Shadow", Int) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // Distance Fade
        [lilHDR]        _DistanceFadeColor          ("Color", Color) = (0,0,0,1)
        [lil3Param]     _DistanceFade               ("Start|End|Strength", Vector) = (0.1,0.01,0,0)

        //----------------------------------------------------------------------------------------------------------------------
        // Fur
        [NoScaleOffset] _FurMap                     ("Fur Map", 2D) = "white" {}
                        _FurDensity                 ("Fur Density", Float) = 1.0
                        _FurLength                  ("Fur Length", Float) = 0.2
                        _FurGravity                 ("Fur Gravity", Range(0,1)) = 0.25
                        _FurSoftness                ("Fur Softness", Range(0.001,1)) = 1.0
                        _FurAO                      ("Fur AO", Range(0,1)) = 0
                        _FurAOColor                 ("Fur AO Color", Color) = (0,0,0,1)

        //----------------------------------------------------------------------------------------------------------------------
        // Advanced
        [lilEnum]                                       _Cull               ("Cull Mode|Off|Front|Back", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]         _SrcBlend           ("SrcBlend", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]         _DstBlend           ("DstBlend", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]         _SrcBlendAlpha      ("SrcBlendAlpha", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]         _DstBlendAlpha      ("DstBlendAlpha", Int) = 10
        [Enum(UnityEngine.Rendering.BlendOp)]           _BlendOp            ("BlendOp", Int) = 0
        [Enum(UnityEngine.Rendering.BlendOp)]           _BlendOpAlpha       ("BlendOpAlpha", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]         _SrcBlendFA         ("ForwardAdd SrcBlend", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]         _DstBlendFA         ("ForwardAdd DstBlend", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]         _SrcBlendAlphaFA    ("ForwardAdd SrcBlendAlpha", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]         _DstBlendAlphaFA    ("ForwardAdd DstBlendAlpha", Int) = 1
        [Enum(UnityEngine.Rendering.BlendOp)]           _BlendOpFA          ("ForwardAdd BlendOp", Int) = 4
        [Enum(UnityEngine.Rendering.BlendOp)]           _BlendOpAlphaFA     ("ForwardAdd BlendOpAlpha", Int) = 4
        [lilToggle]                                     _ZWrite             ("ZWrite", Int) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)]   _ZTest              ("ZTest", Int) = 4
        [IntRange]                                      _StencilRef         ("Stencil Reference Value", Range(0, 255)) = 0
        [IntRange]                                      _StencilReadMask    ("Stencil ReadMask Value", Range(0, 255)) = 255
        [IntRange]                                      _StencilWriteMask   ("Stencil WriteMask Value", Range(0, 255)) = 255
        [Enum(UnityEngine.Rendering.CompareFunction)]   _StencilComp        ("Stencil Compare Function", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)]         _StencilPass        ("Stencil Pass", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)]         _StencilFail        ("Stencil Fail", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)]         _StencilZFail       ("Stencil ZFail", Float) = 0
                                                        _OffsetFactor       ("Offset Factor", Float) = 0
                                                        _OffsetUnits        ("Offset Units", Float) = 0
        [lilColorMask]                                  _ColorMask          ("Color Mask", Int) = 15
    }

//----------------------------------------------------------------------------------------------------------------------
// BRP Start
//
    HLSLINCLUDE
        #define LIL_BRP
        #include "UnityCG.cginc"
        #include "AutoLight.cginc"
        #include "Lighting.cginc"
    ENDHLSL
    SubShader
    {
        Tags {"RenderType" = "TransparentCutout" "Queue" = "AlphaTest"}
        // Forward
        Pass
        {
            Name "FORWARD"
            Tags {"LightMode" = "ForwardBase"}

            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            ColorMask [_ColorMask]
            Offset [_OffsetFactor], [_OffsetUnits]
            BlendOp [_BlendOp], [_BlendOpAlpha]
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            AlphaToMask On

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma fragmentoption ARB_precision_hint_fastest

            // Skip receiving shadow
            #pragma skip_variants SHADOWS_SCREEN

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_main.hlsl"

            ENDHLSL
        }

        // ForwardAdd
        Pass
        {
            Name "FORWARD_ADD"
            Tags {"LightMode" = "ForwardAdd"}

            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }
		    Cull [_Cull]
			ZWrite Off
            ZTest LEqual
            ColorMask [_ColorMask]
            Offset [_OffsetFactor], [_OffsetUnits]
            Blend [_SrcBlendFA] [_DstBlendFA], Zero One
            BlendOp [_BlendOpFA], [_BlendOpAlphaFA]
            Fog { Color(0,0,0,0) }
            AlphaToMask On

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_fwdadd
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma fragmentoption ARB_precision_hint_fastest

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #define LIL_PASS_FORWARDADD
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_main.hlsl"

            ENDHLSL
        }

        // ShadowCaster
        Pass
        {
            Name "SHADOW_CASTER"
            Tags {"LightMode" = "ShadowCaster"}
            Offset 1, 1
            Cull [_Cull]

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #pragma fragmentoption ARB_precision_hint_fastest

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_shadowcaster.hlsl"

            ENDHLSL
        }

        // Meta
        Pass
        {
            Name "META"
            Tags {"LightMode" = "Meta"}
            Cull Off

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature EDITOR_VISUALIZATION

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "UnityMetaPass.cginc"
            #include "lil_fur_meta.hlsl"

            ENDHLSL
        }
    }
//
// BRP End

//----------------------------------------------------------------------------------------------------------------------
// LWRP Start
/*
    HLSLINCLUDE
        #define LIL_LWRP
        #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
    ENDHLSL
    SubShader
    {
        Tags {"RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "ShaderModel" = "4.5"}
        // Forward
        Pass
        {
            Name "FORWARD"
            Tags {"LightMode" = "LightweightForward"}

            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            ColorMask [_ColorMask]
            Offset [_OffsetFactor], [_OffsetUnits]
            BlendOp [_BlendOp], [_BlendOpAlpha]
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            AlphaToMask On

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // Skip receiving shadow
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_main.hlsl"

            ENDHLSL
        }

        // ShadowCaster
        Pass
        {
            Name "SHADOW_CASTER"
            Tags {"LightMode" = "ShadowCaster"}
            Offset 1, 1
            Cull [_Cull]

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_shadowcaster.hlsl"

            ENDHLSL
        }

        // DepthOnly
        Pass
        {
            Name "DEPTHONLY"
            Tags {"LightMode" = "DepthOnly"}
            Cull [_Cull]

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_depthonly.hlsl"

            ENDHLSL
        }

        // Meta
        Pass
        {
            Name "META"
            Tags {"LightMode" = "Meta"}
            Cull Off

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/MetaInput.hlsl"
            float4 MetaVertexPosition(float4 positionOS, float2 uv1, float2 uv2, float4 lmst, float4 dlst) { return MetaVertexPosition(positionOS, uv1, uv2, lmst); }
            #include "lil_fur_meta.hlsl"

            ENDHLSL
        }
    }
    SubShader
    {
        Tags {"RenderType" = "TransparentCutout" "Queue" = "AlphaTest"}
        // Forward
        Pass
        {
            Name "FORWARD"
            Tags {"LightMode" = "LightweightForward"}

            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            ColorMask [_ColorMask]
            Offset [_OffsetFactor], [_OffsetUnits]
            BlendOp [_BlendOp], [_BlendOpAlpha]
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            AlphaToMask On

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            // Skip receiving shadow
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_main.hlsl"

            ENDHLSL
        }

        // ShadowCaster
        Pass
        {
            Name "SHADOW_CASTER"
            Tags {"LightMode" = "ShadowCaster"}
            Offset 1, 1
            Cull [_Cull]

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_shadowcaster.hlsl"

            ENDHLSL
        }

        // DepthOnly
        Pass
        {
            Name "DEPTHONLY"
            Tags {"LightMode" = "DepthOnly"}
            Cull [_Cull]

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma multi_compile_instancing

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_depthonly.hlsl"

            ENDHLSL
        }

        // Meta
        Pass
        {
            Name "META"
            Tags {"LightMode" = "Meta"}
            Cull Off

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma only_renderers gles gles3 glcore d3d11

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/MetaInput.hlsl"
            float4 MetaVertexPosition(float4 positionOS, float2 uv1, float2 uv2, float4 lmst, float4 dlst) { return MetaVertexPosition(positionOS, uv1, uv2, lmst); }
            #include "lil_fur_meta.hlsl"

            ENDHLSL
        }
    }
*/
// LWRP End

//----------------------------------------------------------------------------------------------------------------------
// URP Start
/*
    HLSLINCLUDE
        #define LIL_URP
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    ENDHLSL
    SubShader
    {
        Tags {"RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "ShaderModel" = "4.5"}
        // Forward
        Pass
        {
            Name "FORWARD"
            Tags {"LightMode" = "UniversalForward"}

            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            ColorMask [_ColorMask]
            Offset [_OffsetFactor], [_OffsetUnits]
            BlendOp [_BlendOp], [_BlendOpAlpha]
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            AlphaToMask On

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // Skip receiving shadow
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_main.hlsl"

            ENDHLSL
        }

        // ShadowCaster
        Pass
        {
            Name "SHADOW_CASTER"
            Tags {"LightMode" = "ShadowCaster"}
            Offset 1, 1
            Cull [_Cull]

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_shadowcaster.hlsl"

            ENDHLSL
        }

        // DepthOnly
        Pass
        {
            Name "DEPTHONLY"
            Tags {"LightMode" = "DepthOnly"}
            Cull [_Cull]

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_depthonly.hlsl"

            ENDHLSL
        }

        // DepthNormals
        Pass
        {
            Name "DEPTHNORMALS"
            Tags {"LightMode" = "DepthNormals"}
            Cull [_Cull]

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_depthnormals.hlsl"

            ENDHLSL
        }

        // Meta
        Pass
        {
            Name "META"
            Tags {"LightMode" = "Meta"}
            Cull Off

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            #include "lil_fur_meta.hlsl"

            ENDHLSL
        }
    }
    SubShader
    {
        Tags {"RenderType" = "TransparentCutout" "Queue" = "AlphaTest"}
        // Forward
        Pass
        {
            Name "FORWARD"
            Tags {"LightMode" = "UniversalForward"}

            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            ColorMask [_ColorMask]
            Offset [_OffsetFactor], [_OffsetUnits]
            BlendOp [_BlendOp], [_BlendOpAlpha]
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            AlphaToMask On

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            // Skip receiving shadow
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_main.hlsl"

            ENDHLSL
        }

        // ShadowCaster
        Pass
        {
            Name "SHADOW_CASTER"
            Tags {"LightMode" = "ShadowCaster"}
            Offset 1, 1
            Cull [_Cull]

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_shadowcaster.hlsl"

            ENDHLSL
        }

        // DepthOnly
        Pass
        {
            Name "DEPTHONLY"
            Tags {"LightMode" = "DepthOnly"}
            Cull [_Cull]

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma multi_compile_instancing

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_depthonly.hlsl"

            ENDHLSL
        }

        // DepthNormals
        Pass
        {
            Name "DEPTHNORMALS"
            Tags {"LightMode" = "DepthNormals"}
            Cull [_Cull]

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma multi_compile_instancing

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "lil_fur_depthnormals.hlsl"

            ENDHLSL
        }

        // Meta
        Pass
        {
            Name "META"
            Tags {"LightMode" = "Meta"}
            Cull Off

            HLSLPROGRAM

            //------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma only_renderers gles gles3 glcore d3d11

            //------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "lil_fur_macro.hlsl"
            #include "lil_fur_input.hlsl"
            #include "lil_fur_functions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            #include "lil_fur_meta.hlsl"

            ENDHLSL
        }
    }
*/
// URP End

    Fallback "Unlit/Texture"
    CustomEditor "lilFurGenerator.lilFurGeneratorInspector"
}
