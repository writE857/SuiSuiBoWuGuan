#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using BigInteger = System.Numerics.BigInteger;
using Object = UnityEngine.Object;

public class MuseumSaveEditorTool : EditorWindow
{
	private const string DefaultArtifactsAssetPath = "Assets/MonoBehaviour/Artifacts.asset";
	private static readonly FieldInfo ArtifactIdField = typeof(Artifact).GetField("_ID", BindingFlags.Instance | BindingFlags.NonPublic);

	private Artifacts artifacts;
	private Vector2 scrollPosition;
	private string moneyInput = "1000000";
	private int targetArtifactLevel = 1;
	private bool markGameStarted = true;
	private bool unlockStoneGroupsWithArtifacts = true;
	private bool refreshRunningGame = true;
	private string statusMessage = "";
	private MessageType statusType = MessageType.Info;

	[MenuItem("Tools/Smash Hit Museum/Save Editor")]
	public static void Open()
	{
		MuseumSaveEditorTool window = GetWindow<MuseumSaveEditorTool>("Museum Save");
		window.minSize = new Vector2(420f, 460f);
		window.Show();
	}

	private void OnEnable()
	{
		LoadDefaultArtifactsAsset();
	}

	private void OnGUI()
	{
		if (artifacts == null)
		{
			LoadDefaultArtifactsAsset();
		}

		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
		EditorGUILayout.LabelField("Smash Hit Museum Save Editor", EditorStyles.boldLabel);
		EditorGUILayout.Space(4f);

		DrawSummary();
		EditorGUILayout.Space(8f);

		artifacts = (Artifacts)EditorGUILayout.ObjectField("Artifacts Asset", artifacts, typeof(Artifacts), false);
		markGameStarted = EditorGUILayout.ToggleLeft("Mark save as game started", markGameStarted);
		refreshRunningGame = EditorGUILayout.ToggleLeft("Refresh running game after apply", refreshRunningGame);

		EditorGUILayout.Space(8f);
		DrawMoneyTools();
		EditorGUILayout.Space(8f);
		DrawArtifactTools();
		EditorGUILayout.Space(8f);
		DrawRuntimeTools();

		if (!string.IsNullOrEmpty(statusMessage))
		{
			EditorGUILayout.Space(8f);
			EditorGUILayout.HelpBox(statusMessage, statusType);
		}

		EditorGUILayout.EndScrollView();
	}

	private void DrawSummary()
	{
		SaveData saveData = PlayerPrefsSaveBackend.Load();
		List<Artifact> validArtifacts = GetValidArtifacts();
		HashSet<string> validIds = new HashSet<string>();
		foreach (Artifact artifact in validArtifacts)
		{
			string id;
			if (TryGetArtifactId(artifact, out id))
			{
				validIds.Add(id);
			}
		}

		EditorGUILayout.BeginVertical(EditorStyles.helpBox);
		if (saveData == null)
		{
			EditorGUILayout.LabelField("Save", "No PlayerPrefs save found. Applying any action will create one.");
			EditorGUILayout.LabelField("Artifacts", validArtifacts.Count + " configured");
		}
		else
		{
			EnsureSaveLists(saveData);
			int unlockedCount = saveData.Artifacts.Count(a => a != null && a.CountFound > 0 && validIds.Contains(a.ID));
			EditorGUILayout.LabelField("Money", saveData.Money.ToString());
			EditorGUILayout.LabelField("Artifacts", unlockedCount + " / " + validArtifacts.Count + " unlocked");
			EditorGUILayout.LabelField("Artifact save rows", saveData.Artifacts.Count.ToString());
			EditorGUILayout.LabelField("Stone groups", saveData.StoneTiers.Count(a => a != null && a.IsUnlocked).ToString());
			EditorGUILayout.LabelField("Game started", saveData.IsGameStarted ? "Yes" : "No");
		}
		EditorGUILayout.EndVertical();
	}

