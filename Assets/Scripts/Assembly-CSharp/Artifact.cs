using System;
using System.Linq;
using UnityEngine;

public class Artifact : MonoBehaviour
{
	public ArtifactGroup ArtifactGroup;

	public Sprite MuseumImage;

	[SerializeField]
	private int _ID;

	public string DisplayName;

	[TextArea]
	public string Description;

	public Loot Loot;

	public int BaseValue = 100;

	public int ItemCountTester = 1;

	public string ID
	{
		get
		{
			if (ArtifactGroup == null)
			{
				Debug.LogError("NO ARTIFACT GROUP");
				return null;
			}
			if (_ID == 0)
			{
				_ID = Mathf.Abs(Guid.NewGuid().GetHashCode());
			}
			return "ART-" + ArtifactGroup.GROUPID + "-" + _ID;
		}
	}

	public int CurrentValue
	{
		get
		{
			if (IsUnlocked)
			{
				return Mathf.RoundToInt((float)(BaseValue * Level) * ArtifactGroup.IncomeMultiplier);
			}
			return 0;
		}
	}

	public bool IsUnlocked
	{
		get
		{
			if (!Application.isPlaying)
			{
				return false;
			}
			return SaveManager.Current.GetArtifactData(this).CountFound > 0;
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

	public bool IsMaxLevel => ArtifactGroup.SumsPerArtifactNeeded.Last() <= ItemFoundCount;

	public int ItemFoundCount
	{
		get
		{
			if (IsActiveSave)
			{
				return SaveManager.Current.GetArtifactData(this).CountFound;
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
				return ArtifactGroup.SumsPerArtifactNeeded.FindIndex((int a) => a > ItemFoundCount);
			}
			return ArtifactGroup.SumsPerArtifactNeeded.Count;
		}
	}

	public int CurrentLevelXP
	{
		get
		{
			if (!IsMaxLevel)
			{
				return ItemFoundCount - ((Level != 0) ? ArtifactGroup.SumsPerArtifactNeeded[Level - 1] : 0);
			}
			return ArtifactGroup.CountPerArtifactNeeded.Last();
		}
	}

	public int CurrentXPRequired
	{
		get
		{
			if (!IsMaxLevel)
			{
				return ArtifactGroup.CountPerArtifactNeeded[Level];
			}
			return ArtifactGroup.CountPerArtifactNeeded.Last();
		}
	}

	public float XPRatio => (float)CurrentLevelXP / (float)CurrentXPRequired;
}
