using System.Collections.Generic;
using UnityEngine;

public class Cheats : MonoBehaviour
{
	public Vector2Int MoneyAmount = new Vector2Int(10000, 20000);

	public Vector2Int TicketAmount = new Vector2Int(5, 10);

	public int artifactLevel = 3;

	public List<int> artifactLevels = new List<int>();

	public void UnlockAll()
	{
		Singleton<GameResources>.Current.Artifacts.Entries.ForEach(delegate(Artifact a)
		{
			SaveManager.Current.AddArtifact(a);
		});
	}

	public void AddMoney()
	{
		Singleton<LootManager>.Current.AddMoney(MoneyAmount.GetRandomBetweenXY());
	}

	public void ResetMoney()
	{
		SaveManager.Current.SaveData.Money = 0;
	}

	public void AddTickets()
	{
		SaveManager.Current.SaveData.TicketCount += TicketAmount.GetRandomBetweenXY();
	}

	public void ResetTickets()
	{
		SaveManager.Current.SaveData.TicketCount = 0;
	}

	public void SetAllArtifactLevelTo()
	{
		foreach (Artifact entry in Singleton<GameResources>.Current.Artifacts.Entries)
		{
			SaveManager.Current.GetArtifactData(entry).CountFound = SumCountNeededForLevel(entry, artifactLevel);
		}
	}

	private int SumCountNeededForLevel(Artifact artifact, int level)
	{
		int num = 0;
		ArtifactGroup artifactGroup = artifact.ArtifactGroup;
		for (int i = 0; i < level; i++)
		{
			num += artifactGroup.CountPerArtifactNeeded[i];
		}
		return num;
	}

	public void SetAllArtifactLevelsTo()
	{
		List<Artifact> entries = Singleton<GameResources>.Current.Artifacts.Entries;
		if (entries.Count != artifactLevels.Count)
		{
			Debug.LogError("Not the same size");
			return;
		}
		for (int i = 0; i < entries.Count; i++)
		{
			SaveManager.Current.GetArtifactData(entries[i]).CountFound = SumCountNeededForLevel(entries[i], artifactLevels[i]);
		}
	}
}
