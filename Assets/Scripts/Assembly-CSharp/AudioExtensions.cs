using UnityEngine;

public static class AudioExtensions
{
	public static float PercentToDecibels(int percent)
	{
		if (percent <= 0)
		{
			return -80f;
		}
		return Mathf.Log10((float)percent / 100f) * 20f;
	}
}
