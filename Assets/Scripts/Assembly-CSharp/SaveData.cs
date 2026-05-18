using System;
using System.Collections.Generic;
using System.Numerics;

[Serializable]
public class SaveData
{
	public BigInteger Money = 100;

	public List<StoneTierSaveData> StoneTiers = new List<StoneTierSaveData>();

	public List<ArtifactSaveData> Artifacts = new List<ArtifactSaveData>();

	public List<UpgradeSaveData> Upgrades = new List<UpgradeSaveData>();

	public List<PrestigeSaveData> Prestiges = new List<PrestigeSaveData>();

	public int CoinHeads = 1;

	public int CoinValue = 1;

	public bool IsGameStarted;

	public int TicketCount;

	public int PrestigeRestarts;

	public int visitorCount;

	public int goldenVisitorCount;

	public List<IncomeBarData> IncomeBarDatas = new List<IncomeBarData>();

	public BrickSaveData BrickSaveData = new BrickSaveData();

	public int BricksBought;

	public long GameTimeTicks;

	public int HammerHitCount;

	public float TotalGameSeconds;

	public SaveData()
	{
		GameTimeTicks = new TimeSpan(1, 8, 0, 0).Ticks;
	}
}
