using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public static class PlayerPrefsSaveBackend
{
	private const string Prefix = "SaveData.";

	private const int Version = 1;

	private static string Key(string suffix)
	{
		return Prefix + suffix;
	}

	public static bool HasSave()
	{
		return PlayerPrefs.HasKey(Key("Version"));
	}

	public static SaveData Load()
	{
		if (!HasSave())
		{
			return null;
		}
		SaveData saveData = new SaveData();
		saveData.Money = GetBigInteger("Money", saveData.Money);
		saveData.CoinHeads = PlayerPrefs.GetInt(Key("CoinHeads"), saveData.CoinHeads);
		saveData.CoinValue = PlayerPrefs.GetInt(Key("CoinValue"), saveData.CoinValue);
		saveData.IsGameStarted = GetBool("IsGameStarted", saveData.IsGameStarted);
		saveData.TicketCount = PlayerPrefs.GetInt(Key("TicketCount"), saveData.TicketCount);
		saveData.PrestigeRestarts = PlayerPrefs.GetInt(Key("PrestigeRestarts"), saveData.PrestigeRestarts);
		saveData.visitorCount = PlayerPrefs.GetInt(Key("visitorCount"), saveData.visitorCount);
		saveData.goldenVisitorCount = PlayerPrefs.GetInt(Key("goldenVisitorCount"), saveData.goldenVisitorCount);
		saveData.BricksBought = PlayerPrefs.GetInt(Key("BricksBought"), saveData.BricksBought);
		saveData.GameTimeTicks = GetLong("GameTimeTicks", saveData.GameTimeTicks);
		saveData.HammerHitCount = PlayerPrefs.GetInt(Key("HammerHitCount"), saveData.HammerHitCount);
		saveData.TotalGameSeconds = PlayerPrefs.GetFloat(Key("TotalGameSeconds"), saveData.TotalGameSeconds);
		saveData.StoneTiers = LoadStoneTiers();
		saveData.Artifacts = LoadArtifacts();
		saveData.Upgrades = LoadUpgrades();
		saveData.Prestiges = LoadPrestiges();
		saveData.IncomeBarDatas = LoadIncomeBarDatas();
		saveData.BrickSaveData = LoadBrickSaveData();
		return saveData;
	}

	public static void Save(SaveData saveData)
	{
		PlayerPrefs.SetInt(Key("Version"), Version);
		SetBigInteger("Money", saveData.Money);
		PlayerPrefs.SetInt(Key("CoinHeads"), saveData.CoinHeads);
		PlayerPrefs.SetInt(Key("CoinValue"), saveData.CoinValue);
		SetBool("IsGameStarted", saveData.IsGameStarted);
		PlayerPrefs.SetInt(Key("TicketCount"), saveData.TicketCount);
		PlayerPrefs.SetInt(Key("PrestigeRestarts"), saveData.PrestigeRestarts);
		PlayerPrefs.SetInt(Key("visitorCount"), saveData.visitorCount);
		PlayerPrefs.SetInt(Key("goldenVisitorCount"), saveData.goldenVisitorCount);
		PlayerPrefs.SetInt(Key("BricksBought"), saveData.BricksBought);
		SetLong("GameTimeTicks", saveData.GameTimeTicks);
		PlayerPrefs.SetInt(Key("HammerHitCount"), saveData.HammerHitCount);
		PlayerPrefs.SetFloat(Key("TotalGameSeconds"), saveData.TotalGameSeconds);
		SaveStoneTiers(saveData.StoneTiers);
		SaveArtifacts(saveData.Artifacts);
		SaveUpgrades(saveData.Upgrades);
		SavePrestiges(saveData.Prestiges);
		SaveIncomeBarDatas(saveData.IncomeBarDatas);
		SaveBrickSaveData(saveData.BrickSaveData);
		PlayerPrefs.Save();
	}

	public static void Delete()
	{
		DeletePrimitiveKeys();
		DeleteStoneTiers();
		DeleteArtifacts();
		DeleteUpgrades();
		DeletePrestiges();
		DeleteIncomeBarDatas();
		DeleteBrickSaveData();
		PlayerPrefs.Save();
	}

	private static void DeletePrimitiveKeys()
	{
		DeleteKey("Version");
		DeleteKey("Money");
		DeleteKey("CoinHeads");
		DeleteKey("CoinValue");
		DeleteKey("IsGameStarted");
		DeleteKey("TicketCount");
		DeleteKey("PrestigeRestarts");
		DeleteKey("visitorCount");
		DeleteKey("goldenVisitorCount");
		DeleteKey("BricksBought");
		DeleteKey("GameTimeTicks");
		DeleteKey("HammerHitCount");
		DeleteKey("TotalGameSeconds");
	}

	private static void DeleteStoneTiers()
	{
		int count = PlayerPrefs.GetInt(Key("StoneTiers_Count"), 0);
		for (int i = 0; i < count; i++)
		{
			DeleteKey($"StoneTiers_{i}_ID");
			DeleteKey($"StoneTiers_{i}_IsUnlocked");
			DeleteKey($"StoneTiers_{i}_IsNextFree");
		}
		DeleteKey("StoneTiers_Count");
	}

	private static List<StoneTierSaveData> LoadStoneTiers()
	{
		int count = PlayerPrefs.GetInt(Key("StoneTiers_Count"), 0);
		List<StoneTierSaveData> list = new List<StoneTierSaveData>(count);
		for (int i = 0; i < count; i++)
		{
			StoneTierSaveData item = new StoneTierSaveData
			{
				ID = PlayerPrefs.GetString(Key($"StoneTiers_{i}_ID"), string.Empty),
				IsUnlocked = GetBool($"StoneTiers_{i}_IsUnlocked", false),
				IsNextFree = GetBool($"StoneTiers_{i}_IsNextFree", false)
			};
			list.Add(item);
		}
		return list;
	}

	private static void SaveStoneTiers(List<StoneTierSaveData> stoneTiers)
	{
		DeleteStoneTiers();
		PlayerPrefs.SetInt(Key("StoneTiers_Count"), stoneTiers.Count);
		for (int i = 0; i < stoneTiers.Count; i++)
		{
			StoneTierSaveData stoneTierSaveData = stoneTiers[i];
			PlayerPrefs.SetString(Key($"StoneTiers_{i}_ID"), stoneTierSaveData.ID ?? string.Empty);
			SetBool($"StoneTiers_{i}_IsUnlocked", stoneTierSaveData.IsUnlocked);
			SetBool($"StoneTiers_{i}_IsNextFree", stoneTierSaveData.IsNextFree);
		}
	}

	private static void DeleteArtifacts()
	{
		int count = PlayerPrefs.GetInt(Key("Artifacts_Count"), 0);
		for (int i = 0; i < count; i++)
		{
			DeleteKey($"Artifacts_{i}_ID");
			DeleteKey($"Artifacts_{i}_CountFound");
		}
		DeleteKey("Artifacts_Count");
	}

	private static List<ArtifactSaveData> LoadArtifacts()
	{
		int count = PlayerPrefs.GetInt(Key("Artifacts_Count"), 0);
		List<ArtifactSaveData> list = new List<ArtifactSaveData>(count);
		for (int i = 0; i < count; i++)
		{
			ArtifactSaveData item = new ArtifactSaveData
			{
				ID = PlayerPrefs.GetString(Key($"Artifacts_{i}_ID"), string.Empty),
				CountFound = PlayerPrefs.GetInt(Key($"Artifacts_{i}_CountFound"), 0)
			};
			list.Add(item);
		}
		return list;
	}

	private static void SaveArtifacts(List<ArtifactSaveData> artifacts)
	{
		DeleteArtifacts();
		PlayerPrefs.SetInt(Key("Artifacts_Count"), artifacts.Count);
		for (int i = 0; i < artifacts.Count; i++)
		{
			ArtifactSaveData artifactSaveData = artifacts[i];
			PlayerPrefs.SetString(Key($"Artifacts_{i}_ID"), artifactSaveData.ID ?? string.Empty);
			PlayerPrefs.SetInt(Key($"Artifacts_{i}_CountFound"), artifactSaveData.CountFound);
		}
	}

	private static void DeleteUpgrades()
	{
		int count = PlayerPrefs.GetInt(Key("Upgrades_Count"), 0);
		for (int i = 0; i < count; i++)
		{
			DeleteKey($"Upgrades_{i}_ID");
			DeleteKey($"Upgrades_{i}_Level");
		}
		DeleteKey("Upgrades_Count");
	}

	private static List<UpgradeSaveData> LoadUpgrades()
	{
		int count = PlayerPrefs.GetInt(Key("Upgrades_Count"), 0);
		List<UpgradeSaveData> list = new List<UpgradeSaveData>(count);
		for (int i = 0; i < count; i++)
		{
			UpgradeSaveData item = new UpgradeSaveData
			{
				ID = PlayerPrefs.GetString(Key($"Upgrades_{i}_ID"), string.Empty),
				Level = PlayerPrefs.GetInt(Key($"Upgrades_{i}_Level"), 1)
			};
			list.Add(item);
		}
		return list;
	}

	private static void SaveUpgrades(List<UpgradeSaveData> upgrades)
	{
		DeleteUpgrades();
		PlayerPrefs.SetInt(Key("Upgrades_Count"), upgrades.Count);
		for (int i = 0; i < upgrades.Count; i++)
		{
			UpgradeSaveData upgradeSaveData = upgrades[i];
			PlayerPrefs.SetString(Key($"Upgrades_{i}_ID"), upgradeSaveData.ID ?? string.Empty);
			PlayerPrefs.SetInt(Key($"Upgrades_{i}_Level"), upgradeSaveData.Level);
		}
	}

	private static void DeletePrestiges()
	{
		int count = PlayerPrefs.GetInt(Key("Prestiges_Count"), 0);
		for (int i = 0; i < count; i++)
		{
			DeleteKey($"Prestiges_{i}_ID");
			DeleteKey($"Prestiges_{i}_Level");
		}
		DeleteKey("Prestiges_Count");
	}

	private static List<PrestigeSaveData> LoadPrestiges()
	{
		int count = PlayerPrefs.GetInt(Key("Prestiges_Count"), 0);
		List<PrestigeSaveData> list = new List<PrestigeSaveData>(count);
		for (int i = 0; i < count; i++)
		{
			PrestigeSaveData item = new PrestigeSaveData
			{
				ID = PlayerPrefs.GetString(Key($"Prestiges_{i}_ID"), string.Empty),
				Level = PlayerPrefs.GetInt(Key($"Prestiges_{i}_Level"), 0)
			};
			list.Add(item);
		}
		return list;
	}

	private static void SavePrestiges(List<PrestigeSaveData> prestiges)
	{
		DeletePrestiges();
		PlayerPrefs.SetInt(Key("Prestiges_Count"), prestiges.Count);
		for (int i = 0; i < prestiges.Count; i++)
		{
			PrestigeSaveData prestigeSaveData = prestiges[i];
			PlayerPrefs.SetString(Key($"Prestiges_{i}_ID"), prestigeSaveData.ID ?? string.Empty);
			PlayerPrefs.SetInt(Key($"Prestiges_{i}_Level"), prestigeSaveData.Level);
		}
	}

	private static void DeleteIncomeBarDatas()
	{
		int count = PlayerPrefs.GetInt(Key("IncomeBarDatas_Count"), 0);
		for (int i = 0; i < count; i++)
		{
			int roomCount = PlayerPrefs.GetInt(Key($"IncomeBarDatas_{i}_RoomIncomeDatas_Count"), 0);
			for (int j = 0; j < roomCount; j++)
			{
				DeleteKey($"IncomeBarDatas_{i}_RoomIncomeDatas_{j}_ID");
				DeleteKey($"IncomeBarDatas_{i}_RoomIncomeDatas_{j}_Income");
			}
			DeleteKey($"IncomeBarDatas_{i}_StartTime");
			DeleteKey($"IncomeBarDatas_{i}_RoomIncomeDatas_Count");
		}
		DeleteKey("IncomeBarDatas_Count");
	}

	private static List<IncomeBarData> LoadIncomeBarDatas()
	{
		int count = PlayerPrefs.GetInt(Key("IncomeBarDatas_Count"), 0);
		List<IncomeBarData> list = new List<IncomeBarData>(count);
		for (int i = 0; i < count; i++)
		{
			IncomeBarData incomeBarData = new IncomeBarData
			{
				startTime = PlayerPrefs.GetInt(Key($"IncomeBarDatas_{i}_StartTime"), 0),
				RoomIncomeDatas = new List<RoomIncomeData>()
			};
			int roomCount = PlayerPrefs.GetInt(Key($"IncomeBarDatas_{i}_RoomIncomeDatas_Count"), 0);
			for (int j = 0; j < roomCount; j++)
			{
				RoomIncomeData item = new RoomIncomeData
				{
					ID = PlayerPrefs.GetString(Key($"IncomeBarDatas_{i}_RoomIncomeDatas_{j}_ID"), string.Empty),
					Income = PlayerPrefs.GetInt(Key($"IncomeBarDatas_{i}_RoomIncomeDatas_{j}_Income"), 0)
				};
				incomeBarData.RoomIncomeDatas.Add(item);
			}
			list.Add(incomeBarData);
		}
		return list;
	}

	private static void SaveIncomeBarDatas(List<IncomeBarData> incomeBarDatas)
	{
		DeleteIncomeBarDatas();
		PlayerPrefs.SetInt(Key("IncomeBarDatas_Count"), incomeBarDatas.Count);
		for (int i = 0; i < incomeBarDatas.Count; i++)
		{
			IncomeBarData incomeBarData = incomeBarDatas[i];
			PlayerPrefs.SetInt(Key($"IncomeBarDatas_{i}_StartTime"), incomeBarData.startTime);
			PlayerPrefs.SetInt(Key($"IncomeBarDatas_{i}_RoomIncomeDatas_Count"), incomeBarData.RoomIncomeDatas.Count);
			for (int j = 0; j < incomeBarData.RoomIncomeDatas.Count; j++)
			{
				RoomIncomeData roomIncomeData = incomeBarData.RoomIncomeDatas[j];
				PlayerPrefs.SetString(Key($"IncomeBarDatas_{i}_RoomIncomeDatas_{j}_ID"), roomIncomeData.ID ?? string.Empty);
				PlayerPrefs.SetInt(Key($"IncomeBarDatas_{i}_RoomIncomeDatas_{j}_Income"), roomIncomeData.Income);
			}
		}
	}

	private static void DeleteBrickSaveData()
	{
		int count = PlayerPrefs.GetInt(Key("BrickSaveData_IndicesAlive_Count"), 0);
		for (int i = 0; i < count; i++)
		{
			DeleteKey($"BrickSaveData_IndicesAlive_{i}");
		}
		DeleteKey("BrickSaveData_GroupID");
		DeleteKey("BrickSaveData_seed");
		DeleteKey("BrickSaveData_IndicesAlive_Count");
	}

	private static BrickSaveData LoadBrickSaveData()
	{
		BrickSaveData brickSaveData = new BrickSaveData
		{
			GroupID = PlayerPrefs.GetString(Key("BrickSaveData_GroupID"), string.Empty),
			seed = PlayerPrefs.GetInt(Key("BrickSaveData_seed"), 0),
			IndicesAlive = new List<int>()
		};
		int count = PlayerPrefs.GetInt(Key("BrickSaveData_IndicesAlive_Count"), 0);
		for (int i = 0; i < count; i++)
		{
			brickSaveData.IndicesAlive.Add(PlayerPrefs.GetInt(Key($"BrickSaveData_IndicesAlive_{i}"), 0));
		}
		return brickSaveData;
	}

	private static void SaveBrickSaveData(BrickSaveData brickSaveData)
	{
		DeleteBrickSaveData();
		PlayerPrefs.SetString(Key("BrickSaveData_GroupID"), brickSaveData.GroupID ?? string.Empty);
		PlayerPrefs.SetInt(Key("BrickSaveData_seed"), brickSaveData.seed);
		PlayerPrefs.SetInt(Key("BrickSaveData_IndicesAlive_Count"), brickSaveData.IndicesAlive.Count);
		for (int i = 0; i < brickSaveData.IndicesAlive.Count; i++)
		{
			PlayerPrefs.SetInt(Key($"BrickSaveData_IndicesAlive_{i}"), brickSaveData.IndicesAlive[i]);
		}
	}

	private static void SetBool(string name, bool value)
	{
		PlayerPrefs.SetInt(Key(name), value ? 1 : 0);
	}

	private static bool GetBool(string name, bool defaultValue)
	{
		return PlayerPrefs.GetInt(Key(name), defaultValue ? 1 : 0) == 1;
	}

	private static void SetLong(string name, long value)
	{
		PlayerPrefs.SetString(Key(name), value.ToString());
	}

	private static long GetLong(string name, long defaultValue)
	{
		string @string = PlayerPrefs.GetString(Key(name), defaultValue.ToString());
		if (long.TryParse(@string, out var result))
		{
			return result;
		}
		return defaultValue;
	}

	private static void SetBigInteger(string name, BigInteger value)
	{
		PlayerPrefs.SetString(Key(name), value.ToString());
	}

	private static BigInteger GetBigInteger(string name, BigInteger defaultValue)
	{
		string @string = PlayerPrefs.GetString(Key(name), defaultValue.ToString());
		if (BigInteger.TryParse(@string, out var result))
		{
			return result;
		}
		return defaultValue;
	}

	private static void DeleteKey(string name)
	{
		PlayerPrefs.DeleteKey(Key(name));
	}
}
