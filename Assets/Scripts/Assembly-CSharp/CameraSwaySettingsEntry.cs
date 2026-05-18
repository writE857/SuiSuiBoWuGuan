using System.Collections.Generic;
using UnityEngine;

public class CameraSwaySettingsEntry : SettingsEntryLogic
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
			Debug.LogWarning("Swayer not found.");
			return;
		}
		int num = int.Parse(options[SaveIndex].TrimEnd('%'));
		Singleton<Shaker>.Current.swayMultiplier = (float)num * 0.01f;
		Debug.Log($"Sway set to {num}%");
	}
}
