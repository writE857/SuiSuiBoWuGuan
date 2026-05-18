//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    /// <summary>
    /// 玩家在线时长结果
    /// </summary>
    public class UserOnlineResult
    {
        /// <summary> 相应结果代号,0代表成功，1代表失败 </summary>
        public string result { get; set; }

        /// <summary> 相应结果说明：成功 或 失败 </summary>
        public string message { get; set; }

        /// <summary>
        /// 今日是否为法定节假日
        ///     0:不是法定假日,
        ///     1:是法定假日
        /// </summary>
        public int is_holiday { get; set; }

        /// <summary>
        /// 当前时间是否允许未成年进入:(0不可，1可)
        ///     0-晚22到早8点之间不可以进入；
        ///     1-晚22到早8点之间可以进入；
        /// </summary>
        public int can_play { get; set; }

        /// <summary>
        /// 用户在线信息
        /// </summary>
        public UserOnlineInfo content { get; set; }
    }

    /// <summary>
    /// 玩家在线信息
    /// </summary>
    public class UserOnlineInfo
    {
        /// <summary> 游戏ID </summary>
        public int game_id { get; set; }

        /// <summary> 身份证号 </summary>
        public string card_id { get; set; }

        /// <summary> 真实姓名 </summary>
        public string real_name { get; set; }

        /// <summary> 成年状态 </summary>
        public string auth_status { get; set; }

        /// <summary> 最后在线时间 </summary>
        public long last_online_timestamp { get; set; }

        /// <summary> 今日累计在线时间 </summary>
        public int duration_today { get; set; }
    }
}

//{"result":"0","is_holiday":0,"can_play":1,"message":"成功","content":{"game_id":5,"card_id":"43102420031026035X","real_name":"侯程麟","auth_status":"0","last_online_timestamp":1590810790660,"duration_today":8207}}