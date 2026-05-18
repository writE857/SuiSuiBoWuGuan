using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XPBar : MonoBehaviour
{
	public TMP_Text LevelText;

	public Image FillImage;

	private int lastLevel = int.MinValue;

	private float lastLevelUpRatio = float.MinValue;

	private void OnEnable()
	{
		lastLevel = int.MinValue;
		lastLevelUpRatio = float.MinValue;
		RefreshUI();
	}

	private void Update()
	{
		RefreshUI();
	}

	private void RefreshUI()
	{
		Experience current = Singleton<Experience>.Current;
		int level = current.Level;
		if (level != lastLevel)
		{
			lastLevel = level;
			LevelText.text = "等级 " + level;
		}
		float levelUpRatio = current.LevelUpRatio;
		if (!Mathf.Approximately(levelUpRatio, lastLevelUpRatio))
		{
			lastLevelUpRatio = levelUpRatio;
			FillImage.fillAmount = levelUpRatio;
		}
	}
}
