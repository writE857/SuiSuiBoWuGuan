using System;
using AdEvent;
using PublicComponentCenter;
using UnityEngine;
using AdNotification = AdEvent.Notification;


public static class AdUtils
{
    public static string SetColor(this string s, Color color) => $"<color=#{color.ToHexString()}>{s}</color>";
    public static string SetSize(this string s, int size) => $"<size={size}>{s}</size>";


    public static string ToHexString(this Color color)
    {
        const int mask = 0b1111;
        int r = (int) (color.r * 255),
            g = (int) (color.g * 255),
            b = (int) (color.b * 255),
            a = (int) (color.a * 255);
        return
            $"{Hex[r >> 4 & mask]}{Hex[r & mask]}{Hex[g >> 4 & mask]}{Hex[g & mask]}{Hex[b >> 4 & mask]}{Hex[b & mask]}{Hex[a >> 4 & mask]}{Hex[a & mask]}";
    }

    private static readonly string[] Hex =
        {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F"};

    public static T Log<T>(this T t)
    {
        Debug.Log(t);
        return t;
    }

    public static void Notify(this string s, float seconds = AdNotification.DisplaySeconds)
        => AdNotification.Instance.Notify(s, seconds);

    public static void Notify(this string s, Color bgColor, float seconds = AdNotification.DisplaySeconds)
        => AdNotification.Instance.Notify(s, bgColor, seconds);

    public static void ShowWhiteAd()
    {
        ShowInterstitialAd();
    }

    public static void ShowBlackAd()
    {
        ShowInterstitialAd2();
    }

    public static void ShowBox() => GameEntry.Ad.BaoXiang();
    public static void ShowRewardAD(string name, string method, object args = null) => ShowReward(name, method, args);
    public static void ShowRewardAd(string name, string method, object args = null) => ShowReward(name, method, args);


    public static void ShowReward(string name, string method, object args = null)
    {
        AdEventManager.PostReward(() => GameObject.Find(name).SendMessage(method, args));
    }
    public static void ShowReward(Action onSuccess)
    {
        AdEventManager.PostReward(onSuccess);
    }
	
    public static void EmptyReward(params object[] args)
    {
        if (args == null || args.Length == 0) args = new object[] {string.Empty};
        GameEntry.Ad.ShowRewardedVideoAd(args);
    }

    public static void ShowOpenReward(params object[] args) => GameEntry.Ad.ShowOpenReward(args);

    #region obsolete

    #endregion

    [Obsolete]
    public static void ShowInterstitialAd()
    {
        var ad = GameEntry.Ad;
        ad.ShowInterstitialAd();
    }

    [Obsolete]
    public static void ShowInterstitialAd2()
    {
        var ad = GameEntry.Ad;
        ad.ShowInterstitialAd2();
    }

    [Obsolete]
    public static void BaoXiang() => ShowBox();

    [Obsolete]
    public static void BaoXiangReward(params object[] args) => EmptyReward(args);
}
