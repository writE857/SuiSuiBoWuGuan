using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RageBar : Singleton<RageBar>
{
	public float Max = 100f;

	public float DropSpeed = 1f;

	public float hitCap = 100f;

	public float artifactLevelUpCap = 25f;

	public float artifactGroupLevelUpCap = 5f;

	public float sellValueCap = 1000f;

	public float coinHeadsCap = 100f;

	public float CurrentValue;

	public Image fillImage;

	public float Ratio => Mathf.Clamp01(CurrentValue / Max);

	private void Start()
	{
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnArtifactGroupLevelUp = (UnityAction<ArtifactGroup>)Delegate.Combine(current.OnArtifactGroupLevelUp, new UnityAction<ArtifactGroup>(OnArtifactGroupLevelUp));
		GameEvents current2 = Singleton<GameEvents>.Current;
		current2.OnArtifactLevelUp = (UnityAction<Artifact>)Delegate.Combine(current2.OnArtifactLevelUp, new UnityAction<Artifact>(OnArtifactLevelUp));
		GameEvents current3 = Singleton<GameEvents>.Current;
		current3.OnLootExtracted = (UnityAction<Loot>)Delegate.Combine(current3.OnLootExtracted, new UnityAction<Loot>(OnLootExtracted));
		GameEvents current4 = Singleton<GameEvents>.Current;
		current4.OnLootPickedUp = (UnityAction<Loot>)Delegate.Combine(current4.OnLootPickedUp, new UnityAction<Loot>(OnLootPickedUp));
		GameEvents current5 = Singleton<GameEvents>.Current;
		current5.OnHammerHit = (UnityAction<float>)Delegate.Combine(current5.OnHammerHit, new UnityAction<float>(OnHammerHit));
		GameEvents current6 = Singleton<GameEvents>.Current;
		current6.OnCoinHeads = (UnityAction<int>)Delegate.Combine(current6.OnCoinHeads, new UnityAction<int>(OnCoinHeads));
	}

	private void OnArtifactGroupLevelUp(ArtifactGroup artifactGroup)
	{
		float num = (float)artifactGroup.Level / ((float)artifactGroup.Level + artifactGroupLevelUpCap);
		CurrentValue += num;
	}

	private void OnArtifactLevelUp(Artifact artifact)
	{
		float num = (float)artifact.Level / ((float)artifact.Level + artifactLevelUpCap);
		CurrentValue += num;
	}

	private void OnLootExtracted(Loot loot)
	{
		float num = (float)loot.FinalValue / ((float)loot.FinalValue + sellValueCap);
		CurrentValue += num;
	}

	private void OnLootPickedUp(Loot loot)
	{
		float num = (float)loot.FinalValue / ((float)loot.FinalValue + sellValueCap);
		CurrentValue += num;
	}

	private void OnHammerHit(float damage)
	{
		float num = damage / (damage + hitCap);
		CurrentValue += num;
	}

	private void OnCoinHeads(int heads)
	{
		float num = (float)heads / ((float)heads + coinHeadsCap);
		CurrentValue += num;
	}

	private void Update()
	{
		CurrentValue = Mathf.Clamp(CurrentValue, 0f, Max);
		CurrentValue = Mathf.Lerp(CurrentValue, 0f, Time.deltaTime * DropSpeed);
		fillImage.fillAmount = Ratio;
	}
}
