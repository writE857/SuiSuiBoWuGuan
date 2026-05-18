using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HammerStats : MonoBehaviour
{
	[SerializeField]
	private Stat DamageStat;

	[SerializeField]
	private Stat SizeStat;

	[Header("NOT FRAME INDEPENDENT!!!")]
	[SerializeField]
	private Stat SpeedStat;

	[SerializeField]
	private Stat DepthStat;

	private bool didCacheStats;

	private List<Stat> stats = new List<Stat>();

	public List<StatModifier> statModifiers = new List<StatModifier>();

	public List<PrestigeSkill> HammerSkills = new List<PrestigeSkill>();

	private StatModifier hammerTierModifier;

	public float Damage => DamageStat.Value;

	public float Size => SizeStat.Value;

	public float Speed => SpeedStat.Value;

	public float Depth => DepthStat.Value;

	private void Start()
	{
		GameObject gameObject = new GameObject();
		hammerTierModifier = gameObject.AddComponent<StatModifier>();
		hammerTierModifier.BonusPercent = 0f;
		hammerTierModifier.StatType = DamageStat.StatType;
		AddModifier(hammerTierModifier);
	}

	private void Update()
	{
		DamageStat.BaseAmount = Singleton<Upgrader>.Current.HammerDamage;
		SizeStat.BaseAmount = Singleton<Upgrader>.Current.HammerSize;
		SpeedStat.BaseAmount = Singleton<Upgrader>.Current.HammerSpeed;
		DepthStat.BaseAmount = Singleton<Upgrader>.Current.HammerDepth;
		float bonusPercent = hammerTierModifier.BonusPercent;
		PrestigeSkill prestigeSkill = HammerSkills.LastOrDefault((PrestigeSkill a) => a.IsUnlocked);
		float num = 0f;
		if (prestigeSkill != null)
		{
			num = prestigeSkill.FinalValue / 100f;
		}
		if (bonusPercent != num)
		{
			hammerTierModifier.BonusPercent = num;
			RecalculateModifiers();
		}
	}

	public void AddModifier(StatModifier statModifier)
	{
		statModifier.transform.parent = base.transform;
		statModifiers.Add(statModifier);
		RecalculateModifiers();
	}

	public void RemoveModifier(StatModifier statModifier)
	{
		statModifier.transform.parent = null;
		statModifiers.Remove(statModifier);
		RecalculateModifiers();
	}

	private void RecalculateModifiers()
	{
		if (!didCacheStats)
		{
			stats = new List<Stat> { DamageStat, SizeStat, SpeedStat, DepthStat };
			didCacheStats = true;
		}
		stats.ForEach(delegate(Stat a)
		{
			a.ClearBonuses();
		});
		foreach (StatModifier item in statModifiers)
		{
			stats.ForEach(delegate(Stat a)
			{
				a.TryAdd(item);
			});
		}
	}
}
