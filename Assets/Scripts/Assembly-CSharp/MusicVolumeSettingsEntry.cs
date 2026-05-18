using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicVolumeSettingsEntry : SettingsEntryLogic
{
	public AudioMixer mixer;

	public string exposedParam = "MusicVolume";

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
		if (mixer == null)
		{
			Debug.LogWarning("MusicVolumeSettingsEntry: Mixer not assigned.");
			return;
		}
		int num = int.Parse(options[SaveIndex].TrimEnd('%'));
		float num2 = AudioExtensions.PercentToDecibels(num);
		Singleton<AudioSettingsApplier>.Current.Set(mixer, exposedParam, num2);
		Debug.Log($"Music volume set to {num}% ({num2} dB)");
	}
}
