//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    /// <summary>
    /// 实名认证状态
    /// </summary>
    public enum VerifiedState
    {
        /// <summary> 未认证 </summary>
        NotVerified = 0,

        /// <summary> 认证，未成年 </summary>
        Underage,

        /// <summary> 认证，成年 </summary>
        Adult,
    }
}