using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StonePackageButton : MonoBehaviour
{
	public ArtifactGroup ArtifactGroup;

	public Button Button;

	public TMP_Text PriceText;

	public TMP_Text NameText;

	private void Start()
	{
		Button.onClick.AddListener(BuyClicked);
		PriceText.text = "$" + ArtifactGroup.CurrentPrice.ToString("N0");
		NameText.text = ArtifactGroup.DisplayName;
	}

	private void Update()
	{
		bool interactable = Singleton<GameSession>.Current.Money >= ArtifactGroup.CurrentPrice;
		Button.interactable = interactable;
	}

	public void BuyClicked()
	{
		if (Singleton<GameSession>.Current.Money >= ArtifactGroup.CurrentPrice)
		{
			Singleton<LootManager>.Current.TrySpend(ArtifactGroup.CurrentPrice);
			Singleton<BrickPile>.Current.AddNewRandomBrick(ArtifactGroup);
			Singleton<SteamBasics>.Current.Add(ArtifactGroup.OnEveryArtifactFoundAchievement);
		}
	}
}
