//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

#if UNITY_IOS
using System.Runtime.InteropServices;
#endif
using System;
using UnityEngine;

namespace PublicComponentCenter
{
    /// <summary>
    /// 广告模块
    /// </summary>
    public class AdComponent : ComponentBase
    {
        [SerializeField, HideInInspector] private bool m_IsWhitePackage = true;

        [SerializeField, HideInInspector, CustomLabel("应用发布平台")]
        private AppPlatform m_CurrentPlatformType = AppPlatform.Android;

        [SerializeField, HideInInspector, CustomLabel("游戏运营平台")]
        private GamePlatform m_CurrentGamePlatformType = GamePlatform.Hw;

        [SerializeField, HideInInspector] private bool m_SimulatedRewards = false;
        [SerializeField, HideInInspector, CustomLabel("是否显示激励视频角标")]
        private bool m_RewardIconEnable = false;
#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void ShowIosGameComment();

        /// <summary>
        /// 是否是游戏结束全屏视频
        /// </summary>
        public bool IsGameOverFullScreenAd { get; private set; }

        /// <summary> 显示评论的次数 </summary>
        private int showGameCommentCount = 0;
#endif
        /// <summary>
        /// 是否是白包
        /// </summary>
        public bool IsWhitePackage
        {
            get { return m_IsWhitePackage; }
            set { m_IsWhitePackage = value; }
        }

        /// <summary>
        /// 模拟发放奖励
        /// </summary>
        public bool SimulatedRewards
        {
            get { return m_SimulatedRewards; }
            set { m_SimulatedRewards = value; }
        }

        /// <summary>
        /// 激励视频角标显示开关
        /// </summary>
        public bool RewardIconEnable
        {
            get { return m_RewardIconEnable; }
            set { m_RewardIconEnable = value; }
        }

        private bool m_IsNeedMuteSound = false;

        /// <summary>
        /// 播广告时是否需要静音
        /// </summary>
        public bool IsNeedMuteSound
        {
            get
            {
                Debug.LogFormat("NeedMute:{0}", m_IsNeedMuteSound);
                return m_IsNeedMuteSound;
            }
            set { m_IsNeedMuteSound = value; }
        }

        /// <summary>
        /// 当前游戏运营平台
        /// </summary>
        public GamePlatform CurrentGamePlatformType
        {
            get { return m_CurrentGamePlatformType; }
            set { m_CurrentGamePlatformType = value; }
        }

        /// <summary>
        /// 当前应用发布平台
        /// </summary>
        public AppPlatform CurrentAppPlatform
        {
            get { return m_CurrentPlatformType; }
            set
            {
                m_CurrentPlatformType = value;
                Debug.LogFormat("设置当前发布平台为：{0}", m_CurrentPlatformType);
            }
        }

        /// <summary>
        /// 当前游戏平台
        /// </summary>
        private IPlatform m_CurrentGamePlatform;

        /// <summary>
        /// 当前游戏平台
        /// </summary>
        public IPlatform CurrentGamePlatform
        {
            get { return m_CurrentGamePlatform; }
        }


        private NativeMsgReceiver m_NativeMsgReceiver;

        /// <summary>  原生平台回调接收者  </summary>
        public NativeMsgReceiver NativeMsgReceiver
        {
            get
            {
                if (!m_NativeMsgReceiver)
                {
                    m_NativeMsgReceiver = FindObjectOfType<NativeMsgReceiver>();
                }

                return m_NativeMsgReceiver;
            }
        }

        public override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
        }

        public void Init()
        {
            Debug.LogFormat("白包：{0}", m_IsWhitePackage);
            Debug.LogFormat("模拟发送奖励：{0}", m_SimulatedRewards);
            Debug.LogFormat("当前游戏发布平台：{0}", CurrentAppPlatform.ToString());
            Debug.LogFormat("当前游戏运营平台：{0}", CurrentGamePlatformType.ToString());

            InitCurrentPlatform();
            GetAppType();
            if (CurrentGamePlatformType == GamePlatform.Hw)
            {
                Replenishment();
            }
        }

