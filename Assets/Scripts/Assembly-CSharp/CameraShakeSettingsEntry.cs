using System.Collections.Generic;
using UnityEngine;

public class CameraShakeSettingsEntry : SettingsEntryLogic
{
	protected override void InitOptions()
	{
		options = new List<string>();
		for (int i = 0; i <= 100; i += 5)
		{
			options.Add(i + "%");
		}
		DefaultIndex = options.IndexOf("100%");
	}

	protected override void _Apply()
	{
		if (Singleton<Shaker>.Current == null)
		{
			Debug.LogWarning("Shaker not found.");
			return;
		}
		int num = int.Parse(options[SaveIndex].TrimEnd('%'));
		Singleton<Shaker>.Current.shakeMultiplier = (float)num * 0.01f;
		Debug.Log($"Shake set to {num}%");
	}
}
