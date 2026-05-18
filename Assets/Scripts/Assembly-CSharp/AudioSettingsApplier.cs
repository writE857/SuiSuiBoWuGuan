using System.Collections.Generic;
using UnityEngine.Audio;

public class AudioSettingsApplier : Singleton<AudioSettingsApplier>
{
	private List<AudioMixer> mixers = new List<AudioMixer>();

	private List<string> exposed = new List<string>();

	private List<float> values = new List<float>();

	public void Set(AudioMixer mixer, string exposedParam, float value)
	{
		int num = exposed.IndexOf(exposedParam);
		if (num == -1)
		{
			mixers.Add(mixer);
			exposed.Add(exposedParam);
			values.Add(value);
		}
		num = exposed.IndexOf(exposedParam);
		values[num] = value;
	}

	private void Update()
	{
		for (int i = 0; i < mixers.Count; i++)
		{
			float value = 0f;
			mixers[i].GetFloat(exposed[i], out value);
			if (value != values[i])
			{
				mixers[i].SetFloat(exposed[i], values[i]);
			}
		}
	}
}
