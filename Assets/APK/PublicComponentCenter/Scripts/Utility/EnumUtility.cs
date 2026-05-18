using System;
using UnityEngine;

namespace PublicComponentCenter
{
    /// <summary>
    /// 枚举工具
    /// </summary>
    public static class EnumUtility
    {
        /// <summary>
        /// 将枚举转换为字符串
        /// </summary>
        /// <typeparam name="T"> 枚举类型 </typeparam>
        /// <param name="enumValue"> 枚举值 </param>
        /// <returns> 枚举字符串 </returns>
        public static string EnumConvertToString<T>(T enumValue)
        {
            return Enum.GetName(enumValue.GetType(), enumValue);
        }

        /// <summary>
        /// 将字符串转化为枚举
        /// </summary>
        /// <typeparam name="T"> 枚举类型 </typeparam>
        /// <param name="enumString"> 枚举字符串 </param>
        /// <returns> 转换后的枚举类型 </returns>
        public static T StringConvertToEnum<T>(string enumString)
        {
            T result = default(T);
            try
            {
                result = (T) Enum.Parse(typeof(T), enumString);
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("转换枚举{0}失败:{1}", enumString, e.Message));
                return result;
            }

            return result;
        }

        /// <summary>
        /// 枚举转整数
        /// </summary>
        /// <typeparam name="T"> 枚举类型 </typeparam>
        /// <param name="enumValue"> 枚举 </param>
        /// <returns></returns>
        public static int EnumConvertToInt<T>(T enumValue)
        {
            return (int) Enum.ToObject(typeof(T), enumValue);
        }


        /// <summary>
        /// 整数转换为枚举(转换失败则返回默认值)
        /// </summary>
        /// <typeparam name="T"> 枚举类型 </typeparam>
        /// <param name="enumInt"> 枚举对应的int值 </param>
        /// <returns> 枚举 </returns>
        public static T IntConvertToEnum<T>(int enumInt)
        {
            T result = default(T);
            if (Enum.IsDefined(typeof(T), enumInt))
            {
                return (T) Enum.ToObject(typeof(T), enumInt);
            }

            return result;
        }
    }
}