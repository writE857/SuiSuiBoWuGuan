//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    public interface IPlatform
    {
        /// <summary> 获取是否是白包 </summary>
        bool IsFreeApp { get; }

        /// <summary> 获取是否允许Unity直接退出(-2允许直接退出,其它数值不允许退出) </summary>
        bool AllowUnityQuit { get; }

        /// <summary> 获取游戏类型(进入游戏第一时间调用,向Android层获取黑白包) </summary>
        int GetAppType(params object[] args);

        /// <summary>
        /// 显示激励视频广告
        /// </summary>
        /// <param name="args"> 参数(默认第一个参数为‘奖励物品id’) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        bool ShowRewardedVideoAd(params object[] args);

        /// <summary>
        /// 显示横幅广告
        /// </summary>
        /// <param name="args"> 参数(默认第一个参数为‘奖励物品id’) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        bool ShowBannerAd(params object[] args);
       /// <summary>
       /// 注销账号
       /// </summary>
       /// <param name="args">参数</param> 
       /// <returns>调用Native方法是否成功</returns> 
        bool CleanPackageData(params object[] args);

        /// <summary>
        /// 显示插屏广告
        /// </summary>
        /// <param name="args"> 参数(默认第一个参数为‘奖励物品id’) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        bool ShowInterstitialAd(params object[] args);
        bool ShowInterstitialAd2(params object[] args);

        bool ShowOpenReward(params object[] args);
        bool BaoXiang(params object[] args);
        bool ShowAutoNative(params object[] args);

        /// <summary>
        /// 显示插屏视频广告
        /// </summary>
        /// <param name="args"> 参数(默认第一个参数为‘奖励物品id’) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        bool ShowInterstitialVideoAd(params object[] args);

        /// <summary>
        /// 显示全屏广告
        /// </summary>
        /// <param name="args"> 参数(默认第一个参数为‘奖励物品id’) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        bool ShowFullScreenAd(params object[] args);

        /// <summary>
        /// 支付
        /// </summary>
        /// <param name="money"> 支付金额 </param>
        /// <param name="args"> 参数(默认第一个参数为‘奖励物品id’) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        bool Pay(int money, params object[] args);

        /// <summary>
        /// 补单
        /// </summary>
        /// <returns> 调用 Native 方法是否成功 </returns>
        bool Replenishment(params object[] args);

        /// <summary>
        /// 更多精彩
        /// </summary>
        /// <param name="args"> 参数(暂时无用) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        bool MoreWonderful(params object[] args);

        /// <summary>
        /// 联系客服
        /// </summary>
        /// <param name="args"> 参数(暂时无用) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        bool ContactCustomerService(params object[] args);

        /// <summary>
        /// 隐私政策
        /// </summary>
        /// <param name="args"> 参数(暂时无用) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        bool PrivacyPolicy(params object[] args);

        /// <summary>
        /// 获取游戏语言种类
        /// </summary>
        /// <param name="args"> 参数 </param>
        /// <returns></returns>
        Language GetAppLanguage(params object[] args);

        /// <summary>
        /// 退出游戏
        /// </summary>
        /// <param name="args"> 参数(第一个参数为int型的Language) </param>
        /// <returns> 调用 Native 方法是否成功 </returns>
        bool Quit(params object[] args);
    }
}