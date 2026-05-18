using System;
using UnityEngine;

namespace PublicComponentCenter
{
    /// <summary>
    /// 基础组件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BaseComponent : ComponentBase
    {
        private const int DefaultDpi = 96; // default windows dpi

        /// <summary> 公共组建中心版本 </summary>
        public readonly string PublicComponentCenterVersion = "v3.5.8";

        private float m_GameSpeedBeforePause = 1f;

        [SerializeField, HideInInspector, CustomLabel("编译器语言"),
         Tooltip("该项仅在编译器有效,发布到Native平台会根据Native的返回值来设置,Unspecified代表双语版")]
        private Language m_EditorLanguage = Language.Unspecified;

        //[SerializeField] private int m_FrameRate = 30;

        [SerializeField, HideInInspector] private float m_GameSpeed = 1f;

        [SerializeField, HideInInspector] private bool m_RunInBackground = true;

        [SerializeField, HideInInspector] private bool m_NeverSleep = true;


        /// <summary>
        /// 获取或设置编辑器语言（仅编辑器内有效）。
        /// </summary>
        public Language EditorLanguage
        {
            get { return m_EditorLanguage; }
            set { m_EditorLanguage = value; }
        }


        /// <summary>
        /// 获取或设置游戏帧率。
        /// </summary>
        //public int FrameRate
        //{
        //    get { return m_FrameRate; }
        //    set { Application.targetFrameRate = m_FrameRate = value; }
        //}

        /// <summary>
        /// 获取或设置游戏速度。
        /// </summary>
        public float GameSpeed
        {
            get { return m_GameSpeed; }
            set { Time.timeScale = m_GameSpeed = value >= 0f ? value : 0f; }
        }

        /// <summary>
        /// 获取游戏是否暂停。
        /// </summary>
        public bool IsGamePaused
        {
            get { return m_GameSpeed <= 0f; }
        }

        /// <summary>
        /// 获取是否正常游戏速度。
        /// </summary>
        public bool IsNormalGameSpeed
        {
            get { return m_GameSpeed == 1f; }
        }

        /// <summary>
        /// 获取或设置是否允许后台运行。
        /// </summary>
        public bool RunInBackground
        {
            get { return m_RunInBackground; }
            set { Application.runInBackground = m_RunInBackground = value; }
        }

        /// <summary>
        /// 获取或设置是否禁止休眠。
        /// </summary>
        public bool NeverSleep
        {
            get { return m_NeverSleep; }
            set
            {
                m_NeverSleep = value;
                Screen.sleepTimeout = value ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            }
        }

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            Debug.LogFormat("公共组件中间版本：{0}", PublicComponentCenterVersion);
            Debug.LogFormat("当前编辑器游戏语言为：{0}", m_EditorLanguage);
            InitVersionHelper();
            InitLogHelper();

#if UNITY_5_3_OR_NEWER || UNITY_5_3
            InitZipHelper();
            InitJsonHelper();
            //Application.targetFrameRate = m_FrameRate;
            Time.timeScale = m_GameSpeed;
            Debug.LogFormat("设置游戏速度为：{0}", Time.timeScale);
            Application.runInBackground = m_RunInBackground;
            Screen.sleepTimeout = m_NeverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
#else
            Log.Error("Game Framework only applies with Unity 5.3 and above, but current Unity version is {0}.", Application.unityVersion);
            GameEntry.Shutdown(ShutdownType.Quit);
#endif
#if UNITY_5_6_OR_NEWER
            Application.lowMemory += OnLowMemory;
#endif
        }

        private void Start()
        {
        }

        private void Update()
        {
            // GameFrameworkEntry.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnApplicationQuit()
        {
#if UNITY_5_6_OR_NEWER
            Application.lowMemory -= OnLowMemory;
#endif
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            //GameFrameworkEntry.Shutdown();
        }

        /// <summary>
        /// 暂停游戏。
        /// </summary>
        public void PauseGame()
        {
            if (IsGamePaused)
            {
                return;
            }

            m_GameSpeedBeforePause = GameSpeed;
            GameSpeed = 0f;
        }

        /// <summary>
        /// 恢复游戏。
        /// </summary>
        public void ResumeGame()
        {
            if (!IsGamePaused)
            {
                return;
            }

            GameSpeed = m_GameSpeedBeforePause;
        }

        /// <summary>
        /// 重置为正常游戏速度。
        /// </summary>
        public void ResetNormalGameSpeed()
        {
            if (IsNormalGameSpeed)
            {
                return;
            }

            GameSpeed = 1f;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            Destroy(gameObject);
        }

        private void InitVersionHelper()
        {
        }

        private void InitLogHelper()
        {
        }

        private void InitZipHelper()
        {
        }

        private void InitJsonHelper()
        {
            //TODO Frame 修改为反射生成类
            Utility.Json.SetJsonHelper(new LitJsonHelper());
        }

        private void OnLowMemory()
        {
        }
    }
}