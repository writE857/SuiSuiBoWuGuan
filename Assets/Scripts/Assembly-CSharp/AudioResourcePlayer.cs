using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioResourcePlayer : MonoBehaviour
{
	public AudioSource AudioSource;

	public AudioResource AudioResource;

	public bool PlayOnStart = true;

	public bool RestartWhenStopped = true;

	public float RetryDelay = 0.5f;

	private float nextRetryTime;

	private bool shouldBePlaying;

	private void Awake()
	{
		if (AudioSource == null)
		{
			AudioSource = GetComponent<AudioSource>();
		}
	}

	private void Start()
	{
		if (PlayOnStart)
		{
			shouldBePlaying = true;
			Play();
		}
	}

	private void Update()
	{
		if (!shouldBePlaying || !RestartWhenStopped || Time.unscaledTime < nextRetryTime)
		{
			return;
		}
		if (AudioSource == null || AudioResource == null)
		{
			nextRetryTime = Time.unscaledTime + RetryDelay;
			return;
		}
		if (!AudioSource.isPlaying)
		{
			Play();
		}
	}

	public void Play()
	{
		if (AudioSource == null || AudioResource == null)
		{
			return;
		}
		if (AudioResource.ApplyTo(AudioSource))
		{
			AudioSource.Play();
		}
		nextRetryTime = Time.unscaledTime + RetryDelay;
	}
}
