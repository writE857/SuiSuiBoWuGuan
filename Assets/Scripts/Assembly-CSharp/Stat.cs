using System;
using System.Collections.Generic;

[Serializable]
public class Stat
{
	public StatType StatType;

	public float BaseAmount;

	public float BonusPercent;

	public float BonusFlat;

	public List<StatModifier> Modifiers = new List<StatModifier>();

	public float Value => (BaseAmount + BonusFlat) * (1f + BonusPercent);

	public void ClearBonuses()
	{
		Modifiers.Clear();
		BonusPercent = 0f;
		BonusFlat = 0f;
	}

	public void TryAdd(StatModifier statModifier)
	{
		if (!Modifiers.Contains(statModifier) && StatType == statModifier.StatType)
		{
			BonusPercent += statModifier.BonusPercent;
			BonusFlat += statModifier.BonusFlat;
			Modifiers.Add(statModifier);
		}
	}
}
