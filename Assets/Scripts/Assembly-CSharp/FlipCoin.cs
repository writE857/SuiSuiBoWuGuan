using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class FlipCoin : Singleton<FlipCoin>
{
	public Vector3 originalPosition;

	public float minJumpHeight = 0.1f;

	public float maxJumpHeight = 0.15f;

	public float flipDuration = 1f;

	public int minFlipRotations = 3;

	public int maxFlipRotations = 5;

	public Vector3 maxLandDistance = new Vector3(0.01f, 0f, 0.01f);

	public bool isHead = true;

	public int heads = 1;

	public AudioResource headsSFX;

	public AudioResource tailsSFX;

	public InputActionReference ClickAction;

	public PrestigeSkill HeadChanceSkill;

	public PrestigeSkill ExtraLuckySkill;

	public PrestigeSkill HammerFlipSkill;

	public AnimationCurve AnimationCurve;

	public AnimationCurve JumpCurve;

	private bool isFlipping;

	private float timeDone;

	private Vector3 startPos;

	public Vector3 goalPos;

	private Vector3 startRot;

	private Vector3 goalRot;

	private float jumpHeight;

	private void Start()
	{
		originalPosition = base.transform.position;
		startPos = originalPosition;
		goalPos = originalPosition;
		heads = SaveManager.Current.SaveData.CoinHeads;
		Hammer current = Singleton<Hammer>.Current;
		current.OnHit = (UnityAction)Delegate.Combine(current.OnHit, new UnityAction(OnHammerHit));
	}

	private void Update()
	{
		if (isFlipping)
		{
			float time = Mathf.Clamp01(timeDone / flipDuration);
			float num = AnimationCurve.Evaluate(time);
			base.transform.position = Vector3.Lerp(startPos, goalPos, num);
			base.transform.eulerAngles = Vector3.Lerp(startRot, goalRot, num);
			base.transform.position += Vector3.up * JumpCurve.Evaluate(num) * jumpHeight;
			if (timeDone > flipDuration)
			{
				isFlipping = false;
				base.transform.position = goalPos;
				base.transform.rotation = Quaternion.Euler(goalRot);
				ExecuteResult();
			}
			timeDone += Time.deltaTime;
		}
		if (ClickAction.action.WasPerformedThisFrame())
		{
			FlipTheCoin();
		}
	}

	private void ExecuteResult()
	{
		if (isHead)
		{
			int num = SaveManager.Current.SaveData.CoinValue;
			if (UnityEngine.Random.value < ExtraLuckySkill.FinalValue / 100f)
			{
				num *= 50;
			}
			Singleton<LootManager>.Current.AddMoney(num);
			Singleton<FloatingTextPool>.Current.SpawnMoney($"${num}", base.transform.position);
			Singleton<GameEvents>.Current.OnCoinHeads?.Invoke(num);
			Singleton<AudioPool>.Current.Play(headsSFX, base.transform.position);
			Singleton<Shaker>.Current.ShakeHammerHit();
			Singleton<GameEvents>.Current.OnCoinIncome?.Invoke(num);
			SaveManager.Current.SaveData.CoinHeads++;
		}
		else
		{
			Singleton<AudioPool>.Current.Play(tailsSFX, base.transform.position);
			Singleton<GameEvents>.Current.OnCoinTails?.Invoke();
		}
	}

	private void FlipTheCoin()
	{
		if (!isFlipping && Singleton<GameSession>.Current.IsGameStarted && !Singleton<GameSession>.Current.IsInMenu)
		{
			isFlipping = true;
			jumpHeight = UnityEngine.Random.Range(minJumpHeight, maxJumpHeight);
			startPos = goalPos;
			Vector3 vector = originalPosition + Vector3.Scale(UnityEngine.Random.insideUnitSphere, maxLandDistance);
			goalPos = vector;
			int num = UnityEngine.Random.Range(minFlipRotations, maxFlipRotations + 1) * 2;
			isHead = UnityEngine.Random.value < HeadChanceSkill.FinalValue / 100f;
			if (!isHead)
			{
				num++;
			}
			startRot = new Vector3(goalRot.x % 360f, 0f, 0f);
			float x = 180 * num;
			goalRot = new Vector3(x, 0f, 0f);
			timeDone = 0f;
		}
	}

	private void OnHammerHit()
	{
		if (HammerFlipSkill.IsUnlocked && base.gameObject.activeInHierarchy)
		{
			FlipTheCoin();
		}
	}
}
