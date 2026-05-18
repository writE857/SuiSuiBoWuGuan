using TMPro;
using UnityEngine;

public class IncomeText : MonoBehaviour
{
	private TMP_Text Text;

	private int lastIncomePerMinute = int.MinValue;

	private void OnEnable()
	{
		Text = GetComponent<TMP_Text>();
		lastIncomePerMinute = int.MinValue;
		RefreshText();
	}

	private void Update()
	{
		RefreshText();
	}

	private void RefreshText()
	{
		int incomePerMinute = Singleton<VisitorManager>.Current.IncomePerMinute;
		if (incomePerMinute == lastIncomePerMinute)
		{
			return;
		}
		lastIncomePerMinute = incomePerMinute;
		Text.text = "收入：$" + NumFormat.ToM1Decimal(incomePerMinute) + "/小时";
	}
}
