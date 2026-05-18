using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class IncomeGraph : Singleton<IncomeGraph>
{
	private IncomeBarData currentBar;

	public List<IncomeBarData> BarDatas = new List<IncomeBarData>();

	public int currentMax;

	public PrestigeSkill KeepArtifactsSkill;

	private void Start()
	{
		if (SaveManager.Current.SaveData.IncomeBarDatas.Count > 0)
		{
			BarDatas = new List<IncomeBarData>(SaveManager.Current.SaveData.IncomeBarDatas);
			currentBar = BarDatas.FirstOrDefault();
		}
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnVisitorFeePaid = (UnityAction)Delegate.Combine(current.OnVisitorFeePaid, new UnityAction(OnVisitorFeePaid));
		GameEvents current2 = Singleton<GameEvents>.Current;
		current2.OnLootSold = (UnityAction<Loot>)Delegate.Combine(current2.OnLootSold, new UnityAction<Loot>(OnLootSold));
		GameEvents current3 = Singleton<GameEvents>.Current;
		current3.OnCoinIncome = (UnityAction<int>)Delegate.Combine(current3.OnCoinIncome, new UnityAction<int>(OnCoinIncome));
		GameEvents current4 = Singleton<GameEvents>.Current;
		current4.OnHammerIncome = (UnityAction<int>)Delegate.Combine(current4.OnHammerIncome, new UnityAction<int>(OnHammerIncome));
		GameEvents current5 = Singleton<GameEvents>.Current;
		current5.OnRestart = (UnityAction)Delegate.Combine(current5.OnRestart, new UnityAction(OnRestart));
		GameEvents current6 = Singleton<GameEvents>.Current;
		current6.OnPrestigeChange = (UnityAction)Delegate.Combine(current6.OnPrestigeChange, new UnityAction(OnPrestigeRestart));
		if (BarDatas.Count != 0)
		{
			currentMax = BarDatas.Take(8).Max((IncomeBarData a) => a.SumAmount);
		}
	}

	private void OnRestart()
	{
		BarDatas.Clear();
		currentMax = 0;
	}

	private void OnPrestigeRestart()
	{
		if (!KeepArtifactsSkill.IsUnlocked)
		{
			BarDatas.Clear();
			currentMax = 0;
		}
	}

	private void Update()
	{
		int i;
		for (i = 0; (double)i < Singleton<GameTime>.Current.TimeSpan.TotalHours; i++)
		{
			if (!BarDatas.Exists((IncomeBarData a) => a.startTime == i))
			{
				currentBar = new IncomeBarData
				{
					startTime = i
				};
				BarDatas.Add(currentBar);
				BarDatas = BarDatas.OrderByDescending((IncomeBarData a) => a.startTime).ToList();
			}
		}
	}

	private void OnVisitorFeePaid()
	{
		foreach (ArtifactGroup group in Singleton<GameResources>.Current.Artifacts.Groups)
		{
			if (group.ItemFoundCount > 0)
			{
				currentBar.AddIncome(group.GROUPID, Singleton<VisitorManager>.Current.CollectionFinalValue(group));
			}
		}
		if (BarDatas.Count != 0)
		{
			currentMax = BarDatas.Take(8).Max((IncomeBarData a) => a.SumAmount);
		}
	}

	private void OnLootSold(Loot loot)
	{
		currentBar.AddIncome(ArtifactGroup.SellGroup.GROUPID, loot.FinalValue);
		if (BarDatas.Count != 0)
		{
			currentMax = BarDatas.Take(8).Max((IncomeBarData a) => a.SumAmount);
		}
	}

	private void OnCoinIncome(int amount)
	{
		currentBar.AddIncome(ArtifactGroup.CoinGroup.GROUPID, amount);
		if (BarDatas.Count != 0)
		{
			currentMax = BarDatas.Take(8).Max((IncomeBarData a) => a.SumAmount);
		}
	}

	private void OnHammerIncome(int amount)
	{
		currentBar.AddIncome(ArtifactGroup.HammerGroup.GROUPID, amount);
		if (BarDatas.Count != 0)
		{
			currentMax = BarDatas.Take(8).Max((IncomeBarData a) => a.SumAmount);
		}
	}
}
