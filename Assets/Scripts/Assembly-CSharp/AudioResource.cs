using UnityEngine;

public abstract class AudioResource : ScriptableObject
{
	public abstract bool ApplyTo(AudioSource audioSource);

	protected static float DbToLinear(float db)
	{
		if (db <= -80f)
		{
			return 0f;
		}
		return Mathf.Pow(10f, db / 20f);
	}
}
