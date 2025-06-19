using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace MzTool
{
    [CustomPropertyDrawer(typeof(MethodButton))]
    public class MethodButtonAttribute : PropertyDrawer
    {
        MethodButton battribute;
        Object obj;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            battribute = attribute as MethodButton;
            obj = property.serializedObject.targetObject;
            
            Rect left, right;
            var per = battribute.buttonWidth / position.width;

            position.H_Division((1 - per), out left, out right);

            EditorGUI.PropertyField(left, property);

            List<Rect> rs;
            right.H_Division(battribute.methods.Length, out rs);
            var methodNames = battribute.methods;
            var showNames = battribute.showNames;

            for (int i = 0; i < rs.Count; i++)
            {
                //画Button
                if (GUI.Button(rs[i], new GUIContent(showNames[i])))
                {
                    MethodInfo method = obj.GetType().GetMethod(methodNames[i], BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                    if (method != null) method.Invoke(obj, null);
                    else
                    {
                        Debug.Log("Method is null :" + showNames[i] + "\nMethod only in object, and isn't subobject!!!");
                    }
                }
            }
        }
    }
}