	private void DrawMoneyTools()
	{
		EditorGUILayout.LabelField("Money", EditorStyles.boldLabel);
		moneyInput = EditorGUILayout.TextField("Amount", moneyInput);

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Add Money"))
		{
			ApplyMoney(add: true);
		}
		if (GUILayout.Button("Set Money"))
		{
			ApplyMoney(add: false);
		}
		if (GUILayout.Button("Set 0"))
		{
			moneyInput = "0";
			ApplyMoney(add: false);
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("+1K"))
		{
			ApplyMoneyPreset(new BigInteger(1000));
		}
		if (GUILayout.Button("+100K"))
		{
			ApplyMoneyPreset(new BigInteger(100000));
		}
		if (GUILayout.Button("+1M"))
		{
			ApplyMoneyPreset(new BigInteger(1000000));
		}
		if (GUILayout.Button("+1B"))
		{
			ApplyMoneyPreset(new BigInteger(1000000000));
		}
		EditorGUILayout.EndHorizontal();
	}

	private void DrawArtifactTools()
	{
		EditorGUILayout.LabelField("Artifacts", EditorStyles.boldLabel);
		if (artifacts == null)
		{
			EditorGUILayout.HelpBox("Artifacts asset was not found. Assign Assets/MonoBehaviour/Artifacts.asset manually.", MessageType.Warning);
			if (GUILayout.Button("Find Artifacts Asset"))
			{
				LoadDefaultArtifactsAsset();
			}
			return;
		}

		List<Artifact> validArtifacts = GetValidArtifacts();
		if (validArtifacts.Count == 0)
		{
			EditorGUILayout.HelpBox("The selected Artifacts asset has no valid entries with serialized IDs.", MessageType.Warning);
			return;
		}

		int maxLevel = Mathf.Max(1, validArtifacts.Max(GetMaxArtifactLevel));
		targetArtifactLevel = EditorGUILayout.IntSlider("Raise To Level", targetArtifactLevel, 1, maxLevel);
		unlockStoneGroupsWithArtifacts = EditorGUILayout.ToggleLeft("Unlock related stone groups too", unlockStoneGroupsWithArtifacts);

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Unlock All"))
		{
			RaiseAllArtifactsToCount("Unlocked all artifacts", artifact => 1);
		}
		if (GUILayout.Button("Raise To Level"))
		{
			int level = targetArtifactLevel;
			RaiseAllArtifactsToCount("Raised all artifacts to level " + level, artifact => GetCountForArtifactLevel(artifact, level));
		}
		if (GUILayout.Button("Max All"))
		{
			RaiseAllArtifactsToCount("Maxed all artifacts", GetMaxArtifactCount);
		}
		EditorGUILayout.EndHorizontal();

		if (GUILayout.Button("Unlock All Stone Groups Only"))
		{
			UnlockAllStoneGroupsOnly();
		}
	}

	private void DrawRuntimeTools()
	{
		EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
		EditorGUI.BeginDisabledGroup(!Application.isPlaying);
		if (GUILayout.Button("Force Full Runtime Refresh"))
		{
			ForceFullRuntimeRefresh();
		}
		EditorGUI.EndDisabledGroup();

		if (!Application.isPlaying)
		{
			EditorGUILayout.HelpBox("Runtime refresh is available only while Play Mode is running.", MessageType.None);
		}
	}

	private void ApplyMoney(bool add)
	{
		BigInteger amount;
		if (!TryParseBigInteger(moneyInput, out amount))
		{
			SetStatus("Money amount is not a valid integer.", MessageType.Error);
			return;
		}

		SaveData saveData = LoadOrCreateSave();
		saveData.Money = add ? saveData.Money + amount : amount;
		if (saveData.Money < BigInteger.Zero)
		{
			saveData.Money = BigInteger.Zero;
		}

		SaveAndRefresh(saveData, new List<Artifact>(), new List<Artifact>(), new List<ArtifactGroup>(), moneyChanged: true);
		SetStatus((add ? "Added money. Current money: " : "Set money. Current money: ") + saveData.Money, MessageType.Info);
	}

