using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class PrestigeUpgradeButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
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

	public int BasePrice = 5000;

	public TMP_Text PriceText;

	public AudioResource hoverSFX;

	public AudioResource declinedSound;

	public UnityAction OnDeclined;

	public UnityAction OnAccepted;

	public ConfirmationDialogTexts PrestigeEnterConfrim;

	public bool didClick;

	public CanvasGroup CanvasGroup;

	private int lastPrice = int.MinValue;

	private bool? lastHasTickets;

	public int Price => (SaveManager.Current.SaveData.PrestigeRestarts + 1) * BasePrice;

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
		Singleton<UpgradeShop>.Current.SetHoveredButton(null);
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
	}

	public void OnClicked()
	{
		if (Singleton<LootManager>.Current.Money >= Price && !didClick)
		{
			Singleton<Shaker>.Current.ShakeUpgradeFeelGood();
			Singleton<MainScreen>.Current.ShowConfirmationScreen(PrestigeEnterConfrim, null);
			ConfirmationScreen confirmationScreen = Singleton<MainScreen>.Current.ConfirmationScreen;
			confirmationScreen.OnNoClicked = (UnityAction)Delegate.Combine(confirmationScreen.OnNoClicked, new UnityAction(OnNoClicked));
			ConfirmationScreen confirmationScreen2 = Singleton<MainScreen>.Current.ConfirmationScreen;
			confirmationScreen2.OnYesClicked = (UnityAction)Delegate.Combine(confirmationScreen2.OnYesClicked, new UnityAction(OnYesClicked));
			didClick = true;
			PlayClickAccepted();
		}
		else
		{
			PlayClickDeclined();
		}
	}

	private void OnDisable()
	{
		foreach (Light light in Lights)
		{
			light.intensity = originalIntensities[light];
			light.enabled = false;
		}
	}

	private void OnNoClicked()
	{
		didClick = false;
	}

	private void OnYesClicked()
	{
		didClick = false;
		Singleton<LootManager>.Current.TrySpend(Price);
		Singleton<PrestigeScreen>.Current.IsShoppingMode = true;
		Singleton<MainScreen>.Current.Activate(Singleton<MainScreen>.Current.PrestigeScr);
	}

	private void Update()
	{
		RefreshUI();
	}

	private void RefreshUI(bool force = false)
	{
		int price = Price;
		if (force || price != lastPrice)
		{
			lastPrice = price;
			PriceText.text = $"${price}";
		}

		bool hasTickets = Singleton<LootManager>.Current.Tickets > 0;
		if (!force && lastHasTickets.HasValue && lastHasTickets.Value == hasTickets)
		{
			return;
		}
		lastHasTickets = hasTickets;
		CanvasGroup.alpha = (hasTickets ? 1 : 0);
		CanvasGroup.interactable = hasTickets;
		CanvasGroup.blocksRaycasts = hasTickets;
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
}
