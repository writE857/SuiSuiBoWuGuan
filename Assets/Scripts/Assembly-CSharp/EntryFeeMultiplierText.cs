using TMPro;
using UnityEngine;

public class EntryFeeMultiplierText : MonoBehaviour
{
	private TMP_Text Text;

	private float lastEntryFeeMultiplier = float.MinValue;

	private void OnEnable()
	{
		Text = GetComponent<TMP_Text>();
		lastEntryFeeMultiplier = float.MinValue;
		RefreshText();
	}

	private void Update()
	{
		RefreshText();
	}

	private void RefreshText()
	{
		float entryFeeMultiplier = Singleton<VisitorManager>.Current.EntryFeeMultiplier;
		if (Mathf.Approximately(entryFeeMultiplier, lastEntryFeeMultiplier))
		{
			return;
		}
		lastEntryFeeMultiplier = entryFeeMultiplier;
		Text.text = "入场费：" + entryFeeMultiplier.ToString("0.00") + "倍";
	}
}
