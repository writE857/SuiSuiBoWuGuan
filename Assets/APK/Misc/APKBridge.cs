using System;
namespace AdGeneric{}
public class AdTotalManager
{
    public class Instance
    {
        public static void ShowBlackAd()
        {
            AdUtils.ShowBlackAd();
        }
        public static void ShowWhiteAd()
        {
            AdUtils.ShowWhiteAd();
        }
        public static void ShowBox()
        {
            AdUtils.ShowBox();
        }
        public static void Show(Addition addition) => ShowBox();

        public static void ShowRewardAd(string callBackObjectName, string callBackMethodName, string param = null)
        {
            AdUtils.ShowRewardAd(callBackObjectName, callBackMethodName, param);
        }
    }
}
public enum Addition
{
    宝箱,
}

namespace AD.Runtime.Tools.Tools
{
}

public class ADManager
{
    public class Instance
    {
        public static void ShowBlackAd()
        {
            AdUtils.ShowBlackAd();
        }
        public static void ShowWhiteAd()
        {
            AdUtils.ShowWhiteAd();
        }
        public static void ShowBox()
        {
            AdUtils.ShowBox();
        }
        public static void Show(Addition addition) => ShowBox();
		
		public static void ShowRewardAD(string callBackObjectName, string callBackMethodName, string param = null,Action onFailure=null)
        {
            AdUtils.ShowRewardAD(callBackObjectName, callBackMethodName, param);
        }
		public static void ShowRewardAD(Action onSuccess,Action onFailure=null)
        {
            AdUtils.ShowReward(onSuccess);
        }
    }
}