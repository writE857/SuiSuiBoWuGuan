using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamBasics : Singleton<SteamBasics>
{
	public string Name;

	private List<SteamAchievementData> UnlockedAchievements = new List<SteamAchievementData>();

	public bool debug;

	private IEnumerator Start()
	{
		while (true)
		{
			if (UnlockedAchievements.Count > 0 && debug)
				Debug.Log("Steam disabled, skipping " + UnlockedAchievements.Count + " achievement updates.");
			UnlockedAchievements.Clear();
			yield return null;
		}
	}

	public void Add(SteamAchievementData data)
	{
		if (data == null || string.IsNullOrWhiteSpace(data.SteamKey))
		{
			if (debug)
			{
				Debug.LogError("Wrong data :" + data.name, data);
			}
		}
		else
		{
			UnlockedAchievements.Add(data);
		}
	}

	private void Update()
	{
		Name = string.Empty;
	}
}
