using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
	protected static SaveManager _current;

	public SaveData SaveData;

	[Header("Prestige")]
	public PrestigeSkill KeepLuckyPrestige;

	public PrestigeSkill KeepMoneyPrestige;

	public PrestigeSkill KeepStonePrestige;

	public PrestigeSkill KeepArtifactsPrestige;

	public PrestigeSkill KeepUpgradesPrestige;

	private Dictionary<Artifact, ArtifactSaveData> ArtifactSaveDataCache = new Dictionary<Artifact, ArtifactSaveData>();

	private Dictionary<UpgradeType, UpgradeSaveData> UpgradeSaveDataCache = new Dictionary<UpgradeType, UpgradeSaveData>();

	private Dictionary<PrestigeSkill, PrestigeSaveData> PrestigeSaveDataCache = new Dictionary<PrestigeSkill, PrestigeSaveData>();

	public static SaveManager Current
	{
		get
		{
			if (_current == null)
			{
				_current = Object.FindFirstObjectByType<SaveManager>();
				_current.Load();
			}
			return _current;
		}
	}

	public void Load()
	{
		PrestigeSaveDataCache.Clear();
		UpgradeSaveDataCache.Clear();
		ArtifactSaveDataCache.Clear();
		SaveData = PlayerPrefsSaveBackend.Load();
		if (SaveData != null)
		{
			return;
		}
		Debug.Log("No save file found, new profile created");
		SaveData = new SaveData();
	}

	public void Save()
	{
		if (SaveData == null)
		{
			SaveData = new SaveData();
		}
		SaveData.Money = Singleton<GameSession>.Current.Money;
		SaveData.GameTimeTicks = Singleton<GameTime>.Current.TimeSpan.Ticks;
		if (SaveData != null)
		{
			PlayerPrefsSaveBackend.Save(SaveData);
			Debug.Log("Progress Saved");
		}
	}

	public void FULL_ClearSave()
	{
		SaveData = new SaveData();
		PrestigeSaveDataCache.Clear();
		UpgradeSaveDataCache.Clear();
		ArtifactSaveDataCache.Clear();
		PlayerPrefsSaveBackend.Delete();
		Singleton<GameEvents>.Current.OnRestart?.Invoke();
		if (SaveData != null)
		{
			PlayerPrefsSaveBackend.Save(SaveData);
			Debug.Log("Progress Reset");
		}
	}

	private void OnApplicationQuit()
	{
		AddExtraData();
		Save();
	}

	private void AddExtraData()
	{
		SaveData.IncomeBarDatas = new List<IncomeBarData>(Singleton<IncomeGraph>.Current.BarDatas);
		SaveData.visitorCount = Singleton<VisitorManager>.Current.Visitors.Where((Visitor a) => a != null && !a.IsGolden).Count();
		SaveData.goldenVisitorCount = Singleton<VisitorManager>.Current.Visitors.Where((Visitor a) => a != null && a.IsGolden).Count();
		Brick currentBrick = Singleton<BrickTable>.Current.CurrentBrick;
		if (currentBrick != null)
		{
			SaveData.BrickSaveData.GroupID = currentBrick.LootGenerator.ArtifactGroup.GROUPID;
			SaveData.BrickSaveData.IndicesAlive = currentBrick.CubeModel.GetAlivePieceIndices();
			SaveData.BrickSaveData.seed = currentBrick.Seed;
		}
	}

	private void Start()
	{
		Load();
	}

	public StoneTierSaveData GetBrickSave(string iD)
	{
		StoneTierSaveData stoneTierSaveData = SaveData.StoneTiers.FirstOrDefault((StoneTierSaveData a) => a.ID == iD);
		if (stoneTierSaveData == null)
		{
			stoneTierSaveData = new StoneTierSaveData();
			stoneTierSaveData.ID = iD;
			SaveData.StoneTiers.Add(stoneTierSaveData);
		}
		return stoneTierSaveData;
	}

	public void AddArtifact(Artifact artifact)
	{
		if (string.IsNullOrEmpty(artifact.ID))
		{
			Debug.LogError("Not a collectible or prefid wrong", artifact);
			return;
		}
		ArtifactSaveData artifactSaveData = SaveData.Artifacts.FirstOrDefault((ArtifactSaveData a) => a.ID == artifact.ID);
		if (artifactSaveData == null)
		{
			artifactSaveData = new ArtifactSaveData();
			artifactSaveData.ID = artifact.ID;
			SaveData.Artifacts.Add(artifactSaveData);
		}
		int level = artifact.Level;
		int level2 = artifact.ArtifactGroup.Level;
		artifactSaveData.CountFound += Mathf.RoundToInt(Singleton<LootManager>.Current.ArtifactValueSkill.FinalValue);
		int level3 = artifact.Level;
		int level4 = artifact.ArtifactGroup.Level;
		if (level != level3)
		{
			Singleton<GameEvents>.Current.OnArtifactLevelUp?.Invoke(artifact);
			if (artifactSaveData.CountFound == 1)
			{
				Singleton<GameEvents>.Current.OnArtifactUnlocked?.Invoke(artifact);
			}
		}
		if (level2 != level4)
		{
			Singleton<GameEvents>.Current.OnArtifactGroupLevelUp?.Invoke(artifact.ArtifactGroup);
		}
	}

	public ArtifactSaveData GetArtifactData(Artifact artifact)
	{
		ArtifactSaveDataCache.TryGetValue(artifact, out var value);
		if (value != null)
		{
			return value;
		}
		value = SaveData.Artifacts.FirstOrDefault((ArtifactSaveData a) => a.ID == artifact.ID);
		if (value == null)
		{
			value = new ArtifactSaveData();
			value.ID = artifact.ID;
			SaveData.Artifacts.Add(value);
		}
		ArtifactSaveDataCache[artifact] = value;
		return value;
	}

	public void UnlockArtifactGroup(string iD)
	{
		GetBrickSave(iD).IsUnlocked = true;
	}

	public int GetUpgradeLevel(UpgradeType upgradeType)
	{
		UpgradeSaveDataCache.TryGetValue(upgradeType, out var value);
		if (value != null)
		{
			return value.Level;
		}
		value = SaveData.Upgrades.FirstOrDefault((UpgradeSaveData a) => a.ID == upgradeType.ID);
		if (value == null)
		{
			value = new UpgradeSaveData();
			value.ID = upgradeType.ID;
			SaveData.Upgrades.Add(value);
		}
		UpgradeSaveDataCache[upgradeType] = value;
		return value.Level;
	}

	public int GetPrestigeLevel(PrestigeSkill prestigeSkill)
	{
		PrestigeSaveDataCache.TryGetValue(prestigeSkill, out var value);
		if (value != null)
		{
			return value.Level;
		}
		value = SaveData.Prestiges.FirstOrDefault((PrestigeSaveData a) => a.ID == prestigeSkill.ID);
		if (value == null)
		{
			value = new PrestigeSaveData();
			value.ID = prestigeSkill.ID;
			SaveData.Prestiges.Add(value);
		}
		PrestigeSaveDataCache[prestigeSkill] = value;
		if (value.Level > prestigeSkill.MaxLevel)
		{
			value.Level = prestigeSkill.MaxLevel;
		}
		return value.Level;
	}

	public void SetUpgradeLevel(UpgradeType upgradeType, int newLevel)
	{
		GetUpgradeLevel(upgradeType);
		SaveData.Upgrades.FirstOrDefault((UpgradeSaveData a) => a.ID == upgradeType.ID).Level = newLevel;
	}

	public void SetPrestigeLevel(PrestigeSkill prestigeSkill, int newLevel)
	{
		GetPrestigeLevel(prestigeSkill);
		SaveData.Prestiges.FirstOrDefault((PrestigeSaveData a) => a.ID == prestigeSkill.ID).Level = newLevel;
	}

	public void PRESTIGERESTART()
	{
		SaveData saveData = new SaveData();
		BigInteger money = saveData.Money;
		BigInteger bigInteger = SaveData.Money * (int)KeepMoneyPrestige.FinalValue;
		bigInteger /= (BigInteger)100;
		SaveData.Money = ((money > bigInteger) ? money : bigInteger);
		if (!KeepLuckyPrestige.IsUnlocked)
		{
			SaveData.CoinValue = saveData.CoinValue;
			SaveData.CoinHeads = saveData.CoinHeads;
		}
		SaveData.visitorCount = 0;
		SaveData.goldenVisitorCount = 0;
		SaveData.IncomeBarDatas.Clear();
		SaveData.BrickSaveData = new BrickSaveData();
		if (!KeepStonePrestige.IsUnlocked)
		{
			SaveData.StoneTiers.Clear();
		}
		if (!KeepArtifactsPrestige.IsUnlocked)
		{
			SaveData.Artifacts.Clear();
		}
		if (!KeepUpgradesPrestige.IsUnlocked)
		{
			SaveData.Upgrades.Clear();
		}
		SaveData.PrestigeRestarts++;
		ArtifactSaveDataCache.Clear();
		UpgradeSaveDataCache.Clear();
		SaveData.BricksBought = 0;
		Save();
		Singleton<GameEvents>.Current.OnPrestigeChange?.Invoke();
	}
}
