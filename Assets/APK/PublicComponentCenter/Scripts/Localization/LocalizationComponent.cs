//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;

namespace PublicComponentCenter
{
    public class LocalizationComponent : ComponentBase
    {
        private const int DefaultPriority = 0;
        private ILocalizationManager m_LocalizationManager = null;
        [SerializeField, CustomLabel("修改语言后自动保存"), Tooltip("该选项勾选，则玩家在“设置”面板中设置语言后，会保存到本地，下次打开应用直接显示玩家选择的语言,默认允许")]
        private bool m_SaveLanguage = false;
#if UNITY_EDITOR
        [SerializeField, CustomLabel("允许通过“设置”面板设置语言"),
         Tooltip(
             "该选项用来控制玩家是否可以在“设置”面板中设置语言,仅在Editor模式下回根据Editor的设置走，Native平台会根据平台自动区分是否可以设置,目前仅有双语版支持设置,需要开发者自行根据该数据来决定是否显示“设置”面板中的语言设置(默认不允许)")]
#endif
        private bool m_AllowSetLanguage = false;

        public ILocalizationManager LocalizationManager
        {
            get { return m_LocalizationManager; }
        }

        /// <summary>
        /// 获取或设置本地化语言。
        /// </summary>
        public Language Language
        {
            get { return m_LocalizationManager.Language; }
            set { m_LocalizationManager.Language = value; }
        }

        /// <summary>
        /// 获取系统语言。
        /// </summary>
        public Language SystemLanguage
        {
            get { return m_LocalizationManager.SystemLanguage; }
        }

        /// <summary>
        /// 获取字典数量。
        /// </summary>
        public int DictionaryCount
        {
            get { return m_LocalizationManager.DictionaryCount; }
        }

        /// <summary>
        /// 获取保存游戏语言状态
        /// </summary>
        public bool SaveLanguage
        {
            get { return m_SaveLanguage; }
        }


        /// <summary>
        /// 获取是否允许设置语言
        /// </summary>
        public bool AllowSetLanguage
        {
            get { return m_AllowSetLanguage; }
            private set { m_AllowSetLanguage = value; }
        }

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            //TODO 改为接口式
            m_LocalizationManager = gameObject.AddComponent<LocalizationManager>();
            if (m_LocalizationManager == null)
            {
                Debug.LogError("Localization manager is invalid.");
                return;
            }
        }

        private void Start()
        {
            BaseComponent baseComponent = GameEntry.GetGfComponent<BaseComponent>();
            if (baseComponent == null)
            {
                Debug.LogError("Base component is invalid.");
                return;
            }

            //TODO Frame 通过接口动态创建 
            LocalizationHelperBase localizationHelper =
                new XmlLocalizationHelper();

            m_LocalizationManager.SetLocalizationHelper(localizationHelper);
            Language curLanguage = Language.Unspecified;
            //Editor模式下读取在面板上的设置
            //非Editor模式 会判断从Native层获取到的语言种类，
#if UNITY_EDITOR&&!UNITY_IOS&&!UNITY_ANDROID
            curLanguage = GameEntry.Base.EditorLanguage;
            Debug.LogFormat("Editor模式，设置语言为：{0}", curLanguage);
#else
            Debug.LogFormat("!Editor模式，初始化AD模块：{0}", curLanguage);
            GameEntry.Ad.Init();
            curLanguage = GameEntry.Ad.GetAppLanguage();
            Debug.LogFormat("!Editor模式，设置语言为：{0}", curLanguage);
#endif
            switch (curLanguage)
            {
                //未指定为多语言版本
                case Language.Unspecified:
                    SetAllowSetLanguage(true);
                    //开启设置语言功能
                    if (SaveLanguage)
                    {
                        SetLanguagePlayerSelected();
                    }
                    else
                    {
                        SetLanguageSystem(baseComponent);
                    }

                    break;
                //指定了，那就是单语言版本
                case Language.ChineseSimplified:
                case Language.English:
                    //屏蔽设置语言功能
                    SetAllowSetLanguage(false);
                    m_LocalizationManager.Language = curLanguage;
                    break;
                default:
                    break;
            }

            Debug.LogFormat("当前设置的语言是：{0}", m_LocalizationManager.Language);

            //TODO 修改语言配置文件名称
            GameEntry.Localization.LoadDictionary("Default", false);
        }


        /// <summary>
        /// 加载字典。
        /// </summary>
        /// <param name="dictionaryName">字典名称。</param>
        /// <param name="dictionaryAssetName">字典资源名称。</param>
        public void LoadDictionary(string dictionaryName, string dictionaryAssetName)
        {
            LoadDictionary(dictionaryName, dictionaryAssetName, DefaultPriority, null);
        }

        /// <summary>
        /// 加载字典。
        /// </summary>
        /// <param name="dictionaryName">字典名称。</param>
        /// <param name="dictionaryAssetName">字典资源名称。</param>
        /// <param name="priority">加载字典资源的优先级。</param>
        public void LoadDictionary(string dictionaryName, string dictionaryAssetName, int priority)
        {
            LoadDictionary(dictionaryName, dictionaryAssetName, priority, null);
        }

