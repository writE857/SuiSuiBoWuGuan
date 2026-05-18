//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

namespace PublicComponentCenter
{
    internal sealed class LoadDictionaryInfo : IReference
    {
        private string m_DictionaryName;
        private object m_UserData;

        public LoadDictionaryInfo()
        {
            m_DictionaryName = null;
            m_UserData = null;
        }

        public string DictionaryName
        {
            get { return m_DictionaryName; }
        }

        public object UserData
        {
            get { return m_UserData; }
        }

        public static LoadDictionaryInfo Create(string dictionaryName, object userData)
        {
            LoadDictionaryInfo loadDictionaryInfo = ReferencePool.Acquire<LoadDictionaryInfo>();
            loadDictionaryInfo.m_DictionaryName = dictionaryName;
            loadDictionaryInfo.m_UserData = userData;
            return loadDictionaryInfo;
        }

        public void Clear()
        {
            m_DictionaryName = null;
            m_UserData = null;
        }
    }
}