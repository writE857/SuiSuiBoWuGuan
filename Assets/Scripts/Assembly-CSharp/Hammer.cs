using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

public class Hammer : Singleton<Hammer>
{
	private struct DamageData
	{
		public CubePiece piece;

		public float damage;

		public float distanceRatio;

		public Vector3 distV;

		public Vector3 distVForce;

		public float damageHPRatio;
	}

	public float debugInterval = 0.01f;

	public float debugDamage = 5f;

	private float lastTime = -1f;

	public Vector2 forceRange = new Vector2(0.2f, 1f);

	public int BrokenLayer;

	public LayerMask HammerLayerMask;

	public LayerMask HammerLootLayerMask;

	public SphereCollider SphereCollider;

	public Transform forceSource;

	public float moveUpDistance = 0.02f;

	public PieceDeath PieceDeath;

	private Rigidbody Rigidbody;

	public float nonLethalMoveRate = 0.001f;

	public float nonLethalRotateAngle = 1f;

	public AnimationCurve DamageCurve;

	public bool IsHitting;

	public float flyForce = 1f;

	public bool drawGizmo;

	public AudioResource BrickHitSFX;

	public HammerAnimator HammerAnimator;

	public UnityAction OnHit;

	public HammerStats HammerStats;

	[Header("Prestige")]
	public PrestigeSkill AutoHammerSkill;

	public PrestigeSkill DoubleDamageSkill;

	public PrestigeSkill DoubleSizeSkill;

	public PrestigeSkill MoneyHitSkill;

	private RaycastHit[] hits = new RaycastHit[100];

	private List<Vector3> RayStartPoints = new List<Vector3>();

	private readonly List<CubePiece> piecesInRangeBuffer = new List<CubePiece>(256);

	private readonly HashSet<CubePiece> uniquePiecesBuffer = new HashSet<CubePiece>();

	private readonly List<DamageData> damageBuffer = new List<DamageData>(256);

	public float resolution = 0.1f;

	public int rayCount;

	private float lastRadius = -10f;

	private float lastResolution = -10f;

	public float interval => 1f / HammerStats.Speed;

	public float Damage => HammerStats.Damage;

	public float maxDepth => HammerStats.Depth;

	public float radius => SphereCollider.WorldRadius();

	private void Start()
	{
		Rigidbody = GetComponent<Rigidbody>();
		int brokenLayer = LayerMask.NameToLayer("Broken");
		if (brokenLayer >= 0)
		{
			BrokenLayer = brokenLayer;
		}
		else if (BrokenLayer < 0 || BrokenLayer > 31)
		{
			BrokenLayer = 7;
		}
		lastTime = Time.time;
		HammerAnimator hammerAnimator = HammerAnimator;
		hammerAnimator.OnHitEnd = (UnityAction)Delegate.Combine(hammerAnimator.OnHitEnd, new UnityAction(OnHammerHitEnd));
	}

	private void Update()
	{
		if (IsHitting && AutoHammerSkill.IsUnlocked)
		{
			float num = interval;
			if (Application.isEditor && Input.GetKey(KeyCode.LeftShift))
			{
				num = debugInterval;
			}
			if (Time.time - lastTime > num)
			{
				HitIt();
			}
		}
	}

	public void Use()
	{
		HitIt();
	}

	private void HitIt()
	{
		if (!Singleton<GameSession>.Current.IsInMenu && !Singleton<ArtifactOverlayDisplay>.Current.IsOpen)
		{
			lastTime = Time.time;
			OnHit?.Invoke();
			HammerAnimator.HitNow();
			SaveManager.Current.SaveData.HammerHitCount++;
		}
	}

	private void OnHammerHitEnd()
	{
		DealDamage();
	}

