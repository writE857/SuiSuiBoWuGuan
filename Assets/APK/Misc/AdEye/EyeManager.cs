using System;
using UnityEngine;

namespace APK
{
    public class EyeManager : MonoBehaviour
    {
        [SerializeField] private float firstTime = 180f, repeatTime = 120f;
        private GameObject eyePanel;
        public static EyeManager Instance { get; private set; }
        public bool Enabled { get; set; }

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Enabled = false;
            InvokeRepeating(nameof(RepeatEye), firstTime, repeatTime);
            AutoAdSettings.OnRefresh += SetOrientation;
            SetOrientation();
        }

        private void SetOrientation()
        {
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
            eyePanel = transform.GetChild(AutoAdSettings.Instance.Orientation).gameObject;
        }

        private void OnDestroy()
        {
            AutoAdSettings.OnRefresh -= SetOrientation;
        }

        private void RepeatEye()
        {
            print(nameof(RepeatEye));
            if (!Enabled) return;
            ShowEye();
        }

        public void ShowEye()
        {
            print(nameof(ShowEye));
            eyePanel.SetActive(true);
            AdUtils.ShowBlackAd();
        }

        public void HideEye()
        {
            print(nameof(HideEye));
            eyePanel.SetActive(false);
        }
    }
}