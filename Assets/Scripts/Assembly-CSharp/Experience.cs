using System;
using UnityEngine.Events;

public class Experience : Singleton<Experience>
{
	public int baseNumber = 25;

	public int additionPerLevel = 50;

	public int CurrentXP;

	public int Level = 1;

	public int CurrentRequirement => baseNumber + (Level - 1) * additionPerLevel;

	public int CurrentXPNeeded => CurrentRequirement - CurrentXP;

	public float LevelUpRatio => (float)CurrentXP / (float)CurrentRequirement;

	private void Start()
	{
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnLootPickedUp = (UnityAction<Loot>)Delegate.Combine(current.OnLootPickedUp, new UnityAction<Loot>(OnLootPickedUp));
	}

	private void OnLootPickedUp(Loot loot)
	{
		int finalValue = loot.FinalValue;
		CurrentXP += finalValue;
		while (CurrentXP >= CurrentRequirement)
		{
			LevelUp();
		}
	}

	private void LevelUp()
	{
		if (CurrentXP >= CurrentRequirement)
		{
			CurrentXP -= CurrentRequirement;
			Level++;
		}
	}
}
