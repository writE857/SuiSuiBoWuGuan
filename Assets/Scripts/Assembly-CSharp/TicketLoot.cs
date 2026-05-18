using System;
using UnityEngine;
using UnityEngine.Events;

public class TicketLoot : MonoBehaviour
{
	public Loot Loot;

	public PrestigeSkill TicketValueSkill;

	private void Start()
	{
		Loot loot = Loot;
		loot.OnCollected = (UnityAction)Delegate.Combine(loot.OnCollected, new UnityAction(OnCollected));
	}

	private void OnCollected()
	{
		int num = Mathf.RoundToInt(TicketValueSkill.FinalValue);
		SaveManager.Current.SaveData.TicketCount += num;
		Singleton<FloatingTextPool>.Current.SpawnArtifact($"{num} 张博物馆门票！", base.transform.position);
	}
}
