using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class GameSession : Singleton<GameSession>
{
	public List<SteamAchievementData> HammerAchievements = new List<SteamAchievementData>();

	public SteamAchievementData WelcomeAchievement;

	public SteamAchievementData PerfectStoneAchievement;

	public bool IsInMenu = true;

	public GameObject EndGameCanvas;

	public BigInteger Money => Singleton<LootManager>.Current.Money;

	public bool IsGameStarted => SaveManager.Current.SaveData.IsGameStarted;

	private void OnEnable()
	{
		EndGameCanvas.SetActiveSmart(newState: false);
	}

	internal void StartNewGame()
	{
		SaveManager.Current.SaveData.IsGameStarted = true;
		Singleton<SteamBasics>.Current.Add(WelcomeAchievement);
		IsInMenu = false;
	}

	internal void ContinueSavedGame()
	{
		SaveManager.Current.SaveData.IsGameStarted = true;
		IsInMenu = false;
	}

	public void PrestigeRestart()
	{
		SaveManager.Current.SaveData.IsGameStarted = true;
		IsInMenu = false;
	}

	public void PlayEnd()
	{
		EndGameCanvas.SetActiveSmart(newState: true);
		Singleton<SteamBasics>.Current.Add(PerfectStoneAchievement);
	}
}
