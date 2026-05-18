//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    /// <summary>
    /// 实名验证提示事件参数
    /// </summary>
    public class VerifiedTipsArgs : GlobalEventArgs
    {
        public static readonly int EventId = typeof(VerifiedTipsArgs).GetHashCode();

        public override int Id
        {
            get { return EventId; }
        }

        public override void Clear()
        {
        }

        /// <summary>
        /// 剩余时间(分钟)
        /// </summary>
        public int LeftTime { get; private set; }

        /// <summary>
        /// 是否为法定节假日
        /// </summary>
        public bool IsHoliday { get; private set; }

        /// <summary>
        /// 事件参数填充
        /// </summary>
        /// <param name="leftTime"></param>
        public void Fill(int leftTime, bool isHoliday)
        {
            LeftTime = leftTime;
            IsHoliday = isHoliday;
        }
    }
}