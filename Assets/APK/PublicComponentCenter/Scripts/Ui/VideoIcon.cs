using UnityEngine;

namespace PublicComponentCenter
{
    /// <summary>
    /// 激励视频角标
    /// </summary>
    public class VideoIcon : MonoBehaviour
    {
        void Awake()
        {
			AutoAdSettings.OnRefresh+=SetState;
			SetState();
        }
		void OnDestroy()
		{
			AutoAdSettings.OnRefresh-=SetState;
		}
		private void SetState()
		{
			gameObject.SetActive(GameEntry.Ad.RewardIconEnable);
		}
    }
}