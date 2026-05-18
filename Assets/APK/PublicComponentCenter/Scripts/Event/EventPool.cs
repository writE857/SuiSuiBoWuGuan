//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicComponentCenter
{
    /// <summary>
    /// 事件池
    /// </summary>
    public class EventPool<T> where T : GlobalEventArgs
    {
        /// <summary>
        /// 事件结点
        /// 这里之所以要这么封装是因为事件处理方法的委托采用了C#的EventHandler，其形参为object与EvntArgs
        /// </summary>
        private class Event
        {
            public Event(object sender, T e)
            {
                Sender = sender;
                EventArgs = e;
            }

            /// <summary>
            /// 事件发送者
            /// </summary>
            public object Sender { get; private set; }

            /// <summary>
            /// 事件参数
            /// </summary>
            public T EventArgs { get; private set; }
        }

        /// <summary>
        /// 事件码与对应处理方法的字典
        /// </summary>
        private Dictionary<int, EventHandler<T>> m_EventHandlers;

        /// <summary>
        /// 事件结点队列
        /// </summary>
        private Queue<Event> m_Events;

        public EventPool()
        {
            m_EventHandlers = new Dictionary<int, EventHandler<T>>();
            m_Events = new Queue<Event>();
        }

        #region 事件相关处理

        /// <summary>
        /// 检查订阅事件处理方法是否存在
        /// </summary>
        public bool Check(int id, EventHandler<T> handler)
        {
            if (handler == null)
            {
                Debug.LogError("事件处理方法为空");
                return false;
            }

            EventHandler<T> handlers = null;
            if (!m_EventHandlers.TryGetValue(id, out handlers))
            {
                return false;
            }

            if (handlers == null)
            {
                return false;
            }

            //遍历委托里的所有方法
            foreach (EventHandler<T> i in handlers.GetInvocationList())
            {
                if (i == handler)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe(int id, EventHandler<T> handler)
        {
            if (handler == null)
            {
                Debug.LogError("事件处理方法为空，无法订阅");
                return;
            }

            EventHandler<T> eventHandler = null;
            //检查是否获取处理方法失败或获取到的为空
            if (!m_EventHandlers.TryGetValue(id, out eventHandler) || eventHandler == null)
            {
                m_EventHandlers[id] = handler;
            }
            //不为空，就检查是否处理方法重复了
            else if (Check(id, handler))
            {
                Debug.LogError("要订阅事件的处理方法已存在");
            }
            else
            {
                eventHandler += handler;
                m_EventHandlers[id] = eventHandler;
            }
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public void Unsubscribe(int id, EventHandler<T> handler)
        {
            if (handler == null)
            {
                Debug.LogError("事件处理方法为空，无法取消订阅");
                return;
            }

            if (m_EventHandlers.ContainsKey(id))
            {
                m_EventHandlers[id] -= handler;
            }
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件参数</param>
        private void HandleEvent(object sender, T e)
        {
            //尝试获取事件的处理方法
            int eventId = e.Id;
            EventHandler<T> handlers = null;
            if (m_EventHandlers.TryGetValue(eventId, out handlers))
            {
                if (handlers != null)
                {
                    handlers(sender, e);
                }
                else
                {
                    Debug.LogError("事件没有对应处理方法：" + eventId);
                }
            }

            //向引用池归还事件引用
            ReferencePool.Release(e);
        }

        /// <summary>
        /// 事件池轮询（用于处理线程安全的事件）
        /// </summary>
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            while (m_Events.Count > 0)
            {
                Event e = null;
                lock (m_Events)
                {
                    e = m_Events.Dequeue();
                }

                //从封装的Event中取出事件数据进行处理
                HandleEvent(e.Sender, e.EventArgs);
            }
        }

        /// <summary>
        /// 抛出事件（线程安全）
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">事件参数。</param>
        public void Fire(object sender, T e)
        {
            //将事件源和事件参数封装为Event加入队列
            Event eventNode = new Event(sender, e);
            lock (m_Events)
            {
                m_Events.Enqueue(eventNode);
            }

        }

        /// <summary>
        /// 抛出事件（线程不安全）
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        public void FireNow(object sender, T e)
        {
            HandleEvent(sender, e);
        }

        /// <summary>
        /// 清理事件。
        /// </summary>
        public void Clear()
        {
            lock (m_Events)
            {
                m_Events.Clear();
            }
        }

        /// <summary>
        /// 关闭并清理事件池。
        /// </summary>
        public void Shutdown()
        {
            Clear();
            m_EventHandlers.Clear();
        }

        #endregion
    }
}