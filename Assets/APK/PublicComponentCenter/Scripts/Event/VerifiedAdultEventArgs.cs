//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    /// <summary>
    /// 实名验证 成年 事件
    /// </summary>
    public class VerifiedAdultEventArgs : GlobalEventArgs
    {
        public static readonly int EventId = typeof(VerifiedAdultEventArgs).GetHashCode();

        public override int Id
        {
            get { return EventId; }
        }

        public override void Clear()
        {
        }
    }
}