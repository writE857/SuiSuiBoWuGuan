//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PublicComponentCenter
{
    public partial class NativeMsgReceiver
    {
        /// <summary> Android回调参数和事件的映射 </summary>
        private Dictionary<string, UnityAction<string>> m_MsgDic = new Dictionary<string, UnityAction<string>>();

        /// <summary>
        /// 注册Android回调事件
        /// </summary>
        public bool AddEvent(string args, UnityAction<string> processEvent)
        {
            if (string.IsNullOrEmpty(args)) return false;
            if (!HasEvent(args))
            {
                m_MsgDic.Add(args, processEvent);
                return true;
            }

            return false;
        }


        public bool RemoveEvent(string args)
        {
            if (string.IsNullOrEmpty(args)) return false;
            if (HasEvent(args))
            {
                m_MsgDic.Remove(args);
                return true;
            }

            return false;
        }

        public bool HasEvent(string args)
        {
            return m_MsgDic.ContainsKey(args);
        }


        public bool MsgProcess(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                Debug.LogWarningFormat("发放奖励【{0}】失败.", "null or empty");
                return false;
            }

            UnityAction<string> msgProcessEvent = null;
            Debug.LogFormat("发送奖励，参数:{0}.", args);
            if (m_MsgDic.TryGetValue(args, out msgProcessEvent))
            {
                msgProcessEvent(args);

                return true;
            }

            Debug.LogErrorFormat("发放奖励{0}失败.", args);
            return false;
        }
    }
}