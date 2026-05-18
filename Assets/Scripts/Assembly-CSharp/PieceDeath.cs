using UnityEngine;

public class PieceDeath : MonoBehaviour
{
	public float duration = 1f;

	public AnimationCurve scaleCurve;

	private float since;

	public float startScale = 0.3f;

	public bool IsDoing;

	public bool invertCurve = true;

	public void CopyTo(PieceDeath other)
	{
		other.duration = duration;
		other.scaleCurve = scaleCurve;
		other.startScale = startScale;
	}

	public void Init()
	{
		base.transform.localScale = Vector3.one * startScale;
	}

	private void FixedUpdate()
	{
		if (IsDoing)
		{
			float value = (Time.fixedTime - since) / duration;
			value = Mathf.Clamp01(value);
			value = ((!invertCurve) ? scaleCurve.Evaluate(value) : scaleCurve.Evaluate(1f - value));
			base.transform.localScale = Vector3.one * value * startScale;
		}
	}

	public void StartDoing()
	{
		IsDoing = true;
		since = Time.time;
		Object.Destroy(base.gameObject, duration);
	}
}
