using System;
using UnityEngine;
using UnityEngine.Events;

public class GameTime : Singleton<GameTime>
{
	public TimeSpan TimeSpan;

	public float gameSpeed = 1f;

	private int lastDays;

	private int lastHours;

	public int Days => TimeSpan.Days;

	public int Hours => TimeSpan.Hours;

	public int Minutes => TimeSpan.Minutes;

	public string PlayTimeInMinutes
	{
		get
		{
			if (Application.isPlaying)
			{
				return (SaveManager.Current.SaveData.TotalGameSeconds / 60f).ToString("0") + " 分钟";
			}
			return "-";
		}
	}

	private void Start()
	{
		TimeSpan = TimeSpan.FromTicks(SaveManager.Current.SaveData.GameTimeTicks);
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnRestart = (UnityAction)Delegate.Combine(current.OnRestart, new UnityAction(OnRestart));
	}

	private void OnRestart()
	{
		TimeSpan = TimeSpan.FromTicks(SaveManager.Current.SaveData.GameTimeTicks);
	}

	public void SetInitial(long ticks)
	{
		TimeSpan = new TimeSpan(ticks);
		lastDays = Days;
		lastHours = Hours;
	}

	private void Update()
	{
		if (Singleton<GameSession>.Current.IsGameStarted)
		{
			int milliseconds = (int)(Time.deltaTime * gameSpeed * 60f * 1000f);
			TimeSpan ts = new TimeSpan(0, 0, 0, 0, milliseconds);
			TimeSpan = TimeSpan.Add(ts);
			if (Days > lastDays)
			{
				lastDays = Days;
				Singleton<GameEvents>.Current.OnDayPassed?.Invoke();
			}
			if (Hours > lastHours)
			{
				lastHours = Hours;
				Singleton<GameEvents>.Current.OnHourPassed?.Invoke();
			}
			SaveManager.Current.SaveData.TotalGameSeconds += Time.deltaTime;
		}
	}
}
