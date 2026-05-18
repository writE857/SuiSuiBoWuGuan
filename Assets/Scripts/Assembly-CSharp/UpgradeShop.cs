using DG.Tweening;
using TMPro;
using UnityEngine;

public class UpgradeShop : Singleton<UpgradeShop>
{
	[Header("Bonuses")]
	public float sizePerLevel = 0.0025f;

	public float depthPerLevel = 0.0005f;

	public float damagePerLevel = 0.25f;

	[Header("NOT FRAME INDEPENDENT!!!")]
	public float speedPerLevel = 0.25f;

	public int visitorPerLevel = 10;

	public float feePerLevel = 0.1f;

	public TMP_Text DetailText;

	public RectTransform detailTextRect;

	public string DefaultDetailText = "今天天气真不错！";

	[Header("POP Settings")]
	public float popScale = 1.2f;

	public float popDuration = 0.15f;

	public float textDuration = 0.3f;

	public Vector3 originalScale;

	private void Awake()
	{
		originalScale = detailTextRect.localScale;
		SetHoveredButton(null);
	}

	public void SetHoveredButton(UpgradeButton button)
	{
		if (button == null)
		{
			SetText(DefaultDetailText);
		}
		else
		{
			SetText(button.UpgradeType.Description);
		}
	}

	private void SetText(string text)
	{
		DetailText.DOText(text, textDuration).SetEase(Ease.Linear);
		detailTextRect.localScale = originalScale;
		detailTextRect.DOScale(originalScale * popScale, popDuration).SetEase(Ease.InOutQuad).OnComplete(delegate
		{
			detailTextRect.DOScale(originalScale, popDuration).SetEase(Ease.OutBack);
		});
	}
}
