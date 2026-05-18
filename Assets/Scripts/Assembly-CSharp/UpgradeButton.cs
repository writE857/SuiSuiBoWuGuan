using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class UpgradeButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[Header("Animation Settings")]
	public float hoverScale = 1.1f;

	public float pressScale = 0.9f;

	public float animDuration = 0.15f;

	public float declineShakeStrength = 20f;

	public int declineShakeVibrato = 20;

	public float declineIntensity = 2.5f;

	public float acceptIntensity = 2.5f;

	private RectTransform rect;

	private Vector3 originalScale;

	public Color declineColor = Color.red;

	public float colorFlashDuration = 0.1f;

	private List<Graphic> graphics = new List<Graphic>();

	private Dictionary<Graphic, Color> originalColors = new Dictionary<Graphic, Color>();

	private Dictionary<Light, float> originalIntensities = new Dictionary<Light, float>();

	private List<Light> Lights = new List<Light>();

	public Button Button;

	public TMP_Text NameText;

	public TMP_Text PriceText;

	public Image IconImage;

	public Image LevelRatioFill;

	public UpgradeType UpgradeType;

	public AudioResource hoverSFX;

	public AudioResource declinedSound;

	public UnityAction OnDeclined;

	public UnityAction OnAccepted;

	public GameObject Content;

	private string lastNameText;

	private bool lastIsMaxLevel;

	private int lastLevel = int.MinValue;

	private int lastPrice = int.MinValue;

	private Sprite lastIcon;

	private float lastLevelRatio = float.MinValue;

	public int Price => UpgradeType.CurrentPrice;

	private void Awake()
	{
		rect = GetComponent<RectTransform>();
		originalScale = rect.localScale;
		GetComponentsInChildren(includeInactive: true, graphics);
		GetComponentsInChildren(includeInactive: true, Lights);
		foreach (Graphic graphic in graphics)
		{
			originalColors[graphic] = graphic.color;
		}
		foreach (Light light in Lights)
		{
			originalIntensities[light] = light.intensity;
			light.enabled = false;
		}
		Button.onClick.AddListener(OnClicked);
		RefreshUI(force: true);
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
		foreach (Light l in Lights)
		{
			l.DOIntensity(originalIntensities[l] * declineIntensity, colorFlashDuration).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad)
				.OnKill(delegate
				{
					l.intensity = originalIntensities[l];
				});
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		rect.DOScale(originalScale * hoverScale, animDuration).SetEase(Ease.OutBack);
		Singleton<UpgradeShop>.Current.SetHoveredButton(this);
		Singleton<AudioPool>.Current.Play(hoverSFX, base.transform.position);
		foreach (Light light in Lights)
		{
			light.enabled = true;
			light.intensity = 0f;
			light.DOIntensity(originalIntensities[light], animDuration).SetEase(Ease.OutBack);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		rect.DOScale(originalScale, animDuration).SetEase(Ease.OutBack);
		foreach (Light item in Lights)
		{
			item.DOIntensity(0f, animDuration).SetEase(Ease.OutBack).OnComplete(delegate
			{
				item.enabled = false;
			})
				.OnKill(delegate
				{
					item.enabled = false;
				});
		}
		Singleton<UpgradeShop>.Current.SetHoveredButton(null);
	}

	public void OnClicked()
	{
		if (UpgradeType.IsMaxLevel)
		{
			Singleton<AudioPool>.Current.Play(declinedSound, base.transform.position);
			Singleton<Shaker>.Current.ShakeDenied();
			PlayClickDeclined();
		}
		else if (Singleton<LootManager>.Current.TrySpend(Price))
		{
			Singleton<Shaker>.Current.ShakeUpgradeFeelGood();
			SaveManager.Current.SetUpgradeLevel(UpgradeType, UpgradeType.CurrentLevel + 1);
			Singleton<GameEvents>.Current.OnUpgradeBought?.Invoke(UpgradeType.DisplayName, UpgradeType.CurrentLevel);
			PlayClickAccepted();
		}
		else
		{
			PlayClickDeclined();
		}
	}

	private void Update()
	{
		RefreshUI();
	}

	private void RefreshUI(bool force = false)
	{
		if (UpgradeType == null)
		{
			return;
		}

		int currentLevel = UpgradeType.CurrentLevel;
		string displayName = UpgradeType.DisplayName;
		if (force || currentLevel != lastLevel || displayName != lastNameText)
		{
			lastLevel = currentLevel;
			lastNameText = displayName;
			NameText.text = $"{displayName} ×{currentLevel}";
		}

		bool isMaxLevel = UpgradeType.IsMaxLevel;
		int currentPrice = UpgradeType.CurrentPrice;
		if (force || isMaxLevel != lastIsMaxLevel || currentPrice != lastPrice)
		{
			lastIsMaxLevel = isMaxLevel;
			lastPrice = currentPrice;
			if (isMaxLevel)
			{
				PriceText.text = "满级";
			}
			else
			{
				PriceText.text = "$" + NumFormat.ToM1Decimal(currentPrice);
			}
		}

		Sprite icon = UpgradeType.Icon;
		if (force || icon != lastIcon)
		{
			lastIcon = icon;
			IconImage.sprite = icon;
		}

		float levelRatio = UpgradeType.LevelRatio;
		if (force || !Mathf.Approximately(levelRatio, lastLevelRatio))
		{
			lastLevelRatio = levelRatio;
			LevelRatioFill.fillAmount = levelRatio;
		}
	}

	private void PlayClickAccepted()
	{
		rect.DOScale(originalScale * pressScale, animDuration).SetEase(Ease.InOutQuad).OnComplete(delegate
		{
			rect.DOScale(originalScale, animDuration).SetEase(Ease.OutBack);
		});
		foreach (Light l in Lights)
		{
			l.DOIntensity(originalIntensities[l] * acceptIntensity, colorFlashDuration).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad)
				.OnKill(delegate
				{
					l.intensity = originalIntensities[l];
				});
		}
	}

	private void PlayClickDeclined()
	{
		rect.DOShakeAnchorPos(animDuration, new Vector2(declineShakeStrength, 0f), declineShakeVibrato).SetEase(Ease.OutQuad);
		FlashDeclineColor();
	}

	private void OnDisable()
	{
		foreach (Light light in Lights)
		{
			light.intensity = originalIntensities[light];
			light.enabled = false;
		}
	}
}
