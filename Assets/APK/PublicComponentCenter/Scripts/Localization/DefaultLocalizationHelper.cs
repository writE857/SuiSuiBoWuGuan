//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace PublicComponentCenter
{
    /// <summary>
    /// 默认本地化辅助器。
    /// </summary>
    public class DefaultLocalizationHelper : LocalizationHelperBase
    {
        private static readonly string[] RowSplitSeparator = new string[] {"\r\n", "\r", "\n"};
        private static readonly string[] ColumnSplitSeparator = new string[] {"\t"};
        private const int ColumnCount = 4;

        private ILocalizationManager m_LocalizationManager = null;

        public ILocalizationManager LocalizationManager
        {
            get
            {
                if (m_LocalizationManager == null)
                {
                    m_LocalizationManager = GameEntry.Localization.LocalizationManager;
                }

                return m_LocalizationManager;
            }
        }

        /// <summary>
        /// 获取系统语言。
        /// </summary>
        public override Language SystemLanguage
        {
            get
            {
                switch (Application.systemLanguage)
                {
                    case UnityEngine.SystemLanguage.Chinese: return Language.ChineseSimplified;
                    case UnityEngine.SystemLanguage.ChineseSimplified: return Language.ChineseSimplified;
                    case UnityEngine.SystemLanguage.English: return Language.English;
                    case UnityEngine.SystemLanguage.Afrikaans: return Language.English;
                    case UnityEngine.SystemLanguage.Arabic: return Language.English;
                    case UnityEngine.SystemLanguage.Basque: return Language.English;
                    case UnityEngine.SystemLanguage.Belarusian: return Language.English;
                    case UnityEngine.SystemLanguage.Bulgarian: return Language.English;
                    case UnityEngine.SystemLanguage.Catalan: return Language.English;
                    case UnityEngine.SystemLanguage.ChineseTraditional: return Language.English;
                    case UnityEngine.SystemLanguage.Czech: return Language.English;
                    case UnityEngine.SystemLanguage.Danish: return Language.English;
                    case UnityEngine.SystemLanguage.Dutch: return Language.English;
                    case UnityEngine.SystemLanguage.Estonian: return Language.English;
                    case UnityEngine.SystemLanguage.Faroese: return Language.English;
                    case UnityEngine.SystemLanguage.Finnish: return Language.English;
                    case UnityEngine.SystemLanguage.French: return Language.English;
                    case UnityEngine.SystemLanguage.German: return Language.English;
                    case UnityEngine.SystemLanguage.Greek: return Language.English;
                    case UnityEngine.SystemLanguage.Hebrew: return Language.English;
                    case UnityEngine.SystemLanguage.Hungarian: return Language.English;
                    case UnityEngine.SystemLanguage.Icelandic: return Language.English;
                    case UnityEngine.SystemLanguage.Indonesian: return Language.English;
                    case UnityEngine.SystemLanguage.Italian: return Language.English;
                    case UnityEngine.SystemLanguage.Japanese: return Language.English;
                    case UnityEngine.SystemLanguage.Korean: return Language.English;
                    case UnityEngine.SystemLanguage.Latvian: return Language.English;
                    case UnityEngine.SystemLanguage.Lithuanian: return Language.English;
                    case UnityEngine.SystemLanguage.Norwegian: return Language.English;
                    case UnityEngine.SystemLanguage.Polish: return Language.English;
                    case UnityEngine.SystemLanguage.Portuguese: return Language.English;
                    case UnityEngine.SystemLanguage.Romanian: return Language.English;
                    case UnityEngine.SystemLanguage.Russian: return Language.English;
                    case UnityEngine.SystemLanguage.SerboCroatian: return Language.English;
                    case UnityEngine.SystemLanguage.Slovak: return Language.English;
                    case UnityEngine.SystemLanguage.Slovenian: return Language.English;
                    case UnityEngine.SystemLanguage.Spanish: return Language.English;
                    case UnityEngine.SystemLanguage.Swedish: return Language.English;
                    case UnityEngine.SystemLanguage.Thai: return Language.English;
                    case UnityEngine.SystemLanguage.Turkish: return Language.English;
                    case UnityEngine.SystemLanguage.Ukrainian: return Language.English;
                    case UnityEngine.SystemLanguage.Vietnamese: return Language.English;
                    case UnityEngine.SystemLanguage.Unknown: return Language.English;


                    //case UnityEngine.SystemLanguage.Afrikaans: return Language.Afrikaans;
                    //case UnityEngine.SystemLanguage.Arabic: return Language.Arabic;
                    //case UnityEngine.SystemLanguage.Basque: return Language.Basque;
                    //case UnityEngine.SystemLanguage.Belarusian: return Language.Belarusian;
                    //case UnityEngine.SystemLanguage.Bulgarian: return Language.Bulgarian;
                    //case UnityEngine.SystemLanguage.Catalan: return Language.Catalan;
                    //case UnityEngine.SystemLanguage.ChineseTraditional: return Language.ChineseTraditional;
                    //case UnityEngine.SystemLanguage.Czech: return Language.Czech;
                    //case UnityEngine.SystemLanguage.Danish: return Language.Danish;
                    //case UnityEngine.SystemLanguage.Dutch: return Language.Dutch;
                    //case UnityEngine.SystemLanguage.Estonian: return Language.Estonian;
                    //case UnityEngine.SystemLanguage.Faroese: return Language.Faroese;
                    //case UnityEngine.SystemLanguage.Finnish: return Language.Finnish;
                    //case UnityEngine.SystemLanguage.French: return Language.French;
                    //case UnityEngine.SystemLanguage.German: return Language.German;
                    //case UnityEngine.SystemLanguage.Greek: return Language.Greek;
                    //case UnityEngine.SystemLanguage.Hebrew: return Language.Hebrew;
                    //case UnityEngine.SystemLanguage.Hungarian: return Language.Hungarian;
                    //case UnityEngine.SystemLanguage.Icelandic: return Language.Icelandic;
                    //case UnityEngine.SystemLanguage.Indonesian: return Language.Indonesian;
                    //case UnityEngine.SystemLanguage.Italian: return Language.Italian;
                    //case UnityEngine.SystemLanguage.Japanese: return Language.Japanese;
                    //case UnityEngine.SystemLanguage.Korean: return Language.Korean;
                    //case UnityEngine.SystemLanguage.Latvian: return Language.Latvian;
                    //case UnityEngine.SystemLanguage.Lithuanian: return Language.Lithuanian;
                    //case UnityEngine.SystemLanguage.Norwegian: return Language.Norwegian;
                    //case UnityEngine.SystemLanguage.Polish: return Language.Polish;
                    //case UnityEngine.SystemLanguage.Portuguese: return Language.PortuguesePortugal;
                    //case UnityEngine.SystemLanguage.Romanian: return Language.Romanian;
                    //case UnityEngine.SystemLanguage.Russian: return Language.Russian;
                    //case UnityEngine.SystemLanguage.SerboCroatian: return Language.SerboCroatian;
                    //case UnityEngine.SystemLanguage.Slovak: return Language.Slovak;
                    //case UnityEngine.SystemLanguage.Slovenian: return Language.Slovenian;
                    //case UnityEngine.SystemLanguage.Spanish: return Language.Spanish;
                    //case UnityEngine.SystemLanguage.Swedish: return Language.Swedish;
                    //case UnityEngine.SystemLanguage.Thai: return Language.Thai;
                    //case UnityEngine.SystemLanguage.Turkish: return Language.Turkish;
                    //case UnityEngine.SystemLanguage.Ukrainian: return Language.Ukrainian;
                    //case UnityEngine.SystemLanguage.Unknown: return Language.Unspecified;
                    //case UnityEngine.SystemLanguage.Vietnamese: return Language.Vietnamese;
                    default: return Language.Unspecified;
                    //return Language.Unspecified;
                }
            }
        }

        /// <summary>
        /// 解析字典。
        /// </summary>
        /// <param name="dictionaryData">要解析的字典数据。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否解析字典成功。</returns>
        public override bool ParseDictionary(object dictionaryData, object userData)
        {
            try
            {
                string dictionaryText = dictionaryData as string;
                if (dictionaryText != null)
                {
                    string[] dictionaryRowTexts = dictionaryText.Split(RowSplitSeparator, StringSplitOptions.None);
                    for (int i = 0; i < dictionaryRowTexts.Length; i++)
                    {
                        if (dictionaryRowTexts[i].Length <= 0 || dictionaryRowTexts[i][0] == '#')
                        {
                            continue;
                        }

                        string[] splitLine = dictionaryRowTexts[i].Split(ColumnSplitSeparator, StringSplitOptions.None);
                        if (splitLine.Length != ColumnCount)
                        {
                            Debug.LogWarningFormat("Can not parse dictionary '{0}'.", dictionaryText);
                            return false;
                        }

                        string dictionaryKey = splitLine[1];
                        string dictionaryValue = splitLine[3];
                        if (!AddRawString(dictionaryKey, dictionaryValue))
                        {
                            Debug.LogWarningFormat(
                                "Can not add raw string with dictionary key '{0}' which may be invalid or duplicate.",
                                dictionaryKey);
                            return false;
                        }
                    }

                    return true;
                }

                byte[] dictionaryBytes = dictionaryData as byte[];
                if (dictionaryBytes != null)
                {
                    using (MemoryStream memoryStream = new MemoryStream(dictionaryBytes, false))
                    {
                        using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                        {
                            while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                            {
                                string dictionaryKey = binaryReader.ReadString();
                                string dictionaryValue = binaryReader.ReadString();
                                if (!AddRawString(dictionaryKey, dictionaryValue))
                                {
                                    Debug.LogWarningFormat(
                                        "Can not add raw string with dictionary key '{0}' which may be invalid or duplicate.",
                                        dictionaryKey);
                                    return false;
                                }
                            }
                        }
                    }

                    return true;
                }

                Debug.LogWarningFormat("Can not parse dictionary data which type '{0}' is invalid.",
                    dictionaryData.GetType().FullName);
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogWarningFormat("Can not parse dictionary data with exception '{0}'.", exception.ToString());
                return false;
            }
        }

        /// <summary>
        /// 释放字典资源。
        /// </summary>
        /// <param name="dictionaryAsset">要释放的字典资源。</param>
        public override void ReleaseDictionaryAsset(object dictionaryAsset)
        {
            TextAsset ta = dictionaryAsset as TextAsset;
            if (ta)
            {
                Resources.UnloadAsset(ta);
            }

            Debug.LogErrorFormat("The released resource '{0}' is empty.", dictionaryAsset);
        }

        /// <summary>
        /// 加载字典。
        /// </summary>
        /// <param name="dictionaryName">字典名称。</param>
        /// <param name="dictionaryAssetName">字典资源名称。</param>
        /// <param name="dictionaryObject">字典对象。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否加载成功。</returns>
        protected override bool LoadDictionary(string dictionaryName, string dictionaryAssetName,
            object dictionaryObject, object userData)
        {
            TextAsset dictionaryTextAsset = dictionaryObject as TextAsset;
            if (dictionaryTextAsset != null)
            {
                return LocalizationManager.ParseDictionary(dictionaryTextAsset.text, userData);
            }

            Debug.LogWarningFormat("Dictionary object '{0}' is invalid.", dictionaryName);
            return false;
        }

        /// <summary>
        /// 增加字典。
        /// </summary>
        /// <param name="dictionaryKey">字典主键。</param>
        /// <param name="dictionaryValue">字典内容。</param>
        /// <returns>是否增加字典成功。</returns>
        protected bool AddRawString(string dictionaryKey, string dictionaryValue)
        {
            return LocalizationManager.AddRawString(dictionaryKey, dictionaryValue);
        }

        private void Start()
        {
        }
    }
}