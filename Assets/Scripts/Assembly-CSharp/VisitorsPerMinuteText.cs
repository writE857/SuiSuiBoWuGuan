using TMPro;
using UnityEngine;

public class VisitorsPerMinuteText : MonoBehaviour
{
	private TMP_Text Text;

	private int lastVisitorsPerMinute = int.MinValue;

	private void OnEnable()
	{
		Text = GetComponent<TMP_Text>();
		lastVisitorsPerMinute = int.MinValue;
		RefreshText();
	}

	private void Update()
	{
		RefreshText();
	}

	private void RefreshText()
	{
		int visitorsPerMinute = Mathf.RoundToInt(Singleton<VisitorManager>.Current.CurrentAverageVisitorsPerMinute);
		if (visitorsPerMinute == lastVisitorsPerMinute)
		{
			return;
		}
		lastVisitorsPerMinute = visitorsPerMinute;
		Text.text = "访客：" + visitorsPerMinute.ToString("0") + "/小时";
	}
}
