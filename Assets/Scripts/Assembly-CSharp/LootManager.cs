using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

public class LootManager : Singleton<LootManager>
{
	public Transform DropFrom;

	internal List<Loot> Uncollected = new List<Loot>();

	internal List<Loot> AllLoot = new List<Loot>();

	internal List<Loot> Loots = new List<Loot>();

	public float duration = 0.3f;

	public AudioResource sellLootSFX;

	public AudioResource sellArtifactSFX;

	public AnimationCurve speedCurve;

	public Transform UncollectedParent;

	public PrestigeSkill ArtifactValueSkill;

	public BigInteger Money => SaveManager.Current.SaveData.Money;

	public int Tickets => SaveManager.Current.SaveData.TicketCount;

	private void Start()
	{
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnRestart = (UnityAction)Delegate.Combine(current.OnRestart, new UnityAction(OnRestart));
		GameEvents current2 = Singleton<GameEvents>.Current;
		current2.OnPrestigeChange = (UnityAction)Delegate.Combine(current2.OnPrestigeChange, new UnityAction(OnRestart));
	}

	private void OnRestart()
	{
		Loot[] array = UnityEngine.Object.FindObjectsByType<Loot>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			UnityEngine.Object.Destroy(array[i].gameObject);
		}
	}

	private IEnumerator Do_Add(Loot loot, bool silent = false)
	{
		_ = duration;
		_ = loot.transform.position;
		Singleton<GameEvents>.Current.OnLootPickedUp?.Invoke(loot);
		yield return StartCoroutine(Do_JustAdd(loot));
		if (!Loots.Contains(loot))
		{
			Loots.Add(loot);
		}
		if (loot.Artifact == null)
		{
			AddMoney(loot.FinalValue);
		}
		StartCoroutine(Do_Sell(loot));
	}

	private IEnumerator Do_JustAdd(Loot loot)
	{
		float duration = this.duration;
		float done = 0f;
		UnityEngine.Vector3 startPos = loot.transform.position;
		while (done < duration)
		{
			yield return null;
			done += Time.deltaTime;
			float time = done / duration;
			loot.transform.position = UnityEngine.Vector3.Lerp(startPos, DropFrom.position, speedCurve.Evaluate(time));
		}
		loot.transform.position = DropFrom.position;
		loot.Rigidbody.isKinematic = false;
		if (!Loots.Contains(loot))
		{
			Loots.Add(loot);
		}
	}

	private IEnumerator Do_Sell(Loot loot)
	{
		Loots.Remove(loot);
		if (loot.Artifact == null)
		{
			Singleton<GameEvents>.Current.OnLootSold?.Invoke(loot);
		}
		yield return null;
		UnityEngine.Object.Destroy(loot.gameObject);
	}

	private void OnTriggerExit(Collider other)
	{
		Loot componentInParent = other.GetComponentInParent<Loot>(includeInactive: true);
		if (componentInParent != null && Loots.Contains(componentInParent))
		{
			StartCoroutine(Do_JustAdd(componentInParent));
		}
	}

	public void AddMoney(int amount)
	{
		SaveManager.Current.SaveData.Money += (BigInteger)amount;
		SaveManager.Current.SaveData.Money = Money;
		Singleton<GameEvents>.Current.OnMoneyAdded?.Invoke(amount);
	}

	private void Spend(int amount)
	{
		if (Money < amount)
		{
			Singleton<GameEvents>.Current.OnNotEnoughMoney?.Invoke();
			return;
		}
		new List<Loot>();
		SaveManager.Current.SaveData.Money -= (BigInteger)amount;
		SaveManager.Current.SaveData.Money = Money;
		Singleton<GameEvents>.Current.OnMoneySpent?.Invoke(amount);
	}

	public bool TrySpend(int amount)
	{
		if (amount > Money)
		{
			Singleton<GameEvents>.Current.OnTrySpendDeclined?.Invoke();
			return false;
		}
		Spend(amount);
		return true;
	}

	public void InstantSell(Loot loot)
	{
		if (!(loot == null) && loot.CanBeTaken)
		{
			loot.Collected();
			Singleton<GameEvents>.Current.OnLootSold?.Invoke(loot);
			Singleton<Shaker>.Current.ShakeSellKick();
			Singleton<AudioPool>.Current.Play(sellLootSFX, loot.transform.position);
			if (loot.FinalValue > 0)
			{
				Singleton<FloatingTextPool>.Current.SpawnMoney(loot.DisplayName + " $" + NumFormat.ToM1Decimal(loot.FinalValue) + " ", loot.transform.position);
			}
			AddMoney(loot.FinalValue);
			UnityEngine.Object.Destroy(loot.gameObject);
			loot.CanBeTaken = false;
		}
	}

	public void InstantSellArtifact(Artifact artifact)
	{
		if (!(artifact == null) && artifact.Loot.CanBeTaken)
		{
			artifact.Loot.Collected();
			SaveManager.Current.AddArtifact(artifact);
			Singleton<Shaker>.Current.ShakeSellKick();
			Singleton<AudioPool>.Current.Play(sellArtifactSFX, artifact.transform.position);
			int num = Mathf.RoundToInt(ArtifactValueSkill.FinalValue);
			Singleton<FloatingTextPool>.Current.SpawnArtifact($"+{num} {artifact.DisplayName}", artifact.transform.position);
			UnityEngine.Object.Destroy(artifact.gameObject);
			artifact.Loot.CanBeTaken = false;
		}
	}
}
