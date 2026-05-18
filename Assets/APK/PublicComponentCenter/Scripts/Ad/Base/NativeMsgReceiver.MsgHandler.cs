//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using UnityEngine;

namespace PublicComponentCenter
{
    public partial class NativeMsgReceiver
    {
        //#region 属性

        ///// <summary>
        ///// 游戏最大关卡索引(TODO 修改最大关卡索引)
        ///// </summary>
        //public int MaxLevelIndex = 50;

        //#endregion


        //#region Mono

        //void Awake()
        //{
        //    Debug.Log("开始注册Android回调事件");
        //    RegisterEventLevelUnlock();
        //    RegisterEventCustomer();
        //    //TODO 注册其它事件


        //    Debug.LogFormat("注册Android回调事件完成，共注册事件{0}个.", m_MsgDic.Count);
        //}

        //void OnDestroy()
        //{
        //    UnRegisterEventLevelUnlock();
        //    UnRegisterEventCustomer();
        //    //TODO 注销其它事件
        //    Debug.Log("注销Android回调事件");
        //}

        //#endregion


        //#region 事件注册

        ///// <summary>
        ///// 注册解锁关卡事件
        ///// </summary>
        //private void RegisterEventLevelUnlock()
        //{
        //    for (int i = 0; i < MaxLevelIndex; i++)
        //    {
        //        AddEvent(i.ToString(), OnLevelUnlock);
        //    }
        //}

        ///// <summary>
        ///// 注册自定义事件
        ///// </summary>
        //private void RegisterEventCustomer()
        //{
        //    //TODO 注册自定义事件
        //}

        //#endregion

        //#region 事件注销

        ///// <summary>
        ///// 注销解锁关卡事件
        ///// </summary>
        //private void UnRegisterEventLevelUnlock()
        //{
        //    for (int i = 0; i < MaxLevelIndex; i++)
        //    {
        //        RemoveEvent(i.ToString());
        //    }
        //}

        ///// <summary>
        ///// 注销自定义事件
        ///// </summary>
        //private void UnRegisterEventCustomer()
        //{
        //    //TODO 注销自定义事件
        //}

        //#endregion

        //#region 事件处理

        //private void OnLevelUnlock(string args)
        //{
        //    //TODO 解锁关卡处理
        //    Debug.LogFormat("解锁关卡：{0}", args);
        //}

        //#endregion

        #region Helper

        /// <summary>
        /// 清空穿山甲广告参数
        /// </summary>
        private void ClearArgs()
        {
        }

        #endregion
    }
}