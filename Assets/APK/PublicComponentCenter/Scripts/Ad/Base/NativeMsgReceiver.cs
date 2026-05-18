//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using UnityEngine;

namespace PublicComponentCenter
{
    /// <summary>
    /// Android层回调Unity层事件接收者
    /// </summary>
    public partial class NativeMsgReceiver : MonoBehaviour
    {
        #region Interface (Android只调用这几个接口，其余处理请也在这几个接口中实现)

        /// <summary>
        /// 支付成功回调
        /// </summary>
        /// <param name="args"> 支付奖励物品id/标识符 </param>
        public void OnPaySuccess(string args)
        {
            MsgProcess(args);
        }

        /// <summary>
        /// 激励视频成功回调
        /// </summary>
        /// <param name="args"> 支付奖励物品id/标识符 </param>
        public void OnRewardedVideoAdSuccess(string args)
        {
            MsgProcess(args);
        }


        /// <summary>
        /// 显示Banner成功回调(可忽略)
        /// </summary>
        /// <param name="args"> 支付奖励物品id/标识符 </param>
        public void OnBannerAdSuccess(string args)
        {
            MsgProcess(args);
        }

        /// <summary>
        /// 全屏广告(可忽略)
        /// </summary>
        /// <param name="args"> 支付奖励物品id/标识符 </param>
        public void OnFullScreeAdSuccess(string args)
        {
            MsgProcess(args);
        }

        #endregion
    }
}