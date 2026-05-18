using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class Shaker : Singleton<Shaker>
{
	public float shakeMultiplier = 1f;

	public Transform target;

	private Vector3 originalPos;

	public float swayMultiplier = 1f;

	public Dictionary<CinemachineBasicMultiChannelPerlin, float> originalSwayValues = new Dictionary<CinemachineBasicMultiChannelPerlin, float>();

	public List<CinemachineBasicMultiChannelPerlin> Sways = new List<CinemachineBasicMultiChannelPerlin>();

	[Header("Denied")]
	public Vector3 denyStrength = new Vector3(20f, 0f, 0f);

	public int denyVibratio = 20;

	public float denyDuration = 0.25f;

	[Header("Loot Extracted")]
	public Vector3 hitStrength = new Vector3(20f, 0f, 0f);

	public int hitVibratio = 20;

	public float hitDuration = 0.25f;

	[Header("Sell Kick")]
	public Vector3 kickStrength = new Vector3(20f, 0f, 0f);

	public int kickVibratio = 20;

	public float kickDuration = 0.25f;

	[Header("Upgrade")]
	public Vector3 upgradeStrength = new Vector3(20f, 0f, 0f);

	public int upgradeVibratio = 20;

	public float upgradeDuration = 0.25f;

	[Header("Heavy Thud")]
	public Vector3 thudStrength = new Vector3(20f, 0f, 0f);

	public int thudVibratio = 20;

	public float thudDuration = 0.25f;

	public float thudWait = 0.1f;

	private void Awake()
	{
		originalPos = target.localPosition;
		originalSwayValues = new Dictionary<CinemachineBasicMultiChannelPerlin, float>();
		Sways.ForEach(delegate(CinemachineBasicMultiChannelPerlin a)
		{
			originalSwayValues.Add(a, a.AmplitudeGain);
		});
	}

	private void Update()
	{
		foreach (CinemachineBasicMultiChannelPerlin sway in Sways)
		{
			if (originalSwayValues.ContainsKey(sway))
			{
				sway.AmplitudeGain = originalSwayValues[sway] * swayMultiplier;
			}
		}
	}

	private void KillAndReset()
	{
		target.DOKill();
		target.localPosition = originalPos;
	}

	public void ShakeDenied()
	{
		KillAndReset();
		DOTween.Sequence().Append(target.DOShakePosition(denyDuration, denyStrength * shakeMultiplier, denyVibratio, 0f));
	}

	public void ShakeHammerHit()
	{
		KillAndReset();
		DOTween.Sequence().Append(target.DOShakePosition(hitDuration, hitStrength * shakeMultiplier, hitVibratio, 0f));
	}

	public void ShakeSellKick()
	{
		KillAndReset();
		DOTween.Sequence().Append(target.DOShakePosition(kickDuration, kickStrength * shakeMultiplier, kickVibratio, 0f));
	}

	public void ShakeUpgradeFeelGood()
	{
		KillAndReset();
		DOTween.Sequence().Append(target.DOShakePosition(upgradeDuration, upgradeStrength * shakeMultiplier, upgradeVibratio, 0f));
	}

	public void ShakeHeavyThud()
	{
		KillAndReset();
		DOTween.Sequence().AppendInterval(thudWait).Append(target.DOShakePosition(thudDuration, thudStrength * shakeMultiplier, thudVibratio, 0f));
	}
}
