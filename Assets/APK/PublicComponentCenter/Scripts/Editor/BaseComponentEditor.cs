//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using UnityEditor;
using UnityEngine;

namespace PublicComponentCenter.Editor
{
    [CustomEditor(typeof(BaseComponent))]
    public class BaseComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty m_RunInBackground = null;
        private SerializedProperty m_NeverSleep = null;
        private SerializedProperty m_EditorLanguage = null;
        private SerializedProperty m_GameSpeed = null;
        //private SerializedProperty m_CenterVersion = null;
        private static readonly float[] GameSpeed = new float[] {0f, 0.01f, 0.1f, 0.25f, 0.5f, 1f, 1.5f, 2f, 4f, 8f};

        private static readonly string[] GameSpeedTexts = new string[]
            {"0x", "0.01x", "0.1x", "0.25x", "0.5x", "1x", "1.5x", "2x", "4x", "8x"};

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            BaseComponent t = (BaseComponent) target;

            EditorGUILayout.PropertyField(m_EditorLanguage);

            bool runInBackground = EditorGUILayout.Toggle("允许后台运行", m_RunInBackground.boolValue);
            if (runInBackground != m_RunInBackground.boolValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.RunInBackground = runInBackground;
                }
                else
                {
                    m_RunInBackground.boolValue = runInBackground;
                }
            }

            bool neverSleep = EditorGUILayout.Toggle("永不休眠", m_NeverSleep.boolValue);
            if (neverSleep != m_NeverSleep.boolValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.NeverSleep = neverSleep;
                }
                else
                {
                    m_NeverSleep.boolValue = neverSleep;
                }
            }

            EditorGUILayout.BeginVertical("box");
            {
                float gameSpeed = EditorGUILayout.Slider("Game Speed", m_GameSpeed.floatValue, 0f, 8f);
                int selectedGameSpeed = GUILayout.SelectionGrid(GetSelectedGameSpeed(gameSpeed), GameSpeedTexts, 5);
                if (selectedGameSpeed >= 0)
                {
                    gameSpeed = GetGameSpeed(selectedGameSpeed);
                }

                if (gameSpeed != m_GameSpeed.floatValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.GameSpeed = gameSpeed;
                    }
                    else
                    {
                        m_GameSpeed.floatValue = gameSpeed;
                    }
                }
            }
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            m_RunInBackground = serializedObject.FindProperty("m_RunInBackground");
            m_NeverSleep = serializedObject.FindProperty("m_NeverSleep");
            m_EditorLanguage = serializedObject.FindProperty("m_EditorLanguage");
            m_GameSpeed = serializedObject.FindProperty("m_GameSpeed");
            //m_CenterVersion = serializedObject.FindProperty("PublicComponentCenterVersion");
        }

        private float GetGameSpeed(int selectedGameSpeed)
        {
            if (selectedGameSpeed < 0)
            {
                return GameSpeed[0];
            }

            if (selectedGameSpeed >= GameSpeed.Length)
            {
                return GameSpeed[GameSpeed.Length - 1];
            }

            return GameSpeed[selectedGameSpeed];
        }

        private int GetSelectedGameSpeed(float gameSpeed)
        {
            for (int i = 0; i < GameSpeed.Length; i++)
            {
                if (gameSpeed == GameSpeed[i])
                {
                    return i;
                }
            }

            return -1;
        }
    }
}