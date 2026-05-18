using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RainbowText : MonoBehaviour
{
	public float startHue = 180f;

	public float endHue = 290f;

	public float duration = 2f;

	public Graphic Graphic;

	private void OnEnable()
	{
		if (Graphic == null)
		{
			Graphic = GetComponent<Graphic>();
		}
		StartCoroutine(HueLoop());
	}

	private IEnumerator HueLoop()
	{
		while (true)
		{
			yield return StartCoroutine(AnimateHue(startHue, endHue, 0.9f, 1f));
			yield return StartCoroutine(AnimateHue(endHue, startHue, 0.9f, 1f));
		}
	}

	private IEnumerator AnimateHue(float fromH, float toH, float s, float v)
	{
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime / duration;
			float num = Mathf.Lerp(fromH, toH, t);
			Graphic.color = Color.HSVToRGB(num / 360f, s, v);
			yield return null;
		}
	}
}
