using TMPro;
using UnityEngine;

public class LuckyCoinValueText : MonoBehaviour
{
	public TMP_Text Text;

	private int lastCoinValue = int.MinValue;

	private void OnEnable()
	{
		lastCoinValue = int.MinValue;
		RefreshText();
	}

	private void Update()
	{
		RefreshText();
	}

	private void RefreshText()
	{
		int coinValue = SaveManager.Current.SaveData.CoinValue;
		if (coinValue == lastCoinValue)
		{
			return;
		}
		lastCoinValue = coinValue;
		Text.text = $"${coinValue}";
	}
}
