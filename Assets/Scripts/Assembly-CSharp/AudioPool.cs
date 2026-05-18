using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class AudioPool : Singleton<AudioPool>
{
	public AudioPlayerObject AudioPrefab;

	private List<AudioPlayerObject> AudioPlayerObjects = new List<AudioPlayerObject>();

	public AudioPlayerObject Play(AudioResource audioResource)
	{
		if (audioResource == null)
		{
			return null;
		}
		AudioPlayerObject audioPlayerObject = AudioPlayerObjects.FirstOrDefault((AudioPlayerObject a) => !a.isActiveAndEnabled);
		if (audioPlayerObject == null)
		{
			audioPlayerObject = Object.Instantiate(AudioPrefab, base.transform);
			AudioPlayerObjects.Add(audioPlayerObject);
		}
		if (!audioResource.ApplyTo(audioPlayerObject.AudioSource))
		{
			return null;
		}
		audioPlayerObject.Play();
		return audioPlayerObject;
	}

	public AudioPlayerObject Play(AudioResource audioResource, Vector3 position, int? priority = null)
	{
		if (audioResource == null)
		{
			return null;
		}
		int priority2 = priority ?? 0;
		AudioPlayerObject audioPlayerObject = Play(audioResource);
		audioPlayerObject.transform.position = position;
		audioPlayerObject.AudioSource.priority = priority2;
		return audioPlayerObject;
	}
}
