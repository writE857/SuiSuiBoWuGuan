using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class UpgradeType : ScriptableObject
{
	[Header("DO NOT CHANGE!!!")]
	public string ID;

	public int MaxLevel = 10;

	public string DisplayName;

	public int BasePrice;

	public Sprite Icon;

	[TextArea]
	public string Description;

	public SteamAchievementData SteamAchievement;

	public List<int> Prices = new List<int>();

	public int CurrentLevel => SaveManager.Current.GetUpgradeLevel(this);

	public int CurrentPrice => Prices.ElementAtOrDefault(CurrentLevel - 1);

	public float LevelRatio => ((float)CurrentLevel - 1f) / (float)(MaxLevel - 1);

	public bool IsMaxLevel => CurrentLevel == MaxLevel;
}
