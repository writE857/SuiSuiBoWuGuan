using UnityEngine;

[CreateAssetMenu(menuName = "Compat Audio/Container Element")]
public class AudioContainerElement : AudioResource
{
	public AudioClip m_AudioClip;

	public float m_Volume;

	public bool resourceEnabled = true;

	public override bool ApplyTo(AudioSource audioSource)
	{
		if (audioSource == null || !resourceEnabled || m_AudioClip == null)
		{
			return false;
		}
		audioSource.clip = m_AudioClip;
		audioSource.volume = DbToLinear(m_Volume);
		audioSource.pitch = 1f;
		audioSource.loop = false;
		return true;
	}
}
