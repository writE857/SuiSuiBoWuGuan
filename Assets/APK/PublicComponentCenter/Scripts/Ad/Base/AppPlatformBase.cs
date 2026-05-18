//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    /// <summary>
    /// 应用发布平台基类
    /// </summary>
    public abstract class AppPlatformBase : IPlatform
    {
        public abstract bool IsFreeApp { get; }

        public abstract bool AllowUnityQuit { get; }

        public abstract bool Replenishment(params object[] args);

        public abstract int GetAppType(params object[] args);

        public abstract bool Pay(int money, params object[] args);

        public abstract bool ShowBannerAd(params object[] args);

        public abstract bool ShowFullScreenAd(params object[] args);

        public abstract bool ShowInterstitialAd(params object[] args);
        public abstract bool ShowInterstitialAd2(params object[] args);

        public abstract bool ShowOpenReward(params object[] args);

        public abstract bool BaoXiang(params object[] args);

        public abstract bool ShowAutoNative(params object[] args);

        public abstract bool ShowInterstitialVideoAd(params object[] args);

        public abstract bool ShowRewardedVideoAd(params object[] args);

        public abstract bool MoreWonderful(params object[] args);
        public abstract bool ContactCustomerService(params object[] args);
        public abstract bool PrivacyPolicy(params object[] args);
        public abstract Language GetAppLanguage(params object[] args);
        public abstract bool Quit(params object[] args);
        public abstract bool CleanPackageData(params object[] args);
    }
}