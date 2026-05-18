//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicComponentCenter
{
    /// <summary>
    /// 本地化管理器。
    /// </summary>
    public sealed partial class LocalizationManager : ComponentBase, ILocalizationManager
    {
        private Dictionary<string, string> m_Dictionary;
        private ILocalizationHelper m_LocalizationHelper;
        private Language m_Language;


        /// <summary>
        /// 初始化本地化管理器的新实例。
        /// </summary>
        public override void Awake()
        {
            m_Dictionary = new Dictionary<string, string>();
            m_LocalizationHelper = null;
            m_Language = Language.Unspecified;
        }

        /// <summary>
        /// 获取或设置本地化语言。
        /// </summary>
        public Language Language
        {
            get { return m_Language; }
            set
            {
                if (value == Language.Unspecified)
                {
                    m_Language = Language.English;
                }

                m_Language = value;
            }
        }

        /// <summary>
        /// 获取系统语言。
        /// </summary>
        public Language SystemLanguage
        {
            get
            {
                if (m_LocalizationHelper == null)
                {
                    Debug.LogError("You must set localization helper first.");
                }

                return m_LocalizationHelper.SystemLanguage;
            }
        }

        /// <summary>
        /// 获取字典数量。
        /// </summary>
        public int DictionaryCount
        {
            get { return m_Dictionary.Count; }
        }


        /// <summary>
        /// 设置本地化辅助器。
        /// </summary>
        /// <param name="localizationHelper">本地化辅助器。</param>
        public void SetLocalizationHelper(ILocalizationHelper localizationHelper)
        {
            if (localizationHelper == null)
            {
                Debug.LogError("Localization helper is invalid.");
            }

            m_LocalizationHelper = localizationHelper;
        }

        /// <summary>
        /// 加载字典。
        /// </summary>
        /// <param name="dictionaryAssetName">字典资源名称。</param>
        public void LoadDictionary(string dictionaryAssetName)
        {
            LoadDictionary(dictionaryAssetName, 0, null);
        }

        /// <summary>
        /// 加载字典。
        /// </summary>
        /// <param name="dictionaryAssetName">字典资源名称。</param>
        /// <param name="priority">加载字典资源的优先级。</param>
        public void LoadDictionary(string dictionaryAssetName, int priority)
        {
            LoadDictionary(dictionaryAssetName, priority, null);
        }

        /// <summary>
        /// 加载字典。
        /// </summary>
        /// <param name="dictionaryAssetName">字典资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void LoadDictionary(string dictionaryAssetName, object userData)
        {
            LoadDictionary(dictionaryAssetName, 0, userData);
        }

        /// <summary>
        /// 加载字典。
        /// </summary>
        /// <param name="dictionaryAssetName">字典资源名称。</param>
        /// <param name="priority">加载字典资源的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void LoadDictionary(string dictionaryAssetName, int priority, object userData)
        {
            TextAsset ta = Resources.Load<TextAsset>(dictionaryAssetName);
            if (ta)
            {
                if (ParseDictionary(ta.text, dictionaryAssetName))
                {
                    Debug.LogFormat("解析语言配置{0}成功.", dictionaryAssetName);
                }
            }
            else
            {
                Debug.LogErrorFormat("没有语言配置：{0}", dictionaryAssetName);
            }
        }

        /// <summary>
        /// 解析字典。
        /// </summary>
        /// <param name="dictionaryData">要解析的字典数据。</param>
        /// <returns>是否解析字典成功。</returns>
        public bool ParseDictionary(object dictionaryData)
        {
            return ParseDictionary(dictionaryData, null);
        }

        /// <summary>
        /// 解析字典。
        /// </summary>
        /// <param name="dictionaryData">要解析的字典数据。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否解析字典成功。</returns>
        public bool ParseDictionary(object dictionaryData, object userData)
        {
            if (m_LocalizationHelper == null)
            {
                Debug.LogError("You must set localization helper first.");
                return false;
            }

            try
            {
                return m_LocalizationHelper.ParseDictionary(dictionaryData, userData);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    string.Format("Can not parse dictionary with exception '{0}'.", exception.Message));
                return false;
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Key is invalid.");
            }

            string value = null;
            if (!m_Dictionary.TryGetValue(key, out value))
            {
                return string.Format("<NoKey>{0}", key);
            }

            return value;
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <param name="arg0">字典参数 0。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString(string key, object arg0)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Key is invalid.");
            }

            string value = null;
            if (!m_Dictionary.TryGetValue(key, out value))
            {
                return string.Format("<NoKey>{0}", key);
            }

            try
            {
                return string.Format(value, arg0);
            }
            catch (Exception exception)
            {
                return string.Format("<Error>{0},{1},{2},{3}", key, value, arg0, exception.ToString());
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <param name="arg0">字典参数 0。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString(string key, object arg0, object arg1)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Key is invalid.");
            }

            string value = null;
            if (!m_Dictionary.TryGetValue(key, out value))
            {
                return string.Format("<NoKey>{0}", key);
            }

            try
            {
                return string.Format(value, arg0, arg1);
            }
            catch (Exception exception)
            {
                return string.Format("<Error>{0},{1},{2},{3},{4}", key, value, arg0, arg1, exception.ToString());
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <param name="arg0">字典参数 0。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString(string key, object arg0, object arg1, object arg2)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Key is invalid.");
            }

            string value = null;
            if (!m_Dictionary.TryGetValue(key, out value))
            {
                return string.Format("<NoKey>{0}", key);
            }

            try
            {
                return string.Format(value, arg0, arg1, arg2);
            }
            catch (Exception exception)
            {
                return string.Format("<Error>{0},{1},{2},{3},{4},{5}", key, value, arg0, arg1, arg2,
                    exception.ToString());
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <param name="args">字典参数。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Key is invalid.");
                return string.Empty;
            }

            string value = null;
            if (!m_Dictionary.TryGetValue(key, out value))
            {
                return string.Format("<NoKey>{0}", key);
            }

            try
            {
                return string.Format(value, args);
            }
            catch (Exception exception)
            {
                string errorString = string.Format("<Error>{0},{1}", key, value);
                if (args != null)
                {
                    foreach (object arg in args)
                    {
                        errorString += "," + arg.ToString();
                    }
                }

                errorString += "," + exception.ToString();
                return errorString;
            }
        }

        /// <summary>
        /// 是否存在字典。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>是否存在字典。</returns>
        public bool HasRawString(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Key is invalid.");
            }

            return m_Dictionary.ContainsKey(key);
        }

        /// <summary>
        /// 根据字典主键获取字典值。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>字典值。</returns>
        public string GetRawString(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Key is invalid.");
            }

            string value = null;
            if (m_Dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            return string.Format("<NoKey>{0}", key);
        }

        /// <summary>
        /// 增加字典。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <param name="value">字典内容。</param>
        /// <returns>是否增加字典成功。</returns>
        public bool AddRawString(string key, string value)
        {
            if (HasRawString(key))
            {
                return false;
            }

            //Debug.LogFormat("增加一条数据:\nkey:{0}\n value:{1}", key, value);
            m_Dictionary.Add(key, value ?? string.Empty);
            return true;
        }

        /// <summary>
        /// 移除字典。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>是否移除字典成功。</returns>
        public bool RemoveRawString(string key)
        {
            if (!HasRawString(key))
            {
                return false;
            }

            return m_Dictionary.Remove(key);
        }

        /// <summary>
        /// 清空所有字典。
        /// </summary>
        public void RemoveAllRawStrings()
        {
            m_Dictionary.Clear();
        }
    }
}