using UnityEngine;

[CreateAssetMenu]
public class HammerUpgradeInfo : ScriptableObject
{
	public string DisplayName;

	public int Tier = -1;

	public int Price;

	public SteamAchievementData OnPurchaseAchievement;
}
