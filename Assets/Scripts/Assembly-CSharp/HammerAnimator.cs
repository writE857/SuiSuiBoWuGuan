using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class HammerAnimator : MonoBehaviour
{
	[Header("Hammer Settings")]
	public float downAngle = 5f;

	public float UpAngle = -35f;

	public float swingDownTime = 0.12f;

	public float swingUpTime = 0.15f;

	public Transform target;

	public UnityAction OnHitEnd;

	public bool IsSwinging;

	private Tween swingTween;

	private Vector3 restEulerAngles;

	private bool hasRestPose;

	private void Awake()
	{
		CacheRestPose();
	}

	public void HitNow()
	{
		Swing();
	}

	public void Swing()
	{
		CacheRestPose();
		swingTween?.Kill();
		IsSwinging = true;
		OnHitEnd?.Invoke();
		target.localEulerAngles = new Vector3(UpAngle, 0f, 0f);
		swingTween = DOTween.Sequence()
			.Append(target.DOLocalRotate(new Vector3(downAngle, 0f, 0f), swingDownTime).SetEase(Ease.OutQuad))
			.Append(target.DOLocalRotate(restEulerAngles, swingUpTime).SetEase(Ease.InQuad))
			.OnComplete(delegate
			{
				IsSwinging = false;
			})
			.OnKill(delegate
			{
				IsSwinging = false;
			});
	}

	private void CacheRestPose()
	{
		if (!hasRestPose && target != null)
		{
			restEulerAngles = target.localEulerAngles;
			hasRestPose = true;
		}
	}
}
