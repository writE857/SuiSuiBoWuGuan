using System.Reflection;
using AdEvent;
using PublicComponentCenter;
using UnityEngine;

public class PlatformChangedEvent : ICmdInit
{
    public void Init()
    {
        SetPlatform.OnPlatformChanged += platform =>
        {
            switch (platform)
            {
                case GamePlatform.Harmony:
                    GameEntry.Ad.CurrentAppPlatform = AppPlatform.Harmony;
                    break;
                default:
                    GameEntry.Ad.CurrentAppPlatform = AppPlatform.Android;
                    break;
            }
        };
    }
}