	private void ApplyMoneyPreset(BigInteger amount)
	{
		moneyInput = amount.ToString();
		SaveData saveData = LoadOrCreateSave();
		saveData.Money += amount;
		SaveAndRefresh(saveData, new List<Artifact>(), new List<Artifact>(), new List<ArtifactGroup>(), moneyChanged: true);
		SetStatus("Added money. Current money: " + saveData.Money, MessageType.Info);
	}

	private void RaiseAllArtifactsToCount(string actionLabel, Func<Artifact, int> targetCountProvider)
	{
		List<Artifact> validArtifacts = GetValidArtifacts();
		if (validArtifacts.Count == 0)
		{
			SetStatus("No valid artifacts found.", MessageType.Warning);
			return;
		}

		SaveData saveData = LoadOrCreateSave();
		List<Artifact> newlyUnlocked = new List<Artifact>();
		List<Artifact> levelChanged = new List<Artifact>();
		HashSet<ArtifactGroup> changedGroups = new HashSet<ArtifactGroup>();
		int changedCount = 0;

		foreach (Artifact artifact in validArtifacts)
		{
			string id;
			if (!TryGetArtifactId(artifact, out id))
			{
				continue;
			}

			ArtifactSaveData artifactSaveData = GetOrCreateArtifactSaveData(saveData, id);
			int oldCount = artifactSaveData.CountFound;
			int targetCount = Mathf.Max(0, targetCountProvider(artifact));
			int newCount = Mathf.Max(oldCount, targetCount);

			if (newCount == oldCount)
			{
				continue;
			}

			artifactSaveData.CountFound = newCount;
			changedCount++;

			if (oldCount <= 0 && newCount > 0)
			{
				newlyUnlocked.Add(artifact);
			}
			if (GetArtifactLevelFromCount(artifact, oldCount) != GetArtifactLevelFromCount(artifact, newCount))
			{
				levelChanged.Add(artifact);
			}
			if (artifact.ArtifactGroup != null)
			{
				changedGroups.Add(artifact.ArtifactGroup);
			}
		}

		int stoneGroupsChanged = 0;
		if (unlockStoneGroupsWithArtifacts)
		{
			stoneGroupsChanged = UnlockStoneGroups(saveData, validArtifacts.Select(a => a.ArtifactGroup), changedGroups);
		}

		SaveAndRefresh(saveData, newlyUnlocked, levelChanged, changedGroups.ToList(), moneyChanged: false);
		SetStatus(actionLabel + ". Changed artifacts: " + changedCount + ". Changed stone groups: " + stoneGroupsChanged + ".", MessageType.Info);
	}

	private void UnlockAllStoneGroupsOnly()
	{
		if (artifacts == null)
		{
			SetStatus("Artifacts asset was not found.", MessageType.Warning);
			return;
		}

		SaveData saveData = LoadOrCreateSave();
		HashSet<ArtifactGroup> changedGroups = new HashSet<ArtifactGroup>();
		int changedCount = UnlockStoneGroups(saveData, artifacts.Groups, changedGroups);
		SaveAndRefresh(saveData, new List<Artifact>(), new List<Artifact>(), changedGroups.ToList(), moneyChanged: false);
		SetStatus("Unlocked stone groups. Changed groups: " + changedCount + ".", MessageType.Info);
	}

	private int UnlockStoneGroups(SaveData saveData, IEnumerable<ArtifactGroup> groups, HashSet<ArtifactGroup> changedGroups)
	{
		int changedCount = 0;
		foreach (ArtifactGroup group in groups)
		{
			if (group == null || string.IsNullOrEmpty(group.GROUPID))
			{
				continue;
			}

			StoneTierSaveData stoneTierSaveData = GetOrCreateStoneTierSaveData(saveData, group.GROUPID);
			if (stoneTierSaveData.IsUnlocked)
			{
				continue;
			}

			stoneTierSaveData.IsUnlocked = true;
			changedGroups.Add(group);
			changedCount++;
		}
		return changedCount;
	}

