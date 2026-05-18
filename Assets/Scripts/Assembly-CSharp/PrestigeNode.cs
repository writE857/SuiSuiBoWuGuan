using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

[SelectionBase]
public class PrestigeNode : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[Header("Main")]
	public PrestigeNode From;

	public PrestigeSkill PrestigeSkill;

	[Header("Setup")]
	public List<GameObject> DefaultContent = new List<GameObject>();

	public List<GameObject> MaxContent = new List<GameObject>();

	public GameObject UndiscoveredDetails;

	public GameObject DefaultDetails;

	public List<Image> Icons = new List<Image>();

	public List<Graphic> RecolorOnStateChange = new List<Graphic>();

	public Color NewColor;

	public Color ProgressedColor;

	public Color UnknownColor;

	public Color MaxedColor;

	public CanvasGroup MainCG;

	public TMP_Text NameText;

	public TMP_Text DescriptionText;

	public TMP_Text PriceText;

	public bool isHovered;

	[Header("Animations")]
	public float sizeScale = 1.2f;

	public float scaleDuration = 0.2f;

	public float pressScale = 0.9f;

	public float animDuration = 0.15f;

	public float declineShakeStrength = 20f;

	public int declineShakeVibrato = 20;

	public Color declineColor = Color.red;

	public Color acceptColor = Color.cyan;

	public float colorFlashDuration = 0.1f;

	public AudioResource hoverSFX;

	public RectTransform rect;

	public RectTransform waveTransform;

	public RectTransform boundsRect;

	private List<Graphic> graphics = new List<Graphic>();

	private Dictionary<Graphic, Color> originalColors = new Dictionary<Graphic, Color>();

	public float waveShakeStrength = 20f;

	public int waveShakeVibrato = 20;

	public AudioResource DeniedSFX;

	public AudioResource PurchasedSFX;

	public GameObject DetailPanel;

	public GameObject SelectionHighlight;

	public Button Button;

	public float idleAlpha = 0.75f;

	public float unKnownAlpha = 0.25f;

	public PrestigeState LastState;

	public float CurrentAlpha;

	public Color CurrentColor;

	public static List<PrestigeNode> Entries = new List<PrestigeNode>();

	public Sprite UnknownSprite;

	private float nextHover;

	private float hoverCooldown = 0.1f;

	public float refreshInterval = 0.25f;

	private Tweener waveTween;

	public PrestigeState State
	{
		get
		{
			if (PrestigeSkill.IsMaxLevel)
			{
				return PrestigeState.Maxed;
			}
			if (PrestigeSkill.CurrentLevel > 0)
			{
				return PrestigeState.Progressed;
			}
			if (From != null && (From.LastState == PrestigeState.Progressed || From.LastState == PrestigeState.Maxed))
			{
				return PrestigeState.New;
			}
			if (From != null && From.LastState == PrestigeState.New)
			{
				return PrestigeState.Unknown;
			}
			if (From == null)
			{
				return PrestigeState.New;
			}
			return PrestigeState.Unknown;
		}
	}

	private void Start()
	{
		RebindIconsIfNeeded();
		Entries.Add(this);
		MainCG.alpha = idleAlpha;
		CurrentAlpha = idleAlpha;
		Icons.RemoveAll((Image a) => a == null);
		waveTransform.GetComponentsInChildren(includeInactive: true, graphics);
		foreach (Graphic graphic in graphics)
		{
			originalColors[graphic] = graphic.color;
		}
		Button.onClick.AddListener(OnPurchaseClicked);
		DetailPanel.SetActiveSmart(newState: false);
		LastState = PrestigeState.Unknown;
		UpdateState(forced: true);
		if (PrestigeSkill != null)
		{
			Icons.ForEach(delegate(Image a)
			{
				if (!(a == null))
				{
					a.sprite = PrestigeSkill.Icon;
				}
			});
		}
		SelectionHighlight.SetActiveSmart(newState: false);
		ForceUpdate();
	}

	private void OnEnable()
	{
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnRestart += ForceUpdate;
		current.OnPrestigeChange += ForceUpdate;
	}

	private void OnDisable()
	{
		GameEvents gameEvents = UnityEngine.Object.FindFirstObjectByType<GameEvents>(FindObjectsInactive.Include);
		if (gameEvents != null)
		{
			gameEvents.OnRestart -= ForceUpdate;
			gameEvents.OnPrestigeChange -= ForceUpdate;
		}
	}

	private void OnDrawGizmos()
	{
		RebindIconsIfNeeded();
		Icons.RemoveAll((Image a) => a == null);
		if (From != null)
		{
			if (From.From == this)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawLine(From.transform.position + Vector3.one * 0.1f, base.transform.position + Vector3.one * 0.1f);
			}
			Gizmos.DrawLine(From.transform.position, base.transform.position);
		}
		if (!(PrestigeSkill != null))
		{
			return;
		}
		foreach (Image icon in Icons)
		{
			if (icon == null)
			{
				continue;
			}
			if (icon.sprite != PrestigeSkill.Icon)
			{
				icon.sprite = PrestigeSkill.Icon;
			}
		}
	}

	private void OnDestroy()
	{
		Entries.Remove(this);
	}

	private void ForceUpdate()
	{
		if (PrestigeSkill == null)
		{
			return;
		}
		RebindIconsIfNeeded();
		Icons.RemoveAll((Image a) => a == null);
		DefaultContent.ForEach(delegate(GameObject a)
		{
			a.SetActiveSmart(!PrestigeSkill.IsMaxLevel);
		});
		MaxContent.ForEach(delegate(GameObject a)
		{
			a.SetActiveSmart(PrestigeSkill.IsMaxLevel);
		});
		bool flag = LastState != PrestigeState.Unknown;
		DefaultDetails.SetActiveSmart(flag && isHovered);
		UndiscoveredDetails.SetActiveSmart(!flag && isHovered);
		UpdateState();
		RefreshHoverTexts();
		if (!(PrestigeSkill != null))
		{
			return;
		}
		Sprite sprite = (flag ? PrestigeSkill.Icon : UnknownSprite);
		foreach (Image icon in Icons)
		{
			if (!(icon.sprite == sprite))
			{
				icon.sprite = sprite;
			}
		}
	}

	private void RefreshHoverTexts()
	{
		if (PrestigeSkill == null || !isHovered)
		{
			return;
		}
		PriceText.text = $"花 <b><size=10>{PrestigeSkill.CurrentPrice}</size></b> 张门票购买";
		NameText.text = $"{PrestigeSkill.DisplayName} {PrestigeSkill.CurrentLevel}/{PrestigeSkill.MaxLevel}";
		bool flag = LastState != PrestigeState.Unknown;
		DefaultDetails.SetActiveSmart(flag);
		UndiscoveredDetails.SetActiveSmart(!flag);
	}

	private void UpdateState(bool forced = false)
	{
		if (LastState == State && !forced)
		{
			return;
		}
		LastState = State;
		MainCG.interactable = true;
		MainCG.blocksRaycasts = true;
		switch (LastState)
		{
		case PrestigeState.Unknown:
			MainCG.DOFade(unKnownAlpha, animDuration);
			CurrentAlpha = unKnownAlpha;
			RecolorOnStateChange.ForEach(delegate(Graphic a)
			{
				a.DOColor(UnknownColor, animDuration);
			});
			RecolorOnStateChange.ForEach(delegate(Graphic a)
			{
				originalColors[a] = UnknownColor;
			});
			CurrentColor = UnknownColor;
			break;
		case PrestigeState.New:
			MainCG.DOFade(idleAlpha, animDuration);
			CurrentAlpha = idleAlpha;
			RecolorOnStateChange.ForEach(delegate(Graphic a)
			{
				a.DOColor(NewColor, animDuration);
			});
			RecolorOnStateChange.ForEach(delegate(Graphic a)
			{
				originalColors[a] = NewColor;
			});
			CurrentColor = NewColor;
			break;
		case PrestigeState.Progressed:
			MainCG.DOFade(idleAlpha, animDuration);
			CurrentAlpha = idleAlpha;
			RecolorOnStateChange.ForEach(delegate(Graphic a)
			{
				a.DOColor(ProgressedColor, animDuration);
			});
			RecolorOnStateChange.ForEach(delegate(Graphic a)
			{
				originalColors[a] = ProgressedColor;
			});
			CurrentColor = ProgressedColor;
			break;
		case PrestigeState.Maxed:
			MainCG.DOFade(idleAlpha, animDuration);
			CurrentAlpha = idleAlpha;
			RecolorOnStateChange.ForEach(delegate(Graphic a)
			{
				a.DOColor(ProgressedColor, animDuration);
			});
			RecolorOnStateChange.ForEach(delegate(Graphic a)
			{
				originalColors[a] = ProgressedColor;
			});
			CurrentColor = MaxedColor;
			break;
		}
	}

	private void OnPurchaseClicked()
	{
		if (PrestigeSkill == null)
		{
			return;
		}
		nextHover = Time.time + hoverCooldown;
		if (!Singleton<PrestigeScreen>.Current.IsShoppingMode)
		{
			Singleton<PrestigeScreen>.Current.AttemptedShopping();
			return;
		}
		if ((State != PrestigeState.New && State != PrestigeState.Progressed) || PrestigeSkill.CurrentPrice > Singleton<LootManager>.Current.Tickets)
		{
			Singleton<Shaker>.Current.ShakeDenied();
			PlayClickDeclined();
			Singleton<PrestigeScreen>.Current.AttemptedShopping();
		}
		else
		{
			Singleton<Shaker>.Current.ShakeUpgradeFeelGood();
			PlayClickAccepted();
			SaveManager.Current.SaveData.TicketCount -= PrestigeSkill.CurrentPrice;
			SaveManager.Current.SetPrestigeLevel(PrestigeSkill, PrestigeSkill.CurrentLevel + 1);
			Singleton<AudioPool>.Current.Play(PurchasedSFX);
		}
		Singleton<GameEvents>.Current.OnPrestigeChange?.Invoke();
		ForceUpdate();
		OnPointerEnter(null);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		isHovered = true;
		rect.DOScale(Vector3.one * sizeScale, animDuration).SetEase(Ease.OutBack);
		if (nextHover < Time.time)
		{
			Singleton<AudioPool>.Current.Play(hoverSFX, base.transform.position);
		}
		waveTween = waveTransform.DOShakeAnchorPos(animDuration, new Vector2(waveShakeStrength, waveShakeStrength), waveShakeVibrato).SetEase(Ease.OutQuad).SetLoops(-1, LoopType.Yoyo);
		SelectionHighlight.SetActiveSmart(newState: true);
		DetailPanel.SetActiveSmart(newState: true);
		MainCG.DOFade(1f, animDuration);
		DescriptionText.text = PrestigeSkill.DebugDescription;
		base.transform.SetAsLastSibling();
		RefreshHoverTexts();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		isHovered = false;
		rect.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack);
		waveTween?.Kill();
		MainCG.DOFade(CurrentAlpha, animDuration);
		SelectionHighlight.SetActiveSmart(newState: false);
		DetailPanel.SetActiveSmart(newState: false);
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

	private void PlayClickAccepted()
	{
		rect.DOScale(Vector3.one * pressScale, animDuration).SetEase(Ease.InOutQuad).OnComplete(delegate
		{
			rect.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack);
		});
	}

	private void PlayClickDeclined()
	{
		rect.DOShakeAnchorPos(animDuration, new Vector2(declineShakeStrength, 0f), declineShakeVibrato).SetEase(Ease.OutQuad);
		FlashDeclineColor();
	}

	private void RebindIconsIfNeeded()
	{
		bool needsRebind = Icons == null || Icons.Count == 0 || Icons.Exists((Image a) => a == null);
		if (!needsRebind)
		{
			return;
		}
		if (Icons == null)
		{
			Icons = new List<Image>();
		}
		Icons.RemoveAll((Image a) => a == null);
		AddNamedIcon("Default Icon");
		AddNamedIcon("Max Icon");
	}

	private void AddNamedIcon(string childName)
	{
		Transform transform = base.transform.Find(childName);
		if (transform == null)
		{
			return;
		}
		Image component = transform.GetComponent<Image>();
		if (component != null && !Icons.Contains(component))
		{
			Icons.Add(component);
		}
	}
}
