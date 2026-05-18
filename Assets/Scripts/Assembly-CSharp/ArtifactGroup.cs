using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class ArtifactGroup : ScriptableObject
{
	[Header("Group")]
	public string DisplayName;

	[Header("DO NOT CHANGE!!!!!!!")]
	public string GROUPID = "";

	public Sprite Barcode;

	public List<Artifact> Artifacts = new List<Artifact>();

	public List<int> RequiredLevels = new List<int>();

	public Loot HighestLoot;

	public SteamAchievementData OnEveryArtifactFoundAchievement;

	[Header("Health")]
	public int BaseHP = 1;

	public int HPIncrement = 1;

	public List<int> HPOverrides = new List<int>();

	[Header("Price")]
	public int BasePrice = 1;

	public float PriceExponent = 1.4f;

	public List<int> PriceOverrides = new List<int>();

	[Header("Runtime")]
	public CubeModel prefab;

	public Material Material;

	public int ItemCountTester = 1;

	public List<int> CountPerArtifactNeeded = new List<int> { 1, 2, 4, 6, 8, 10, 12, 14, 16, 20 };

	public List<int> CountPerGroupNeeded = new List<int> { 4, 8, 16, 24, 32, 40, 48, 56, 64, 80 };

	public List<int> SumsPerArtifactNeeded = new List<int>();

	public List<int> SumsPerGroupNeeded = new List<int>();

	[Header("Prestige")]
	public PrestigeSkill MinLootTierSkill;

	public PrestigeSkill MaxLootTierSkill;

	public PrestigeSkill IncomeExtraSkill;

	public List<int> MineralCount = new List<int>();

	public Vector3 startScale = new Vector3(1f, 0.5f, 1f);

	public Vector3 endScale = new Vector3(1f, 1.5f, 1f);

	[TextArea]
	public string Description = "";

	public const int ScaleLevelCap = 25;

	public float CoinChanceMultiplier = 1f;

	public float TicketChanceMultiplier = 1f;

	public int ExtraMinLootCount;

	public int ExtraMaxLootCount;

	public int PriorityIndex;

	private static ArtifactGroup _sellGroup;

	private static ArtifactGroup _coinGroup;

	private static ArtifactGroup _hammerGroup;

	public List<Artifact> AvailableArtifacts
	{
		get
		{
			List<Artifact> list = new List<Artifact>();
			for (int i = 0; i < Artifacts.Count; i++)
			{
				if (i == 0)
				{
					list.Add(Artifacts[i]);
					continue;
				}
				int num = RequiredLevels[i];
				if (Level >= num)
				{
					list.Add(Artifacts[i]);
				}
			}
			return list;
		}
	}

	public int MinLootTierExtra
	{
		get
		{
			if (!(MinLootTierSkill == null))
			{
				return Mathf.RoundToInt(MinLootTierSkill.FinalValue);
			}
			return 0;
		}
	}

	public int MaxLootTierExtra
	{
		get
		{
			if (!(MaxLootTierSkill == null))
			{
				return Mathf.RoundToInt(MaxLootTierSkill.FinalValue);
			}
			return 0;
		}
	}

	public float IncomeMultiplier
	{
		get
		{
			if (!(IncomeExtraSkill == null))
			{
				return 1f + 0.1f * (float)Level + IncomeExtraSkill.FinalValue;
			}
			return 1f;
		}
	}

	public float debugMultiplier
	{
		get
		{
			if (!Application.isPlaying)
			{
				return 1f;
			}
			return IncomeMultiplier;
		}
	}

	public bool IsActiveSave
	{
		get
		{
			if (Application.isPlaying && SaveManager.Current != null)
			{
				return SaveManager.Current.SaveData.IsGameStarted;
			}
			return false;
		}
	}

	public bool IsMaxLevel
	{
		get
		{
			List<int> sumsPerArtifactNeeded = SumsPerArtifactNeeded;
			if (sumsPerArtifactNeeded == null || sumsPerArtifactNeeded.Count <= 0)
			{
				return true;
			}
			return SumsPerGroupNeeded.Last() <= ItemFoundCount;
		}
	}

	public int ItemFoundCount
	{
		get
		{
			if (IsActiveSave)
			{
				return Artifacts.Sum((Artifact a) => SaveManager.Current.GetArtifactData(a).CountFound);
			}
			return ItemCountTester;
		}
	}

	public int Level
	{
		get
		{
			if (!IsMaxLevel)
			{
				return SumsPerGroupNeeded.FindIndex((int a) => a > ItemFoundCount) + 1;
			}
			return SumsPerGroupNeeded.Count;
		}
	}

	public StoneTierSaveData SaveData => SaveManager.Current.GetBrickSave(GROUPID);

	public int CurrentPrice
	{
		get
		{
			if (!Application.isPlaying)
			{
				return GetPriceAtLevel(Level);
			}
			if (!SaveData.IsNextFree)
			{
				return GetPriceAtLevel(Level);
			}
			return 0;
		}
	}

	public int CurrentLevelXP
	{
		get
		{
			if (!IsMaxLevel)
			{
				return ItemFoundCount - ((Level != 1) ? SumsPerGroupNeeded[Level - 2] : 0);
			}
			return CountPerGroupNeeded.Last();
		}
	}

	public int CurrentXPRequired
	{
		get
		{
			if (!IsMaxLevel)
			{
				return CountPerGroupNeeded[Level - 1];
			}
			return CountPerGroupNeeded.Last();
		}
	}

	public float XPRatio => (float)CurrentLevelXP / (float)CurrentXPRequired;

	public int CurrentMaxHP => GetMaxHPAtLevel(Level);

	public float LevelRatio => Mathf.Clamp01((float)Level / 25f);

	public Vector3 CurrentScale => Vector3.Lerp(startScale, endScale, LevelRatio);

	public string HPScaling
	{
		get
		{
			string text = "";
			for (int i = 0; i < 10; i++)
			{
				text = text + GetMaxHPAtLevel(i + 1) + ", ";
			}
			return text;
		}
	}

	public string PriceScaling
	{
		get
		{
			string text = "";
			for (int i = 0; i < 10; i++)
			{
				text = text + GetPriceAtLevel(i + 1) + ", ";
			}
			return text;
		}
	}

	public static ArtifactGroup SellGroup
	{
		get
		{
			if (_sellGroup == null)
			{
				_sellGroup = new ArtifactGroup();
				_sellGroup.GROUPID = "SELL";
				_sellGroup.DisplayName = "已售";
				_sellGroup.PriorityIndex = 9;
			}
			return _sellGroup;
		}
	}

	public static ArtifactGroup CoinGroup
	{
		get
		{
			if (_coinGroup == null)
			{
				_coinGroup = new ArtifactGroup();
				_coinGroup.GROUPID = "COIN";
				_coinGroup.DisplayName = "幸运币";
				_coinGroup.PriorityIndex = 10;
			}
			return _coinGroup;
		}
	}

	public static ArtifactGroup HammerGroup
	{
		get
		{
			if (_hammerGroup == null)
			{
				_hammerGroup = new ArtifactGroup();
				_hammerGroup.GROUPID = "HAMMER";
				_hammerGroup.DisplayName = "锤子";
				_hammerGroup.PriorityIndex = 11;
			}
			return _hammerGroup;
		}
	}

	private void OnValidate()
	{
		SumsPerArtifactNeeded.Clear();
		for (int i = 0; i < CountPerArtifactNeeded.Count; i++)
		{
			SumsPerArtifactNeeded.Add(CountPerArtifactNeeded.Take(i + 1).Sum());
		}
		SumsPerGroupNeeded.Clear();
		for (int j = 0; j < CountPerGroupNeeded.Count; j++)
		{
			SumsPerGroupNeeded.Add(CountPerGroupNeeded.Take(j + 1).Sum());
		}
	}

	public int GetMaxHPAtLevel(int level)
	{
		int baseHP = BaseHP;
		baseHP = HPOverrides.ElementAtOrDefault(level - 1);
		if (baseHP == 0)
		{
			baseHP = BaseHP;
			baseHP += HPIncrement * (level - 1);
		}
		return baseHP;
	}

	public int GetPriceAtLevel(int level)
	{
		int num = PriceOverrides.ElementAtOrDefault(level - 1);
		if (num == 0)
		{
			num = Mathf.RoundToInt((float)BasePrice * Mathf.Pow(PriceExponent, level - 1));
			num /= 10;
			num *= 10;
		}
		return num;
	}

	public int UnlockedAt(Artifact item)
	{
		int index = Artifacts.IndexOf(item);
		return RequiredLevels[index];
	}

	public int GetArtifactsRequired(Artifact artifact)
	{
		if (IsAvailable(artifact))
		{
			return 0;
		}
		int num = UnlockedAt(artifact);
		return SumsPerGroupNeeded.ElementAtOrDefault(num - 2);
	}

	public bool IsAvailable(Artifact artifact)
	{
		int num = UnlockedAt(artifact);
		if (Level >= num)
		{
			return true;
		}
		return false;
	}
}
