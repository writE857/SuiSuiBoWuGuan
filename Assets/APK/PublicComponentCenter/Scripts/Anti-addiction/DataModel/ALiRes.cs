//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    /// <summary>
    /// 从阿里获取的验证信息
    /// </summary>
    public class ALiRes
    {
        public ALiPlayerInfo data { get; set; }
        public ALiResp resp { get; set; }
    }

    /// <summary>
    /// 从阿里获取的玩家身份信息
    /// </summary>
    public class ALiPlayerInfo
    {
        public string sex { get; set; }

        public string address { get; set; }
        public string birthday { get; set; }

        //public ALiResp resp { get; set; }
    }

    /// <summary>
    /// 从阿里获取的实名认证结果
    /// </summary>
    public class ALiResp
    {
        public int code { get; set; }
        public string desc { get; set; }
    }

    //"  { \"data\":{ \"sex\":\"男\",\"address\":\"湖南省 - 郴州市 - 嘉禾县\",\"birthday\":\"2003 - 10 - 26\" },\"resp\":{ \"code\":0,\"desc\":\"匹配\"} }";
}