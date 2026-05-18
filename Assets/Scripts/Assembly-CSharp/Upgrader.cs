using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Upgrader : Singleton<Upgrader>
{
	[Header("Visitor Count")]
	public int VisitorsBase = 25;

	public float VisitorMultiplier = 2f;

	public UpgradeType VisitorType;

	[Header("Entry Fee")]
	public int EntryFeeBase = 1;

	public float EntryFeeMultiplier = 0.2f;

	public UpgradeType EntryFeeType;

	[Header("Hammer Damage")]
	public int HammerDamageBase = 1;

	public float HammerDamageBonus = 0.2f;

	public UpgradeType HammerDamageType;

	public List<int> Damages = new List<int>();

	[Header("Hammer Speed")]
	public float HammerSpeedBase = 1f;

	public float HammerSpeedBonus = 0.2f;

	public UpgradeType HammerSpeedType;

	[Header("Hammer Size")]
	public float HammerSizeBase = 1f;

	public float HammerSizeBonus = 0.2f;

	public UpgradeType HammerSizeType;

	[Header("Hammer Depth")]
	public float HammerDepthBase = 0.015f;

	public float HammerDepthBonus = 0.0025f;

	public UpgradeType HammerDepthType;

	public int VisitorsPerMinute => Mathf.RoundToInt((float)VisitorsBase + (float)(VisitorType.CurrentLevel - 1) * VisitorMultiplier * (float)VisitorsBase);

	public string VisitorTest
	{
		get
		{
			string text = "";
			for (int i = 1; i <= 25; i++)
			{
				text = text + Mathf.RoundToInt((float)VisitorsBase + (float)(i - 1) * VisitorMultiplier * (float)VisitorsBase) + ", ";
			}
			return text;
		}
	}

	public float EntryFee => (float)EntryFeeBase + (float)(EntryFeeType.CurrentLevel - 1) * EntryFeeMultiplier * (float)EntryFeeBase;

	public string EntryFeeTest
	{
		get
		{
			string text = "";
			for (int i = 1; i <= 25; i++)
			{
				text = text + ((float)EntryFeeBase + (float)(i - 1) * EntryFeeMultiplier * (float)EntryFeeBase).ToString("0.00") + ", ";
			}
			return text;
		}
	}

	public float HammerDamage => Damages.ElementAtOrDefault(HammerDamageType.CurrentLevel - 1);

	public string HammerDamageTest
	{
		get
		{
			string text = "";
			for (int i = 1; i <= 25; i++)
			{
				text = text + ((float)HammerDamageBase + (float)(i - 1) * HammerDamageBonus).ToString("0") + ", ";
			}
			return text;
		}
	}

	public float HammerSpeed => HammerSpeedBase + (float)(HammerSpeedType.CurrentLevel - 1) * HammerSpeedBonus;

	public string HammerSpeedTest
	{
		get
		{
			string text = "";
			for (int i = 1; i <= 25; i++)
			{
				text = text + (HammerSpeedBase + (float)(i - 1) * HammerSpeedBonus).ToString("0.0") + ", ";
			}
			return text;
		}
	}

	public float HammerSize => HammerSizeBase + (float)(HammerSizeType.CurrentLevel - 1) * HammerSizeBonus;

	public string HammerSizeTest
	{
		get
		{
			string text = "";
			for (int i = 1; i <= 25; i++)
			{
				text = text + (HammerSizeBase + (float)(i - 1) * HammerSizeBonus).ToString("0.0000") + ", ";
			}
			return text;
		}
	}

	public float HammerDepth => HammerDepthBase + (float)(HammerDepthType.CurrentLevel - 1) * HammerDepthBonus;

	public string HammerDepthTest
	{
		get
		{
			string text = "";
			for (int i = 1; i <= 10; i++)
			{
				text = text + (HammerDepthBase + (float)(i - 1) * HammerDepthBonus).ToString("0.0000") + ", ";
			}
			return text;
		}
	}
}
