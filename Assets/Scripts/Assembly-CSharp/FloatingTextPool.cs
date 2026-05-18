using UnityEngine;

public class FloatingTextPool : Singleton<FloatingTextPool>
{
	public FloatingText moneyPrefab;

	public FloatingText damagePrefab;

	public FloatingText artifactPrefab;

	public FloatingText coinPrefab;

	public RectTransform Parent;

	public void SpawnMoney(string text, Vector3 worldPos)
	{
		if (moneyPrefab == null)
		{
			Debug.LogError("FloatingText prefab not set! Call FloatingText.SetPrefab().");
			return;
		}
		FloatingText floatingText = Object.Instantiate(moneyPrefab, Parent);
		Vector3 vector = Camera.main.WorldToScreenPoint(worldPos);
		RectTransform component = floatingText.GetComponent<RectTransform>();
		vector.z = 0f;
		component.anchoredPosition = vector;
		floatingText.Play(text);
	}

	public void SpawnDamage(string text, Vector3 worldPos)
	{
		if (moneyPrefab == null)
		{
			Debug.LogError("FloatingText prefab not set! Call FloatingText.SetPrefab().");
			return;
		}
		FloatingText floatingText = Object.Instantiate(damagePrefab, Parent);
		Vector3 vector = Camera.main.WorldToScreenPoint(worldPos);
		RectTransform component = floatingText.GetComponent<RectTransform>();
		vector.z = 0f;
		component.anchoredPosition = vector;
		floatingText.Play(text);
	}

	public void SpawnArtifact(string text, Vector3 worldPos)
	{
		if (artifactPrefab == null)
		{
			Debug.LogError("FloatingText prefab not set! Call FloatingText.SetPrefab().");
			return;
		}
		FloatingText floatingText = Object.Instantiate(artifactPrefab, Parent);
		Vector3 vector = Camera.main.WorldToScreenPoint(worldPos);
		RectTransform component = floatingText.GetComponent<RectTransform>();
		vector.z = 0f;
		component.anchoredPosition = vector;
		floatingText.Play(text);
	}

	internal void SpawnCoin(string text, Vector3 worldPos)
	{
		if (coinPrefab == null)
		{
			Debug.LogError("FloatingText prefab not set! Call FloatingText.SetPrefab().");
			return;
		}
		FloatingText floatingText = Object.Instantiate(coinPrefab, Parent);
		Vector3 vector = Camera.main.WorldToScreenPoint(worldPos);
		RectTransform component = floatingText.GetComponent<RectTransform>();
		vector.z = 0f;
		component.anchoredPosition = vector;
		floatingText.Play(text);
	}
}
