#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace lilFurGenerator
{
    //------------------------------------------------------------------------------------------------------------------------------
    // PropertyDrawer
    public class lilHDRDrawer : MaterialPropertyDrawer
    {
        // Gamma HDR
        // [lilHDR]
        public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
        {
            float xMax = position.xMax;
            position.width = Mathf.Min(position.width, EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth);
            Color value = prop.colorValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            #if UNITY_2018_1_OR_NEWER
                value = EditorGUI.ColorField(position, new GUIContent(label), value, true, true, true);
            #else
                value = EditorGUI.ColorField(position, new GUIContent(label), value, true, true, true, null);
            #endif
            EditorGUI.showMixedValue = false;

            if(EditorGUI.EndChangeCheck())
            {
                prop.colorValue = value;
            }

            #if UNITY_2019_1_OR_NEWER
                // Hex
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = prop.hasMixedValue;
                float intensity = value.maxColorComponent > 1.0f ? value.maxColorComponent : 1.0f;
                Color value2 = new Color(value.r / intensity, value.g / intensity, value.b / intensity, 1.0f);
                string hex = ColorUtility.ToHtmlStringRGB(value2);
                position.x += position.width + 4.0f;
                position.width = Mathf.Max(50.0f, xMax - position.x);
                hex = "#" + EditorGUI.TextField(position, GUIContent.none, hex);
                if(EditorGUI.EndChangeCheck())
                {
                    if(!ColorUtility.TryParseHtmlString(hex, out value2)) return;
                    value.r = value2.r * intensity;
                    value.g = value2.g * intensity;
                    value.b = value2.b * intensity;
                    prop.colorValue = value;
                }
                EditorGUI.showMixedValue = false;
            #endif
        }
    }

    public class lilToggleDrawer : MaterialPropertyDrawer
    {
        // Toggle without setting shader keyword
        // [lilToggle]
        public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
        {
            bool value = (prop.floatValue != 0.0f);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            value = EditorGUI.Toggle(position, label, value);
            EditorGUI.showMixedValue = false;

            if(EditorGUI.EndChangeCheck())
            {
                prop.floatValue = value ? 1.0f : 0.0f;
            }
        }
    }

    public class lilVec3Drawer : MaterialPropertyDrawer
    {
        // Draw vector4 as vector3
        // [lilVec3]
        public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
        {
            Vector3 vec = new Vector3(prop.vectorValue.x, prop.vectorValue.y, prop.vectorValue.z);
            float unused = prop.vectorValue.w;

            EditorGUIUtility.wideMode = true;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            vec = EditorGUI.Vector3Field(position, label, vec);
            EditorGUI.showMixedValue = false;

            if(EditorGUI.EndChangeCheck())
            {
                prop.vectorValue = new Vector4(vec.x, vec.y, vec.z, unused);
            }
        }
    }

    public class lilVec3FloatDrawer : MaterialPropertyDrawer
    {
        // Draw vector4 as vector3 and float
        // [lilVec3Float]
        public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
        {
            string[] labels = label.Split('|');
            Vector3 vec = new Vector3(prop.vectorValue.x, prop.vectorValue.y, prop.vectorValue.z);
            float length = prop.vectorValue.w;

            EditorGUIUtility.wideMode = true;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            vec = EditorGUI.Vector3Field(position, labels[0], vec);
            length = EditorGUI.FloatField(EditorGUILayout.GetControlRect(), labels[1], length);
            EditorGUI.showMixedValue = false;

            if(EditorGUI.EndChangeCheck())
            {
                prop.vectorValue = new Vector4(vec.x, vec.y, vec.z, length);
            }
        }
    }

    public class lilEnum : MaterialPropertyDrawer
    {
        // [lilEnum]
        public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
        {
            string[] labels = label.Split('|');
            string[] enums = new string[labels.Length-1];
            Array.Copy(labels, 1, enums, 0, labels.Length-1);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            float value = (float)EditorGUI.Popup(position, labels[0], (int)prop.floatValue, enums);
            EditorGUI.showMixedValue = false;

            if(EditorGUI.EndChangeCheck())
            {
                prop.floatValue = value;
            }
        }
    }

    public class lil3Param : MaterialPropertyDrawer
    {
        // [lil3Param]
        public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
        {
            string[] labels = label.Split('|');
            float param1 = prop.vectorValue.x;
            float param2 = prop.vectorValue.y;
            float param3 = prop.vectorValue.z;
            float unused = prop.vectorValue.w;

            EditorGUI.indentLevel++;
            Rect position1 = EditorGUILayout.GetControlRect();
            Rect position2 = EditorGUILayout.GetControlRect();

            EditorGUI.BeginChangeCheck();
            param1 = EditorGUI.FloatField(position, labels[0], param1);
            param2 = EditorGUI.FloatField(position1, labels[1], param2);
            param3 = EditorGUI.FloatField(position2, labels[2], param3);
            EditorGUI.indentLevel--;

            if(EditorGUI.EndChangeCheck())
            {
                prop.vectorValue = new Vector4(param1, param2, param3, unused);
            }
        }
    }
}
#endif