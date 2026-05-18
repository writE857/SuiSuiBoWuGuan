using TMPro;
using UnityEngine;

public class TicketCountText : MonoBehaviour
{
	public TMP_Text Text;

	private int lastTicketCount = int.MinValue;

	private void OnEnable()
	{
		lastTicketCount = int.MinValue;
		RefreshText();
	}

	private void Update()
	{
		RefreshText();
	}

	private void RefreshText()
	{
		int ticketCount = SaveManager.Current.SaveData.TicketCount;
		if (ticketCount == lastTicketCount)
		{
			return;
		}
		lastTicketCount = ticketCount;
		Text.text = $"{ticketCount} 张门票";
	}
}
