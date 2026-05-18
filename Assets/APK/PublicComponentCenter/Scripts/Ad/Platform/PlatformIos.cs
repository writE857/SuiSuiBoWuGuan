using UnityEngine;

namespace PublicComponentCenter
{
#if UNITY_IOS
    /// <summary>
    /// IOS平台AD处理
    /// </summary>
    public class PlatformIos : AppPlatformBase
    {
        #region Properties

        private AppPlatformBase _currentPlatform;

        #endregion

        #region Interface

        public override bool IsFreeApp
        {
            get { return _currentPlatform.IsFreeApp; }
        }

        public override bool AllowUnityQuit
        {
            get { return _currentPlatform.AllowUnityQuit; }
        }

        public PlatformIos()
        {
            switch (GameEntry.Localization.Language)
            {
                //case Language.Unspecified:
                //    Debug.Log("未指定语言");
                //    switch (Application.systemLanguage)
                //    {
                //        case SystemLanguage.ChineseSimplified:
                //        case SystemLanguage.Chinese:
                //            _currentPlatform = new PlatformIosChineseSimplified();
                //            break;
                //        case SystemLanguage.English:
                //            _currentPlatform = new PlatformIosEnglish();
                //            break;
                //        default:
                //            _currentPlatform = new PlatformIosEnglish();

                //            //Debug.LogWarningFormat("Dxy Default 11111111 Current App IOS platform is :{0},{1}",
                //            //    _currentPlatform.GetType().Name, Application.systemLanguage);
                //            break;
                //    }

                //    Debug.LogFormat("Dxy Default  Current App IOS platform is :{0}",
                //        _currentPlatform.GetType().Name);
                //    break;
                case Language.ChineseSimplified:
                    _currentPlatform = new PlatformIosChineseSimplified();
                    break;
                case Language.English:
                    _currentPlatform = new PlatformIosEnglish();
                    break;
                default:
                    _currentPlatform = new PlatformIosEnglish();
                    break;
            }

            Debug.LogFormat("Dxy Current App IOS platform is :{0}",
                _currentPlatform.GetType().Name);
        }

        public override bool ShowFullScreenAd(params object[] args)
        {
            return _currentPlatform.ShowFullScreenAd();
        }

        public override bool ShowRewardedVideoAd(params object[] args)
        {
            //TODO 广告时静音处理
            //GameEntry.Sound.TryMuteSound();
            return _currentPlatform.ShowRewardedVideoAd(args);
        }

        public override bool ShowInterstitialAd(params object[] args)
        {
            return _currentPlatform.ShowInterstitialAd();
        }

        public override bool ShowInterstitialVideoAd(params object[] args)
        {
            return _currentPlatform.ShowInterstitialVideoAd(args);
        }

        public override bool ShowBannerAd(params object[] args)
        {
            return _currentPlatform.ShowBannerAd();
        }

        public override bool MoreWonderful(params object[] args)
        {
            return _currentPlatform.MoreWonderful(args);
        }

        public override bool ContactCustomerService(params object[] args)
        {
            return false;
        }
        
        public override bool PrivacyPolicy(params object[] args)
        {
            return false;
        }

        public override Language GetAppLanguage(params object[] args)
        {
            return _currentPlatform.GetAppLanguage(args);
        }

        public override bool Quit(params object[] args)
        {
            return _currentPlatform.Quit(args);
        }

        public override bool Pay(int money, params object[] args)
        {
            return _currentPlatform.Pay(money, args);
        }


        public override bool Replenishment(params object[] args)
        {
            return _currentPlatform.Replenishment(args);
        }

        public override int GetAppType(params object[] args)
        {
            return _currentPlatform.GetAppType(args);
        }

        #endregion
    }

#endif
}