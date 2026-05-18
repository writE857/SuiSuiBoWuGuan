//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    public static partial class Utility
    {
        /// <summary>
        /// 时间工具
        /// </summary>
        public static class Timer
        {
            /// <summary>
            /// 毫秒转分钟
            /// </summary>
            /// <param name="millisecond"> 毫秒 </param>
            /// <returns></returns>
            public static int MillisecondToMinute(long millisecond)
            {
                return (int) (millisecond / 1000 / 60);
            }

            /// <summary>
            /// 分钟转秒
            /// </summary>
            /// <param name="minute"> 分钟 </param>
            /// <returns></returns>
            public static int MinuteToSecond(int minute)
            {
                return minute * 60;
            }
        }
    }
}