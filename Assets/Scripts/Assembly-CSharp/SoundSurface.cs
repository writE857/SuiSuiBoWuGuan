using UnityEngine;
using UnityEngine.Audio;

public class SoundSurface : MonoBehaviour
{
	public AudioResource AudioResource;

	public Vector2 VelocityRange = new Vector2(0.1f, 1f);

	public float minDelay = 0.05f;

	private float lastTime;

	private void OnCollisionEnter(Collision collision)
	{
		if (!(Time.time - lastTime < minDelay) && !(collision.relativeVelocity.magnitude < VelocityRange.x) && collision.gameObject.GetComponentInParent<SoundSurface>() != null)
		{
			Singleton<AudioPool>.Current.Play(AudioResource, collision.contacts[0].point);
			lastTime = Time.time;
		}
	}
}
