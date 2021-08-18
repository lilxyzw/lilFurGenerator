Shader "_lil/[Example] FurGeneratorUnlit"
{
    Properties
    {
                        _MainTex                    ("Texture", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Int) = 0

        // These properties are displayed in the editor window
        [NoScaleOffset] _FurMap                     ("Fur Map", 2D) = "white" {}
                        _FurDensity                 ("Fur Density", Float) = 1.0
                        _FurLength                  ("Fur Length", Float) = 0.2
                        _FurGravity                 ("Fur Gravity", Range(0,1)) = 0.25
                        _FurSoftness                ("Fur Softness", Range(0.001,1)) = 1.0
                        _FurAO                      ("Fur AO", Range(0,1)) = 0
    }
    SubShader
    {
        Tags {"RenderType" = "TransparentCutout" "Queue" = "AlphaTest"}
        Pass
        {
            // Cull Off is recommended
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Unity variables
            float4x4 unity_ObjectToWorld;
            float4x4 unity_MatrixVP;
            float4 unity_WorldTransformParams;

            // Material variables
            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            Texture2D _FurMap;
            SamplerState sampler_linear_repeatU_clampV;
            float4 _MainTex_ST;
            float _FurLength;
            float _FurGravity;
            float _FurAO;
            float _FurDensity;
            float _FurSoftness;

            // Fur calculation in world space
            void CalcFur(inout float3 positionWS, float3 normalOS, float4 tangentOS, float4 color, float2 uv4)
            {
                // Base
                float3 bitangentOS = cross(normalOS, tangentOS.xyz) * (tangentOS.w * unity_WorldTransformParams.w);
                float3x3 tbnOS = float3x3(tangentOS.xyz, bitangentOS, normalOS);
                float3 vectorWS = mul((float3x3)unity_ObjectToWorld, mul(color.xyz, tbnOS));
                float furLength = length(vectorWS);

                // Motion
                float3 motionWS = float3(0.0, -_FurGravity, 0.0);
                float motionStrength = _FurSoftness * color.w * furLength;

                // Blend
                vectorWS = normalize(vectorWS + motionWS * motionStrength) * (furLength * _FurLength * uv4.y);

                positionWS = uv4.y > -0.5 ? positionWS + vectorWS : positionWS;
            }

            // Fur Map
            // Do not sample textures on the body
            float GetFurMap(float2 uv4)
            {
                float furmap = _FurMap.Sample(sampler_linear_repeatU_clampV, uv4).a;
                return uv4.y > -0.5 ? furmap : 1.0;
            }

            struct appdata
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                float2 uv4          : TEXCOORD4;
                float4 color        : COLOR;

                // uv4.x: fur map uv.x
                // uv4.y: fur map uv.y and depth
                //        body: -1.0
                //        root: 0.0
                //        end: 1.0

                // color.r: fur vector x
                // color.g: fur vector y
                // color.b: fur vector z
                // color.a: softness
                // You can get fur length by length(color.rgb)
            };

            struct v2f
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float2 uv4          : TEXCOORD1;
            };

            v2f vert(appdata input)
            {
                v2f output;

                float3 positionWS = mul(unity_ObjectToWorld, float4(input.positionOS.xyz,1.0)).xyz;
                CalcFur(positionWS, input.normalOS, input.tangentOS, input.color, input.uv4);
                output.positionCS = mul(unity_MatrixVP, float4(positionWS,1.0));
                output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                output.uv4 = input.uv4;
                output.uv4.x *= _FurDensity;

                return output;
            }

            float4 frag(v2f input) : SV_Target
            {
                float4 col = _MainTex.Sample(sampler_MainTex, input.uv);

                // Fur Map
                float furmap = GetFurMap(input.uv4);
                furmap = saturate(furmap + fwidth(input.uv4.y) * 2.0);
                col.a *= furmap;
                clip(col.a - 0.5);

                // AO
                col.rgb *= saturate(input.uv4.y) * _FurAO + 1.0 - _FurAO;
                return col;
            }
            ENDHLSL
        }
    }
}
