using UnityEngine;

public class AudioPlayerObject : MonoBehaviour
{
	public AudioSource AudioSource;

	public void Play()
	{
		if (AudioSource == null || AudioSource.clip == null)
		{
			base.gameObject.SetActiveSmart(newState: false);
			return;
		}
		base.gameObject.SetActiveSmart(newState: true);
		AudioSource.Play();
	}

	private void Update()
	{
		if (!AudioSource.isPlaying)
		{
			base.gameObject.SetActiveSmart(newState: false);
		}
	}
}
