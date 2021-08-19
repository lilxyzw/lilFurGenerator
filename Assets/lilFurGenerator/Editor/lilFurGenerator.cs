#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static lilFurGenerator.lilFurGeneratorInspector;

namespace lilFurGenerator
{
    public class lilFurGeneratorWindow : EditorWindow
    {
        static Texture2D furMapW;
        static float furLengthW;
        static float furDensityW;
        static float furAOW;
        static float furGravityW;
        static float furSoftnessW;

        [MenuItem("Window/_lil/Fur Generator")]
        static void Init()
        {
            lilFurGeneratorWindow window = (lilFurGeneratorWindow)EditorWindow.GetWindow(typeof(lilFurGeneratorWindow), false, windowName);
            window.Show();
        }

        [InitializeOnLoadMethod]
        static void StartUp()
        {
            if(!File.Exists(versionInfoTempPath))
            {
                CoroutineHandler.StartStaticCoroutine(GetLatestVersionInfo());
            }

            AssetDatabase.importPackageCompleted += OnImportPackageCompleted =>
            {
                string shaderPath = AssetDatabase.GUIDToAssetPath(fgShaderGUID);
                string hqshaderPath = AssetDatabase.GUIDToAssetPath(hqfgShaderGUID);
                if(String.IsNullOrEmpty(shaderPath) || !File.Exists(shaderPath) || String.IsNullOrEmpty(hqshaderPath) || !File.Exists(hqshaderPath)) return;

                // Render Pipeline
                // BRP : null
                // LWRP : LightweightPipeline.LightweightRenderPipelineAsset
                // URP : Universal.UniversalRenderPipelineAsset
                lilRenderPipeline lilRP = lilRenderPipeline.BRP;
                if(UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null)
                {
                    string renderPipelineName = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset.ToString();
                    if(String.IsNullOrEmpty(renderPipelineName))        lilRP = lilRenderPipeline.BRP;
                    else if(renderPipelineName.Contains("Lightweight")) lilRP = lilRenderPipeline.LWRP;
                    else if(renderPipelineName.Contains("Universal"))   lilRP = lilRenderPipeline.URP;
                }
                else
                {
                    lilRP = lilRenderPipeline.BRP;
                }
                RewriteShaderRP(shaderPath, lilRP);
                RewriteShaderRP(hqshaderPath, lilRP);
            };
        }

        void OnGUI()
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
            boxOuter.margin.left = 4;
            GUIStyle wrapLabel = new GUIStyle(GUI.skin.label);
            wrapLabel.wordWrap = true;

            //------------------------------------------------------------------------------------------------------------------------------
            // Initialize Setting
            ApplyEditorSettingTemp();
            edSet.scrollPosition = EditorGUILayout.BeginScrollView(edSet.scrollPosition);
            DrawWebPagesInWindow();
            edSet.languageNum = selectLang(edSet.languageNum);
            EditorGUILayout.Space();

            //------------------------------------------------------------------------------------------------------------------------------
            // Select the mesh
            EditorGUILayout.BeginVertical(boxOuter);
            EditorGUILayout.LabelField(GetLoc("sSelectMesh"), customToggleFont);
            EditorGUILayout.BeginVertical(boxInnerHalf);
            edSet.gameObject = (GameObject)EditorGUILayout.ObjectField(GetLoc("sMeshDD"), edSet.gameObject, typeof(GameObject), true);
            if(edSet.gameObject == null)
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                FinishMainGUI();
                return;
            }

            // Get mesh data
            MeshFilter meshFilter = edSet.gameObject.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = edSet.gameObject.GetComponent<MeshRenderer>();
            SkinnedMeshRenderer skinnedMeshRenderer = edSet.gameObject.GetComponent<SkinnedMeshRenderer>();
            Mesh sharedMesh;
            Material[] materials = null;
            if(meshFilter == null && skinnedMeshRenderer == null)
            {
                EditorGUILayout.HelpBox(GetLoc("sHelpSelectMesh"), MessageType.Warning);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                FinishMainGUI();
                return;
            }
            else if(AssetDatabase.Contains(edSet.gameObject))
            {
                EditorGUILayout.HelpBox(GetLoc("sHelpSelectFromScene"), MessageType.Warning);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                FinishMainGUI();
                return;
            }
            else if(meshFilter != null)
            {
                sharedMesh = meshFilter.sharedMesh;
                if(meshRenderer != null) materials = meshRenderer.sharedMaterials;
            }
            else
            {
                sharedMesh = skinnedMeshRenderer.sharedMesh;
                materials = skinnedMeshRenderer.sharedMaterials;
            }

