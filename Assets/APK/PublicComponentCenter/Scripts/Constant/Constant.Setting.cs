//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    /// <summary>
    /// 常量
    /// </summary>
    public static partial class Constant
    {
        /// <summary> 设置 </summary>
        public static partial class Setting
        {
            /// <summary> 日志开关 </summary>
            public const bool LogEnable = true;

            /// <summary> DXY服务器配置的游戏Id </summary>
            /// TODO：必改项
            public const string GameId = "5";

            //public const string TestUserName = "李建良";
            //public const string TestUserIdentityNum = "45270220031223001X";

            public const string TestUserName = "侯程麟";
            public const string TestUserIdentityNum = "43102420031026035X";

            public const string AliRespInTestMode =
                "  { \"data\":{ \"sex\":\"男\",\"address\":\"湖南省 - 郴州市 - 嘉禾县\",\"birthday\":\"2003 - 10 - 26\" },\"resp\":{ \"code\":0,\"desc\":\"匹配\"} }";

            /// <summary> APP类型 </summary>
            public const string IsFreeApp = "App_t" + GameId;

            /// <summary> 玩家选择的语言 </summary>
            public const string Language = "App_Language" + GameId;
        }
    }
}