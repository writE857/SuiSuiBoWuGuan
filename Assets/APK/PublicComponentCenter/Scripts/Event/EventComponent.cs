//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using System;

namespace PublicComponentCenter
{
    public class EventComponent : ComponentBase
    {
        #region 字段、属性

        /// <summary>
        /// 事件池
        /// </summary>
        private EventPool<GlobalEventArgs> m_EventPool;

        #endregion


        #region 生命周期

        public override void Awake()
        {
            base.Awake();
            m_EventPool = new EventPool<GlobalEventArgs>();
        }

        public override void Shutdown()
        {
            //关闭并清理事件池
            m_EventPool.Shutdown();
            base.Shutdown();
        }

        /// <summary>
        /// 轮询事件池
        /// </summary>
        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            m_EventPool.Update(elapseSeconds, realElapseSeconds);
        }

        #endregion

        #region 事件相关

        /// <summary>
        /// 检查订阅事件处理方法是否存在
        /// </summary>
        public bool Check(int id, EventHandler<GlobalEventArgs> handler)
        {
            return m_EventPool.Check(id, handler);
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe(int id, EventHandler<GlobalEventArgs> handler)
        {
            m_EventPool.Subscribe(id, handler);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public void Unsubscribe(int id, EventHandler<GlobalEventArgs> handler)
        {
            m_EventPool.Unsubscribe(id, handler);
        }

        /// <summary>
        /// 抛出事件（线程安全）
        /// </summary>
        public void Fire(object sender, GlobalEventArgs e)
        {
            m_EventPool.Fire(sender, e);
        }

        /// <summary>
        /// 抛出事件（线程不安全）
        /// </summary>
        public void FireNow(object sender, GlobalEventArgs e)
        {
            m_EventPool.FireNow(sender, e);
        }

        #endregion
    }
}