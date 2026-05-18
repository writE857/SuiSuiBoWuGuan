//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using UnityEngine;

namespace PublicComponentCenter
{
    /// <summary>
    /// 组件基类
    /// </summary>
    public class ComponentBase : MonoBehaviour
    {
        public virtual void Awake()
        {
            //ComponentCenter.RegisterComponent(this);
        }

        public virtual void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }

        public virtual void Shutdown()
        {
        }
    }
}