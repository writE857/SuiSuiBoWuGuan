using TMPro;
using UnityEngine;

public class Notification : MonoBehaviour
{
	public TMP_Text Text;

	public string DisplayText;

	public CanvasGroup CanvasGroup;

	public void Init()
	{
		Text.text = DisplayText;
	}

	public void SetAlpha(float alpha)
	{
		CanvasGroup.alpha = alpha;
	}
}
