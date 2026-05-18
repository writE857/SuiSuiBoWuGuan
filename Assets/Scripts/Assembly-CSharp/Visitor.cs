using UnityEngine;
using UnityEngine.Events;

public class Visitor : MonoBehaviour
{
	public float speedVariation = 0.5f;

	public float speed = 5f;

	public float chanceToStopPerSec = 0.2f;

	public Vector2 stopDuration = new Vector2(1f, 3f);

	public bool IsStopped;

	public float stopLeft;

	public bool IsEntered;

	public float minX;

	public float maxX = 100f;

	public int FeeMultiplier = 1;

	public bool IsGolden;

	public Vector3 localPosition = Vector3.zero;

	public UnityAction<Visitor> OnEnter;

	public UnityAction<Visitor> OnExit;

	private void Start()
	{
		localPosition = base.transform.localPosition;
		speed *= Random.Range(1f - speedVariation, 1f + speedVariation);
	}

	internal void ManualUpdate(float deltaTime)
	{
		if (IsStopped)
		{
			stopLeft -= deltaTime;
			if (stopLeft < 0f)
			{
				IsStopped = false;
			}
			return;
		}
		if (Random.value < chanceToStopPerSec * deltaTime)
		{
			IsStopped = true;
			stopLeft = stopDuration.GetRandomBetweenXY();
			Singleton<VisitorManager>.Current.OnVisitorStopped?.Invoke(this, stopLeft);
			return;
		}
		localPosition += Vector3.right * speed * deltaTime * Singleton<VisitorManager>.Current.VisitorSpeedMultiplier;
		if (localPosition.x > maxX)
		{
			OnExit?.Invoke(this);
			Object.Destroy(base.gameObject);
		}
		else if (!IsEntered && localPosition.x > minX)
		{
			OnEnter?.Invoke(this);
			IsEntered = true;
		}
		base.transform.localPosition = localPosition;
	}
}
