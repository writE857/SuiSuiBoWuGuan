using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Compat Audio/Random Container")]
public class AudioRandomContainer : AudioResource
{
	public float m_Volume;

	public float m_Pitch;

	public float m_AutomaticTriggerTime;

	public int m_LoopCount = 1;

	public Vector2 m_VolumeRandomizationRange;

	public Vector2 m_PitchRandomizationRange;

	public Vector2 m_AutomaticTriggerTimeRandomizationRange;

	public Vector2 m_LoopCountRandomizationRange;

	public List<AudioResource> m_Elements = new List<AudioResource>();

	public int m_AvoidRepeatingLast = 3;

	public int m_PlaybackMode = 1;

	public int m_TriggerMode;

	public int m_AutomaticTriggerMode = 1;

	public int m_LoopMode = 2;

	public bool m_VolumeRandomizationEnabled;

	public bool m_PitchRandomizationEnabled;

	public bool m_AutomaticTriggerTimeRandomizationEnabled;

	public bool m_LoopCountRandomizationEnabled;

	[System.NonSerialized]
	private readonly Queue<int> recentIndices = new Queue<int>();

	public override bool ApplyTo(AudioSource audioSource)
	{
		if (audioSource == null)
		{
			return false;
		}
		List<AudioResource> playable = m_Elements.Where(e => e != null).ToList();
		if (playable.Count == 0)
		{
			return false;
		}
		int index = GetNextIndex(playable);
		AudioResource element = playable[index];
		if (element == null || !element.ApplyTo(audioSource))
		{
			return false;
		}
		float volumeDb = m_Volume;
		if (m_VolumeRandomizationEnabled)
		{
			volumeDb += Random.Range(m_VolumeRandomizationRange.x, m_VolumeRandomizationRange.y);
		}
		float pitchCents = m_Pitch;
		if (m_PitchRandomizationEnabled)
		{
			pitchCents += Random.Range(m_PitchRandomizationRange.x, m_PitchRandomizationRange.y);
		}
		audioSource.volume *= DbToLinear(volumeDb);
		audioSource.pitch = Mathf.Clamp(Mathf.Pow(2f, pitchCents / 1200f), 0.01f, 4f);
		audioSource.loop = ShouldLoop(audioSource.clip);
		return true;
	}

	private bool ShouldLoop(AudioClip clip)
	{
		if (m_LoopMode == 0 || clip == null)
		{
			return false;
		}
		return clip.length >= 10f;
	}

	private int GetNextIndex(List<AudioResource> playable)
	{
		if (playable.Count == 1)
		{
			return 0;
		}
		if (m_PlaybackMode == 0)
		{
			int next = 0;
			if (recentIndices.Count > 0)
			{
				next = (recentIndices.Last() + 1) % playable.Count;
			}
			Remember(next);
			return next;
		}
		List<int> candidates = new List<int>();
		for (int i = 0; i < playable.Count; i++)
		{
			if (!recentIndices.Contains(i))
			{
				candidates.Add(i);
			}
		}
		if (candidates.Count == 0)
		{
			candidates.AddRange(Enumerable.Range(0, playable.Count));
		}
		int chosen = candidates[Random.Range(0, candidates.Count)];
		Remember(chosen);
		return chosen;
	}

	private void Remember(int index)
	{
		if (m_AvoidRepeatingLast <= 0)
		{
			recentIndices.Clear();
			return;
		}
		recentIndices.Enqueue(index);
		while (recentIndices.Count > m_AvoidRepeatingLast)
		{
			recentIndices.Dequeue();
		}
	}
}
