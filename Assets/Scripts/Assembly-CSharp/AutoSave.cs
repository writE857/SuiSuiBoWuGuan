using UnityEngine;

public class AutoSave : MonoBehaviour
{
	public int intervalSeconds = 60;

	private float nextSaveTime;

	private void OnEnable()
	{
		nextSaveTime = Time.unscaledTime + intervalSeconds;
	}

	private void Update()
	{
		if (!Singleton<GameSession>.Current.IsGameStarted)
		{
			return;
		}
		if (Time.unscaledTime < nextSaveTime)
		{
			return;
		}
		SaveNow();
	}

	private void OnApplicationPause(bool pauseStatus)
	{
		if (pauseStatus)
		{
			SaveNow();
		}
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (!hasFocus)
		{
			SaveNow();
		}
	}

	private void SaveNow()
	{
		if (!Singleton<GameSession>.Current.IsGameStarted)
		{
			return;
		}
		nextSaveTime = Time.unscaledTime + intervalSeconds;
		SaveManager.Current.Save();
	}
}
