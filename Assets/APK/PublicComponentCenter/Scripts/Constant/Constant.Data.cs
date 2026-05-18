//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    public static partial class Constant
    {
        public static partial class Data
        {
            /// <summary> 是否已经实名制验证过 </summary>
            public const string IsVerifiedKey = "IsVerified" + Setting.GameId;

            /// <summary> 身份证号 </summary>
            public const string IdentityNum = "IdentityNum" + Setting.GameId;

            /// <summary> 用户名 </summary>
            public const string UserName = "UserName" + Setting.GameId;
        }
    }
}