            // Select sub mesh
            EditorGUILayout.LabelField(GetLoc("sSubMeshList"));
            EditorGUI.indentLevel++;
            if(edSet.SubMeshToggle == null || edSet.SubMeshToggle.Length != materials.Length)
            {
                edSet.SubMeshToggle = new bool[materials.Length];
            }
            if(edSet.m == null || edSet.m.Length != materials.Length)
            {
                edSet.m = new lilFGMeshSettings[materials.Length];
                for(int i = 0; i < materials.Length; i++)
                {
                    edSet.m[i] = new lilFGMeshSettings
                    {
                        _RandomStrength = 0.1f,
                        _FurLayerNum = 1,
                        _FurJointNum = 1,
                        _FurLengthMask = null,
                        _FurVectorTex = null,
                        _FurSoftnessMask = null,
                        _FurDensityMask = null,
                        _FurVector = new Vector3(0.0f, 0.0f, 1.0f),
                        _FurVectorScale = 1.0f
                    };
                }
            }
            String[] subMeshNames = new string[sharedMesh.subMeshCount];
            for(int i = 0; i < sharedMesh.subMeshCount; i++)
            {
                subMeshNames[i] = i + ": ";
                if(i < materials.Length && materials[i] != null)
                {
                    String matName = materials[i].name;
                    if(!String.IsNullOrEmpty(matName)) subMeshNames[i] += matName;
                }
                edSet.SubMeshToggle[i] = EditorGUILayout.ToggleLeft(subMeshNames[i], edSet.SubMeshToggle[i]);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            //------------------------------------------------------------------------------------------------------------------------------
            // Mesh Editor
            EditorGUILayout.BeginVertical(boxOuter);
            EditorGUILayout.LabelField(GetLoc("sGenerateAndSave"), customToggleFont);
            EditorGUILayout.BeginVertical(boxInnerHalf);

            DrawFurSettings(sharedMesh, materials, subMeshNames);

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button(GetLoc("sGenerateTest")))
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                FinishMainGUI();
                GenerateFurFromMesh(edSet);
                return;
            }

            GameObject furObject = null;
            if(edSet.gameObject.transform.parent != null)
            {
                for(int i = 0; i < edSet.gameObject.transform.parent.childCount; i++)
                {
                    GameObject childObject = edSet.gameObject.transform.parent.GetChild(i).gameObject;
                    if(childObject.name.Contains(edSet.gameObject.name + " (FurGenerator)"))
                    {
                        furObject = childObject;
                        break;
                    }
                }
            }
            else
            {
                furObject = GameObject.Find(edSet.gameObject.name + " (FurGenerator)");
            }

            MeshFilter furMeshFilter = null;
            MeshRenderer furMeshRenderer = null;
            SkinnedMeshRenderer furSkinnedMeshRenderer = null;
            Material[] furMaterials = new Material[materials.Length];
            Mesh furMesh = null;

            if(furObject != null)
            {
                furMeshFilter = furObject.GetComponent<MeshFilter>();
                furMeshRenderer = furObject.GetComponent<MeshRenderer>();
                furSkinnedMeshRenderer = furObject.GetComponent<SkinnedMeshRenderer>();
            }

            if(furMeshRenderer != null)
            {
                furMaterials = furMeshRenderer.sharedMaterials;
                furMesh = furMeshFilter.sharedMesh;
            }
            else if(furSkinnedMeshRenderer != null)
            {
                furMaterials = furSkinnedMeshRenderer.sharedMaterials;
                furMesh = furSkinnedMeshRenderer.sharedMesh;
            }

            if(furObject == null || furMeshRenderer == null && furSkinnedMeshRenderer == null || furMesh == null)
            {
                GUI.enabled = false;
                GUILayout.Button(GetLoc("sSaveMesh"));
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                FinishMainGUI();
                return;
            }

            GUIStyle saveButton = new GUIStyle(GUI.skin.button);
            if(!AssetDatabase.Contains(furMesh))
            {
                saveButton.normal.textColor = Color.red;
                saveButton.fontStyle = FontStyle.Bold;
            }

            if(GUILayout.Button(GetLoc("sSaveMesh"), saveButton))
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                FinishMainGUI();
                string path = "Assets/";
                if(AssetDatabase.Contains(furMesh))
                {
                    path = AssetDatabase.GetAssetPath(furMesh);
                    if(String.IsNullOrEmpty(path)) path = "Assets/";
                    else path = Path.GetDirectoryName(path);
                }
                path = EditorUtility.SaveFilePanel(GetLoc("sSaveMesh"), path, furMesh.name, "asset");
                if(String.IsNullOrEmpty(path))
                {
                    return;
                }
                AssetDatabase.CreateAsset(furMesh, FileUtil.GetProjectRelativePath(path));
                return;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            //------------------------------------------------------------------------------------------------------------------------------
            // Material Editor
            for(int i = 0; i < furMaterials.Length; i++)
            {
                if(!edSet.SubMeshToggle[i]) continue;
                if(furMaterials[i] == null || !furMaterials[i].shader.name.Contains("FurGenerator"))
                {
                    Shader shader = (Shader)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(fgShaderGUID), typeof(Shader));
                    furMaterials[i] = new Material(shader);
                    furMaterials[i].name = "Fur";
                    if(materials[i] != null) CopyMaterialData(materials[i], furMaterials[i]);
                }
            }
            if(furMeshRenderer != null) furMeshRenderer.sharedMaterials = furMaterials;
            if(furSkinnedMeshRenderer != null) furSkinnedMeshRenderer.sharedMaterials = furMaterials;

            EditorGUILayout.BeginVertical(boxOuter);
            EditorGUILayout.LabelField(GetLoc("sEditMaterial"), customToggleFont);
            EditorGUILayout.BeginVertical(boxInnerHalf);

            saveButton.normal.textColor = Color.red;
            saveButton.fontStyle = FontStyle.Bold;

            FurMaterialGUI(furMaterials, subMeshNames, saveButton, furMeshRenderer, furSkinnedMeshRenderer);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            FinishMainGUI();
        }

        //------------------------------------------------------------------------------------------------------------------------------
        // Editor
        static void FinishMainGUI()
        {
            EditorGUILayout.EndScrollView();
            SaveEditorSettingTemp();
        }

        static void CopyFurSettingFromMaterial(Material material, int i)
        {
            if(material.HasProperty("_FurVector"))
            {
                Vector4 furVec = material.GetVector("_FurVector");
                edSet.m[i]._FurVector.Set(furVec.x, furVec.y, furVec.z);
            }
            if(material.HasProperty("_FurLengthMask")) edSet.m[i]._FurLengthMask = (Texture2D)material.GetTexture("_FurLengthMask");
            if(material.HasProperty("_FurVectorScale")) edSet.m[i]._FurVectorScale = material.GetFloat("_FurVectorScale");
            if(material.HasProperty("_FurVectorTex")) edSet.m[i]._FurVectorTex = (Texture2D)material.GetTexture("_FurVectorTex");
            if(material.HasProperty("_FurLayerNum")) edSet.m[i]._FurLayerNum = material.GetInt("_FurLayerNum");
        }

        static void DrawFurSettings(Mesh sharedMesh, Material[] materials, string[] subMeshNames)
        {
            int originalPolygons = 0;
            int estimatedPolygons = 0;
            for(int i = 0; i < materials.Length; i++)
            {
                originalPolygons += sharedMesh.GetIndices(i).Length / 3;
                if(!edSet.SubMeshToggle[i]) continue;
                Material material = materials[i];
                if(material != null && material.shader.name.Contains("lilToonFur"))
                {
                    if(GUILayout.Button(GetLoc("sAutoSettingFromMaterial")))
                    {
                        CopyFurSettingFromMaterial(material, i);
                    }
                    DrawLine();
                }
                EditorGUILayout.LabelField(subMeshNames[i], EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                edSet.m[i]._FurLengthMask = (Texture2D)EditorGUILayout.ObjectField(GetLoc("sLengthMask"), edSet.m[i]._FurLengthMask, typeof(Texture2D), false);
                DrawLine();
                EditorGUIUtility.wideMode = true;
                edSet.m[i]._FurVector = EditorGUILayout.Vector3Field(GetLoc("sVector"), edSet.m[i]._FurVector);
                edSet.m[i]._FurVectorScale = EditorGUILayout.FloatField(GetLoc("sNormalMapStrength"), edSet.m[i]._FurVectorScale);
                edSet.m[i]._FurVectorTex = (Texture2D)EditorGUILayout.ObjectField(GetLoc("sNormalMap"), edSet.m[i]._FurVectorTex, typeof(Texture2D), false);
                edSet.m[i]._RandomStrength = EditorGUILayout.FloatField(GetLoc("sRandomize"), edSet.m[i]._RandomStrength);
                DrawLine();
                edSet.m[i]._FurLayerNum = EditorGUILayout.IntField(GetLoc("sLayerNum"), edSet.m[i]._FurLayerNum);
                DrawLine();
                EditorGUI.indentLevel++;
                Rect position = EditorGUILayout.GetControlRect();
                EditorGUI.LabelField(position, "Advanced", EditorStyles.boldLabel);
                EditorGUI.indentLevel--;

                edSet.isShowAdvanced = EditorGUI.Foldout(position, edSet.isShowAdvanced, "");
                if(edSet.isShowAdvanced)
                {
                    EditorGUI.indentLevel++;
                    edSet.m[i]._FurSoftnessMask = (Texture2D)EditorGUILayout.ObjectField(GetLoc("sSoftnessMask"), edSet.m[i]._FurSoftnessMask, typeof(Texture2D), false);
                    edSet.m[i]._FurDensityMask = (Texture2D)EditorGUILayout.ObjectField(GetLoc("sDensityMask"), edSet.m[i]._FurDensityMask, typeof(Texture2D), false);
                    edSet.m[i]._FurJointNum = EditorGUILayout.IntField(GetLoc("sJointNum"), edSet.m[i]._FurJointNum);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
                estimatedPolygons += sharedMesh.GetIndices(i).Length * 2 * edSet.m[i]._FurLayerNum * edSet.m[i]._FurJointNum;
            }

            EditorGUILayout.HelpBox(GetLoc("sOriginal") + ": " + originalPolygons + " " + GetLoc("sPolygons") + "\r\n" + GetLoc("sFurEstimated") + ": " + estimatedPolygons + " " + GetLoc("sPolygons"), MessageType.Info);

            if(estimatedPolygons > 100000)
            {
                EditorGUILayout.HelpBox(GetLoc("sHelpManyPolygons"), MessageType.Warning);
            }
        }

        static void CopyMaterialData(Material material, Material furMaterial)
        {
            if(material.HasProperty("_AsUnlit")) furMaterial.SetFloat("_AsUnlit", material.GetFloat("_AsUnlit"));
            if(material.HasProperty("_VertexLightStrength")) furMaterial.SetFloat("_VertexLightStrength", material.GetFloat("_VertexLightStrength"));
            if(material.HasProperty("_LightMinLimit")) furMaterial.SetFloat("_LightMinLimit", material.GetFloat("_LightMinLimit"));

            if(material.HasProperty("_MainTex")) furMaterial.SetTexture("_MainTex", material.GetTexture("_MainTex"));

            if(material.HasProperty("_UseShadow") && material.GetInt("_UseShadow") == 1)
            {
                if(material.HasProperty("_ShadowColor")) furMaterial.SetColor("_ShadowColor", material.GetColor("_ShadowColor"));
                if(material.HasProperty("_ShadowBorder")) furMaterial.SetFloat("_ShadowBorder", material.GetFloat("_ShadowBorder"));
                if(material.HasProperty("_ShadowBlur")) furMaterial.SetFloat("_ShadowBlur", material.GetFloat("_ShadowBlur"));
                if(material.HasProperty("_ShadowBorderColor")) furMaterial.SetColor("_ShadowBorderColor", material.GetColor("_ShadowBorderColor"));
                if(material.HasProperty("_ShadowBorderRange")) furMaterial.SetFloat("_ShadowBorderRange", material.GetFloat("_ShadowBorderRange"));
                if(material.HasProperty("_ShadowReceive")) furMaterial.SetFloat("_ShadowReceive", material.GetFloat("_ShadowReceive"));
            }
            else if(material.HasProperty("_UseShadow") && material.GetInt("_UseShadow") == 0)
            {
                if(material.HasProperty("_ShadowColor")) furMaterial.SetColor("_ShadowColor", Color.white);
            }

            if(material.HasProperty("_DistanceFadeColor")) furMaterial.SetColor("_DistanceFadeColor", material.GetColor("_DistanceFadeColor"));
            if(material.HasProperty("_DistanceFade")) furMaterial.SetVector("_DistanceFade", material.GetVector("_DistanceFade"));

            if(material.HasProperty("_FurVector")) furMaterial.SetFloat("_FurLength", material.GetVector("_FurVector").w);
            if(material.HasProperty("_FurGravity")) furMaterial.SetFloat("_FurGravity", material.GetFloat("_FurGravity"));
            if(material.HasProperty("_FurAO")) furMaterial.SetFloat("_FurAO", material.GetFloat("_FurAO"));
        }

        static void ShaderSelectGUI(Material furMaterial)
        {
            string[] shaderGUIDs = AssetDatabase.FindAssets("t:shader");
            List<Shader> shaders = new List<Shader>();
            List<string> shaderNames = new List<string>();
            int shaderID = 0;
            foreach(string shaderGUID in shaderGUIDs)
            {
                Shader shader = (Shader)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(shaderGUID), typeof(Shader));
                if(shader != null && !String.IsNullOrEmpty(shader.name) && shader.name.Contains("FurGenerator"))
                {
                    shaders.Add(shader);
                    shaderNames.Add(shader.name);
                    if(furMaterial.shader.name == shader.name) shaderID = shaderNames.Count - 1;
                }
            }

            string[] shaderNamesArray = new string[shaderNames.Count];
            for(int i = 0; i < shaderNames.Count; i++)
            {
                shaderNamesArray[i] = shaderNames[i];
            }

            int shaderID2 = EditorGUILayout.Popup("Shader", shaderID, shaderNamesArray);
            if(shaderID != shaderID2)
            {
                furMaterial.shader = shaders[shaderID2];
            }
        }

        static void FurMaterialGUI(Material[] furMaterials, string[] subMeshNames, GUIStyle saveButton, MeshRenderer furMeshRenderer, SkinnedMeshRenderer furSkinnedMeshRenderer)
        {
            for(int i = 0; i < furMaterials.Length; i++)
            {
                if(!edSet.SubMeshToggle[i]) continue;
                Material furMaterial = furMaterials[i];
                EditorGUILayout.LabelField(subMeshNames[i], EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                ShaderSelectGUI(furMaterial);
                if(furMaterial.HasProperty("_FurMap"))
                {
                    furMapW = (Texture2D)furMaterial.GetTexture("_FurMap");
                    EditorGUI.BeginChangeCheck();
                    furMapW = (Texture2D)EditorGUILayout.ObjectField(GetLoc("sMapTexture"), furMapW, typeof(Texture2D), false);
                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(furMaterial, "Modify Fur Map of Material");
                        furMaterial.SetTexture("_FurMap", furMapW);
                        EditorUtility.SetDirty(furMaterial);
                    }
                }
                if(furMaterial.HasProperty("_FurDensity"))
                {
                    furDensityW = furMaterial.GetFloat("_FurDensity");
                    EditorGUI.BeginChangeCheck();
                    furDensityW = EditorGUILayout.FloatField(GetLoc("sDensity"), furDensityW);
                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(furMaterial, "Modify Density of Material");
                        furMaterial.SetFloat("_FurDensity", furDensityW);
                        EditorUtility.SetDirty(furMaterial);
                    }
                }
                DrawLine();
                if(furMaterial.HasProperty("_FurLength"))
                {
                    furLengthW = furMaterial.GetFloat("_FurLength");
                    EditorGUI.BeginChangeCheck();
                    furLengthW = EditorGUILayout.FloatField(GetLoc("sLength"), furLengthW);
                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(furMaterial, "Modify Length of Material");
                        furMaterial.SetFloat("_FurLength", furLengthW);
                        EditorUtility.SetDirty(furMaterial);
                    }
                }
                if(furMaterial.HasProperty("_FurGravity"))
                {
                    furGravityW = furMaterial.GetFloat("_FurGravity");
                    EditorGUI.BeginChangeCheck();
                    furGravityW = EditorGUILayout.Slider(GetLoc("sGravity"), furGravityW, 0.0f, 1.0f);
                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(furMaterial, "Modify Gravity of Material");
                        furMaterial.SetFloat("_FurGravity", furGravityW);
                        EditorUtility.SetDirty(furMaterial);
                    }
                }
                if(furMaterial.HasProperty("_FurSoftness"))
                {
                    furSoftnessW = furMaterial.GetFloat("_FurSoftness");
                    EditorGUI.BeginChangeCheck();
                    furSoftnessW = EditorGUILayout.Slider(GetLoc("sSoftness"), furSoftnessW, 0.0f, 1.0f);
                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(furMaterial, "Modify Softness of Material");
                        furMaterial.SetFloat("_FurSoftness", furSoftnessW);
                        EditorUtility.SetDirty(furMaterial);
                    }
                }
                if(furMaterial.HasProperty("_FurAO"))
                {
                    furAOW = furMaterial.GetFloat("_FurAO");
                    EditorGUI.BeginChangeCheck();
                    furAOW = EditorGUILayout.Slider(GetLoc("sAO"), furAOW, 0.0f, 1.0f);
                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(furMaterial, "Modify AO of Material");
                        furMaterial.SetFloat("_FurAO", furAOW);
                        EditorUtility.SetDirty(furMaterial);
                    }
                }

                if(AssetDatabase.Contains(furMaterial))
                {
                    GUI.enabled = false;
                    GUILayout.Button(GetLoc("sAlreadySaved"));
                    GUI.enabled = true;
                }
                else if(GUILayout.Button(GetLoc("sSaveMaterial"), saveButton))
                {
                    string path = EditorUtility.SaveFilePanel(GetLoc("sSaveMaterial"), "Assets/", furMaterial.name, "mat");
                    if(!String.IsNullOrEmpty(path))
                    {
                        AssetDatabase.CreateAsset(furMaterial, FileUtil.GetProjectRelativePath(path));
                        if(furMeshRenderer != null)
                        {
                            furMeshRenderer.sharedMaterials[i] = furMaterial;
                        }
                        else if(furSkinnedMeshRenderer != null)
                        {
                            furSkinnedMeshRenderer.sharedMaterials[i] = furMaterial;
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        static IEnumerator GetLatestVersionInfo()
        {
            using(UnityWebRequest webRequest = UnityWebRequest.Get(versionInfoURL))
            {
                #if UNITY_2017_2_OR_NEWER
                    yield return webRequest.SendWebRequest();
                #else
                    yield return webRequest.Send();
                #endif
                #if UNITY_2020_2_OR_NEWER
                if (webRequest.result != UnityWebRequest.Result.ConnectionError)
                #else
                if (!webRequest.isNetworkError)
                #endif
                {
                    StreamWriter sw = new StreamWriter(versionInfoTempPath,false);
                    sw.Write(webRequest.downloadHandler.text);
                    sw.Close();
                }
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------
        // Mesh Generator
        static bool CopyUVs(int[] sharedIndices, int mi, Vector2[] uvs, List<int> skipList, ref List<Vector2> uvlist, string message = "Copying UV...")
        {
            int[] index = new int[3];
            Vector2 uvc;
            float lpB = 0.0f;
            float lpA = 0.0f;
            Vector2[] mixUV = new Vector2[3];
            for(int i = 0; i < sharedIndices.Length / 3; ++i)
            {
                index[0] = sharedIndices[i * 3 + 0];
                index[1] = sharedIndices[i * 3 + 1];
                index[2] = sharedIndices[i * 3 + 2];
                uvc = (uvs[index[0]] + uvs[index[1]] + uvs[index[2]]) * 0.333333333333f;

                for(int fl = 0; fl < edSet.m[mi]._FurLayerNum; fl++)
                {
                    if(skipList.Contains(i*edSet.m[mi]._FurLayerNum+fl)) continue;
                    lpB = (float)fl/(float)edSet.m[mi]._FurLayerNum;
                    lpA = 1.0f - lpB;
                    mixUV[0] = uvs[index[0]] * lpA + uvc * lpB;
                    mixUV[1] = uvs[index[1]] * lpA + uvc * lpB;
                    mixUV[2] = uvs[index[2]] * lpA + uvc * lpB;

                    for(int fj = 0; fj < edSet.m[mi]._FurJointNum; fj++)
                    {
                        uvlist.Add(mixUV[0]);
                        uvlist.Add(mixUV[0]);
                        uvlist.Add(mixUV[1]);
                        uvlist.Add(mixUV[1]);
                        uvlist.Add(mixUV[2]);
                        uvlist.Add(mixUV[2]);
                        uvlist.Add(mixUV[0]);
                        uvlist.Add(mixUV[0]);
                    }
                }
                if(EditorUtility.DisplayCancelableProgressBar(windowName, message, (float)i / (float)sharedIndices.Length * 3.0f))
                {
                    EditorUtility.ClearProgressBar();
                    return false;
                }
            }

            EditorUtility.ClearProgressBar();
            return true;
        }

        static bool CopyBoneWeights(int[] sharedIndices, int mi, BoneWeight[] meshBoneWeights, List<int> skipList, ref List<BoneWeight> boneWeights, string message = "Copying bone weight...")
        {
            int[] index = new int[3];
            float[] midBoneWeights = new float[12];
            int[] midBoneIndexs = new int[12];
            float lpB = 0.0f;
            float lpA = 0.0f;
            BoneWeight[] mixBoneWeight = new BoneWeight[3];
            float[] mixBoneWeights2 = new float[4];
            int[] mixBoneIndexs = new int[4];
            float weightMix = 0.0f;
            for(int i = 0; i < sharedIndices.Length / 3; ++i)
            {
                index[0] = sharedIndices[i * 3 + 0];
                index[1] = sharedIndices[i * 3 + 1];
                index[2] = sharedIndices[i * 3 + 2];

                midBoneWeights[0] = meshBoneWeights[index[0]].weight0 * 0.333333333333f;
                midBoneWeights[1] = meshBoneWeights[index[0]].weight1 * 0.333333333333f;
                midBoneWeights[2] = meshBoneWeights[index[0]].weight2 * 0.333333333333f;
                midBoneWeights[3] = meshBoneWeights[index[0]].weight3 * 0.333333333333f;
                midBoneWeights[4] = meshBoneWeights[index[1]].weight0 * 0.333333333333f;
                midBoneWeights[5] = meshBoneWeights[index[1]].weight1 * 0.333333333333f;
                midBoneWeights[6] = meshBoneWeights[index[1]].weight2 * 0.333333333333f;
                midBoneWeights[7] = meshBoneWeights[index[1]].weight3 * 0.333333333333f;
                midBoneWeights[8] = meshBoneWeights[index[2]].weight0 * 0.333333333333f;
                midBoneWeights[9] = meshBoneWeights[index[2]].weight1 * 0.333333333333f;
                midBoneWeights[10] = meshBoneWeights[index[2]].weight2 * 0.333333333333f;
                midBoneWeights[11] = meshBoneWeights[index[2]].weight3 * 0.333333333333f;

                midBoneIndexs[0] = meshBoneWeights[index[0]].boneIndex0;
                midBoneIndexs[1] = meshBoneWeights[index[0]].boneIndex1;
                midBoneIndexs[2] = meshBoneWeights[index[0]].boneIndex2;
                midBoneIndexs[3] = meshBoneWeights[index[0]].boneIndex3;
                midBoneIndexs[4] = meshBoneWeights[index[1]].boneIndex0;
                midBoneIndexs[5] = meshBoneWeights[index[1]].boneIndex1;
                midBoneIndexs[6] = meshBoneWeights[index[1]].boneIndex2;
                midBoneIndexs[7] = meshBoneWeights[index[1]].boneIndex3;
                midBoneIndexs[8] = meshBoneWeights[index[2]].boneIndex0;
                midBoneIndexs[9] = meshBoneWeights[index[2]].boneIndex1;
                midBoneIndexs[10] = meshBoneWeights[index[2]].boneIndex2;
                midBoneIndexs[11] = meshBoneWeights[index[2]].boneIndex3;

                for(int fl = 0; fl < edSet.m[mi]._FurLayerNum; fl++)
                {
                    if(skipList.Contains(i*edSet.m[mi]._FurLayerNum+fl)) continue;
                    lpB = (float)fl/(float)edSet.m[mi]._FurLayerNum;
                    lpA = 1.0f - lpB;

                    mixBoneWeight[0] = meshBoneWeights[index[0]];
                    mixBoneWeight[1] = meshBoneWeights[index[1]];
                    mixBoneWeight[2] = meshBoneWeights[index[2]];

                    for(int mbw = 0; mbw < 3; mbw++)
                    {
                        mixBoneWeights2[0] = mixBoneWeight[mbw].weight0 * lpA;
                        mixBoneWeights2[1] = mixBoneWeight[mbw].weight1 * lpA;
                        mixBoneWeights2[2] = mixBoneWeight[mbw].weight2 * lpA;
                        mixBoneWeights2[3] = mixBoneWeight[mbw].weight3 * lpA;

                        mixBoneIndexs[0] = mixBoneWeight[mbw].boneIndex0;
                        mixBoneIndexs[1] = mixBoneWeight[mbw].boneIndex1;
                        mixBoneIndexs[2] = mixBoneWeight[mbw].boneIndex2;
                        mixBoneIndexs[3] = mixBoneWeight[mbw].boneIndex3;

                        for(int bw = 0; bw < 4; bw++)
                        {
                            for(int bw2 = 0; bw2 < 12; bw2++)
                            {
                                if(mixBoneIndexs[bw] == midBoneIndexs[bw2]) mixBoneWeights2[bw] += midBoneWeights[bw2] * lpB;
                            }
                        }

                        weightMix = (mixBoneWeight[mbw].weight0 + mixBoneWeight[mbw].weight1 + mixBoneWeight[mbw].weight2 + mixBoneWeight[mbw].weight3) / (mixBoneWeights2[0] + mixBoneWeights2[1] + mixBoneWeights2[2] + mixBoneWeights2[3]);

                        mixBoneWeight[mbw].weight0 = mixBoneWeights2[0] * weightMix;
                        mixBoneWeight[mbw].weight1 = mixBoneWeights2[1] * weightMix;
                        mixBoneWeight[mbw].weight2 = mixBoneWeights2[2] * weightMix;
                        mixBoneWeight[mbw].weight3 = mixBoneWeights2[3] * weightMix;
                    }

                    for(int fj = 0; fj < edSet.m[mi]._FurJointNum; fj++)
                    {
                        boneWeights.Add(mixBoneWeight[0]);
                        boneWeights.Add(mixBoneWeight[0]);
                        boneWeights.Add(mixBoneWeight[1]);
                        boneWeights.Add(mixBoneWeight[1]);
                        boneWeights.Add(mixBoneWeight[2]);
                        boneWeights.Add(mixBoneWeight[2]);
                        boneWeights.Add(mixBoneWeight[0]);
                        boneWeights.Add(mixBoneWeight[0]);
                    }
                }
                if(EditorUtility.DisplayCancelableProgressBar(windowName, message, (float)i / (float)sharedIndices.Length * 3.0f))
                {
                    EditorUtility.ClearProgressBar();
                    return false;
                }
            }

            EditorUtility.ClearProgressBar();
            return true;
        }

        static void GetReadableTexture(ref Texture2D tex)
        {
            #if UNITY_2018_3_OR_NEWER
            if(!tex.isReadable)
            #endif
            {
                RenderTexture bufRT = RenderTexture.active;
                RenderTexture texR = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(tex, texR);
                RenderTexture.active = texR;
                tex = new Texture2D(texR.width, texR.height);
                tex.ReadPixels(new Rect(0, 0, texR.width, texR.height), 0, 0);
                tex.Apply();
                RenderTexture.active = bufRT;
                RenderTexture.ReleaseTemporary(texR);
            }
        }

        static void GenerateFurFromMesh(lilFurEditorSetting edSet)
        {
            // Check
            if(edSet.gameObject == null) return;
            MeshFilter meshFilter = edSet.gameObject.GetComponent<MeshFilter>();
            SkinnedMeshRenderer skinnedMeshRenderer = edSet.gameObject.GetComponent<SkinnedMeshRenderer>();
            Mesh sharedMesh;
            if(meshFilter == null && skinnedMeshRenderer == null)
            {
                return;
            }
            else if(meshFilter != null)
            {
                sharedMesh = meshFilter.sharedMesh;
            }
            else
            {
                sharedMesh = skinnedMeshRenderer.sharedMesh;
            }

            bool hasVertices = sharedMesh.vertices != null && sharedMesh.vertices.Length > 2;
            bool hasNormals = sharedMesh.normals != null && sharedMesh.normals.Length > 2;
            bool hasTangents = sharedMesh.tangents != null && sharedMesh.tangents.Length > 2;
            bool hasUV0 = sharedMesh.uv != null && sharedMesh.uv.Length > 2;
            bool hasUV1 = sharedMesh.uv2 != null && sharedMesh.uv2.Length > 2;
            bool hasUV2 = sharedMesh.uv3 != null && sharedMesh.uv3.Length > 2;
            bool hasUV3 = sharedMesh.uv4 != null && sharedMesh.uv4.Length > 2;
            bool hasBoneWeights = sharedMesh.boneWeights != null && sharedMesh.boneWeights.Length > 2;

            if(!hasVertices)
            {
                EditorUtility.DisplayDialog(windowName, GetLoc("sNoVertices"), GetLoc("sCancel"));
                return;
            }

            if(!hasUV0)
            {
                EditorUtility.DisplayDialog(windowName, GetLoc("sNoUV"), GetLoc("sCancel"));
                return;
            }

            if(!hasNormals)
            {
                EditorUtility.DisplayDialog(windowName, GetLoc("sNoNormals"), GetLoc("sCancel"));
                return;
            }

            if(!hasTangents)
            {
                EditorUtility.DisplayDialog(windowName, GetLoc("sNoTangents"), GetLoc("sCancel"));
                return;
            }

            // Messages
            string copyingUV1 = "Copying UV1...";
            string copyingUV2 = "Copying UV2...";
            string copyingUV3 = "Copying UV3...";
            string copyingBW = "Copying bone weight...";

            Vector2 bodyUV4 = new Vector2(0.0f, -1.0f);

            // Base data
            var indices = new List<int>[sharedMesh.subMeshCount];
            var vertices = new List<Vector3>(sharedMesh.vertices);
            var normals = new List<Vector3>(sharedMesh.normals);
            var tangents = new List<Vector4>(sharedMesh.tangents);
            var uv0 = new List<Vector2>(sharedMesh.uv);
            var uv1 = new List<Vector2>();
            var uv2 = new List<Vector2>();
            var uv3 = new List<Vector2>();
            var boneWeights = new List<BoneWeight>();
            if(hasUV1) uv1 = new List<Vector2>(sharedMesh.uv2);
            if(hasUV2) uv2 = new List<Vector2>(sharedMesh.uv3);
            if(hasUV3) uv3 = new List<Vector2>(sharedMesh.uv4);
            if(hasBoneWeights) boneWeights = new List<BoneWeight>(sharedMesh.boneWeights);

            // Fur data
            var uv4 = new List<Vector2>(Enumerable.Repeat(bodyUV4, sharedMesh.vertices.Length).ToArray());
            var colors = new List<Color>(Enumerable.Repeat(Color.black, sharedMesh.vertices.Length).ToArray());

            // Blend shapes
            var deltaVertices = new Vector3[sharedMesh.blendShapeCount][];
            var deltaNormals = new Vector3[sharedMesh.blendShapeCount][];
            var deltaTangents = new Vector3[sharedMesh.blendShapeCount][];
            var newDeltaVertices = new List<Vector3>[sharedMesh.blendShapeCount];
            var newDeltaNormals = new List<Vector3>[sharedMesh.blendShapeCount];
            var newDeltaTangents = new List<Vector3>[sharedMesh.blendShapeCount];
            for(int bi = 0; bi < sharedMesh.blendShapeCount; bi++)
            {
                deltaVertices[bi] = new Vector3[sharedMesh.vertices.Length];
                deltaNormals[bi] = new Vector3[sharedMesh.vertices.Length];
                deltaTangents[bi] = new Vector3[sharedMesh.vertices.Length];
                sharedMesh.GetBlendShapeFrameVertices(bi, 0, deltaVertices[bi], deltaNormals[bi], deltaTangents[bi]);
                newDeltaVertices[bi] = new List<Vector3>(deltaVertices[bi]);
                newDeltaNormals[bi] = new List<Vector3>(deltaNormals[bi]);
                newDeltaTangents[bi] = new List<Vector3>(deltaTangents[bi]);
            }

            Texture2D furLengthMask;
            Texture2D furVectorTex;
            Texture2D furSoftnessMask;
            Texture2D furDensityMask;
            int polys = 0;
            int[] index = new int[3];
            Vector3 vpc;
            Vector3 ndc;
            Vector4 tdc;
            Vector2 uv0c;
            float lpB = 0.0f;
            float lpA = 0.0f;
            Vector2[] mixUV0 = new Vector2[3];
            Vector3[] mixPos = new Vector3[3];
            Vector3[] mixNormal = new Vector3[3];
            Vector4[] mixTangent = new Vector4[3];
            float[] mixFurLength = new float[3];
            float[] uvDist = new float[4];
            Color[] mixFurVectorColor = new Color[3];
            Vector3[] mixFurNormal = new Vector3[3];
            Vector3 randomVector = Vector3.zero;
            Vector3[] mixFurVector = new Vector3[3];
            float[] mixFurSoftness = new float[3];
            Color[] mixColor = new Color[3];
            float uvyi = 0.0f;
            float uvyo = 0.0f;
            int startIndex = 0;
            int i3 = 0;

            for(int mi = 0; mi < sharedMesh.subMeshCount; mi++)
            {
                // Base
                int[] sharedIndices = sharedMesh.GetIndices(mi);
                polys = sharedIndices.Length / 3;
                indices[mi] = new List<int>(sharedIndices);
                var skipList = new List<int>();
                if(!edSet.SubMeshToggle[mi]) continue;

                // Initialize Texture
                furLengthMask = edSet.m[mi]._FurLengthMask;
                furVectorTex = edSet.m[mi]._FurVectorTex;
                furSoftnessMask = edSet.m[mi]._FurSoftnessMask;
                furDensityMask = edSet.m[mi]._FurDensityMask;
                if(furLengthMask == null) furLengthMask = Texture2D.whiteTexture;
                if(furSoftnessMask == null) furSoftnessMask = Texture2D.whiteTexture;
                if(furDensityMask == null) furDensityMask = Texture2D.whiteTexture;
                if(furVectorTex == null)
                {
                    #if UNITY_2019_1_OR_NEWER
                        furVectorTex = Texture2D.normalTexture;
                    #else
                        Texture2D normalTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
                        normalTexture.SetPixel(0, 0, new Color(0.5f,0.5f,1.0f,1.0f));
                        normalTexture.Apply();
                        furVectorTex = normalTexture;
                    #endif
                }
                GetReadableTexture(ref furLengthMask);
                GetReadableTexture(ref furVectorTex);
                GetReadableTexture(ref furSoftnessMask);
                GetReadableTexture(ref furDensityMask);

                // Generate fur
                for(int i = 0; i < polys; ++i)
                {
                    index[0] = sharedIndices[i * 3 + 0];
                    index[1] = sharedIndices[i * 3 + 1];
                    index[2] = sharedIndices[i * 3 + 2];
                    vpc  = (sharedMesh.vertices[index[0]]  +sharedMesh.vertices[index[1]]    +sharedMesh.vertices[index[2]])   * 0.333333333333f;
                    ndc  = (sharedMesh.normals[index[0]]   +sharedMesh.normals[index[1]]     +sharedMesh.normals[index[2]])    * 0.333333333333f;
                    tdc  = (sharedMesh.tangents[index[0]]  +sharedMesh.tangents[index[1]]    +sharedMesh.tangents[index[2]])   * 0.333333333333f;
                    uv0c = (sharedMesh.uv[index[0]]        +sharedMesh.uv[index[1]]          +sharedMesh.uv[index[2]])         * 0.333333333333f;

                    for(int fl = 0; fl < edSet.m[mi]._FurLayerNum; fl++)
                    {
                        lpB = (float)fl/(float)edSet.m[mi]._FurLayerNum;
                        lpA = 1.0f - lpB;

                        // Base data
                        mixUV0[0] = sharedMesh.uv[index[0]] * lpA + uv0c * lpB;
                        mixUV0[1] = sharedMesh.uv[index[1]] * lpA + uv0c * lpB;
                        mixUV0[2] = sharedMesh.uv[index[2]] * lpA + uv0c * lpB;
                        mixFurLength[0] = furLengthMask.GetPixelBilinear(mixUV0[0].x, mixUV0[0].y).r;
                        mixFurLength[1] = furLengthMask.GetPixelBilinear(mixUV0[1].x, mixUV0[1].y).r;
                        mixFurLength[2] = furLengthMask.GetPixelBilinear(mixUV0[2].x, mixUV0[2].y).r;

                        if(mixFurLength[0] < 0.01f && mixFurLength[1] < 0.01f && mixFurLength[2] < 0.01f)
                        {
                            skipList.Add(i*edSet.m[mi]._FurLayerNum+fl);
                            continue;
                        }

                        mixPos[0] = sharedMesh.vertices[index[0]] * lpA + vpc * lpB;
                        mixPos[1] = sharedMesh.vertices[index[1]] * lpA + vpc * lpB;
                        mixPos[2] = sharedMesh.vertices[index[2]] * lpA + vpc * lpB;
                        mixNormal[0] = sharedMesh.normals[index[0]] * lpA + ndc * lpB;
                        mixNormal[1] = sharedMesh.normals[index[1]] * lpA + ndc * lpB;
                        mixNormal[2] = sharedMesh.normals[index[2]] * lpA + ndc * lpB;
                        mixTangent[0] = sharedMesh.tangents[index[0]] * lpA + tdc * lpB;
                        mixTangent[1] = sharedMesh.tangents[index[1]] * lpA + tdc * lpB;
                        mixTangent[2] = sharedMesh.tangents[index[2]] * lpA + tdc * lpB;

                        // Fur Data
                        uvDist[0] = UnityEngine.Random.Range(-0.5f, 0.5f);
                        uvDist[1] = uvDist[0] + Vector3.Distance(mixPos[0], mixPos[1]) * 10.0f * furDensityMask.GetPixelBilinear(mixUV0[0].x, mixUV0[0].y).r;
                        uvDist[2] = uvDist[1] + Vector3.Distance(mixPos[1], mixPos[2]) * 10.0f * furDensityMask.GetPixelBilinear(mixUV0[1].x, mixUV0[1].y).r;
                        uvDist[3] = uvDist[2] + Vector3.Distance(mixPos[2], mixPos[0]) * 10.0f * furDensityMask.GetPixelBilinear(mixUV0[2].x, mixUV0[2].y).r;

                        mixFurVectorColor[0] = furVectorTex.GetPixelBilinear(mixUV0[0].x, mixUV0[0].y);
                        mixFurVectorColor[1] = furVectorTex.GetPixelBilinear(mixUV0[1].x, mixUV0[1].y);
                        mixFurVectorColor[2] = furVectorTex.GetPixelBilinear(mixUV0[2].x, mixUV0[2].y);
                        mixFurNormal[0] = UnpackNormal(mixFurVectorColor[0], edSet.m[mi]._FurVectorScale);
                        mixFurNormal[1] = UnpackNormal(mixFurVectorColor[1], edSet.m[mi]._FurVectorScale);
                        mixFurNormal[2] = UnpackNormal(mixFurVectorColor[2], edSet.m[mi]._FurVectorScale);
                        mixFurNormal[0].Set(mixFurNormal[0].x + edSet.m[mi]._FurVector.x, mixFurNormal[0].y + edSet.m[mi]._FurVector.y, mixFurNormal[0].z * edSet.m[mi]._FurVector.z);
                        mixFurNormal[1].Set(mixFurNormal[1].x + edSet.m[mi]._FurVector.x, mixFurNormal[1].y + edSet.m[mi]._FurVector.y, mixFurNormal[1].z * edSet.m[mi]._FurVector.z);
                        mixFurNormal[2].Set(mixFurNormal[2].x + edSet.m[mi]._FurVector.x, mixFurNormal[2].y + edSet.m[mi]._FurVector.y, mixFurNormal[2].z * edSet.m[mi]._FurVector.z);
                        randomVector = new Vector3(UnityEngine.Random.Range(-edSet.m[mi]._RandomStrength, edSet.m[mi]._RandomStrength), UnityEngine.Random.Range(-edSet.m[mi]._RandomStrength, edSet.m[mi]._RandomStrength), UnityEngine.Random.Range(-edSet.m[mi]._RandomStrength, edSet.m[mi]._RandomStrength));
                        mixFurVector[0] = Vector3.Normalize(mixFurNormal[0] + randomVector) * mixFurLength[0];
                        mixFurVector[1] = Vector3.Normalize(mixFurNormal[1] + randomVector) * mixFurLength[1];
                        mixFurVector[2] = Vector3.Normalize(mixFurNormal[2] + randomVector) * mixFurLength[2];

                        mixFurSoftness[0] = furSoftnessMask.GetPixelBilinear(mixUV0[0].x, mixUV0[0].y).r;
                        mixFurSoftness[1] = furSoftnessMask.GetPixelBilinear(mixUV0[1].x, mixUV0[1].y).r;
                        mixFurSoftness[2] = furSoftnessMask.GetPixelBilinear(mixUV0[2].x, mixUV0[2].y).r;

                        mixColor[0] = new Color(mixFurVector[0].x, mixFurVector[0].y, mixFurVector[0].z, mixFurSoftness[0]);
                        mixColor[1] = new Color(mixFurVector[1].x, mixFurVector[1].y, mixFurVector[1].z, mixFurSoftness[1]);
                        mixColor[2] = new Color(mixFurVector[2].x, mixFurVector[2].y, mixFurVector[2].z, mixFurSoftness[2]);

                        for(int fj = 0; fj < edSet.m[mi]._FurJointNum; fj++)
                        {
                            uvyi = (float)(fj+0) / (float)edSet.m[mi]._FurJointNum;
                            uvyo = (float)(fj+1) / (float)edSet.m[mi]._FurJointNum;

                            startIndex = vertices.Count;
                            indices[mi].Add(startIndex+0);
                            indices[mi].Add(startIndex+1);
                            indices[mi].Add(startIndex+2);
                            indices[mi].Add(startIndex+1);
                            indices[mi].Add(startIndex+2);
                            indices[mi].Add(startIndex+3);

                            indices[mi].Add(startIndex+2);
                            indices[mi].Add(startIndex+3);
                            indices[mi].Add(startIndex+4);
                            indices[mi].Add(startIndex+3);
                            indices[mi].Add(startIndex+4);
                            indices[mi].Add(startIndex+5);

                            indices[mi].Add(startIndex+4);
                            indices[mi].Add(startIndex+5);
                            indices[mi].Add(startIndex+6);
                            indices[mi].Add(startIndex+5);
                            indices[mi].Add(startIndex+6);
                            indices[mi].Add(startIndex+7);

                            for(int i2 = 0; i2 < 4; i2++)
                            {
                                i3 = i2 == 3 ? 0 : i2;

                                vertices.Add(mixPos[i3]);
                                normals.Add(mixNormal[i3]);
                                tangents.Add(mixTangent[i3]);
                                uv0.Add(mixUV0[i3]);
                                uv4.Add(new Vector2(uvDist[i2], uvyi));
                                colors.Add(mixColor[i3]);

                                vertices.Add(mixPos[i3]);
                                normals.Add(mixNormal[i3]);
                                tangents.Add(mixTangent[i3]);
                                uv0.Add(mixUV0[i3]);
                                uv4.Add(new Vector2(uvDist[i2], uvyo));
                                colors.Add(mixColor[i3]);

                                for(int bi = 0; bi < sharedMesh.blendShapeCount; bi++)
                                {
                                    newDeltaVertices[bi].Add(deltaVertices[bi][index[i3]]);
                                    newDeltaVertices[bi].Add(deltaVertices[bi][index[i3]]);
                                    newDeltaNormals[bi].Add(deltaNormals[bi][index[i3]]);
                                    newDeltaNormals[bi].Add(deltaNormals[bi][index[i3]]);
                                    newDeltaTangents[bi].Add(deltaTangents[bi][index[i3]]);
                                    newDeltaTangents[bi].Add(deltaTangents[bi][index[i3]]);
                                }
                            }
                        }
                    }
                    if(EditorUtility.DisplayCancelableProgressBar(windowName, "Generating Mesh...", (float)i / (float)sharedIndices.Length * 3.0f))
                    {
                        EditorUtility.ClearProgressBar();
                        return;
                    }
                }

                if(hasUV1 && !CopyUVs(sharedIndices, mi, sharedMesh.uv2, skipList, ref uv1, copyingUV1)) return;
                if(hasUV2 && !CopyUVs(sharedIndices, mi, sharedMesh.uv3, skipList, ref uv2, copyingUV2)) return;
                if(hasUV3 && !CopyUVs(sharedIndices, mi, sharedMesh.uv4, skipList, ref uv3, copyingUV3)) return;
                if(hasBoneWeights && !CopyBoneWeights(sharedIndices, mi, sharedMesh.boneWeights, skipList, ref boneWeights, copyingBW)) return;
            }

            // Save mesh
            var mesh = new Mesh();
            mesh.name = sharedMesh.name + " (FurGenerator)";
            #if UNITY_2017_3_OR_NEWER
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            #endif

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTangents(tangents);
            mesh.SetUVs(0, uv0);
            if(hasUV1) mesh.SetUVs(1, uv1);
            if(hasUV2) mesh.SetUVs(2, uv2);
            if(hasUV3) mesh.SetUVs(3, uv3);
            if(hasBoneWeights)
            {
                mesh.boneWeights = boneWeights.ToArray();
                mesh.bindposes = sharedMesh.bindposes;
            }
            mesh.SetUVs(4, uv4);
            mesh.SetColors(colors);

            // Blend shapes
            for(int bi = 0; bi < sharedMesh.blendShapeCount; bi++)
            {
                mesh.AddBlendShapeFrame(sharedMesh.GetBlendShapeName(bi), 100, newDeltaVertices[bi].ToArray(), newDeltaNormals[bi].ToArray(), newDeltaTangents[bi].ToArray());
            }

            // Set sub mesh
            mesh.subMeshCount = sharedMesh.subMeshCount;
            for(int mi = 0; mi < sharedMesh.subMeshCount; mi++)
            {
                int[] indicesArray = indices[mi].ToArray();
                mesh.SetIndices(indicesArray, MeshTopology.Triangles, mi);
            }

            mesh.RecalculateBounds();

            GameObject furObject = null;
            if(edSet.gameObject.transform.parent != null)
            {
                for(int i = 0; i < edSet.gameObject.transform.parent.childCount; i++)
                {
                    GameObject childObject = edSet.gameObject.transform.parent.GetChild(i).gameObject;
                    if(childObject.name.Contains(edSet.gameObject.name + " (FurGenerator)"))
                    {
                        furObject = childObject;
                        break;
                    }
                }
            }
            else
            {
                furObject = GameObject.Find(edSet.gameObject.name + " (FurGenerator)");
            }
            if(furObject == null)
            {
                furObject = Instantiate(edSet.gameObject);
                furObject.name = edSet.gameObject.name + " (FurGenerator)";
                furObject.transform.parent = edSet.gameObject.transform.parent;
            }

            if(meshFilter != null)
            {
                MeshFilter furMeshFilter = furObject.GetComponent<MeshFilter>();
                furMeshFilter.mesh = mesh;
            }
            else
            {
                SkinnedMeshRenderer furSkinnedMeshRenderer = furObject.GetComponent<SkinnedMeshRenderer>();
                furSkinnedMeshRenderer.sharedMesh = mesh;
            }
        }

        static Vector3 UnpackNormal(Color col, float scale)
        {
            col.a *= col.r;
            Vector3 normal;
            normal.x = col.a * 2.0f - 1.0f;
            normal.y = col.g * 2.0f - 1.0f;
            normal.z = Mathf.Max(1.0e-16f, Mathf.Sqrt(1.0f - Mathf.Clamp01(normal.x * normal.x + normal.y * normal.y)));
            normal.x *= scale;
            normal.y *= scale;
            return normal;
        }
    }

    public class CoroutineHandler : MonoBehaviour
    {
        static protected CoroutineHandler m_Instance;
        static public CoroutineHandler instance
        {
            get
            {
                if(m_Instance == null)
                {
                    GameObject o = new GameObject("CoroutineHandler");
                    o.hideFlags = HideFlags.HideAndDontSave;
                    m_Instance = o.AddComponent<CoroutineHandler>();
                }

                return m_Instance;
            }
        }

        public void OnDisable()
        {
            if(m_Instance)
                Destroy(m_Instance.gameObject);
        }

        static public Coroutine StartStaticCoroutine(IEnumerator coroutine)
        {
            return instance.StartCoroutine(coroutine);
        }
    }
}
#endif