	private void SaveAndRefresh(SaveData saveData, List<Artifact> newlyUnlocked, List<Artifact> levelChanged, List<ArtifactGroup> changedGroups, bool moneyChanged)
	{
		EnsureSaveLists(saveData);
		if (markGameStarted)
		{
			saveData.IsGameStarted = true;
		}

		PlayerPrefsSaveBackend.Save(saveData);

		if (refreshRunningGame && Application.isPlaying)
		{
			RefreshRunningGame(newlyUnlocked, levelChanged, changedGroups, moneyChanged);
		}

		Repaint();
	}

	private void RefreshRunningGame(List<Artifact> newlyUnlocked, List<Artifact> levelChanged, List<ArtifactGroup> changedGroups, bool moneyChanged)
	{
		SaveManager saveManager = Object.FindObjectOfType<SaveManager>();
		if (saveManager != null)
		{
			saveManager.Load();
		}

		GameEvents gameEvents = Object.FindObjectOfType<GameEvents>();
		if (gameEvents != null)
		{
			if (moneyChanged)
			{
				gameEvents.OnMoneyAdded?.Invoke(0);
			}

			foreach (Artifact artifact in newlyUnlocked.Where(a => a != null).Distinct())
			{
				gameEvents.OnArtifactUnlocked?.Invoke(artifact);
			}
			foreach (Artifact artifact in levelChanged.Where(a => a != null).Distinct())
			{
				gameEvents.OnArtifactLevelUp?.Invoke(artifact);
			}
			foreach (ArtifactGroup group in changedGroups.Where(g => g != null).Distinct())
			{
				gameEvents.OnArtifactGroupLevelUp?.Invoke(group);
			}
		}

		EditorApplication.QueuePlayerLoopUpdate();
		SceneView.RepaintAll();
	}

	private void ForceFullRuntimeRefresh()
	{
		if (!Application.isPlaying)
		{
			SetStatus("Play Mode is not running.", MessageType.Warning);
			return;
		}

		SaveManager saveManager = Object.FindObjectOfType<SaveManager>();
		if (saveManager != null)
		{
			saveManager.Load();
		}

		GameEvents gameEvents = Object.FindObjectOfType<GameEvents>();
		if (gameEvents != null)
		{
			gameEvents.OnMoneyAdded?.Invoke(0);
			gameEvents.OnRestart?.Invoke();
		}

		EditorApplication.QueuePlayerLoopUpdate();
		SceneView.RepaintAll();
		SetStatus("Forced runtime refresh. Current brick may be cleared by the game's restart handlers.", MessageType.Info);
	}

