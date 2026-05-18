using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UI;

public class MoneyTextAnimations : MonoBehaviour
{
	public float animDuration = 0.15f;

	public float declineShakeStrength = 20f;

	public int declineShakeVibrato = 20;

	private RectTransform rect;

	private Vector3 originalScale;

	public Color declineColor = Color.red;

	public float colorFlashDuration = 0.1f;

	public float pressScale = 0.9f;

	private List<Graphic> graphics = new List<Graphic>();

	private Dictionary<Graphic, Color> originalColors = new Dictionary<Graphic, Color>();

	public AudioResource declinedSound;

	public AudioResource spentSound;

	private void Start()
	{
		rect = GetComponent<RectTransform>();
		originalScale = rect.localScale;
		GetComponentsInChildren(includeInactive: true, graphics);
		foreach (Graphic graphic in graphics)
		{
			originalColors[graphic] = graphic.color;
		}
		GameEvents current2 = Singleton<GameEvents>.Current;
		current2.OnTrySpendDeclined = (UnityAction)Delegate.Combine(current2.OnTrySpendDeclined, new UnityAction(OnTrySpendDeclined));
		GameEvents current3 = Singleton<GameEvents>.Current;
		current3.OnMoneySpent = (UnityAction<int>)Delegate.Combine(current3.OnMoneySpent, new UnityAction<int>(OnMoneySpent));
	}

	private void OnTrySpendDeclined()
	{
		Singleton<AudioPool>.Current.Play(declinedSound, base.transform.position);
		Singleton<Shaker>.Current.ShakeDenied();
		PlayClickDeclined();
	}

	private void OnMoneySpent(int amount)
	{
		Singleton<AudioPool>.Current.Play(spentSound, base.transform.position);
		PlayClickAccepted();
	}

	private void PlayClickAccepted()
	{
		rect.DOScale(originalScale * pressScale, animDuration).SetEase(Ease.InOutQuad).OnComplete(delegate
		{
			rect.DOScale(originalScale, animDuration).SetEase(Ease.OutBack);
		});
	}

	private void PlayClickDeclined()
	{
		rect.DOShakeAnchorPos(animDuration, new Vector2(declineShakeStrength, 0f), declineShakeVibrato).SetEase(Ease.OutQuad);
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
