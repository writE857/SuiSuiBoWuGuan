using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BasicButtonAnimation : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[Header("Animation Settings")]
	public float hoverScale = 1.1f;

	public float animDuration = 0.15f;

	private RectTransform rect;

	public AudioResource HoverSFX;

	public AudioResource ClickSFX;

	public Button Button;

	private void Start()
	{
		rect = GetComponent<RectTransform>();
		Button = GetComponent<Button>();
		Button.onClick.AddListener(OnClicked);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		rect.DOScale(Vector3.one * hoverScale, animDuration).SetEase(Ease.OutBack);
		Singleton<AudioPool>.Current.Play(HoverSFX, base.transform.position);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		rect.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack);
	}

	private void OnClicked()
	{
		Singleton<AudioPool>.Current.Play(ClickSFX, base.transform.position);
	}
}
