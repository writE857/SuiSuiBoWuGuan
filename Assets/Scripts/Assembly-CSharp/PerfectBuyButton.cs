using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PerfectBuyButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public Button Button;

	public TMP_Text NameText;

	public TMP_Text PriceText;

	public float yUpOnHover = 75f;

	public float yUpDefault = 30f;

	public RectTransform dragTransform;

	public RectTransform waveTransform;

	public RectTransform declineColorTransform;

	public float upDuration;

	[Header("Animations")]
	public float hoverScale = 1.1f;

	public float pressScale = 0.9f;

	public float animDuration = 0.15f;

	public float declineShakeStrength = 20f;

	public int declineShakeVibrato = 20;

	private Vector3 originalScale;

	public Color declineColor = Color.red;

	public float colorFlashDuration = 0.1f;

	private List<Graphic> graphics = new List<Graphic>();

	private Dictionary<Graphic, Color> originalColors = new Dictionary<Graphic, Color>();

	public AudioResource hoverSFX;

	public bool IsHovered;

	[Header("Hover Wave")]
	[Header("Noise Strength")]
	public float positionAmplitude = 4f;

	public float rotationAmplitude = 1.5f;

	public float scaleAmplitude = 0.015f;

	public float noiseSpeed = 0.6f;

	public float blendSpeed = 6f;

	private float noiseTime;

	private float hoverBlend;

	public int Price;

	private Tweener waveTween;

	public string test => int.MaxValue.ToString();

	private void Start()
	{
		Button.onClick.AddListener(BuyClicked);
		originalScale = Vector3.one;
		declineColorTransform.GetComponentsInChildren(includeInactive: true, graphics);
		foreach (Graphic graphic in graphics)
		{
			originalColors[graphic] = graphic.color;
		}
	}

	private void Update()
	{
		RefreshUI();
		WaveOnHovered();
	}

	private void WaveOnHovered()
	{
		hoverBlend = Mathf.MoveTowards(hoverBlend, IsHovered ? 1f : 0f, Time.deltaTime * blendSpeed);
		if (!(hoverBlend <= 0f))
		{
			noiseTime += Time.deltaTime * noiseSpeed;
			float x = Mathf.PerlinNoise(noiseTime, 0f) - 0.5f;
			float y = Mathf.PerlinNoise(0f, noiseTime) - 0.5f;
			float num = Mathf.PerlinNoise(noiseTime, noiseTime) - 0.5f;
			Vector2 anchoredPosition = new Vector2(x, y) * positionAmplitude * hoverBlend;
			Vector3 localEulerAngles = new Vector3(0f, 0f, num * rotationAmplitude * hoverBlend);
			Vector3 vector = Vector3.one * (num * scaleAmplitude * hoverBlend);
			waveTransform.anchoredPosition = anchoredPosition;
			waveTransform.localEulerAngles = localEulerAngles;
			waveTransform.localScale = Vector3.one + vector;
		}
	}

	private void BuyClicked()
	{
		if (Singleton<LootManager>.Current.TrySpend(Price))
		{
			Singleton<BrickTable>.Current.RemoveBricks();
			Singleton<BrickShop>.Current.NuclearStone.gameObject.SetActive(value: true);
			Singleton<BrickShop>.Current.NuclearStone.IsBought = true;
			PlayClickAccepted();
			SaveManager.Current.SaveData.BricksBought++;
		}
		else
		{
			PlayClickDeclined();
		}
	}

	private void RefreshUI()
	{
		PriceText.text = "$" + Price.ToString("N0");
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		Singleton<AudioPool>.Current.Play(hoverSFX, base.transform.position);
		IsHovered = true;
		GoUp();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		waveTween?.Kill();
		Singleton<HoverInfo>.Current.CurrentHoveredArtifactGroup = null;
		IsHovered = false;
		GoBack();
	}

	private void GoUp()
	{
		dragTransform.DOAnchorPosY(yUpOnHover, upDuration).SetEase(Ease.OutQuad);
	}

	private void GoBack()
	{
		dragTransform.DOAnchorPosY(yUpDefault, upDuration).SetEase(Ease.OutQuad);
	}

	private void PlayClickAccepted()
	{
		dragTransform.DOScale(originalScale * pressScale, animDuration).SetEase(Ease.InOutQuad).OnComplete(delegate
		{
			dragTransform.DOScale(originalScale, animDuration).SetEase(Ease.OutBack);
		});
	}

	private void PlayClickDeclined()
	{
		dragTransform.DOShakeAnchorPos(animDuration, new Vector2(declineShakeStrength, 0f), declineShakeVibrato).SetEase(Ease.OutQuad);
		FlashDeclineColor();
	}

	private void FlashDeclineColor()
	{
		foreach (Graphic g in graphics)
		{
			g.DOColor(declineColor, colorFlashDuration).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad)
				.OnKill(delegate
				{
					g.color = originalColors[g];
				});
		}
	}
}
