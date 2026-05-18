//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    /// <summary>
    /// 实名验证失败事件(进入游客模式)
    /// </summary>
    public class VerifiedFailedEventArgs : GlobalEventArgs
    {
        public static readonly int EventId = typeof(VerifiedFailedEventArgs).GetHashCode();

        public override int Id
        {
            get { return EventId; }
        }

        public override void Clear()
        {
        }
    }
}