using System.Collections;
using System.Numerics;
using TMPro;
using UnityEngine;

public class MoneyText : MonoBehaviour
{
	public TMP_Text Text;

	private BigInteger lastAmountBig = 0;

	public float animationDuration = 0.4f;

	private BigInteger lastGoal;

	private Coroutine animRoutine;

	private bool hasRendered;

	private void Awake()
	{
		ConfigureText();
	}

	private void Update()
	{
		GameSession current = Singleton<GameSession>.Current;
		if (current == null || Text == null)
		{
			return;
		}
		if (!hasRendered || lastGoal != current.Money)
		{
			SetMoney(current.Money);
		}
	}

	public void SetMoney(BigInteger newAmount)
	{
		if (Text == null)
		{
			return;
		}
		ConfigureText();
		if (animRoutine != null)
		{
			StopCoroutine(animRoutine);
		}
		animRoutine = StartCoroutine(AnimateMoney(lastAmountBig, newAmount));
		lastGoal = newAmount;
		lastAmountBig = newAmount;
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
			lastAmountBig = BigIntegerLerp(from, to, t);
			UpdateText(lastAmountBig);
			yield return null;
		}
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
}
