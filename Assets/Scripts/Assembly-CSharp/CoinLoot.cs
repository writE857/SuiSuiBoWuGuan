using System;
using UnityEngine;
using UnityEngine.Events;

public class CoinLoot : MonoBehaviour
{
	public Loot Loot;

	public PrestigeSkill DoubleLuckyCoinValueSkill;

	private void Start()
	{
		Loot loot = Loot;
		loot.OnCollected = (UnityAction)Delegate.Combine(loot.OnCollected, new UnityAction(OnCollected));
	}

	private void OnCollected()
	{
		int num = ((!DoubleLuckyCoinValueSkill.IsUnlocked) ? 1 : 2);
		SaveManager.Current.SaveData.CoinValue += num;
		Singleton<FloatingTextPool>.Current.SpawnCoin($"+{num} 幸运币！", Singleton<FlipCoin>.Current.transform.position);
	}
}
