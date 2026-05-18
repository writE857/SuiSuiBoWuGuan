//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    /// <summary>
    /// 实名验证提示事件参数
    /// </summary>
    public class VerifiedQuitGameArgs : GlobalEventArgs
    {
        public static readonly int EventId = typeof(VerifiedQuitGameArgs).GetHashCode();

        public override int Id
        {
            get { return EventId; }
        }

        public override void Clear()
        {
        }

        /// <summary>
        /// 是否在线时间达到上限(否为宵禁)
        /// </summary>
        public bool IsOnlineTimeup;

        /// <summary>
        /// 提示内容
        /// </summary>
        public string Tips;

        /// <summary>
        /// 事件参数填充
        /// </summary>
        /// <param name="isOnlineTimeup"> 是否在线时间达到上限 </param>
        /// <param name="tips"> 提示内容 </param>
        public void Fill(bool isOnlineTimeup, string tips="")
        {
            IsOnlineTimeup = isOnlineTimeup;
            Tips = tips;
        }
    }
}