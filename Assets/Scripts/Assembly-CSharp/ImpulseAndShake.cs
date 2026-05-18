using Unity.Cinemachine;
using UnityEngine.Events;

public class ImpulseAndShake : Singleton<ImpulseAndShake>
{
	public float ImpulseRatio;

	public float Multiplier = 0.5f;

	public CinemachineImpulseSource CinemachineImpulseSource;

	public UnityAction<float> OnShake;

	public void EmitImpulse(float v)
	{
		CinemachineImpulseSource.GenerateImpulseWithForce(v);
		OnShake?.Invoke(v);
	}
}
