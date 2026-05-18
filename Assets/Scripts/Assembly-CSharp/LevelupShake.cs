using UnityEngine;

public class LevelupShake : MonoBehaviour
{
	public float impulseGain = 1f;

	private void Start()
	{
	}

	private void OnLevelUp()
	{
		Singleton<ImpulseAndShake>.Current.EmitImpulse(impulseGain);
	}
}
