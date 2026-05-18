using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UI;

public class PrestigeScreen : Singleton<PrestigeScreen>
{
	public MonitorScreen MonitorScreen;

	public bool IsShoppingMode;

	private List<PrestigeNode> Nodes = new List<PrestigeNode>();

	public RectTransform ViewModeRect;

	public GameObject TicketCounter;

	public GameObject ViewWarning;

	public GameObject PreviewText2;

	public Color declineColor = Color.red;

	public float colorFlashDuration = 0.1f;

	public AudioResource DeniedSFX;

	public float animDuration = 0.15f;

	public float declineShakeStrength = 20f;

	public int declineShakeVibrato = 20;

	private List<Graphic> graphics = new List<Graphic>();

	private Dictionary<Graphic, Color> originalColors = new Dictionary<Graphic, Color>();

	public GameObject BasicBackButton;

	public Button DoneBackButton;

	public bool didPressDone;

	public ConfirmationDialogTexts PrestigeDoneDialog;

	internal void AttemptedShopping()
	{
		Singleton<Shaker>.Current.ShakeDenied();
		PlayClickDeclined();
		Singleton<AudioPool>.Current.Play(DeniedSFX);
	}

	private void PlayClickDeclined()
	{
		ViewModeRect.DOShakeAnchorPos(animDuration, new Vector2(declineShakeStrength, 0f), declineShakeVibrato).SetEase(Ease.OutQuad);
		FlashDeclineColor();
	}

	private void Start()
	{
		GetComponentsInChildren(includeInactive: true, Nodes);
		ViewModeRect.GetComponentsInChildren(includeInactive: true, graphics);
		foreach (Graphic graphic in graphics)
		{
			originalColors[graphic] = graphic.color;
		}
		DoneBackButton.onClick.AddListener(OnDoneClicked);
	}

	private void Update()
	{
		BasicBackButton.gameObject.SetActiveSmart(!IsShoppingMode);
		DoneBackButton.gameObject.SetActiveSmart(IsShoppingMode);
		TicketCounter.gameObject.SetActiveSmart(IsShoppingMode);
		ViewWarning.gameObject.SetActiveSmart(!IsShoppingMode);
		PreviewText2.gameObject.SetActiveSmart(!IsShoppingMode);
	}

	private void OnDoneClicked()
	{
		if (!didPressDone)
		{
			didPressDone = true;
			Singleton<MainScreen>.Current.ShowConfirmationScreen(PrestigeDoneDialog, Singleton<MainScreen>.Current.PrestigeScr);
			ConfirmationScreen confirmationScreen = Singleton<MainScreen>.Current.ConfirmationScreen;
			confirmationScreen.OnNoClicked = (UnityAction)Delegate.Combine(confirmationScreen.OnNoClicked, new UnityAction(OnNoClicked));
			ConfirmationScreen confirmationScreen2 = Singleton<MainScreen>.Current.ConfirmationScreen;
			confirmationScreen2.OnYesClicked = (UnityAction)Delegate.Combine(confirmationScreen2.OnYesClicked, new UnityAction(OnYesClicked));
		}
	}

	private void OnNoClicked()
	{
		didPressDone = false;
	}

	private void OnYesClicked()
	{
		didPressDone = false;
		SaveManager.Current.PRESTIGERESTART();
		Singleton<GameSession>.Current.PrestigeRestart();
		Singleton<BrickTable>.Current.RemoveBricks();
		Singleton<MainScreen>.Current.Activate(Singleton<MainScreen>.Current.RoomScreen);
		Singleton<GameSession>.Current.IsInMenu = false;
		IsShoppingMode = false;
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