        /// <summary>
        /// 加载字典。
        /// </summary>
        /// <param name="dictionaryName">字典名称。</param>
        /// <param name="dictionaryAssetName">字典资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void LoadDictionary(string dictionaryName, string dictionaryAssetName, object userData)
        {
            LoadDictionary(dictionaryName, dictionaryAssetName, DefaultPriority, userData);
        }

        /// <summary>
        /// 加载字典。
        /// </summary>
        /// <param name="dictionaryName">字典名称。</param>
        /// <param name="dictionaryAssetName">字典资源名称。</param>
        /// <param name="priority">加载字典资源的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void LoadDictionary(string dictionaryName, string dictionaryAssetName, int priority, object userData)
        {
            if (string.IsNullOrEmpty(dictionaryName))
            {
                Debug.LogError("Dictionary name is invalid.");
                return;
            }

            m_LocalizationManager.LoadDictionary(dictionaryAssetName, priority,
                LoadDictionaryInfo.Create(dictionaryName, userData));
        }

        /// <summary>
        /// 解析字典。
        /// </summary>
        /// <param name="dictionaryData">要解析的字典数据。</param>
        /// <returns>是否解析字典成功。</returns>
        public bool ParseDictionary(object dictionaryData)
        {
            return m_LocalizationManager.ParseDictionary(dictionaryData);
        }

        /// <summary>
        /// 解析字典。
        /// </summary>
        /// <param name="dictionaryData">要解析的字典数据。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否解析字典成功。</returns>
        public bool ParseDictionary(object dictionaryData, object userData)
        {
            return m_LocalizationManager.ParseDictionary(dictionaryData, userData);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString(string key)
        {
            return m_LocalizationManager.GetString(key);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <param name="arg0">字典参数 0。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString(string key, object arg0)
        {
            return m_LocalizationManager.GetString(key, arg0);
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
            return m_LocalizationManager.GetString(key, arg0, arg1);
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
            return m_LocalizationManager.GetString(key, arg0, arg1, arg2);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <param name="args">字典参数。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString(string key, params object[] args)
        {
            return m_LocalizationManager.GetString(key, args);
        }

        /// <summary>
        /// 获取UiSprite资源
        /// </summary>
        /// <param name="assetName"> 资源名称 </param>
        /// <returns></returns>
        public Sprite GetUiSprite(string assetName)
        {
            return Resources.Load<Sprite>(AssetUtility.GetUiSpriteAsset(assetName));
        }

        /// <summary>
        /// 是否存在字典。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>是否存在字典。</returns>
        public bool HasRawString(string key)
        {
            return m_LocalizationManager.HasRawString(key);
        }

        /// <summary>
        /// 根据字典主键获取字典值。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>字典值。</returns>
        public string GetRawString(string key)
        {
            return m_LocalizationManager.GetRawString(key);
        }

        /// <summary>
        /// 移除字典。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>是否移除字典成功。</returns>
        public bool RemoveRawString(string key)
        {
            return m_LocalizationManager.RemoveRawString(key);
        }

        /// <summary>
        /// 清空所有字典。
        /// </summary>
        public void RemoveAllRawStrings()
        {
            m_LocalizationManager.RemoveAllRawStrings();
        }

        public void ChangeLanguage(Language language)
        {
            BaseComponent baseComponent = GameEntry.GetGfComponent<BaseComponent>();
            if (baseComponent == null)
            {
                Debug.LogError("Base component is invalid.");
                return;
            }

            m_LocalizationManager.Language = language;
            if (SaveLanguage)
            {
                GameEntry.Setting.SetInt(Constant.Setting.Language, (int) m_LocalizationManager.Language);
            }

            Debug.LogFormat("当前设置的语言是：{0}", m_LocalizationManager.Language);

            RemoveAllRawStrings();
            GameEntry.Localization.LoadDictionary("Default", false);
            //TODO 重新加载游戏,这里可以设置切换语言后加载的场景
            SceneManager.LoadScene(1);
        }

        /// <summary>
        /// 设置玩家选择的语言
        /// </summary>
        private void SetLanguagePlayerSelected()
        {
            int languageInt = GameEntry.Setting.GetInt(Constant.Setting.Language, 0);

            var saveLanguage =
                EnumUtility.IntConvertToEnum<Language>(languageInt);
            m_LocalizationManager.Language =
                saveLanguage != Language.Unspecified
                    ? saveLanguage
                    : m_LocalizationManager.SystemLanguage;
            Debug.LogFormat("根据玩家选择配置语言,语言序号：{0}，语言种类：{1}", languageInt, saveLanguage);
        }

        /// <summary>
        /// 设置系统默认语言
        /// </summary>
        /// <param name="baseComponent"> 框架基础组件 </param>
        private void SetLanguageSystem(BaseComponent baseComponent)
        {
            //如果没有指定语言，那就为系统语言，指定了，就为指定语言
            m_LocalizationManager.Language =
                baseComponent.EditorLanguage != Language.Unspecified
                    ? baseComponent.EditorLanguage
                    : m_LocalizationManager.SystemLanguage;
            Debug.Log("系统默认配置语言");
        }


        private void SetAllowSetLanguage(bool allow)
        {
//#if UNITY_EDITOR
//            Debug.LogFormat("语言设置权限为:{0}", AllowSetLanguage);
//#else
            AllowSetLanguage = allow;
            Debug.LogFormat("设置语言设置权限为:{0}", allow);
//#endif
        }
    }
}