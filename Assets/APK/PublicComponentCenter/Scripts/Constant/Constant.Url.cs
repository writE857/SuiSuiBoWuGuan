namespace PublicComponentCenter
{
    public static partial class Constant
    {
        /// <summary>
        /// URL常量
        /// </summary>
        public static partial class Url
        {
            /// <summary> 阿里云验证地址 </summary>
            public const string VerifySysUrl = "https://idcard.market.alicloudapi.com/lianzhuo/idcard";

            /// <summary> 阿里云AppCode </summary>
            public const string AppCode = "APPCODE 802b51229c4a4fcab1712cb7c185d96e";

            public const string HeaderName = "Authorization";


            /// <summary> 查询认证状态 </summary>
            public const string AuthenticationStatusQuery =
                "https://www.dxyqd.cn/polymerization-tools/antiindulge/get-auth-status?";

            /// <summary> 修改认证状态 </summary>
            public const string AuthenticationStatusModify =
                "https://www.dxyqd.cn/polymerization-tools/antiindulge/set-auth-status";

            /// <summary> 获取今日累积在线时间 </summary>
            public const string GetOnlineTimeToday =
                "https://www.dxyqd.cn/polymerization-tools/antiindulge/game-duration-today";

            public const string RequestHeaderName = "Content-Type";
            public const string RequestHeaderValue = "application/json;charset=utf-8";
            public const string RequestMethodPost = "POST";
        }
    }
}