        /// <summary> 获取是否是白包 </summary>
        public bool IsFreeApp
        {
            get { return m_CurrentGamePlatform.IsFreeApp; }
        }

        /// <summary> 获取是否允许Unity直接退出(-2允许直接退出,其它数值不允许退出) </summary>
        public bool AllowUnityQuit
        {
            get { return m_CurrentGamePlatform.AllowUnityQuit; }
        }

        /// <summary>
        /// 获取游戏类型(进入游戏第一时间调用,向Android层获取运营类型0为国内白；1为国内黑，-1为海外)
        /// </summary>
        /// <returns> 0为国内白；1为国内黑，-1为海外 </returns>
        public int GetAppType()
        {
            return m_CurrentGamePlatform.GetAppType();
        }


        /// <summary>
        /// 是否为海外版
        /// </summary>
        /// <returns> 是否为海外包 </returns>
        public bool IsSea()
        {
            return GetAppType() == -1;
        }

        /// <summary>
        /// 是否为海外版
        /// </summary>
        /// <returns> 是否为海外包 </returns>
        public bool IsOverseas()
        {
            return IsSea();
        }

        /// <summary>
        /// 显示激励视频广告
        /// </summary>
        /// <param name="args"> 参数(默认第一个参数为‘奖励物品id’) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        public bool ShowRewardedVideoAd(params object[] args)
        {
            bool res;
            if (SimulatedRewards)
            {
                res = true;
                Debug.LogFormat("模拟发送奖励，参数:{0}.", args.Length > 0 ? args[0].ToString() : string.Empty);
                NativeMsgReceiver.OnRewardedVideoAdSuccess(args.Length > 0 ? args[0].ToString() : string.Empty);
            }
            else
            {
                res = m_CurrentGamePlatform.ShowRewardedVideoAd(args);
            }

            return res;
        }

        public bool ShowOpenReward(params object[] args)
        {
            return m_CurrentGamePlatform.ShowOpenReward(args);
        }

        public bool BaoXiang(params object[] args)
        {
            return m_CurrentGamePlatform.BaoXiang();
        }

        /// <summary>
        /// 显示插屏广告
        /// </summary>
        /// <param name="args"> 参数(默认第一个参数为‘奖励物品id’) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        public bool ShowInterstitialAd(params object[] args)
        {
            return m_CurrentGamePlatform.ShowInterstitialAd(args);
        }

        public bool ShowInterstitialAd2(params object[] args)
        {
            return m_CurrentGamePlatform.ShowInterstitialAd2(args);
        }

        public bool ShowAutoNative(params object[] args)
        {
            return m_CurrentGamePlatform.ShowAutoNative(args);
        }

        /// <summary>
        /// 显示插屏视频广告仅Android有效
        /// </summary>
        /// <param name="args"> 参数(默认第一个参数为‘奖励物品id’) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        public bool ShowInterstitialVideoAd(params object[] args)
        {
            return m_CurrentGamePlatform.ShowInterstitialVideoAd(args);
        }

        /// <summary>
        /// 显示横幅广告
        /// </summary>
        /// <param name="args"> 参数(默认第一个参数为‘奖励物品id’) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        public bool ShowBannerAd(params object[] args)
        {
            bool res = m_CurrentGamePlatform.ShowBannerAd(args);
            if (SimulatedRewards)
            {
                NativeMsgReceiver.OnBannerAdSuccess(args.Length > 0 ? args[0].ToString() : string.Empty);
            }

            return res;
        }

        /// <summary>
        /// 显示全屏广告
        /// </summary>
        /// <param name="args"> 参数(默认第一个参数为‘bool类型的是否是游戏结束弹的全屏视频’) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        public bool ShowFullScreenAd(params object[] args)
        {
            bool res = m_CurrentGamePlatform.ShowFullScreenAd(args);

//#if UNITY_IOS
//            bool isGameOverAd = false;
//            if (args.Length > 0)
//            {
//                isGameOverAd = bool.Parse(args[0].ToString());
//            }

//            IsGameOverFullScreenAd = isGameOverAd;
//            Debug.LogFormat("IsGameOverFullScreenAd：{0}", IsGameOverFullScreenAd);
//#endif

            if (SimulatedRewards)
            {
                NativeMsgReceiver.OnFullScreeAdSuccess(args.Length > 0 ? args[0].ToString() : string.Empty);
            }

            return res;
        }

