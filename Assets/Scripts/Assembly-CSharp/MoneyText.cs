using System.Collections;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class MoneyText : MonoBehaviour
{
	public TMP_Text Text;

	private BigInteger displayedAmount = 0;

	public float animationDuration = 0.4f;

	private BigInteger targetAmount;

	private Coroutine animRoutine;

	private bool hasRendered;

	private void Awake()
	{
		ConfigureText();
	}

	private void OnEnable()
	{
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnMoneyAdded += OnMoneyChanged;
		current.OnMoneySpent += OnMoneyChanged;
		current.OnRestart += RefreshImmediate;
		current.OnPrestigeChange += RefreshImmediate;
		RefreshImmediate();
	}

	private void OnDisable()
	{
		GameEvents gameEvents = UnityEngine.Object.FindFirstObjectByType<GameEvents>(FindObjectsInactive.Include);
		if (gameEvents == null)
		{
			return;
		}
		gameEvents.OnMoneyAdded -= OnMoneyChanged;
		gameEvents.OnMoneySpent -= OnMoneyChanged;
		gameEvents.OnRestart -= RefreshImmediate;
		gameEvents.OnPrestigeChange -= RefreshImmediate;
	}

	private void Update()
	{
		if (Text == null)
		{
			return;
		}
		BigInteger currentMoney = GetCurrentMoney();
		if (!hasRendered || targetAmount != currentMoney)
		{
			SetMoney(currentMoney);
		}
	}

	public void SetMoney(BigInteger newAmount)
	{
		SetMoney(newAmount, immediate: false);
	}

	private void SetMoney(BigInteger newAmount, bool immediate)
	{
		if (Text == null)
		{
			return;
		}
		ConfigureText();
		if (immediate)
		{
			if (animRoutine != null)
			{
				StopCoroutine(animRoutine);
				animRoutine = null;
			}
			displayedAmount = newAmount;
			targetAmount = newAmount;
			UpdateText(newAmount);
			hasRendered = true;
			return;
		}
		if (animRoutine != null)
		{
			StopCoroutine(animRoutine);
		}
		animRoutine = StartCoroutine(AnimateMoney(displayedAmount, newAmount));
		targetAmount = newAmount;
		hasRendered = true;
	}

	private IEnumerator AnimateMoney(BigInteger from, BigInteger to)
	{
		float elapsed = 0f;
		while (elapsed < animationDuration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / animationDuration);
			t = Mathf.SmoothStep(0f, 1f, t);
			displayedAmount = BigIntegerLerp(from, to, t);
			UpdateText(displayedAmount);
			yield return null;
		}
		displayedAmount = to;
		animRoutine = null;
		UpdateText(to);
	}

	private void UpdateText(BigInteger value)
	{
		if (Text != null)
		{
			Text.text = "$" + value.ToString("N0");
		}
	}

	private void ConfigureText()
	{
		if (Text == null)
		{
			return;
		}
		Text.enableWordWrapping = false;
		Text.overflowMode = TextOverflowModes.Overflow;
		Text.enableAutoSizing = true;
		Text.fontSizeMin = Mathf.Min(Text.fontSizeMin, 8f);
		Text.fontSizeMax = Mathf.Min(Text.fontSizeMax, 56f);
	}

	public BigInteger BigIntegerLerp(BigInteger a, BigInteger b, float t)
	{
		if (t <= 0f)
		{
			return a;
		}
		if (t >= 1f)
		{
			return b;
		}
		BigInteger bigInteger = (BigInteger)(t * 1000000f);
		BigInteger bigInteger2 = 1000000;
		BigInteger bigInteger3 = b - a;
		return a + bigInteger3 * bigInteger / bigInteger2;
	}

	private void OnMoneyChanged(int amount)
	{
		SetMoney(GetCurrentMoney());
	}

	private void RefreshImmediate()
	{
		SetMoney(GetCurrentMoney(), immediate: true);
	}

	private BigInteger GetCurrentMoney()
	{
		return SaveManager.Current != null && SaveManager.Current.SaveData != null ? SaveManager.Current.SaveData.Money : 0;
	}
}