	private void LoadDefaultArtifactsAsset()
	{
		artifacts = AssetDatabase.LoadAssetAtPath<Artifacts>(DefaultArtifactsAssetPath);
		if (artifacts != null)
		{
			return;
		}

		string[] guids = AssetDatabase.FindAssets("t:Artifacts");
		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			Artifacts candidate = AssetDatabase.LoadAssetAtPath<Artifacts>(path);
			if (candidate != null)
			{
				artifacts = candidate;
				return;
			}
		}
	}

	private SaveData LoadOrCreateSave()
	{
		SaveData saveData = PlayerPrefsSaveBackend.Load();
		if (saveData == null)
		{
			saveData = new SaveData();
		}
		EnsureSaveLists(saveData);
		return saveData;
	}

	private static void EnsureSaveLists(SaveData saveData)
	{
		if (saveData.StoneTiers == null)
		{
			saveData.StoneTiers = new List<StoneTierSaveData>();
		}
		if (saveData.Artifacts == null)
		{
			saveData.Artifacts = new List<ArtifactSaveData>();
		}
		if (saveData.Upgrades == null)
		{
			saveData.Upgrades = new List<UpgradeSaveData>();
		}
		if (saveData.Prestiges == null)
		{
			saveData.Prestiges = new List<PrestigeSaveData>();
		}
		if (saveData.IncomeBarDatas == null)
		{
			saveData.IncomeBarDatas = new List<IncomeBarData>();
		}
		if (saveData.BrickSaveData == null)
		{
			saveData.BrickSaveData = new BrickSaveData();
		}
	}

	private ArtifactSaveData GetOrCreateArtifactSaveData(SaveData saveData, string id)
	{
		ArtifactSaveData artifactSaveData = saveData.Artifacts.FirstOrDefault(a => a != null && a.ID == id);
		if (artifactSaveData != null)
		{
			return artifactSaveData;
		}

		artifactSaveData = new ArtifactSaveData { ID = id };
		saveData.Artifacts.Add(artifactSaveData);
		return artifactSaveData;
	}

	private StoneTierSaveData GetOrCreateStoneTierSaveData(SaveData saveData, string id)
	{
		StoneTierSaveData stoneTierSaveData = saveData.StoneTiers.FirstOrDefault(a => a != null && a.ID == id);
		if (stoneTierSaveData != null)
		{
			return stoneTierSaveData;
		}

		stoneTierSaveData = new StoneTierSaveData { ID = id };
		saveData.StoneTiers.Add(stoneTierSaveData);
		return stoneTierSaveData;
	}

	private List<Artifact> GetValidArtifacts()
	{
		List<Artifact> result = new List<Artifact>();
		if (artifacts == null || artifacts.Entries == null)
		{
			return result;
		}

		HashSet<string> ids = new HashSet<string>();
		foreach (Artifact artifact in artifacts.Entries)
		{
			string id;
			if (!TryGetArtifactId(artifact, out id))
			{
				continue;
			}
			if (ids.Add(id))
			{
				result.Add(artifact);
			}
		}
		return result;
	}

	private static bool TryGetArtifactId(Artifact artifact, out string id)
	{
		id = null;
		if (artifact == null || artifact.ArtifactGroup == null)
		{
			return false;
		}

		if (ArtifactIdField != null)
		{
			object rawValue = ArtifactIdField.GetValue(artifact);
			if (!(rawValue is int) || (int)rawValue == 0)
			{
				return false;
			}

			id = "ART-" + artifact.ArtifactGroup.GROUPID + "-" + (int)rawValue;
			return !string.IsNullOrEmpty(id);
		}

		id = artifact.ID;
		return !string.IsNullOrEmpty(id);
	}

	private static int GetMaxArtifactLevel(Artifact artifact)
	{
		List<int> sums = GetArtifactLevelSums(artifact);
		return Mathf.Max(1, sums.Count);
	}

	private static int GetMaxArtifactCount(Artifact artifact)
	{
		List<int> sums = GetArtifactLevelSums(artifact);
		return Mathf.Max(1, sums[sums.Count - 1]);
	}

	private static int GetCountForArtifactLevel(Artifact artifact, int level)
	{
		List<int> sums = GetArtifactLevelSums(artifact);
		int index = Mathf.Clamp(level, 1, sums.Count) - 1;
		return Mathf.Max(1, sums[index]);
	}

	private static int GetArtifactLevelFromCount(Artifact artifact, int count)
	{
		if (count <= 0)
		{
			return 0;
		}

		List<int> sums = GetArtifactLevelSums(artifact);
		for (int i = 0; i < sums.Count; i++)
		{
			if (count < sums[i])
			{
				return i;
			}
		}
		return sums.Count;
	}

	private static List<int> GetArtifactLevelSums(Artifact artifact)
	{
		if (artifact != null && artifact.ArtifactGroup != null)
		{
			List<int> sums = artifact.ArtifactGroup.SumsPerArtifactNeeded;
			if (sums != null && sums.Count > 0)
			{
				return sums;
			}

			List<int> counts = artifact.ArtifactGroup.CountPerArtifactNeeded;
			if (counts != null && counts.Count > 0)
			{
				List<int> calculated = new List<int>(counts.Count);
				int total = 0;
				foreach (int count in counts)
				{
					total += Mathf.Max(0, count);
					calculated.Add(total);
				}
				return calculated;
			}
		}

		return new List<int> { 1 };
	}

	private static bool TryParseBigInteger(string text, out BigInteger value)
	{
		value = BigInteger.Zero;
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}

		string normalized = text.Replace(",", "").Replace(" ", "").Trim();
		return BigInteger.TryParse(normalized, out value);
	}

	private void SetStatus(string message, MessageType type)
	{
		statusMessage = message;
		statusType = type;
	}
}
#endif
