using UnityEngine;

[CreateAssetMenu]
public class PrestigeSkill : ScriptableObject
{
	public string ID = "??";

	public string DisplayName;

	public string Description;

	public int MaxLevel = 1;

	public int BasePrice = 1;

	public int ExtraPricePerLevel = 1;

	public Sprite Icon;

	public float BaseAmount;

	public float IncreasePerLevel;

	public int DebugSumPrice;

	public bool IMPLEMENTED;

	public int CurrentLevel => SaveManager.Current.GetPrestigeLevel(this);

	public bool IsUnlocked => CurrentLevel > 0;

	public bool IsMaxLevel => CurrentLevel == MaxLevel;

	public int CurrentPrice => BasePrice + CurrentLevel * ExtraPricePerLevel;

	public float FinalValue => BaseAmount + (float)((!Application.isPlaying) ? 1 : CurrentLevel) * IncreasePerLevel;

	public float FinalNextValue => BaseAmount + (float)((!Application.isPlaying) ? 1 : (IsMaxLevel ? CurrentLevel : (CurrentLevel + 1))) * IncreasePerLevel;

	public string DebugValues
	{
		get
		{
			string text = "";
			for (int i = 0; i < MaxLevel + 1; i++)
			{
				text = text + (BaseAmount + (float)i * IncreasePerLevel) + ", ";
			}
			text.Trim();
			return text;
		}
	}

	public string DebugPrices
	{
		get
		{
			DebugSumPrice = 0;
			string text = "";
			for (int i = 0; i < MaxLevel; i++)
			{
				text = text + (BasePrice + i * ExtraPricePerLevel) + ", ";
				DebugSumPrice += BasePrice + i * ExtraPricePerLevel;
			}
			return text;
		}
	}

	public string DebugDescription => string.Format(Description, FinalNextValue);
}
