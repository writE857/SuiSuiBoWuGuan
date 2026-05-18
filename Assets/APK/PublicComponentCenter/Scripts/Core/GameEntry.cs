//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicComponentCenter
{
    /// <summary>
    /// 游戏入口
    /// </summary>
    public partial class GameEntry : MonoBehaviour
    {
        #region Properties

        /// <summary> 组件映射表 </summary>
        static Dictionary<string, ComponentBase> m_Components = new Dictionary<string, ComponentBase>();

        public static BaseComponent Base { get; private set; }

        /// <summary> 防沉迷 </summary>
        public static AntiaddictionComponent Antiaddiction { get; private set; }

        /// <summary> 事件 </summary>
        public static EventComponent Event { get; private set; }

        /// <summary> 广告组件 </summary>
        public static AdComponent Ad { get; private set; }

        public static SettingComponent Setting { get; private set; }


        /// <summary>  获取本地化组件  </summary>
        public static LocalizationComponent Localization { get; private set; }

        /// <summary> 组件中心游戏物体 </summary>
        private static GameObject ComponentCenterGo;

        #endregion

        #region Mono

        void Awake()
        {
            ComponentCenterGo = gameObject;
            Base = GetGfComponent<BaseComponent>();
            Antiaddiction = GetGfComponent<AntiaddictionComponent>();
            Event = GetGfComponent<EventComponent>();
            Ad = GetGfComponent<AdComponent>();
            Localization = GetGfComponent<LocalizationComponent>();
            Setting = GetGfComponent<SettingComponent>();
        }


        void Start()
        {
            DontDestroyOnLoad(this);
            transform.SetAsLastSibling();
        }


        void Update()
        {
            foreach (KeyValuePair<string, ComponentBase> component in m_Components)
            {
                component.Value.OnUpdate(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }


        private void OnDestroy()
        {
            //关闭并清理所有管理器
            foreach (KeyValuePair<string, ComponentBase> component in m_Components)
            {
                component.Value.Shutdown();
            }

            m_Components.Clear();
        }

        #endregion

        #region Interface

        /// <summary>
        /// 获取游戏框架组件。
        /// </summary>
        /// <param name="component">要获取的游戏框架组件类型。</param>
        /// <returns>要获取的游戏框架组件。</returns>
        public static T GetGfComponent<T>() where T : ComponentBase
        {
            T component = (T) GetGfComponent(typeof(T));
            if (component)
            {
                //Debug.LogFormat("成功获取注册组件：{0}", component.GetType().Name);
                return component;
            }

            component = ComponentCenterGo.GetComponentInChildren<T>();
            if (component)
            {
                RegisterComponent(component);
                //Debug.LogFormat("成功直接获取组件：{0}", component.GetType().Name);
            }

            return component;
        }

        /// <summary>
        /// 获取游戏框架组件。
        /// </summary>
        /// <param name="T">要获取的游戏框架组件类型。</param>
        /// <returns>要获取的游戏框架组件。</returns>
        public static T GetCustomComponent<T>()
        {
            T component = ComponentCenterGo.GetComponentInChildren<T>();
            if (component != null)
            {
                ComponentBase cb = component as ComponentBase;
                if (cb)
                {
                    RegisterComponent(cb);
                }
            }

            Debug.LogErrorFormat("Component {0} is null.", typeof(T).Name);
            return component;
        }

        /// <summary>
        /// 注册组件
        /// </summary>
        /// <param name="component"> 要注册的组件 </param>
        public static void RegisterComponent(ComponentBase component)
        {
            string componentName = component.GetType().Name;
            if (!m_Components.ContainsKey(componentName))
            {
                //Debug.LogFormat("添加组件：{0}", componentName);
                m_Components.Add(componentName, component);
            }
        }

        #endregion

        #region Helper

        private static ComponentBase GetGfComponent(Type type)
        {
            string componentName = type.Name;
            if (m_Components.ContainsKey(componentName))
            {
                return m_Components[componentName];
            }

            return null;
        }

        #endregion
    }
}