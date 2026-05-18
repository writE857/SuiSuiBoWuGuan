//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    /// <summary>
    /// 实名验证未成年事件
    /// </summary>
    public class VerifiedUnderageEventArgs : GlobalEventArgs
    {
        public static readonly int EventId = typeof(VerifiedUnderageEventArgs).GetHashCode();

        public override int Id
        {
            get { return EventId; }
        }

        public override void Clear()
        {
        }
    }
}