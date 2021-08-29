#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace lilFurGenerator
{
    public class lilFurGeneratorInspector : ShaderGUI
    {
        //------------------------------------------------------------------------------------------------------------------------------
        // Enum
        public enum lilRenderPipeline
        {
            BRP,
            LWRP,
            URP
        }

        //------------------------------------------------------------------------------------------------------------------------------
        // Constant
        public const string currentVersionName = "1.0.1";
        public const int currentVersionValue = 2;

        public const string boothURL                = "https://lilxyzw.booth.pm/";
        public const string githubURL               = "https://github.com/lilxyzw/lilFurGenerator";
        public const string versionInfoURL          = "https://raw.githubusercontent.com/lilxyzw/lilFurGenerator/master/version.json";
        public const string editorFolderGUID        = "59970fc0d266132478e5327940963258"; // "Assets/lilFurGenerator/Editor"
        public const string languageFileGUID        = "31b8eeba6110d59439de5782850a4574"; // "Assets/lilFurGenerator/Editor/lang.txt"
        public const string fgShaderGUID            = "2c540356c3ded7340b263ceb4ace7e37"; // "Assets/lilFurGenerator/Shader/lilFurGeneratorToon.shader"
        public const string hqfgShaderGUID          = "7fc1943976840044e82d0503f1f70d23"; // "Assets/lilFurGenerator/Shader/lilFurGeneratorToonHQ.shader"
        public const string ShaderSettingGUID       = "13cb99d89b41fd9428d5101d176b6408"; // "Assets/lilFurGenerator/Editor/ShaderSetting.asset"
        public const string ShaderSettingHLSLGUID   = "f79e08f45a0f3b640b563a49ff855b0c"; // "Assets/lilFurGenerator/Shader/lil_fur_setting.hlsl"
        public const string editorSettingTempPath   = "Temp/lilFurGeneratorEditorSetting";
        public const string versionInfoTempPath     = "Temp/lilFurGeneratorVersion";
        public const string windowName = "lilFurGenerator";

        public static string GetEditorFolderPath()
        {
            return AssetDatabase.GUIDToAssetPath(editorFolderGUID);
        }

        public static string GetShaderSettingPath()
        {
            return AssetDatabase.GUIDToAssetPath(ShaderSettingGUID);
        }

        public static string GetShaderSettingHLSLPath()
        {
            return AssetDatabase.GUIDToAssetPath(ShaderSettingHLSLGUID);
        }

        public static readonly Vector2 defaultTextureOffset = new Vector2(0.0f,0.0f);
        public static readonly Vector2 defaultTextureScale = new Vector2(1.0f,1.0f);
        public static readonly Vector4 defaultDistanceFadeParams = new Vector4(0.1f,0.01f,0.0f,0.0f);
        public static readonly Color lineColor = EditorGUIUtility.isProSkin ? new Color(0.35f,0.35f,0.35f,1.0f) : new Color(0.4f,0.4f,0.4f,1.0f);

        //------------------------------------------------------------------------------------------------------------------------------
        // Editor
        static lilFurGeneratorSetting shaderSetting;
        public static Dictionary<string, string> loc = new Dictionary<string, string>();

        [Serializable]
        public class lilFurEditorSetting
        {
            public int languageNum = -1;
            public string languageNames = "";
            public string languageName = "English";
            public bool isShowMainUV            = false;
            public bool isShowMain              = false;
            public bool isShowShadow            = false;
            public bool isShowDistanceFade      = false;
            public bool isShowStencil           = false;
            public bool isShowFur               = false;
            public bool isShowRendering         = false;
            public bool isShowOptimization      = false;
            public bool isShowBlend             = false;
            public bool isShowBlendAdd          = false;
            public bool isShowWebPages          = false;
            public bool isShowAdvanced          = false;
            public bool isShowShaderSetting     = false;
            public bool isShaderSettingChanged  = false;

            // for generator window
            public GameObject gameObject;
            public bool[] SubMeshToggle         = null;
            public lilFGMeshSettings[] m        = null;
            public Vector2 scrollPosition       = Vector2.zero;
        }

        public struct lilFGMeshSettings
        {
            public Texture2D _FurLengthMask;
            public Texture2D _FurVectorTex;
            public Texture2D _FurSoftnessMask;
            public Texture2D _FurDensityMask;
            public Vector3 _FurVector;
            public float _FurVectorScale;
            public float _RandomStrength;
            public int _FurLayerNum;
            public int _FurJointNum;
        }

        public static lilFurEditorSetting edSet = new lilFurEditorSetting();

        [Serializable]
        public class lilFurVersion
        {
            public string latest_vertion_name;
            public int latest_vertion_value;
        }
        public static lilFurVersion latestVersion = new lilFurVersion
        {
            latest_vertion_name = "",
            latest_vertion_value = 0
        };

        //------------------------------------------------------------------------------------------------------------------------------
        // Material properties
        MaterialProperty asUnlit;
        MaterialProperty cutoff;
        MaterialProperty vertexLightStrength;
        MaterialProperty lightMinLimit;
            MaterialProperty cull;
            MaterialProperty srcBlend;
            MaterialProperty dstBlend;
            MaterialProperty srcBlendAlpha;
            MaterialProperty dstBlendAlpha;
            MaterialProperty blendOp;
            MaterialProperty blendOpAlpha;
            MaterialProperty srcBlendFA;
            MaterialProperty dstBlendFA;
            MaterialProperty srcBlendAlphaFA;
            MaterialProperty dstBlendAlphaFA;
            MaterialProperty blendOpFA;
            MaterialProperty blendOpAlphaFA;
            MaterialProperty zwrite;
            MaterialProperty ztest;
            MaterialProperty stencilRef;
            MaterialProperty stencilReadMask;
            MaterialProperty stencilWriteMask;
            MaterialProperty stencilComp;
            MaterialProperty stencilPass;
            MaterialProperty stencilFail;
            MaterialProperty stencilZFail;
            MaterialProperty offsetFactor;
            MaterialProperty offsetUnits;
            MaterialProperty colorMask;
        MaterialProperty mainTex;
        //MaterialProperty useShadow;
            MaterialProperty shadowBorder;
            MaterialProperty shadowBlur;
            MaterialProperty shadowColor;
            MaterialProperty shadowBorderColor;
            MaterialProperty shadowBorderRange;
            MaterialProperty shadowReceive;
        //MaterialProperty useDistanceFade;
            MaterialProperty distanceFadeColor;
            MaterialProperty distanceFade;
        MaterialProperty useClippingCanceller;
        //MaterialProperty useFur;
            MaterialProperty furMap;
            MaterialProperty furDensity;
            MaterialProperty furLength;
            MaterialProperty furAO;
            MaterialProperty furAOColor;
            MaterialProperty furGravity;
            MaterialProperty furSoftness;
            MaterialProperty furWindFreq1;
            MaterialProperty furWindMove1;
            MaterialProperty furWindFreq2;
            MaterialProperty furWindMove2;
            MaterialProperty furTouchStrength;

        //------------------------------------------------------------------------------------------------------------------------------
        // GUI
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
	    {
            //------------------------------------------------------------------------------------------------------------------------------
            // EditorAssets
            GUIStyle boxOuter        = GUI.skin.box;
            GUIStyle boxInnerHalf    = GUI.skin.box;
            GUIStyle boxInner        = GUI.skin.box;
            GUIStyle customBox       = GUI.skin.box;
            GUIStyle customToggleFont = EditorStyles.label;
            GUIStyle offsetButton = new GUIStyle(GUI.skin.button);
            string editorFolderPath = GetEditorFolderPath();
            if(EditorGUIUtility.isProSkin)
            {
                boxOuter        = new GUIStyle(((GUISkin)AssetDatabase.LoadAssetAtPath(editorFolderPath + "/gui_box_outer_2019.guiskin", typeof(GUISkin))).box);
                boxInnerHalf    = new GUIStyle(((GUISkin)AssetDatabase.LoadAssetAtPath(editorFolderPath + "/gui_box_inner_half_2019.guiskin", typeof(GUISkin))).box);
                boxInner        = new GUIStyle(((GUISkin)AssetDatabase.LoadAssetAtPath(editorFolderPath + "/gui_box_inner_2019.guiskin", typeof(GUISkin))).box);
                customBox       = new GUIStyle(((GUISkin)AssetDatabase.LoadAssetAtPath(editorFolderPath + "/gui_custom_box_2019.guiskin", typeof(GUISkin))).box);
                customToggleFont = EditorStyles.label;
                offsetButton.margin.left = 24;
            }
            else
            {
                boxOuter        = new GUIStyle(((GUISkin)AssetDatabase.LoadAssetAtPath(editorFolderPath + "/gui_box_outer_2018.guiskin", typeof(GUISkin))).box);
                boxInnerHalf    = new GUIStyle(((GUISkin)AssetDatabase.LoadAssetAtPath(editorFolderPath + "/gui_box_inner_half_2018.guiskin", typeof(GUISkin))).box);
                boxInner        = new GUIStyle(((GUISkin)AssetDatabase.LoadAssetAtPath(editorFolderPath + "/gui_box_inner_2018.guiskin", typeof(GUISkin))).box);
                customBox       = GUI.skin.box;
                customToggleFont = new GUIStyle();
                customToggleFont.normal.textColor = Color.white;
                customToggleFont.contentOffset = new Vector2(2f,0f);
                offsetButton.margin.left = 20;
            }
            GUIStyle wrapLabel = new GUIStyle(GUI.skin.label);
            wrapLabel.wordWrap = true;

            Shader fc = (Shader)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(fgShaderGUID), typeof(Shader));
            Shader fchq = (Shader)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(hqfgShaderGUID), typeof(Shader));

            //------------------------------------------------------------------------------------------------------------------------------
            // Initialize Setting
            ApplyEditorSettingTemp();
            InitializeShaderSetting(ref shaderSetting);

            //------------------------------------------------------------------------------------------------------------------------------
            // Load Material
            Material material = (Material)materialEditor.target;
            bool isHQ = material.shader.name.Contains("HQ");

            //------------------------------------------------------------------------------------------------------------------------------
            // Load Properties
            LoadProperties(props);
            if(isHQ) LoadHQProperties(props);

            //------------------------------------------------------------------------------------------------------------------------------
            // Remove Shader Keywords
            RemoveShaderKeywords(material);

            //------------------------------------------------------------------------------------------------------------------------------
            // Info
            DrawWebPages();

            //------------------------------------------------------------------------------------------------------------------------------
            // Language
            edSet.languageNum = selectLang(edSet.languageNum);
            string sCullModes = GetLoc("sCullMode") + "|" + GetLoc("sCullModeOff") + "|" + GetLoc("sCullModeFront") + "|" + GetLoc("sCullModeBack");
            GUIContent textureRGBAContent = new GUIContent(GetLoc("sTexture"), GetLoc("sTextureRGBA"));

            EditorGUILayout.Space();

            //------------------------------------------------------------------------------------------------------------------------------
            // Draw Properties

            //------------------------------------------------------------------------------------------------------------------------------
            // Base Setting
            GUILayout.Label(" " + GetLoc("sBaseSetting"), EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(customBox);
            {
                materialEditor.ShaderProperty(cutoff, GetLoc("sCutoff"));
                materialEditor.ShaderProperty(cull, sCullModes);
                    EditorGUI.indentLevel++;
                    if(cull.floatValue == 1.0f)
                    {
                        EditorGUILayout.HelpBox(GetLoc("sHelpCullMode"),MessageType.Warning);
                    }
                    EditorGUI.indentLevel--;
                materialEditor.ShaderProperty(zwrite, GetLoc("sZWrite"));
                    EditorGUI.indentLevel++;
                    if(zwrite.floatValue != 1.0f)
                    {
                        EditorGUILayout.HelpBox(GetLoc("sHelpZWrite"),MessageType.Warning);
                    }
                    EditorGUI.indentLevel--;
                if(shaderSetting.LIL_FEATURE_CLIPPING_CANCELLER) materialEditor.ShaderProperty(useClippingCanceller, GetLoc("sClippingCanceller"));
                DrawLine();
                materialEditor.ShaderProperty(asUnlit, GetLoc("sAsUnlit"));
                materialEditor.ShaderProperty(vertexLightStrength, GetLoc("sVertexLightStrength"));
                materialEditor.ShaderProperty(lightMinLimit, GetLoc("sLightMinLimit"));
            }
            EditorGUILayout.EndVertical();

            //------------------------------------------------------------------------------------------------------------------------------
            // UV
            edSet.isShowMainUV = Foldout(GetLoc("sMainUV"), GetLoc("sMainUVTips"), edSet.isShowMainUV);
            if(edSet.isShowMainUV)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField(GetLoc("sMainUV"), customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);
                materialEditor.TextureScaleOffsetProperty(mainTex);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            //------------------------------------------------------------------------------------------------------------------------------
            // Colors
            GUILayout.Label(" " + GetLoc("sColors"), EditorStyles.boldLabel);

            //------------------------------------------------------------------------------------------------------------------------------
            // Main Color
            edSet.isShowMain = Foldout(GetLoc("sMainColorSetting"), GetLoc("sMainColorTips"), edSet.isShowMain);
            if(edSet.isShowMain)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField(GetLoc("sMainColor"), customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);
                materialEditor.TexturePropertySingleLine(textureRGBAContent, mainTex);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            //------------------------------------------------------------------------------------------------------------------------------
            // Shadow
            if(shaderSetting.LIL_FEATURE_SHADOW)
            {
                edSet.isShowShadow = Foldout(GetLoc("sShadowSetting"), GetLoc("sShadowTips"), edSet.isShowShadow);
                if(edSet.isShowShadow)
                {
                    EditorGUILayout.BeginVertical(boxOuter);
                    EditorGUILayout.LabelField(GetLoc("sShadow"), customToggleFont);
                    EditorGUILayout.BeginVertical(boxInnerHalf);
                    materialEditor.ShaderProperty(shadowColor, GetLoc("sShadowColor"));
                    materialEditor.ShaderProperty(shadowBorder, GetLoc("sBorder"));
                    materialEditor.ShaderProperty(shadowBlur, GetLoc("sBlur"));
                    DrawLine();
                    materialEditor.ShaderProperty(shadowBorderColor, GetLoc("sShadowBorderColor"));
                    materialEditor.ShaderProperty(shadowBorderRange, GetLoc("sShadowBorderRange"));
                    if(shaderSetting.LIL_FEATURE_RECEIVE_SHADOW) materialEditor.ShaderProperty(shadowReceive, GetLoc("sReceiveShadow"));
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space();
            }

            //------------------------------------------------------------------------------------------------------------------------------
            // Advanced
            GUILayout.Label(" " + GetLoc("sAdvanced"), EditorStyles.boldLabel);

            //------------------------------------------------------------------------------------------------------------------------------
            // Distance Fade
            if(shaderSetting.LIL_FEATURE_DISTANCE_FADE)
            {
                edSet.isShowDistanceFade = Foldout(GetLoc("sDistanceFade"), GetLoc("sDistanceFadeTips"), edSet.isShowDistanceFade);
                if(edSet.isShowDistanceFade)
                {
                    EditorGUILayout.BeginVertical(boxOuter);
                    EditorGUILayout.LabelField(GetLoc("sDistanceFade"), customToggleFont);
                    EditorGUILayout.BeginVertical(boxInnerHalf);
                    materialEditor.ShaderProperty(distanceFadeColor, GetLoc("sColor"));
                    materialEditor.ShaderProperty(distanceFade, GetLoc("sStartDistance")+"|"+GetLoc("sEndDistance")+"|"+GetLoc("sStrength"));
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndVertical();
                }
            }

            //------------------------------------------------------------------------------------------------------------------------------
            // Fur
            edSet.isShowFur = Foldout(GetLoc("sFurSetting"), GetLoc("sFurTips"), edSet.isShowFur);
            if(edSet.isShowFur)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField(GetLoc("sFur"), customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);
                materialEditor.TexturePropertySingleLine(new GUIContent(GetLoc("sMapTexture"), GetLoc("sAlphaA")), furMap);
                materialEditor.ShaderProperty(furDensity, GetLoc("sDensity"));
                materialEditor.ShaderProperty(furLength, GetLoc("sLength"));
                materialEditor.ShaderProperty(furGravity, GetLoc("sGravity"));
                materialEditor.ShaderProperty(furSoftness, GetLoc("sSoftness"));
                materialEditor.ShaderProperty(furAO, GetLoc("sAO"));
                materialEditor.ShaderProperty(furAOColor, GetLoc("sAOColor"));
                DrawLine();
                bool isHQ2 = EditorGUILayout.Toggle(GetLoc("sAdvProperties"), isHQ);
                if(isHQ != isHQ2)
                {
                    material.shader = isHQ2 ? fchq : fc;
                }
                if(isHQ)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Wind 1", EditorStyles.boldLabel);
                    materialEditor.ShaderProperty(furWindFreq1, GetLoc("sFrequency"));
                    materialEditor.ShaderProperty(furWindMove1, GetLoc("sStrength") + "|" + GetLoc("sDetail"));
                    DrawLine();
                    EditorGUILayout.LabelField("Wind 2", EditorStyles.boldLabel);
                    materialEditor.ShaderProperty(furWindFreq2, GetLoc("sFrequency"));
                    materialEditor.ShaderProperty(furWindMove2, GetLoc("sStrength") + "|" + GetLoc("sDetail"));
                    DrawLine();
                    materialEditor.ShaderProperty(furTouchStrength, GetLoc("sTouchStrength"));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            //------------------------------------------------------------------------------------------------------------------------------
            // Stencil
            edSet.isShowStencil = Foldout(GetLoc("sStencilSetting"), GetLoc("sStencilTips"), edSet.isShowStencil);
            if(edSet.isShowStencil)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField(GetLoc("sStencilSetting"), customToggleFont);
                EditorGUILayout.BeginVertical(boxInner);
                //------------------------------------------------------------------------------------------------------------------------------
                // Auto Setting
                if(GUILayout.Button("Set Writer"))
                {
                    stencilRef.floatValue = 1;
                    stencilReadMask.floatValue = 255.0f;
                    stencilWriteMask.floatValue = 255.0f;
                    stencilComp.floatValue = (float)UnityEngine.Rendering.CompareFunction.Always;
                    stencilPass.floatValue = (float)UnityEngine.Rendering.StencilOp.Replace;
                    stencilFail.floatValue = (float)UnityEngine.Rendering.StencilOp.Keep;
                    stencilZFail.floatValue = (float)UnityEngine.Rendering.StencilOp.Keep;
                    material.shader = material.shader;
                }
                if(GUILayout.Button("Set Reader"))
                {
                    stencilRef.floatValue = 1;
                    stencilReadMask.floatValue = 255.0f;
                    stencilWriteMask.floatValue = 255.0f;
                    stencilComp.floatValue = (float)UnityEngine.Rendering.CompareFunction.NotEqual;
                    stencilPass.floatValue = (float)UnityEngine.Rendering.StencilOp.Keep;
                    stencilFail.floatValue = (float)UnityEngine.Rendering.StencilOp.Keep;
                    stencilZFail.floatValue = (float)UnityEngine.Rendering.StencilOp.Keep;
                    material.shader = material.shader;
                }
                if(GUILayout.Button("Reset"))
                {
                    stencilRef.floatValue = 0;
                    stencilReadMask.floatValue = 255.0f;
                    stencilWriteMask.floatValue = 255.0f;
                    stencilComp.floatValue = (float)UnityEngine.Rendering.CompareFunction.Always;
                    stencilPass.floatValue = (float)UnityEngine.Rendering.StencilOp.Keep;
                    stencilFail.floatValue = (float)UnityEngine.Rendering.StencilOp.Keep;
                    stencilZFail.floatValue = (float)UnityEngine.Rendering.StencilOp.Keep;
                    material.shader = material.shader;
                }

                //------------------------------------------------------------------------------------------------------------------------------
                // Base
                {
                    DrawLine();
                    materialEditor.ShaderProperty(stencilRef, "Ref");
                    materialEditor.ShaderProperty(stencilReadMask, "ReadMask");
                    materialEditor.ShaderProperty(stencilWriteMask, "WriteMask");
                    materialEditor.ShaderProperty(stencilComp, "Comp");
                    materialEditor.ShaderProperty(stencilPass, "Pass");
                    materialEditor.ShaderProperty(stencilFail, "Fail");
                    materialEditor.ShaderProperty(stencilZFail, "ZFail");
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            //------------------------------------------------------------------------------------------------------------------------------
            // Rendering
            edSet.isShowRendering = Foldout(GetLoc("sRenderingSetting"), GetLoc("sRenderingTips"), edSet.isShowRendering);
            if(edSet.isShowRendering)
            {
                //------------------------------------------------------------------------------------------------------------------------------
                // Reset Button
                if(GUILayout.Button(GetLoc("sRenderingReset"), offsetButton))
                {
                    material.enableInstancing = false;
                    material.shader = material.shader;
                    material.SetInt("_ZWrite", 1);
                    material.SetInt("_ZTest", 4);
                    material.SetFloat("_OffsetFactor", 0.0f);
                    material.SetFloat("_OffsetUnits", 0.0f);
                    material.SetInt("_ColorMask", 15);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_BlendOp", (int)UnityEngine.Rendering.BlendOp.Add);
                    material.SetInt("_BlendOpAlpha", (int)UnityEngine.Rendering.BlendOp.Add);
                    material.SetInt("_SrcBlendFA", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlendFA", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_SrcBlendAlphaFA", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_DstBlendAlphaFA", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_BlendOpFA", (int)UnityEngine.Rendering.BlendOp.Max);
                    material.SetInt("_BlendOpAlphaFA", (int)UnityEngine.Rendering.BlendOp.Max);
                }

                //------------------------------------------------------------------------------------------------------------------------------
                // Base
                {
                    EditorGUILayout.BeginVertical(boxOuter);
                    EditorGUILayout.LabelField(GetLoc("sRenderingSetting"), customToggleFont);
                    EditorGUILayout.BeginVertical(boxInner);
                    //------------------------------------------------------------------------------------------------------------------------------
                    // Rendering
                    materialEditor.ShaderProperty(cull, sCullModes);
                    materialEditor.ShaderProperty(zwrite, GetLoc("sZWrite"));
                    materialEditor.ShaderProperty(ztest, GetLoc("sZTest"));
                    materialEditor.ShaderProperty(offsetFactor, GetLoc("sOffsetFactor"));
                    materialEditor.ShaderProperty(offsetUnits, GetLoc("sOffsetUnits"));
                    materialEditor.ShaderProperty(colorMask, GetLoc("sColorMask"));
                    DrawLine();
                    BlendSettingGUI(materialEditor, ref edSet.isShowBlend, GetLoc("sForward"), srcBlend, dstBlend, srcBlendAlpha, dstBlendAlpha, blendOp, blendOpAlpha);
                    DrawLine();
                    BlendSettingGUI(materialEditor, ref edSet.isShowBlendAdd, GetLoc("sForwardAdd"), srcBlendFA, dstBlendFA, srcBlendAlphaFA, dstBlendAlphaFA, blendOpFA, blendOpAlphaFA);
                    DrawLine();
                    materialEditor.EnableInstancingField();
                    materialEditor.DoubleSidedGIField();
                    materialEditor.RenderQueueField();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndVertical();
                }
            }

            //------------------------------------------------------------------------------------------------------------------------------
            // Optimization
            GUILayout.Label(" " + GetLoc("sOptimization"), EditorStyles.boldLabel);
            edSet.isShowOptimization = Foldout(GetLoc("sOptimization"), GetLoc("sOptimizationTips"), edSet.isShowOptimization);
            if(edSet.isShowOptimization)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField(GetLoc("sOptimization"), customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);
                if(GUILayout.Button(GetLoc("sRemoveUnused"))) RemoveUnusedProperties(material);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            //------------------------------------------------------------------------------------------------------------------------------
            // Shader Setting
            edSet.isShowShaderSetting = Foldout(GetLoc("sShaderSetting"), GetLoc("sShaderSettingTips"), edSet.isShowShaderSetting);
            if(edSet.isShowShaderSetting)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField(GetLoc("sShaderSetting"), customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);
                EditorGUILayout.HelpBox(GetLoc("sHelpShaderSetting"),MessageType.Info);
                ShaderSettingGUI();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            SaveEditorSettingTemp();
        }

        //------------------------------------------------------------------------------------------------------------------------------
        // Property loader
        void LoadProperties(MaterialProperty[] props)
        {
            asUnlit = FindProperty("_AsUnlit", props);
            cutoff = FindProperty("_Cutoff", props);
            vertexLightStrength = FindProperty("_VertexLightStrength", props);
            lightMinLimit = FindProperty("_LightMinLimit", props);
            useClippingCanceller = FindProperty("_UseClippingCanceller", props);
                cull = FindProperty("_Cull", props);
                srcBlend = FindProperty("_SrcBlend", props);
                dstBlend = FindProperty("_DstBlend", props);
                srcBlendAlpha = FindProperty("_SrcBlendAlpha", props);
                dstBlendAlpha = FindProperty("_DstBlendAlpha", props);
                blendOp = FindProperty("_BlendOp", props);
                blendOpAlpha = FindProperty("_BlendOpAlpha", props);
                srcBlendFA = FindProperty("_SrcBlendFA", props);
                dstBlendFA = FindProperty("_DstBlendFA", props);
                srcBlendAlphaFA = FindProperty("_SrcBlendAlphaFA", props);
                dstBlendAlphaFA = FindProperty("_DstBlendAlphaFA", props);
                blendOpFA = FindProperty("_BlendOpFA", props);
                blendOpAlphaFA = FindProperty("_BlendOpAlphaFA", props);
                zwrite = FindProperty("_ZWrite", props);
                ztest = FindProperty("_ZTest", props);
                stencilRef = FindProperty("_StencilRef", props);
                stencilReadMask = FindProperty("_StencilReadMask", props);
                stencilWriteMask = FindProperty("_StencilWriteMask", props);
                stencilComp = FindProperty("_StencilComp", props);
                stencilPass = FindProperty("_StencilPass", props);
                stencilFail = FindProperty("_StencilFail", props);
                stencilZFail = FindProperty("_StencilZFail", props);
                offsetFactor = FindProperty("_OffsetFactor", props);
                offsetUnits = FindProperty("_OffsetUnits", props);
                colorMask = FindProperty("_ColorMask", props);
            // Main
            mainTex = FindProperty("_MainTex", props);
            // Shadow
            //useShadow = FindProperty("_UseShadow", props);
                shadowBorder = FindProperty("_ShadowBorder", props);
                shadowBlur = FindProperty("_ShadowBlur", props);
                shadowColor = FindProperty("_ShadowColor", props);
                shadowBorderColor = FindProperty("_ShadowBorderColor", props);
                shadowBorderRange = FindProperty("_ShadowBorderRange", props);
                shadowReceive = FindProperty("_ShadowReceive", props);
            distanceFade = FindProperty("_DistanceFade", props);
                distanceFadeColor = FindProperty("_DistanceFadeColor", props);
            useClippingCanceller = FindProperty("_UseClippingCanceller", props);
            //useFur = FindProperty("_UseFur", props);
                furMap = FindProperty("_FurMap", props);
                furDensity = FindProperty("_FurDensity", props);
                furLength = FindProperty("_FurLength", props);
                furAO = FindProperty("_FurAO", props);
                furAOColor = FindProperty("_FurAOColor", props);
                furGravity = FindProperty("_FurGravity", props);
                furSoftness = FindProperty("_FurSoftness", props);
        }

        void LoadHQProperties(MaterialProperty[] props)
        {
            furWindFreq1 = FindProperty("_FurWindFreq1", props);
            furWindMove1 = FindProperty("_FurWindMove1", props);
            furWindFreq2 = FindProperty("_FurWindFreq2", props);
            furWindMove2 = FindProperty("_FurWindMove2", props);
            furTouchStrength = FindProperty("_FurTouchStrength", props);
        }

        //------------------------------------------------------------------------------------------------------------------------------
        // Rendering Pipeline
        public static void RewriteShaderRP(string shaderPath, lilRenderPipeline lilRP)
        {
            string path = shaderPath;
            StreamReader sr = new StreamReader(path);
            string s = sr.ReadToEnd();
            sr.Close();
            RewriteBRP(ref s, lilRP == lilRenderPipeline.BRP);
            RewriteLWRP(ref s, lilRP == lilRenderPipeline.LWRP);
            RewriteURP(ref s, lilRP == lilRenderPipeline.URP);
            StreamWriter sw = new StreamWriter(path,false);
            sw.Write(s);
            sw.Close();
        }

        static void RewriteBRP(ref string s, bool isActive)
        {
            if(isActive)
            {
                s = s.Replace(
                    "// BRP Start\r\n/*",
                    "// BRP Start\r\n//");
                s = s.Replace(
                    "*/\r\n// BRP End",
                    "//\r\n// BRP End");
            }
            else
            {
                s = s.Replace(
                    "// BRP Start\r\n//",
                    "// BRP Start\r\n/*");
                s = s.Replace(
                    "//\r\n// BRP End",
                    "*/\r\n// BRP End");
            }
        }

        static void RewriteLWRP(ref string s, bool isActive)
        {
            if(isActive)
            {
                s = s.Replace(
                    "// LWRP Start\r\n/*",
                    "// LWRP Start\r\n//");
                s = s.Replace(
                    "*/\r\n// LWRP End",
                    "//\r\n// LWRP End");
            }
            else
            {
                s = s.Replace(
                    "// LWRP Start\r\n//",
                    "// LWRP Start\r\n/*");
                s = s.Replace(
                    "//\r\n// LWRP End",
                    "*/\r\n// LWRP End");
            }
        }

        static void RewriteURP(ref string s, bool isActive)
        {
            if(isActive)
            {
                s = s.Replace(
                    "// URP Start\r\n/*",
                    "// URP Start\r\n//");
                s = s.Replace(
                    "*/\r\n// URP End",
                    "//\r\n// URP End");
            }
            else
            {
                s = s.Replace(
                    "// URP Start\r\n//",
                    "// URP Start\r\n/*");
                s = s.Replace(
                    "//\r\n// URP End",
                    "*/\r\n// URP End");
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------
        // Editor
        public static string GetLoc(string value)
        {
            if(loc.ContainsKey(value)) return loc[value];
            return value;
        }

        static void VersionCheck()
        {
            if(String.IsNullOrEmpty(latestVersion.latest_vertion_name))
            {
                if(!File.Exists(versionInfoTempPath))
                {
                    latestVersion.latest_vertion_name = currentVersionName;
                    latestVersion.latest_vertion_value = currentVersionValue;
                    return;
                }
                StreamReader sr = new StreamReader(versionInfoTempPath);
                string s = sr.ReadToEnd();
                sr.Close();
                if(!String.IsNullOrEmpty(s) && s.Contains("latest_vertion_name") && s.Contains("latest_vertion_value"))
                {
                    EditorJsonUtility.FromJsonOverwrite(s,latestVersion);
                }
                else
                {
                    latestVersion.latest_vertion_name = currentVersionName;
                    latestVersion.latest_vertion_value = currentVersionValue;
                    return;
                }
            }
        }

        public static void ApplyEditorSettingTemp()
        {
            if(String.IsNullOrEmpty(edSet.languageNames))
            {
                if(!File.Exists(editorSettingTempPath))
                {
                    return;
                }
                StreamReader sr = new StreamReader(editorSettingTempPath);
                string s = sr.ReadToEnd();
                sr.Close();
                if(!String.IsNullOrEmpty(s))
                {
                    EditorJsonUtility.FromJsonOverwrite(s,edSet);
                }
            }
        }

        public static void SaveEditorSettingTemp()
        {
            StreamWriter sw = new StreamWriter(editorSettingTempPath,false);
            sw.Write(EditorJsonUtility.ToJson(edSet));
            sw.Close();
        }

        public static void DrawLine()
        {
            EditorGUI.DrawRect(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, 1)), lineColor);
        }

        static void DrawWebButton(string text, string URL)
        {
            Rect position = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            GUIContent icon = EditorGUIUtility.IconContent("BuildSettings.Web.Small");
            icon.text = text;
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.padding = new RectOffset();
            if(GUI.Button(position, icon, style)){
                Application.OpenURL(URL);
            }
        }

        public static void DrawWebPages()
        {
            VersionCheck();
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontStyle = FontStyle.Bold;
            string versionLabel = "FurGenerator " + currentVersionName;
            if(latestVersion != null && latestVersion.latest_vertion_name != null && latestVersion.latest_vertion_value > currentVersionValue)
            {
                versionLabel = "[Update] FurGenerator " + currentVersionName + " -> " + latestVersion.latest_vertion_name;
                labelStyle.normal.textColor = Color.red;
            }
            EditorGUI.indentLevel++;
            Rect position = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(position, versionLabel, labelStyle);
            EditorGUI.indentLevel--;

            position.x += 10;
            edSet.isShowWebPages = EditorGUI.Foldout(position, edSet.isShowWebPages, "");
            if(edSet.isShowWebPages)
            {
                EditorGUI.indentLevel++;
                DrawWebButton("BOOTH", boothURL);
                DrawWebButton("GitHub", githubURL);
                EditorGUI.indentLevel--;
            }
        }

        public static void DrawWebPagesInWindow()
        {
            VersionCheck();
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontStyle = FontStyle.Bold;
            string versionLabel = "FurGenerator " + currentVersionName;
            if(latestVersion != null && latestVersion.latest_vertion_name != null && latestVersion.latest_vertion_value > currentVersionValue)
            {
                versionLabel = "[Update] FurGenerator " + currentVersionName + " -> " + latestVersion.latest_vertion_name;
                labelStyle.normal.textColor = Color.red;
            }
            EditorGUI.indentLevel++;
            Rect position = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(position, versionLabel, labelStyle);
            EditorGUI.indentLevel--;

            edSet.isShowWebPages = EditorGUI.Foldout(position, edSet.isShowWebPages, "");
            if(edSet.isShowWebPages)
            {
                EditorGUI.indentLevel++;
                DrawWebButton("BOOTH", boothURL);
                DrawWebButton("GitHub", githubURL);
                EditorGUI.indentLevel--;
            }
        }

        static bool Foldout(string title, string help, bool display)
        {
            GUIStyle style = new GUIStyle("ShurikenModuleTitle");
            #if UNITY_2019_1_OR_NEWER
                style.fontSize = 12;
            #else
                style.fontSize = 11;
            #endif
            style.border = new RectOffset(15, 7, 4, 4);
            style.contentOffset = new Vector2(20f, -2f);
            style.fixedHeight = 22;
            Rect rect = GUILayoutUtility.GetRect(16f, 20f, style);
            GUI.Box(rect, new GUIContent(title, help), style);

            Event e = Event.current;

            Rect toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            if(e.type == EventType.Repaint) {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }

            rect.width -= 24;
            if(e.type == EventType.MouseDown & rect.Contains(e.mousePosition)) {
                display = !display;
                e.Use();
            }

            return display;
        }

        public static int selectLang(int lnum)
        {
            int outnum = lnum;
            outnum = InitializeLanguage(outnum);

            // Select language
            string[] langName = edSet.languageNames.Split('\t');
            outnum = EditorGUILayout.Popup("Language", outnum, langName);

            // Load language
            if(outnum != lnum)
            {
                string langPath = AssetDatabase.GUIDToAssetPath(languageFileGUID);
                if(String.IsNullOrEmpty(langPath) || !File.Exists(langPath)) return outnum;
                StreamReader sr = new StreamReader(langPath);
                string langBuf = sr.ReadToEnd();
                sr.Close();

                string[] langData = langBuf.Split('\n');
                edSet.languageNames = langData[0].Substring(langData[0].IndexOf("\t")+1);
                edSet.languageName = edSet.languageNames.Split('\t')[outnum];
                for(int i = 0; i < langData.Length; i++)
                {
                    string[] lineContents = langData[i].Split('\t');
                    loc[lineContents[0]] = lineContents[outnum+1];
                }
            }

            if(!String.IsNullOrEmpty(GetLoc("sLanguageWarning"))) EditorGUILayout.HelpBox(GetLoc("sLanguageWarning"),MessageType.Warning);

            return outnum;
        }

        public static int InitializeLanguage(int lnum)
        {
            if(lnum == -1)
            {
                if(Application.systemLanguage == SystemLanguage.Japanese)                   lnum = 1;
                else if(Application.systemLanguage == SystemLanguage.Korean)                lnum = 2;
                else if(Application.systemLanguage == SystemLanguage.ChineseSimplified)     lnum = 3;
                else if(Application.systemLanguage == SystemLanguage.ChineseTraditional)    lnum = 4;
                else                                                                        lnum = 0;
            }

            if(loc.Count == 0)
            {
                string langPath = AssetDatabase.GUIDToAssetPath(languageFileGUID);
                if(String.IsNullOrEmpty(langPath) || !File.Exists(langPath)) return lnum;
                StreamReader sr = new StreamReader(langPath);
                string langBuf = sr.ReadToEnd();
                sr.Close();

                string[] langData = langBuf.Split('\n');
                edSet.languageNames = langData[0].Substring(langData[0].IndexOf("\t")+1);
                edSet.languageName = edSet.languageNames.Split('\t')[lnum];
                for(int i = 0; i < langData.Length; i++)
                {
                    string[] lineContents = langData[i].Split('\t');
                    loc[lineContents[0]] = lineContents[lnum+1];
                }
            }

            return lnum;
        }

        public static void InitializeShaderSetting(ref lilFurGeneratorSetting shaderSetting)
        {
            if(shaderSetting != null) return;
            string shaderSettingPath = GetShaderSettingPath();
            shaderSetting = (lilFurGeneratorSetting)AssetDatabase.LoadAssetAtPath(shaderSettingPath, typeof(lilFurGeneratorSetting));
            if(shaderSetting == null)
            {
                shaderSetting = ScriptableObject.CreateInstance<lilFurGeneratorSetting>();
                AssetDatabase.CreateAsset(shaderSetting, shaderSettingPath);
                shaderSetting.LIL_FEATURE_SHADOW = true;
                shaderSetting.LIL_FEATURE_RECEIVE_SHADOW = false;
                shaderSetting.LIL_FEATURE_CLIPPING_CANCELLER = false;
                shaderSetting.LIL_FEATURE_DISTANCE_FADE = false;
                EditorUtility.SetDirty(shaderSetting);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        static void ShaderSettingGUI()
        {
            GUIStyle applyButton = new GUIStyle(GUI.skin.button);
            applyButton.normal.textColor = Color.red;
            applyButton.fontStyle = FontStyle.Bold;

            // Apply Button
            if(edSet.isShaderSettingChanged && GUILayout.Button("Apply", applyButton))
            {
                ApplyShaderSetting(shaderSetting);
                edSet.isShaderSettingChanged = false;
            }
            DrawLine();

            EditorGUI.BeginChangeCheck();

            lilToggleGUI(GetLoc("sSettingShadow"), ref shaderSetting.LIL_FEATURE_SHADOW);
            if(shaderSetting.LIL_FEATURE_SHADOW)
            {
                EditorGUI.indentLevel++;
                lilToggleGUI(GetLoc("sSettingReceiveShadow"), ref shaderSetting.LIL_FEATURE_RECEIVE_SHADOW);
                EditorGUI.indentLevel--;
            }
            DrawLine();

            lilToggleGUI(GetLoc("sSettingClippingCanceller"), ref shaderSetting.LIL_FEATURE_CLIPPING_CANCELLER);
            DrawLine();

            lilToggleGUI(GetLoc("sSettingDistanceFade"), ref shaderSetting.LIL_FEATURE_DISTANCE_FADE);

            if(EditorGUI.EndChangeCheck())
            {
                edSet.isShaderSettingChanged = true;
                EditorUtility.SetDirty(shaderSetting);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        static void lilToggleGUI(string label, ref bool value)
        {
            value = EditorGUILayout.ToggleLeft(label, value);
        }

        public static void ApplyShaderSetting(lilFurGeneratorSetting shaderSetting)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("#ifndef LIL_FUR_SETTING_INCLUDED\r\n#define LIL_FUR_SETTING_INCLUDED\r\n\r\n");
            if(shaderSetting.LIL_FEATURE_SHADOW)
            {
                sb.Append("#define LIL_FEATURE_SHADOW\r\n");
                if(shaderSetting.LIL_FEATURE_RECEIVE_SHADOW) sb.Append("#define LIL_FEATURE_RECEIVE_SHADOW\r\n");
            }
            if(shaderSetting.LIL_FEATURE_CLIPPING_CANCELLER) sb.Append("#define LIL_FEATURE_CLIPPING_CANCELLER\r\n");
            if(shaderSetting.LIL_FEATURE_DISTANCE_FADE) sb.Append("#define LIL_FEATURE_DISTANCE_FADE\r\n");
            sb.Append("\r\n#endif");
            string shaderSettingString = sb.ToString();

            string shaderSettingStringBuf = "";
            string shaderSettingHLSLPath = GetShaderSettingHLSLPath();
            if(File.Exists(shaderSettingHLSLPath))
            {
                StreamReader sr = new StreamReader(shaderSettingHLSLPath);
                shaderSettingStringBuf = sr.ReadToEnd();
                sr.Close();
            }

            if(shaderSettingString != shaderSettingStringBuf)
            {
                StreamWriter sw = new StreamWriter(shaderSettingHLSLPath,false);
                sw.Write(shaderSettingString);
                sw.Close();
                RewriteReceiveShadow(AssetDatabase.GUIDToAssetPath(fgShaderGUID), shaderSetting.LIL_FEATURE_SHADOW && shaderSetting.LIL_FEATURE_RECEIVE_SHADOW);
                RewriteReceiveShadow(AssetDatabase.GUIDToAssetPath(hqfgShaderGUID), shaderSetting.LIL_FEATURE_SHADOW && shaderSetting.LIL_FEATURE_RECEIVE_SHADOW);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(shaderSettingHLSLPath);
                AssetDatabase.Refresh();
            }
        }

        static void RewriteReceiveShadow(string path, bool enable)
        {
            if(String.IsNullOrEmpty(path) || !File.Exists(path)) return;
            StreamReader sr = new StreamReader(path);
            string s = sr.ReadToEnd();
            sr.Close();
            if(enable)
            {
                // BRP
                s = s.Replace(
                    "            // Skip receiving shadow\r\n            #pragma skip_variants SHADOWS_SCREEN",
                    "            // Skip receiving shadow\r\n            //#pragma skip_variants SHADOWS_SCREEN");
                // LWRP & URP
                s = s.Replace(
                    "            // Skip receiving shadow\r\n            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN\r\n            //#pragma multi_compile_fragment _ _SHADOWS_SOFT",
                    "            // Skip receiving shadow\r\n            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN\r\n            #pragma multi_compile_fragment _ _SHADOWS_SOFT");
            }
            else
            {
                // BRP
                s = s.Replace(
                    "            // Skip receiving shadow\r\n            //#pragma skip_variants SHADOWS_SCREEN",
                    "            // Skip receiving shadow\r\n            #pragma skip_variants SHADOWS_SCREEN");
                // LWRP & URP
                s = s.Replace(
                    "            // Skip receiving shadow\r\n            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN\r\n            #pragma multi_compile_fragment _ _SHADOWS_SOFT",
                    "            // Skip receiving shadow\r\n            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN\r\n            //#pragma multi_compile_fragment _ _SHADOWS_SOFT");
            }
            StreamWriter sw = new StreamWriter(path,false);
            sw.Write(s);
            sw.Close();
        }

        static void RewriteReceiveShadow(Shader shader, bool enable)
        {
            string path = AssetDatabase.GetAssetPath(shader);
            RewriteReceiveShadow(path, enable);
        }

        //------------------------------------------------------------------------------------------------------------------------------
        // Material Setup
        static void RemoveShaderKeywords(Material material)
        {
            foreach(string keyword in material.shaderKeywords)
            {
                material.DisableKeyword(keyword);
            }
        }

        static void RemoveUnusedProperties(Material material)
        {
            Material newMaterial = new Material(material.shader);
            newMaterial.name                    = material.name;
            newMaterial.doubleSidedGI           = material.doubleSidedGI;
            newMaterial.globalIlluminationFlags = material.globalIlluminationFlags;
            newMaterial.renderQueue             = material.renderQueue;
            int propCount = ShaderUtil.GetPropertyCount(material.shader);
            for(int i = 0; i < propCount; i++)
            {
                string propName = ShaderUtil.GetPropertyName(material.shader, i);
                ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(material.shader, i);
                if(propType == ShaderUtil.ShaderPropertyType.Color)    newMaterial.SetColor(propName,  material.GetColor(propName));
                if(propType == ShaderUtil.ShaderPropertyType.Vector)   newMaterial.SetVector(propName, material.GetVector(propName));
                if(propType == ShaderUtil.ShaderPropertyType.Float)    newMaterial.SetFloat(propName,  material.GetFloat(propName));
                if(propType == ShaderUtil.ShaderPropertyType.Range)    newMaterial.SetFloat(propName,  material.GetFloat(propName));
                if(propType == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    newMaterial.SetTexture(propName, material.GetTexture(propName));
                    newMaterial.SetTextureOffset(propName, material.GetTextureOffset(propName));
                    newMaterial.SetTextureScale(propName, material.GetTextureScale(propName));
                }
            }
            string matPath = AssetDatabase.GetAssetPath(material);
            string newMatPath = matPath + "_new";
            AssetDatabase.CreateAsset(newMaterial, newMatPath);
            FileUtil.ReplaceFile(newMatPath, matPath);
            AssetDatabase.DeleteAsset(newMatPath);
        }

        //------------------------------------------------------------------------------------------------------------------------------
        // Property drawer
        void BlendSettingGUI(MaterialEditor materialEditor, ref bool isShow, string labelName, MaterialProperty srcRGB, MaterialProperty dstRGB, MaterialProperty srcA, MaterialProperty dstA, MaterialProperty opRGB, MaterialProperty opA)
        {
            // Make space for foldout
            EditorGUI.indentLevel++;
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(rect, labelName);
            EditorGUI.indentLevel--;

            rect.x += 10;
            isShow = EditorGUI.Foldout(rect, isShow, "");
            if(isShow)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(srcRGB, GetLoc("sSrcBlendRGB"));
                materialEditor.ShaderProperty(dstRGB, GetLoc("sDstBlendRGB"));
                materialEditor.ShaderProperty(srcA, GetLoc("sSrcBlendAlpha"));
                materialEditor.ShaderProperty(dstA, GetLoc("sDstBlendAlpha"));
                materialEditor.ShaderProperty(opRGB, GetLoc("sBlendOpRGB"));
                materialEditor.ShaderProperty(opA, GetLoc("sBlendOpAlpha"));
                EditorGUI.indentLevel--;
            }
        }
    }
}
#endif