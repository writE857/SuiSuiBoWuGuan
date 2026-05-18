//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    /// <summary>
    /// 工具类
    /// </summary>
    public static partial class Utility
    {
        /// <summary>
        /// URL工具
        /// </summary>
        public static class Url
        {
            /// <summary>
            /// 获取查询认证状态URL
            /// </summary>
            /// <param name="identityNumber"> 身份证号 </param>
            /// <returns></returns>
            public static string GetAuthenticationStatusQueryUrl(string identityNumber)
            {
                return Constant.Url.AuthenticationStatusQuery +
                       string.Format("gameid={0}&cardid={1}", Constant.Setting.GameId, identityNumber);
            }

            /// <summary>
            /// 修改认证状态
            /// </summary>
            /// <returns></returns>
            public static string GetAuthenticationStatusModifyUrl()
            {
                //
                return Constant.Url.AuthenticationStatusModify;
            }

            /// <summary>
            /// 获取今日累积在线时间
            /// </summary>
            /// <param name="identityNumber"> 身份证号 </param>
            /// <returns></returns>
            public static string GetOnlineTimeTodayUrl(string identityNumber)
            {
                return Constant.Url.GetOnlineTimeToday +
                       string.Format("?gameid={0}&cardid={1}", Constant.Setting.GameId, identityNumber);
            }
        }
    }
}