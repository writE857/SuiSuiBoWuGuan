using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AchievementUnlocker : MonoBehaviour
{
	public SteamAchievementData WelcomeAchievement;

	public SteamAchievementData AutoHammerUnlockedAchievement;

	public SteamAchievementData TenThousand_Achievement;

	public SteamAchievementData HundredThousand;

	public SteamAchievementData OneMillion_Achievement;

	public SteamAchievementData TenMillion_Achievement;

	public SteamAchievementData FirstDay_Achievement;

	public SteamAchievementData FirstWeek_Achievement;

	public SteamAchievementData FirstMonth_Achievement;

	public SteamAchievementData HundredThousandHits_Achievement;

	public SteamAchievementData SixtyNineLucky_Achievement;

	public List<UpgradeType> UpgradeTypes = new List<UpgradeType>();

	public PrestigeSkill AutoHammerSkill;

	private void Start()
	{
		StartCoroutine(Do_Unlocks());
	}

	private IEnumerator Do_Unlocks()
	{
		while (true)
		{
			yield return new WaitForSeconds(1f);
			if (Singleton<GameSession>.Current.IsGameStarted)
			{
				Singleton<SteamBasics>.Current.Add(WelcomeAchievement);
			}
			foreach (ArtifactGroup group in Singleton<GameResources>.Current.Artifacts.Groups)
			{
				if (group.Artifacts.Count == group.Artifacts.Where((Artifact a) => a.IsUnlocked)?.Count())
				{
					Singleton<SteamBasics>.Current.Add(group.OnEveryArtifactFoundAchievement);
				}
			}
			foreach (UpgradeType upgradeType in UpgradeTypes)
			{
				if (upgradeType.IsMaxLevel)
				{
					Singleton<SteamBasics>.Current.Add(upgradeType.SteamAchievement);
				}
			}
			if (AutoHammerSkill.IsUnlocked)
			{
				Singleton<SteamBasics>.Current.Add(AutoHammerUnlockedAchievement);
			}
			if (Singleton<LootManager>.Current.Money >= 10000L)
			{
				Singleton<SteamBasics>.Current.Add(TenThousand_Achievement);
			}
			if (Singleton<LootManager>.Current.Money >= 100000L)
			{
				Singleton<SteamBasics>.Current.Add(HundredThousand);
			}
			if (Singleton<LootManager>.Current.Money >= 1000000L)
			{
				Singleton<SteamBasics>.Current.Add(OneMillion_Achievement);
			}
			if (Singleton<LootManager>.Current.Money >= 10000000L)
			{
				Singleton<SteamBasics>.Current.Add(TenMillion_Achievement);
			}
			if (Singleton<GameTime>.Current.Days > 1)
			{
				Singleton<SteamBasics>.Current.Add(FirstDay_Achievement);
			}
			if (Singleton<GameTime>.Current.Days > 7)
			{
				Singleton<SteamBasics>.Current.Add(FirstWeek_Achievement);
			}
			if (Singleton<GameTime>.Current.Days > 30)
			{
				Singleton<SteamBasics>.Current.Add(FirstMonth_Achievement);
			}
			if (SaveManager.Current.SaveData.HammerHitCount > 100000)
			{
				Singleton<SteamBasics>.Current.Add(HundredThousandHits_Achievement);
			}
			if (SaveManager.Current.SaveData.CoinValue >= 69)
			{
				Singleton<SteamBasics>.Current.Add(SixtyNineLucky_Achievement);
			}
		}
	}
}
