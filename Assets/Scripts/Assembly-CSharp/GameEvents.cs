using UnityEngine.Events;

public class GameEvents : Singleton<GameEvents>
{
	public UnityAction<Loot> OnLootPickedUp;

	public UnityAction<Loot> OnLootSold;

	public UnityAction<Loot> OnLootExtracted;

	public UnityAction OnNotEnoughMoney;

	public UnityAction<Artifact> OnArtifactLevelUp;

	public UnityAction<Artifact> OnArtifactUnlocked;

	public UnityAction<ArtifactGroup> OnArtifactGroupLevelUp;

	public UnityAction<string, int> OnUpgradeBought;

	public UnityAction<int> OnMoneyAdded;

	public UnityAction<int> OnMoneySpent;

	public UnityAction OnTrySpendDeclined;

	public UnityAction OnVisitorFeePaid;

	public UnityAction<float> OnHammerHit;

	public UnityAction OnDayPassed;

	public UnityAction OnHourPassed;

	public UnityAction<int> OnCoinHeads;

	public UnityAction OnCoinTails;

	public UnityAction<int> OnCoinIncome;

	public UnityAction<int> OnHammerIncome;

	public UnityAction OnRestart;

	public UnityAction OnPrestigeChange;
}
