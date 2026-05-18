using System;
using UnityEngine;
using UnityEngine.Events;

public class ShakeOnHammerHit : MonoBehaviour
{
	public ImpulseAndShake ImpulseAndShake;

	private void OnEnable()
	{
		Hammer current = Singleton<Hammer>.Current;
		current.OnHit = (UnityAction)Delegate.Combine(current.OnHit, new UnityAction(OnHammerHit));
	}

	private void OnHammerHit()
	{
		ImpulseAndShake.EmitImpulse(1f);
	}
}