        /// <summary>
        /// 更多精彩
        /// </summary>
        /// <param name="args"> 参数 </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        public bool MoreWonderful(params object[] args)
        {
            return m_CurrentGamePlatform.MoreWonderful(args);
        }

        /// <summary>
        /// 联系客服
        /// </summary>
        /// <param name="args"> 参数(暂时无用) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        public bool ContactCustomerService(params object[] args)
        {
            return m_CurrentGamePlatform.ContactCustomerService(args);
        }

        /// <summary>
        /// 隐私政策
        /// </summary>
        /// <param name="args"> 参数(暂时无用) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        public bool PrivacyPolicy(params object[] args)
        {
            return m_CurrentGamePlatform.PrivacyPolicy(args);
        }

        public bool CleanPackageData(params object[] args)
        {
            return m_CurrentGamePlatform.CleanPackageData(args);
        }


        /// <summary>
        /// 支付
        /// </summary>
        /// <param name="money"> 支付金额 </param>
        /// <param name="args"> 参数(默认第一个参数为‘奖励物品id’) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        public bool Pay(int money, params object[] args)
        {
            bool res = m_CurrentGamePlatform.Pay(money, args);
            if (SimulatedRewards)
            {
                NativeMsgReceiver.OnPaySuccess(args.Length > 0 ? args[0].ToString() : string.Empty);
            }

            return res;
        }

        /// <summary>
        /// 补单
        /// </summary>
        /// <returns> 调用 Native 方法是否成功 </returns>
        public bool Replenishment()
        {
            return m_CurrentGamePlatform.Replenishment();
        }

        /// <summary>
        /// 从Native层获取游戏语言
        /// </summary>
        /// <returns> 游戏语言 </returns>
        public Language GetAppLanguage()
        {
            return m_CurrentGamePlatform.GetAppLanguage();
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        /// <returns> 调用 Native 方法是否成功 </returns>
        public bool Quit()
        {
            return m_CurrentGamePlatform.Quit(
                EnumUtility.EnumConvertToInt(GameEntry.Localization.Language));
        }

        /// <summary>
        /// 游戏评论(IOS专有)
        /// </summary>
        public void ShowGameComment()
        {
#if UNITY_IOS
            if (showGameCommentCount == 0)
            {
                Debug.Log("Unity Send ShowIosGameComment Method.");
                try
                {
                    showGameCommentCount++;
                    ShowIosGameComment();
                }
                catch (Exception e)
                {
                    Debug.LogWarningFormat("Call IOS Method Error:{0}", e.Message);
                }
            }
#endif
        }

        private void InitCurrentPlatform()
        {
            switch (m_CurrentPlatformType)
            {
                case AppPlatform.Pc:
                case AppPlatform.Android:
                    m_CurrentGamePlatform = new PlatformAndroid();
                    break;
                case AppPlatform.Ios:
#if UNITY_IOS||UNITY_IPHONE
                    m_CurrentGamePlatform = new PlatformIos();
#endif
                    break;
                case AppPlatform.Harmony:
                    m_CurrentGamePlatform = new PlatformHarmony();
                    break;
                default:
                    m_CurrentGamePlatform = new PlatformAndroid();
                    break;
            }

            NativeMsgReceiver aacr = FindObjectOfType<NativeMsgReceiver>();
            if (!aacr)
            {
                aacr = GameObject.Find("Game Framework").AddComponent<NativeMsgReceiver>();
                Debug.LogFormat("添加{0}到Game Framework.", aacr.GetType().Name);
            }
            else
            {
                Debug.LogFormat("已有{0},无需再次添加.", aacr.GetType().Name);
            }

            m_NativeMsgReceiver = aacr;
        }
    }
}