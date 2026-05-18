//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using UnityEditor;

namespace PublicComponentCenter.Editor
{
    [CustomEditor(typeof(AntiaddictionComponent))]
    public class AntiaddictionComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty m_TestMode = null;
        private SerializedProperty m_ClearDataRuntime = null;
        private SerializedProperty m_Enable = null;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            AntiaddictionComponent t = (AntiaddictionComponent) target;

            bool adtEnable = EditorGUILayout.Toggle("开启实名制", m_Enable.boolValue);
            if (adtEnable != m_Enable.boolValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.Enable = adtEnable;
                }
                else
                {
                    m_Enable.boolValue = adtEnable;
                }
            }


            bool testMode = EditorGUILayout.Toggle("测试模式", m_TestMode.boolValue);
            if (testMode != m_TestMode.boolValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.TestMode = testMode;
                }
                else
                {
                    m_TestMode.boolValue = testMode;
                }
            }

            bool clearDataRuntime = EditorGUILayout.Toggle("运行时清空数据", m_ClearDataRuntime.boolValue);
            if (clearDataRuntime != m_ClearDataRuntime.boolValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.ClearDataRuntime = clearDataRuntime;
                }
                else
                {
                    m_ClearDataRuntime.boolValue = clearDataRuntime;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            m_Enable = serializedObject.FindProperty("m_Enable");
            m_TestMode = serializedObject.FindProperty("m_TestMode");
            m_ClearDataRuntime = serializedObject.FindProperty("m_ClearDataRuntime");
        }
    }
}