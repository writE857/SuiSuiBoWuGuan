//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace PublicComponentCenter
{
    /// <summary>
    /// Android平台
    /// </summary>
    public class PlatformAndroid : AppPlatformBase
    {
        #region Interface

        /// <summary>
        /// 游戏类型
        /// </summary>
        private int m_AppType = 0;

        public override bool IsFreeApp
        {
            get
            {
                if (GetAppType() == -1)
                {
                    return false;
                }

                //string code = PlayerPrefs.GetString(Constant.Setting.IsFreeApp, 0.ToString());
                return m_AppType == 0;
            }
        }

        public override bool AllowUnityQuit
        {
            get
            {
                int res = Call<int>(Constant.AdMethodName.AllowUnityQuit);
                Debug.LogFormat("允许Unity退出？：{0}，返回值为：{1}", res == default(int), res);
                return res == default(int);
            }
        }

        public override bool ShowFullScreenAd(params object[] args)
        {
            return Call(Constant.AdMethodName.ShowFullScreenAd, args);
        }

        public override bool ShowInterstitialAd(params object[] args)
        {
            return Call(Constant.AdMethodName.ShowInterstitialAd, args);
        }
        public override bool ShowInterstitialAd2(params object[] args)
        {
            return Call(Constant.AdMethodName.ShowInterstitialAd2, args);
        }
        public override bool ShowOpenReward(params object[] args)
        {
            return Call(Constant.AdMethodName.ShowOpenReward, args);
        }

        public override bool BaoXiang(params object[] args)
        {
            return Call(Constant.AdMethodName.BaoXiang, args);
        }

        public override bool ShowAutoNative(params object[] args)
        {
            return Call(Constant.AdMethodName.ShowAutoNative, args);
        }

        public override bool ShowInterstitialVideoAd(params object[] args)
        {
            return Call(Constant.AdMethodName.ShowInterstitialVideoAd, args);
        }
        
        public override bool ShowRewardedVideoAd(params object[] args)
        {
            return Call(Constant.AdMethodName.ShowRewardedVideoAd, args);
        }

        public override bool MoreWonderful(params object[] args)
        {
            return Call(Constant.AdMethodName.MoreWonderful, args);
        }

        public override bool ContactCustomerService(params object[] args)
        {
            return Call(Constant.AdMethodName.ContactCustomerService, args);
        }
        
        public override bool CleanPackageData(params object[] args)
        {
            return Call(Constant.AdMethodName.CleanPackageData, args);
        }

        public override bool PrivacyPolicy(params object[] args)
        {
            return Call(Constant.AdMethodName.PrivacyPolicy, args);
        }

        public override Language GetAppLanguage(params object[] args)
        {
            int res = default(int);
            res = args.Length > 0
                ? Call<int>(Constant.AdMethodName.GetAppLanguage, args)
                : Call<int>(Constant.AdMethodName.GetAppLanguage);
            return EnumUtility.IntConvertToEnum<Language>(res);
        }

        public override bool Pay(int money, params object[] args)
        {
            return Call(Constant.AdMethodName.Pay, args);
        }

        public override bool ShowBannerAd(params object[] args)
        {
            return Call(Constant.AdMethodName.ShowBannerAd, args);
        }

        public override bool Replenishment(params object[] args)
        {
            if (args.Length > 0)
            {
                return Call(Constant.AdMethodName.Replenishment, args);
            }

            return Call(Constant.AdMethodName.Replenishment);
        }

        public override int GetAppType(params object[] args)
        {
            //30秒之后默认就以最后一次获取的结果为准
            if (Time.realtimeSinceStartup > 30)
            {
                return m_AppType;
            }

            string res = 0.ToString();
            res = Call<string>(Constant.AdMethodName.GetAppType);
            //PlayerPrefs.SetString(Constant.Setting.IsFreeApp, res);
            try
            {
                m_AppType = int.Parse(res);
            }
            catch (Exception e)
            {
                m_AppType = 0;
#if UNITY_EDITOR
                Debug.LogFormat("GetAppType Fail:{0}", e.Message);
#endif
            }
#if UNITY_EDITOR
            Debug.LogFormat("CurrentState is :{0}", res);
#endif
            return m_AppType;
        }

        public override bool Quit(params object[] args)
        {
            if (!AllowUnityQuit)
            {
                if (args.Length > 0)
                {
                    return Call(Constant.AdMethodName.Quit, (int) args[0]);
                }

                return Call(Constant.AdMethodName.Quit);
            }
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
            return true;
#else
                Application.Quit();
                return true;
#endif
        }

        #endregion

        #region Tools

        /// <summary>
        /// 获取AndroidJavaObject
        /// </summary>
        /// <returns> AndroidJavaObject </returns>
        private AndroidJavaObject GetAndroidJavaObject()
        {
            AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
            return jo;
        }

        private bool Call(string method, params object[] args)
        {
            try
            {
                GetAndroidJavaObject().Call(method, args);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat("调用Android层参数为【{0}】的方法【{1}】时发生错误：{2}", GetArgsStr(args), method, e.Message);
                return false;
            }
        }

        private T Call<T>(string method, params object[] args)
        {
            T res;
            try
            {
                res = GetAndroidJavaObject().Call<T>(method, args);
            }
            catch (Exception e)
            {
                res = default(T);
                Debug.LogWarningFormat("调用Android层参数为【{0}】的方法【{1}】时发生错误：{2}", GetArgsStr(args), method, e.Message);
            }

            return res;
        }

        private bool CallStatic(string method, params object[] args)
        {
            try
            {
                GetAndroidJavaObject().CallStatic(method, args);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat("调用Android层参数为【{0}】的静态方法【{1}】时发生错误：{2}", GetArgsStr(args), method, e.Message);
                return false;
            }
        }

        private T CallStatic<T>(string method, params object[] args)
        {
            T res;
            try
            {
                res = GetAndroidJavaObject().CallStatic<T>(method, args);
            }
            catch (Exception e)
            {
                res = default(T);
                Debug.LogWarningFormat("调用Android层参数为【{0}】的静态方法【{1}】时发生错误：{2}", GetArgsStr(args), method, e.Message);
            }

            return res;
        }

        private string GetArgsStr(params object[] args)
        {
            string argsStr = string.Empty;
            if (args.Length <= 0)
            {
                return "空";
            }

            foreach (var o in args)
            {
                argsStr += o.ToString() + ";";
            }

            return argsStr;
        }

        #endregion
    }
}