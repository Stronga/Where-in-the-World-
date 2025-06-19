/****************************************************************************** 
 * 
 * Maintaince Logs: 
 * 2020-10-30       WP      Initial version
 * 
 * *****************************************************************************/

using UnityEngine;
using UnityEditor;

namespace MzTool
{

    [CustomEditor(typeof(MzUGUIScrollCtrl))]
    public class Ins_MzUGUIScrollCtrl : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawProperty("scrollRect");
            DrawProperty("item01");
            DrawProperty("transCenter");
            DrawProperty("loadOnStart");

            var record = DrawProperty("recordCurrentItem");
            if (record.boolValue)
            {
                DrawProperty("isStickCurrentOnEnable");
                DrawProperty("recordKey");
            }

            DrawProperty("stickAnimCurve");
            DrawProperty("minMoveDelta");
            DrawProperty("minStickSpeed");
            DrawProperty("minStickDistance");
            DrawProperty("stickOnEndDragSpeed");
            var hasCenter = DrawProperty("isCenterScaleEffect");
            if (hasCenter.boolValue)
            {
                DrawProperty("itemEffectSize");
                DrawProperty("scaleRange");
            }

            DrawProperty("onSelectItemChange");
            DrawProperty("onCurrentItemChange");

#if UNITY_2017_1_OR_NEWER
            if (Application.isPlaying)
            {
                GUI.enabled = false;
                DrawProperty("isSticking");
                DrawProperty("moveDelta");
                DrawVector2("curScrollPos");
                DrawVector2("preScrollPos");
                DrawProperty("lastEndDragSpeed");
                DrawProperty("mSelectItem");
                DrawProperty("mCurrentItem");
                DrawProperty("fromPos");
                DrawProperty("toPos");
                DrawProperty("keyParam");
                GUI.enabled = true;
            }
#endif

            serializedObject.ApplyModifiedProperties();
        }

        private SerializedProperty DrawVector2(string name)
        {
            var pro = GetProperty(name);

            if (pro == null)
            {
                Debug.Log("Don't Find Property : " + name);
                return null;
            }

            EditorGUILayout.Vector2Field(pro.displayName, pro.vector2Value);
            return pro;
        }

        private SerializedProperty DrawProperty(string name)
        {
            var pro = GetProperty(name);

            if (pro == null)
            {
                Debug.Log("Don't Find Property : " + name);
                return null;
            }

            EditorGUILayout.PropertyField(pro);
            return pro;
        }

        private SerializedProperty GetProperty(string name)
        {
            return serializedObject.FindProperty(name);
        }
    }
}