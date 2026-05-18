//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using UnityEditor;

namespace PublicComponentCenter.Editor
{
    [CustomEditor(typeof(AdComponent))]
    public class AdComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty m_IsWhitePackage = null;
        private SerializedProperty m_SimulatedRewards = null;
        private SerializedProperty m_RewardIconEnable = null;
        private SerializedProperty m_CurrentPlatformType = null;
        private SerializedProperty m_CurrentGamePlatformType = null;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            AdComponent t = (AdComponent) target;

            EditorGUILayout.PropertyField(m_CurrentPlatformType);
            EditorGUILayout.PropertyField(m_CurrentGamePlatformType);

            bool adtEnable = EditorGUILayout.Toggle("白包", m_IsWhitePackage.boolValue);
            if (adtEnable != m_IsWhitePackage.boolValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.IsWhitePackage = adtEnable;
                }
                else
                {
                    m_IsWhitePackage.boolValue = adtEnable;
                }
            }

            bool simulatedRewards = EditorGUILayout.Toggle("模拟Android发放给奖励", m_SimulatedRewards.boolValue);
            if (simulatedRewards != m_SimulatedRewards.boolValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.SimulatedRewards = simulatedRewards;
                }
                else
                {
                    m_SimulatedRewards.boolValue = simulatedRewards;
                }
            }

            bool rewardIconEnable = EditorGUILayout.Toggle("激励视频角标显示开关", m_RewardIconEnable.boolValue);
            if (rewardIconEnable != m_RewardIconEnable.boolValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.RewardIconEnable = rewardIconEnable;
                }
                else
                {
                    m_RewardIconEnable.boolValue = rewardIconEnable;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            m_IsWhitePackage = serializedObject.FindProperty("m_IsWhitePackage");
            m_SimulatedRewards = serializedObject.FindProperty("m_SimulatedRewards");
            m_RewardIconEnable = serializedObject.FindProperty("m_RewardIconEnable");
            m_CurrentPlatformType = serializedObject.FindProperty("m_CurrentPlatformType");
            m_CurrentGamePlatformType = serializedObject.FindProperty("m_CurrentGamePlatformType");
        }
    }
}