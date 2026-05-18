using DG.Tweening;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
	[Header("FX Settings")]
	public float moveDistance = 50f;

	public float duration = 1f;

	public Color startColor = new Color(1f, 1f, 1f, 1f);

	public Color endColor = new Color(1f, 1f, 1f, 0f);

	private TextMeshProUGUI tmp;

	private RectTransform rect;

	private void Awake()
	{
		tmp = GetComponent<TextMeshProUGUI>();
		rect = GetComponent<RectTransform>();
	}

	public void Play(string textValue)
	{
		tmp.text = textValue;
		tmp.color = startColor;
		rect.localScale = Vector3.one * 0.5f;
		rect.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
		rect.DOAnchorPosY(rect.anchoredPosition.y + moveDistance, duration).SetEase(Ease.OutQuad);
		tmp.DOColor(endColor, duration).SetEase(Ease.Linear);
		Object.Destroy(base.gameObject, duration + 0.1f);
	}
}
