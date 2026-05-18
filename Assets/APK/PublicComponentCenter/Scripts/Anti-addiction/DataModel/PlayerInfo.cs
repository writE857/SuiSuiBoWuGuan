//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    /// <summary>
    /// 玩家信息
    /// </summary>
    public class PlayerInfo
    {
        /// <summary> 游戏Id </summary>
        public string gameid { get; set; }

        /// <summary> 身份证号 </summary>
        public string cardid { get; set; }

        /// <summary> 真实姓名 </summary>
        public string realname { get; set; }

        /// <summary>
        /// 玩家信息新实例
        /// </summary>
        /// <param name="gameid"> 游戏Id </param>
        /// <param name="cardid"> 身份证号 </param>
        /// <param name="realname"> 真实姓名 </param>
        public PlayerInfo(string gameid, string cardid, string realname)
        {
            this.gameid = gameid;
            this.cardid = cardid;
            this.realname = realname;
        }
    }
}