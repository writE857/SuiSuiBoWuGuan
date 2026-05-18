using TMPro;
using UnityEngine;

public class GameTimeDisplay : MonoBehaviour
{
	public TMP_Text Text;

	private int lastDays = int.MinValue;

	private int lastHours = int.MinValue;

	private int lastMinutes = int.MinValue;

	private void OnEnable()
	{
		lastDays = int.MinValue;
		lastHours = int.MinValue;
		lastMinutes = int.MinValue;
		RefreshText();
	}

	private void Update()
	{
		RefreshText();
	}

	private void RefreshText()
	{
		GameTime current = Singleton<GameTime>.Current;
		int days = current.Days;
		int hours = current.Hours;
		int minutes = current.Minutes;
		if (days == lastDays && hours == lastHours && minutes == lastMinutes)
		{
			return;
		}
		lastDays = days;
		lastHours = hours;
		lastMinutes = minutes;
		Text.text = "第 " + days + " 天 " + hours.ToString("00") + ":" + minutes.ToString("00");
	}
}
