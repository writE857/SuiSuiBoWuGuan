using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BrickBuyButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public Button Button;

	public TMP_Text NameText;

	public TMP_Text PriceText;

	public TMP_Text LevelText;

	public TMP_Text XPText;

	public Image XPBar;

	public ArtifactGroup ArtifactGroup;

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

	[Header("Prestige")]
	public PrestigeSkill FreeStoneSkill;

	public Image BarCodeImage;

	public Color MaxColor;

	public Color DefaultLevelColor;

	private Tweener waveTween;

	private Tweener hoverTween;

	private Tweener pressTween;

	private Tweener declineTween;

	private string lastNameText;

	private int lastPrice = int.MinValue;

	private bool lastIsMaxLevel;

	private int lastLevel = int.MinValue;

	private int lastCurrentLevelXP = int.MinValue;

	private int lastCurrentXPRequired = int.MinValue;

	private float lastXPRatio = float.MinValue;

	private Sprite lastBarcode;

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
		if (ArtifactGroup != null)
		{
			RefreshUI(force: true);
		}
	}

	private void Update()
	{
		if (!(ArtifactGroup == null))
		{
			RefreshUI();
			WaveOnHovered();
		}
	}

	private void OnDisable()
	{
		HoverInfo hoverInfo = Singleton<HoverInfo>.Current;
		if (ArtifactGroup != null && hoverInfo != null && hoverInfo.CurrentHoveredArtifactGroup == ArtifactGroup)
		{
			hoverInfo.CurrentHoveredArtifactGroup = null;
		}
		hoverTween?.Kill();
		pressTween?.Kill();
		declineTween?.Kill();
		waveTween?.Kill();
		IsHovered = false;
		hoverBlend = 0f;
		waveTransform.anchoredPosition = Vector2.zero;
		waveTransform.localEulerAngles = Vector3.zero;
		waveTransform.localScale = Vector3.one;
	}

	private void WaveOnHovered()
	{
		hoverBlend = Mathf.MoveTowards(hoverBlend, IsHovered ? 1f : 0f, Time.deltaTime * blendSpeed);
		if (hoverBlend <= 0f)
		{
			waveTransform.anchoredPosition = Vector2.zero;
			waveTransform.localEulerAngles = Vector3.zero;
			waveTransform.localScale = Vector3.one;
			return;
		}
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

	private void BuyClicked()
	{
		if (ArtifactGroup == null)
		{
			return;
		}
		if (Singleton<LootManager>.Current.TrySpend(ArtifactGroup.CurrentPrice) && Singleton<BrickShop>.Current.canBuy)
		{
			Singleton<BrickPile>.Current.AddNewRandomBrick(ArtifactGroup);
			SaveManager.Current.UnlockArtifactGroup(ArtifactGroup.GROUPID);
			PlayClickAccepted();
			SaveManager.Current.SaveData.BricksBought++;
			ArtifactGroup.SaveData.IsNextFree = false;
			if (Random.value < FreeStoneSkill.FinalValue / 100f)
			{
				ArtifactGroup.SaveData.IsNextFree = true;
			}
			Singleton<BrickShop>.Current.nextBuy = Time.time + Singleton<BrickShop>.Current.buyInterval;
		}
		else
		{
			PlayClickDeclined();
		}
	}

	private void RefreshUI(bool force = false)
	{
		string displayName = ArtifactGroup.DisplayName;
		if (force || displayName != lastNameText)
		{
			lastNameText = displayName;
			NameText.text = displayName;
		}

		int currentPrice = ArtifactGroup.CurrentPrice;
		if (force || currentPrice != lastPrice)
		{
			lastPrice = currentPrice;
			if (currentPrice == 0)
			{
				PriceText.text = "免费";
			}
			else
			{
				PriceText.text = "$" + currentPrice.ToString("N0");
			}
		}

		bool isMaxLevel = ArtifactGroup.IsMaxLevel;
		int level = ArtifactGroup.Level;
		int currentLevelXP = ArtifactGroup.CurrentLevelXP;
		int currentXPRequired = ArtifactGroup.CurrentXPRequired;
		float xpRatio = ArtifactGroup.XPRatio;
		if (force || isMaxLevel != lastIsMaxLevel || level != lastLevel || currentLevelXP != lastCurrentLevelXP || currentXPRequired != lastCurrentXPRequired || !Mathf.Approximately(xpRatio, lastXPRatio))
		{
			lastIsMaxLevel = isMaxLevel;
			lastLevel = level;
			lastCurrentLevelXP = currentLevelXP;
			lastCurrentXPRequired = currentXPRequired;
			lastXPRatio = xpRatio;
			if (isMaxLevel)
			{
				LevelText.text = "满级";
				XPText.text = "满级";
				XPBar.fillAmount = 1f;
				LevelText.color = MaxColor;
				XPText.color = MaxColor;
			}
			else
			{
				LevelText.text = level.ToString();
				XPText.text = currentLevelXP + "/" + currentXPRequired + " 经验";
				XPBar.fillAmount = xpRatio;
				LevelText.color = DefaultLevelColor;
				XPText.color = DefaultLevelColor;
			}
		}

		Sprite barcode = ArtifactGroup.Barcode;
		if (force || barcode != lastBarcode)
		{
			lastBarcode = barcode;
			BarCodeImage.sprite = barcode;
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (IsHovered || ArtifactGroup == null)
		{
			return;
		}
		Singleton<AudioPool>.Current.Play(hoverSFX, base.transform.position);
		Singleton<HoverInfo>.Current.CurrentHoveredArtifactGroup = ArtifactGroup;
		IsHovered = true;
		GoUp();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!IsHovered)
		{
			return;
		}
		waveTween?.Kill();
		HoverInfo hoverInfo = Singleton<HoverInfo>.Current;
		if (hoverInfo != null && hoverInfo.CurrentHoveredArtifactGroup == ArtifactGroup)
		{
			hoverInfo.CurrentHoveredArtifactGroup = null;
		}
		IsHovered = false;
		GoBack();
	}

	private void GoUp()
	{
		hoverTween?.Kill();
		hoverTween = dragTransform.DOAnchorPosY(yUpOnHover, upDuration).SetEase(Ease.OutQuad);
	}

	private void GoBack()
	{
		hoverTween?.Kill();
		hoverTween = dragTransform.DOAnchorPosY(yUpDefault, upDuration).SetEase(Ease.OutQuad);
	}

	private void PlayClickAccepted()
	{
		pressTween?.Kill();
		pressTween = dragTransform.DOScale(originalScale * pressScale, animDuration).SetEase(Ease.InOutQuad).OnComplete(delegate
		{
			pressTween = dragTransform.DOScale(originalScale, animDuration).SetEase(Ease.OutBack);
		});
	}

	private void PlayClickDeclined()
	{
		declineTween?.Kill();
		declineTween = dragTransform.DOShakeAnchorPos(animDuration, new Vector2(declineShakeStrength, 0f), declineShakeVibrato).SetEase(Ease.OutQuad);
		FlashDeclineColor();
	}

	private void FlashDeclineColor()
	{
		foreach (Graphic g in graphics)
		{
			g.DOKill();
			g.color = originalColors[g];
			g.DOColor(declineColor, colorFlashDuration).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad)
				.OnKill(delegate
				{
					g.color = originalColors[g];
				});
		}
	}
}
