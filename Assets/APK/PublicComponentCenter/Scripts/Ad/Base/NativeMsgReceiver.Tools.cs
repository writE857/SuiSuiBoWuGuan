//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using System.Text.RegularExpressions;

namespace PublicComponentCenter
{
    public partial class NativeMsgReceiver
    {
        /// <summary>
        /// 判断输入字符串是否为数字字符串
        /// </summary>
        /// <param name="input"> 输入字符串 </param>
        /// <returns> 是否是数字字符串 </returns>
        private bool IsNumber(string input)
        {
            return Regex.IsMatch(input, Constant.Regex.Num);
        }
    }
}