	private void DealDamage()
	{
		if (UnityEngine.Random.value < DoubleSizeSkill.FinalValue / 100f)
		{
			SphereCollider.radius = HammerStats.Size * 2f;
		}
		else
		{
			SphereCollider.radius = HammerStats.Size * 1f;
		}
		List<CubePiece> piecesInRange = GetPiecesInRange();
		List<DamageData> damages = GetDamages(piecesInRange);
		float num = 0f;
		foreach (DamageData item in damages)
		{
			item.piece.transform.position += item.distV.normalized * item.damageHPRatio * nonLethalMoveRate;
			item.piece.transform.eulerAngles += item.distV.normalized * item.damageHPRatio * nonLethalRotateAngle;
			float num2 = item.damage;
			if (UnityEngine.Random.value < DoubleDamageSkill.FinalValue / 100f)
			{
				num2 *= 2f;
			}
			if (Application.isEditor && Input.GetKey(KeyCode.LeftShift))
			{
				float num3 = Mathf.Min(item.piece.hp, num2 + debugDamage);
				item.piece.Hit(num2 + debugDamage);
				num += num3;
			}
			else
			{
				float num4 = Mathf.Min(item.piece.hp, num2);
				if (item.piece.HitAnyway)
				{
					item.piece.Hit(1f);
				}
				else
				{
					item.piece.Hit(num2);
				}
				num += num4;
			}
			if (item.piece.hp <= 0f)
			{
				item.piece.gameObject.layer = BrokenLayer;
				if (item.piece.body != null)
				{
					Mathf.Lerp(forceRange.x, forceRange.y, item.damageHPRatio);
					UnityEngine.Random.Range(0.75f, 1.25f);
					item.piece.body.MovePosition(item.piece.transform.position + moveUpDistance * Vector3.up);
					item.piece.body.velocity = item.distVForce.normalized * forceRange.GetRandomBetweenXY() * flyForce;
				}
				PieceDeath pieceDeath = item.piece.gameObject.AddComponent<PieceDeath>();
				item.piece.PieceDeath = pieceDeath;
				PieceDeath.CopyTo(pieceDeath);
				pieceDeath.Init();
			}
		}
		int num5 = Mathf.RoundToInt(num * (MoneyHitSkill.FinalValue / 100f));
		if (num5 > 0)
		{
			Singleton<GameEvents>.Current.OnHammerIncome?.Invoke(num5);
			Singleton<LootManager>.Current.AddMoney(num5);
			Singleton<FloatingTextPool>.Current.SpawnMoney("$" + NumFormat.ToM1Decimal(num5), base.transform.position);
		}
		Singleton<GameEvents>.Current.OnHammerHit?.Invoke(num);
		if (num >= 1f)
		{
			Singleton<FloatingTextPool>.Current.SpawnDamage(NumFormat.ToM1Decimal(num) ?? "", base.transform.position);
		}
		if (damages.Count > 0)
		{
			Singleton<AudioPool>.Current.Play(BrickHitSFX, base.transform.position);
		}
	}

	private List<CubePiece> GetPiecesInRange()
	{
		piecesInRangeBuffer.Clear();
		uniquePiecesBuffer.Clear();
		CalculateRayStartPoints();
		Vector3 position = base.transform.position;
		foreach (Vector3 rayStartPoint in RayStartPoints)
		{
			int num = Physics.RaycastNonAlloc(new Ray(position + rayStartPoint + Vector3.up, Vector3.down), hits, float.MaxValue, HammerLayerMask);
			for (int i = 0; i < num; i++)
			{
				CubePiece component = hits[i].collider.GetComponent<CubePiece>();
				if (!(component != null))
				{
					continue;
				}
				if (component.HitAnyway)
				{
					if (uniquePiecesBuffer.Add(component))
					{
						piecesInRangeBuffer.Add(component);
					}
				}
				else if (!((component.transform.position - position).y < 0f - maxDepth) && uniquePiecesBuffer.Add(component))
				{
					piecesInRangeBuffer.Add(component);
				}
			}
		}
		return piecesInRangeBuffer;
	}

	private List<DamageData> GetDamages(List<CubePiece> pieces)
	{
		damageBuffer.Clear();
		foreach (CubePiece piece in pieces)
		{
			Vector3 distVForce = piece.transform.position - forceSource.position;
			Vector3 vector = piece.transform.position - base.transform.position;
			float value = 1f - vector.X0Z().magnitude / SphereCollider.radius;
			float num = Mathf.Abs(vector.y);
			float value2 = 1f - num / maxDepth;
			value2 = Mathf.Clamp01(value2);
			value = Mathf.Clamp01(value);
			value *= value2;
			value = DamageCurve.Evaluate(value);
			float num2 = Damage * value;
			if (piece.HitAnyway)
			{
				num2 = Damage;
			}
			float damageHPRatio = Mathf.Clamp01(num2 / piece.maxHp);
			damageBuffer.Add(new DamageData
			{
				piece = piece,
				damage = num2,
				distanceRatio = value,
				distV = vector,
				distVForce = distVForce,
				damageHPRatio = damageHPRatio
			});
		}
		return damageBuffer;
	}

	private void OnDrawGizmosSelected()
	{
		if (!drawGizmo || radius <= 0f || resolution <= 0f)
		{
			return;
		}
		Vector3 position = base.transform.position;
		rayCount = 0;
		CalculateRayStartPoints();
		foreach (Vector3 rayStartPoint in RayStartPoints)
		{
			Vector3 vector = position + rayStartPoint;
			float magnitude = rayStartPoint.magnitude;
			float t = Mathf.Pow(magnitude / radius, 2.2f);
			t = Mathf.Lerp(0f, 0.5f, t);
			Gizmos.color = Color.HSVToRGB(1f - t, 1f, 1f);
			if (magnitude <= radius)
			{
				Vector3 to = vector + Vector3.down * 1f;
				Gizmos.DrawLine(vector, to);
				rayCount++;
			}
		}
	}

	private void CalculateRayStartPoints()
	{
		if (lastRadius == radius && lastResolution == resolution)
		{
			return;
		}
		lastRadius = radius;
		lastResolution = resolution;
		RayStartPoints.Clear();
		rayCount = 0;
		for (float num = 0f - radius; num <= radius; num += resolution)
		{
			for (float num2 = 0f - radius; num2 <= radius; num2 += resolution)
			{
				Vector3 item = new Vector3(num, 0f, num2);
				if (item.magnitude <= radius)
				{
					RayStartPoints.Add(item);
					rayCount++;
				}
			}
		}
	}
}
