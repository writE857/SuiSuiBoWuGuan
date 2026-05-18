using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Fix;
using UnityEditor;
using UnityEngine;

namespace Fix.Editor
{
    [CustomPropertyDrawer(typeof(DisplayAttribute))]
    public sealed class DisplayHelper : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (CanDisplay(property))
                EditorGUI.PropertyField(position, property, label);
        }
 
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            CanDisplay(property)
                ? EditorGUIUtility.singleLineHeight
                : 0;
 
        private static bool CanDisplay(SerializedProperty property)
        {
            var o = property.serializedObject.targetObject.Find(property.propertyPath.ParentPath());
            var type = o.GetType();
            var thisField = type.GetField(property.name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Default);
            if (thisField == null) return false;
            return CustomAttributeExtensions.GetCustomAttributes(thisField,typeof(DisplayAttribute))
                .OfType<DisplayAttribute>()
                .All(attr =>
                    !(Equals(attr.TargetFieldValue, type.GetField(attr.TargetFieldName,
                              BindingFlags.Instance
                              | BindingFlags.Public
                              | BindingFlags.NonPublic
                              | BindingFlags.Default
                          )
                          ?.GetValue(o)
                          ?.ToString()) ^
                      attr.Operator));
        }
    }
}